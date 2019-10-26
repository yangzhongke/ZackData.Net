using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Dynamic.Core;
using System.Reflection;
using System.Text;
using ZackData.NetStandard.Exceptions;
using ZackData.NetStandard.Parsers;

namespace ZackData.NetStandard.EF
{
    public class RepositoryStubGenerator
    {
        public static ConcurrentDictionary<string, Type> typesCache = new ConcurrentDictionary<string,Type>();
        
        public TRepository Create<TEntity, ID, TRepository>(DbContext dbCtx) where TEntity : class
            where TRepository : class
        {
            string cacheKey = typeof(TEntity) + "." + typeof(ID) + "." + typeof(TRepository);
            Type repositoryImplType;
            if (!typesCache.TryGetValue(cacheKey, out repositoryImplType))
            {
                repositoryImplType = BuildRepositoryImplType<TEntity, ID, TRepository>();
                typesCache[cacheKey] = repositoryImplType;
            }            
            return (TRepository)Activator.CreateInstance(repositoryImplType, dbCtx);
        }

        private Type BuildRepositoryImplType<TEntity, ID, TRepository>()
            where TEntity : class
            where TRepository : class
        {
            Type repositoryImplType;
            StringBuilder sbCode = new StringBuilder();
            sbCode.AppendLine(@"using Microsoft.EntityFrameworkCore;
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using ZackData.NetStandard.Exceptions;
                using ZackData.NetStandard;");
            string repositoryInterfaceName = typeof(TRepository).Name;
            string repositoryInterfaceNamespace = typeof(TRepository).Namespace;
            string repositoryImplName = repositoryInterfaceName + "Impl";
            string entityTypeFullName = typeof(TEntity).FullName;
            string idTypeFullName = typeof(ID).FullName;
            sbCode.Append(@"namespace ").AppendLine(repositoryInterfaceNamespace);
            sbCode.AppendLine(@"
            {");
            sbCode.AppendLine($"public class {repositoryImplName} : BaseEFCrudRepository<{entityTypeFullName},{idTypeFullName}>,{repositoryInterfaceName}");
            sbCode.AppendLine(@"
                {");
            sbCode.AppendLine($"public {repositoryImplName}(DbContext dbCtx):base(dbCtx)");
            sbCode.AppendLine("{}");

            //all the methods of interfaces must be implemented
            List<Type> interfaces = new List<Type>();
            interfaces.Add(typeof(TRepository));

            //if the customed IXXXRepository has customed parent interface,like IMyCrudRepository
            Helper.GetAllParentInterface(typeof(TRepository), interfaces);
            interfaces.Remove(typeof(ICrudRepository<TEntity, ID>));//don't implement methods of ICrudRepository
            //because the methods of ICrudRepository already are implemented by BaseEFCrudRepository

            foreach (var intfType in interfaces)
            {
                foreach (var intfMethod in intfType.GetMethods())
                {
                    string methodName = intfMethod.Name;
                    if (methodName.StartsWithIgnoreCase("Find")|| methodName.StartsWithIgnoreCase("Query")
                        ||methodName.StartsWithIgnoreCase("Get"))
                    {
                        sbCode.AppendLine().Append(CreateFindMethod<TEntity>(intfMethod)).AppendLine();
                    }
                    else if (methodName.StartsWithIgnoreCase("Delete"))
                    {

                    }
                    else if (methodName.StartsWithIgnoreCase("Update"))
                    {

                    }
                    else if (methodName.StartsWithIgnoreCase("Count"))
                    {

                    }
                    else
                    {
                        throw new ConventionException("MethodName must start with Find, Delete or Update");
                    }
                }
            }


            sbCode.AppendLine(@"
                }
            }");
            string repositoryImplAssemblyName = repositoryInterfaceNamespace + "." + repositoryImplName;

            //todo 1:Cache the assembly
            //todo 2:use Emit instead of Roslyn
            var currentAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            List<MetadataReference> metaReferences = new List<MetadataReference>();
            foreach (var asm in currentAssemblies)
            {
                if (asm.IsDynamic) continue;
                if (string.IsNullOrWhiteSpace(asm.Location)) continue;
                metaReferences.Add(MetadataReference.CreateFromFile(asm.Location));
            }
            metaReferences.Add(MetadataReference.CreateFromFile(typeof(DynamicQueryableExtensions).Assembly.Location));

            var syntaxTree = SyntaxFactory.ParseSyntaxTree(sbCode.ToString());
            CSharpCompilationOptions compilationOpt = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var compilation = CSharpCompilation.Create(repositoryImplAssemblyName, new SyntaxTree[] { syntaxTree }, metaReferences, compilationOpt);
            using (var ms = new MemoryStream())
            {
                var emitResult = compilation.Emit(ms);
                if (!emitResult.Success)
                {
                    StringBuilder sbError = new StringBuilder();
                    foreach (var diag in emitResult.Diagnostics)
                    {
                        sbError.AppendLine(diag.GetMessage());
                    }
                    throw new Exception(sbError.ToString());
                }
                ms.Position = 0;
                Assembly asm = Assembly.Load(ms.ToArray());
                repositoryImplType = asm.GetType($"{repositoryInterfaceNamespace}.{repositoryImplName}");
            }

            return repositoryImplType;
        }

        private string CreateFindMethod<TEntity>(MethodInfo method)
        {
            var findMethodBaseInfo = FindMethodNameParser.Parse(method);
            //ReturnType can be Page<T>,IEnumerable<T>,IQueryable<T> or T(single item)
            //If there is a parameter of type PageRequest<T>, return type must be Page<T>
            if (findMethodBaseInfo.PageRequestParameter != null)
            {
                if(!method.ReturnType.IsGenericType
                    ||method.ReturnType.GetGenericTypeDefinition()!=typeof(Page<>))
                {
                    throw new ConventionException($"since there is a parameter '{findMethodBaseInfo.PageRequestParameter.Name}' of type PageRequest, the return type of {method} must be Page<T>");
                }
            }

            StringBuilder sbCode = new StringBuilder();
            sbCode.AppendLine(Helper.CreateCodeFromMethodDelaration(method));
            sbCode.AppendLine("{");
            string predicate;//where condition
            List<string> plainActualArguments = new List<string>();//plain Actual Arguments(PageRequest,Order, Order[] and string predicate ) those will be passed to Find()
            var plainFormalParameterNames = findMethodBaseInfo.PlainParameters.Select(p=>p.Name);//plain Formal Parameters of current method

            //begin calculate the predicate
            if (findMethodBaseInfo is FindByPredicateMethodInfo)
            {
                var predicateMethodInfo = (FindByPredicateMethodInfo)findMethodBaseInfo;
                predicate = predicateMethodInfo.Predicate;
                /*
                 * For example
                 * @Predicate("id=@0 and name=@1 or age>@2")
                 * FindFoo(long id,string name,int age)
                 */ 
                plainActualArguments.AddRange(plainFormalParameterNames);                               
            }
            else if (findMethodBaseInfo is FindWithoutByMethodInfo)
            {
                predicate = null;
                if (plainFormalParameterNames.Count() > 0)
                {
                    Debug.Write($"It is expected that {method} has 0 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found, so they will be ignored.");
                }
            }
            else if (findMethodBaseInfo is FindByTwoPropertiesMethodInfo)
            {
                var twoPInfo = (FindByTwoPropertiesMethodInfo)findMethodBaseInfo;
                predicate = twoPInfo.PropertyName1+"=@0 "+twoPInfo.Operator+" "+twoPInfo.PropertyName2+"=@1";
                if(plainFormalParameterNames.Count()<2)
                {
                    throw new ConventionException($"It is expected that {method} has 2 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found.");
                }
                else if(plainFormalParameterNames.Count()>2)
                {
                    Debug.Write($"It is expected that {method} has 2 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found, so they will be ignored.");
                }
                else
                {
                    plainActualArguments.AddRange(plainFormalParameterNames);
                }
            }
            else if (findMethodBaseInfo is FindByOnePropertyMethodInfo)
            {
                var findByOnePropertyMethodInfo = (FindByOnePropertyMethodInfo)findMethodBaseInfo;
                predicate = findByOnePropertyMethodInfo.PropertyName + "=@0";
                if (plainFormalParameterNames.Count() < 1)
                {
                    throw new ConventionException($"It is expected that {method} has 1 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found.");
                }
                else if (plainFormalParameterNames.Count() > 1)
                {
                    Debug.Write($"It is expected that {method} has 1 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found, so they will be ignored.");
                }
                else
                {
                    plainActualArguments.Add(plainFormalParameterNames.Single());
                }
            }
            else if (findMethodBaseInfo is FindByPropertyVerbMethodInfo)
            {
                var pvInfo = (FindByPropertyVerbMethodInfo)findMethodBaseInfo;
                //todo: Use FromSQL to support functions that are not supported by System.Linq.Dynamic.Core
                //such as 'like','not like','not in'
                switch(pvInfo.Verb)
                {
                    case PropertyVerb.Between:
                        predicate = pvInfo.PropertyName + ">@0 and "+ pvInfo.PropertyName+"<@1";
                        if (plainFormalParameterNames.Count() < 2)
                        {
                            throw new ConventionException($"It is expected that {method} has 1 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found.");
                        }
                        else if (plainFormalParameterNames.Count() > 2)
                        {
                            Debug.Write($"It is expected that {method} has 2 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found, so they will be ignored.");
                        }
                        else
                        {
                            plainActualArguments.AddRange(plainFormalParameterNames);
                        }
                        break;
                    case PropertyVerb.Contains:
                        predicate = pvInfo.PropertyName + ".Contains(@0)";
                        if (plainFormalParameterNames.Count() < 1)
                        {
                            throw new ConventionException($"It is expected that {method} has 1 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found.");
                        }
                        else if (plainFormalParameterNames.Count() > 1)
                        {
                            Debug.Write($"It is expected that {method} has 1 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found, so they will be ignored.");
                        }
                        else
                        {
                            plainActualArguments.AddRange(plainFormalParameterNames);
                        }
                        break;
                    case PropertyVerb.EndsWith:
                        predicate = pvInfo.PropertyName + ".EndsWith(@0)";
                        if (plainFormalParameterNames.Count() < 1)
                        {
                            throw new ConventionException($"It is expected that {method} has 1 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found.");
                        }
                        else if (plainFormalParameterNames.Count() > 1)
                        {
                            Debug.Write($"It is expected that {method} has 1 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found, so they will be ignored.");
                        }
                        else
                        {
                            plainActualArguments.AddRange(plainFormalParameterNames);
                        }
                        break;
                    case PropertyVerb.Equals:
                        predicate = pvInfo.PropertyName + "=@0";
                        if (plainFormalParameterNames.Count() < 1)
                        {
                            throw new ConventionException($"It is expected that {method} has 1 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found.");
                        }
                        else if (plainFormalParameterNames.Count() > 1)
                        {
                            Debug.Write($"It is expected that {method} has 1 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found, so they will be ignored.");
                        }
                        else
                        {
                            plainActualArguments.AddRange(plainFormalParameterNames);
                        }
                        break;
                    case PropertyVerb.False:
                        predicate = pvInfo.PropertyName + "=false";
                        if (plainFormalParameterNames.Count() > 0)
                        {
                            Debug.Write($"It is expected that {method} has 0 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found, so they will be ignored.");
                        }
                        break;
                    case PropertyVerb.GreaterThan:
                        predicate = pvInfo.PropertyName + ">@0";
                        if (plainFormalParameterNames.Count() < 1)
                        {
                            throw new ConventionException($"It is expected that {method} has 1 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found.");
                        }
                        else if (plainFormalParameterNames.Count() > 1)
                        {
                            Debug.Write($"It is expected that {method} has 1 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found, so they will be ignored.");
                        }
                        else
                        {
                            plainActualArguments.AddRange(plainFormalParameterNames);
                        }
                        break;
                    case PropertyVerb.GreaterThanEqual:
                        predicate = pvInfo.PropertyName + ">=@0";
                        if (plainFormalParameterNames.Count() < 1)
                        {
                            throw new ConventionException($"It is expected that {method} has 1 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found.");
                        }
                        else if (plainFormalParameterNames.Count() > 1)
                        {
                            Debug.Write($"It is expected that {method} has 1 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found, so they will be ignored.");
                        }
                        else
                        {
                            plainActualArguments.AddRange(plainFormalParameterNames);
                        }
                        break;
                    case PropertyVerb.In:
                        predicate = pvInfo.PropertyName + " in @0";
                        if (plainFormalParameterNames.Count() < 1)
                        {
                            throw new ConventionException($"It is expected that {method} has 1 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found.");
                        }
                        else if (plainFormalParameterNames.Count() > 1)
                        {
                            Debug.Write($"It is expected that {method} has 1 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found, so they will be ignored.");
                        }
                        else
                        {
                            plainActualArguments.AddRange(plainFormalParameterNames);
                        }
                        break;
                    case PropertyVerb.IsNotNull:
                        predicate = pvInfo.PropertyName + "!=null";
                        if (plainFormalParameterNames.Count() > 0)
                        {
                            Debug.Write($"It is expected that {method} has 0 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found, so they will be ignored.");
                        }
                        break;
                    case PropertyVerb.IsNull:
                        predicate = pvInfo.PropertyName + "==null";
                        if (plainFormalParameterNames.Count() > 0)
                        {
                            Debug.Write($"It is expected that {method} has 0 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found, so they will be ignored.");
                        }
                        break;
                    case PropertyVerb.LessThan:
                        predicate = pvInfo.PropertyName + "<@0";
                        if (plainFormalParameterNames.Count() < 1)
                        {
                            throw new ConventionException($"It is expected that {method} has 1 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found.");
                        }
                        else if (plainFormalParameterNames.Count() > 1)
                        {
                            Debug.Write($"It is expected that {method} has 1 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found, so they will be ignored.");
                        }
                        else
                        {
                            plainActualArguments.AddRange(plainFormalParameterNames);
                        }
                        break;
                    case PropertyVerb.LessThanEqual:
                        predicate = pvInfo.PropertyName + "<=@0";
                        if (plainFormalParameterNames.Count() < 1)
                        {
                            throw new ConventionException($"It is expected that {method} has 1 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found.");
                        }
                        else if (plainFormalParameterNames.Count() > 1)
                        {
                            Debug.Write($"It is expected that {method} has 1 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found, so they will be ignored.");
                        }
                        else
                        {
                            plainActualArguments.AddRange(plainFormalParameterNames);
                        }
                        break;
                    case PropertyVerb.NotEquals:
                        predicate = pvInfo.PropertyName + "!=@0";
                        if (plainFormalParameterNames.Count() < 1)
                        {
                            throw new ConventionException($"It is expected that {method} has 1 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found.");
                        }
                        else if (plainFormalParameterNames.Count() > 1)
                        {
                            Debug.Write($"It is expected that {method} has 1 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found, so they will be ignored.");
                        }
                        else
                        {
                            plainActualArguments.AddRange(plainFormalParameterNames);
                        }
                        break;
                    case PropertyVerb.StartsWith:
                        predicate = pvInfo.PropertyName + ".StartsWith(@0)";
                        if (plainFormalParameterNames.Count() < 1)
                        {
                            throw new ConventionException($"It is expected that {method} has 1 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found.");
                        }
                        else if (plainFormalParameterNames.Count() > 1)
                        {
                            Debug.Write($"It is expected that {method} has 1 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found, so they will be ignored.");
                        }
                        else
                        {
                            plainActualArguments.AddRange(plainFormalParameterNames);
                        }
                        break;
                    case PropertyVerb.True:
                        predicate = pvInfo.PropertyName + "=true";
                        if (plainFormalParameterNames.Count() > 0)
                        {
                            Debug.Write($"It is expected that {method} has 0 plain parameters(except for PageQuest,Sort and Sort[]),but {plainFormalParameterNames.Count()} was found, so they will be ignored.");
                        }
                        break;
                    default:
                        throw new ConventionException($"Unkown PropertyVerb {pvInfo.Verb}");
                }
            }
            else
            {
                throw new ApplicationException($"type of findMethodBaseInfo is unknown, {findMethodBaseInfo.GetType()}");
            }
            //end calculate the predicate

            if(findMethodBaseInfo.PageRequestParameter!=null)
            {
                if(!findMethodBaseInfo.ReturnType.IsGenericType&& findMethodBaseInfo.ReturnType.GetGenericTypeDefinition()!= typeof(Page<>))
                {
                    throw new ConventionException($"there is a Parameter of type PageRequest in method {method} , so the return type must be Page<T>");
                }
                if(findMethodBaseInfo.OrderParameter!=null|| findMethodBaseInfo.OrdersParameter != null)
                {
                    throw new ConventionException($"since there is a Parameter of type PageRequest in method {method} , and there are Sort[] in PageRequest, so it is not allowed to have Sort or Sort[] Parameters in this method");
                }
                //Page<TEntity> Find(PageRequest pageRequest, string predicate, params object[] args)
                sbCode.Append("return this.Find(").Append(findMethodBaseInfo.PageRequestParameter.Name)
                    .Append(",").Append("\"").Append(predicate);
                sbCode.Append("\"");
                if(plainActualArguments.Count>0)
                {
                    sbCode.Append(",");
                }                
                sbCode.Append(string.Join(",",plainActualArguments)).AppendLine(");");
            }
            else if(findMethodBaseInfo.OrderParameter!=null)
            {
                if (!findMethodBaseInfo.ReturnType.IsGenericType && findMethodBaseInfo.ReturnType.GetGenericTypeDefinition() != typeof(Page<>))
                {
                    throw new ConventionException($"there is a Parameter of type PageRequest in method {method} , so the return type must be Page<T>");
                }
                if (findMethodBaseInfo.OrderInMethodName!=null)
                {
                    throw new ConventionException($"the name of method {method} alread contains OrderBy, so it is not allowed to contains parameter of type Order or Order[]");
                }
                //public IQueryable<TEntity> Find(Order order, string predicate, params object[] args)
                sbCode.Append("return this.Find(").Append(findMethodBaseInfo.OrderParameter.Name)
                    .Append(",").Append("\"").Append(predicate);
                sbCode.Append("\"");
                if (plainActualArguments.Count > 0)
                {
                    sbCode.Append(",");
                }
                sbCode.Append(string.Join(",", plainActualArguments)).AppendLine(");");
            }
            else if (findMethodBaseInfo.OrdersParameter != null)
            {
                if (findMethodBaseInfo.OrderInMethodName != null)
                {
                    throw new ConventionException($"the name of method {method} alread contains OrderBy, so it is not allowed to contains parameter of type Order or Order[]");
                }
                //public IQueryable<TEntity> Find(Order[] order, string predicate, params object[] args)
                sbCode.Append("return this.Find(").Append(findMethodBaseInfo.OrdersParameter.Name)
                    .Append(",").Append("\"").Append(predicate);
                sbCode.Append("\"");
                if (plainActualArguments.Count > 0)
                {
                    sbCode.Append(",");
                }
                sbCode.Append(string.Join(",", plainActualArguments)).AppendLine(");");
            }
            else if(findMethodBaseInfo.OrderInMethodName!=null)//FindByPriceOrNameOrderByPrice
            {
                Order order = findMethodBaseInfo.OrderInMethodName;
                sbCode.Append("return this.Find(").Append("new Order(\"").Append(order.Property).Append("\",")
                    .Append(order.Ascending.ToString().ToLower()).Append(")")
                    .Append(",").Append("\"").Append(predicate);
                sbCode.Append("\"");
                if (plainActualArguments.Count > 0)
                {
                    sbCode.Append(",");
                }
                sbCode.Append(string.Join(",", plainActualArguments)).AppendLine(");");
            }
            else
            {
                //IQueryable<TEntity> Find(string predicate, params object[] args)
                //IQueryable<TEntity> FindByPriceAndName(double price,string name)
                //TEntity FindByPriceAndName(double price,string name)

                sbCode.Append("return this.Find(")
                    .Append("\"").Append(predicate);
                sbCode.Append("\"");
                if (plainActualArguments.Count > 0)
                {
                    sbCode.Append(",");
                }
                sbCode.Append(string.Join(",", plainActualArguments)).Append(")");
                if(findMethodBaseInfo.ReturnType.IsGenericType
                    &&(findMethodBaseInfo.ReturnType.GetGenericTypeDefinition()==typeof(IQueryable<>)||
                    findMethodBaseInfo.ReturnType.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                    ))
                {
                    //do nothing
                }
                else if(findMethodBaseInfo.ReturnType.IsArray)
                {
                    sbCode.Append(".ToArray()");
                }
                else if (findMethodBaseInfo.ReturnType.IsGenericType && findMethodBaseInfo.ReturnType.GetGenericTypeDefinition() == typeof(Page<>))
                {
                    throw new ConventionException($"There is no OrderBy in the methodName or parameters in the method {method},so there return type cannot be Page<T>");
                }
                else if(findMethodBaseInfo.ReturnType==typeof(TEntity))
                {
                    sbCode.Append(".SingleOrDefault()");
                }
                sbCode.AppendLine(";");
            }

            sbCode.AppendLine("}");
            return sbCode.ToString();
        }
    }
}
