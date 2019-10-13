using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Dynamic.Core;
using System.Reflection;
using ZackData.NetStandard.Exceptions;
using System.Text.RegularExpressions;
using ZackData.NetStandard.Parsers;

namespace ZackData.NetStandard.EF
{
    public class RepositoryStubGenerator
    {
        private Func<DbContext> dbContextCreator;
        public RepositoryStubGenerator(Func<DbContext> dbContextCreator)
        {
            this.dbContextCreator = dbContextCreator;
        }
        
        public TRepository Create<TEntity, ID, TRepository>() where TEntity : class
            where TRepository : class
        {
            StringBuilder sbCode = new StringBuilder();
            sbCode.AppendLine(@"using Microsoft.EntityFrameworkCore;
                using System;
                using System.Collections.Generic;
                using System.Linq;
                using System.Linq.Dynamic.Core;
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
            sbCode.AppendLine($"public {repositoryImplName}(Func<DbContext> dbContextCreator):base(dbContextCreator)");
            sbCode.AppendLine("{}");

            //all the methods of interfaces must be implemented
            List<Type> interfaces = new List<Type>();
            interfaces.Add(typeof(TRepository));

            //if the customed IXXXRepository has customed parent interface,like IMyCrudRepository
            Helper.GetAllParentInterface(typeof(TRepository), interfaces);
            interfaces.Remove(typeof(ICrudRepository<TEntity,ID>));//don't implement methods of ICrudRepository
            //because the methods of ICrudRepository already are implemented by BaseEFCrudRepository

            foreach (var intfType in interfaces)
            {
                foreach(var intfMethod in intfType.GetMethods())
                {
                    string methodName = intfMethod.Name;
                    if(methodName.StartsWithIgnoreCase("Find"))
                    {
                        sbCode.AppendLine().Append(CreateFindMethod(intfMethod)).AppendLine();
                    }
                    else if(methodName.StartsWithIgnoreCase("Delete"))
                    {

                    }
                    else if(methodName.StartsWithIgnoreCase("Update"))
                    {

                    }
                    else if(methodName.StartsWithIgnoreCase("Count"))
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
            foreach(var asm in currentAssemblies)
            {
                if (asm.IsDynamic) continue;
                metaReferences.Add(MetadataReference.CreateFromFile(asm.Location));
            }
            metaReferences.Add(MetadataReference.CreateFromFile(typeof(DynamicQueryableExtensions).Assembly.Location));

            var syntaxTree = SyntaxFactory.ParseSyntaxTree(sbCode.ToString());
            CSharpCompilationOptions compilationOpt = new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary);
            var compilation = CSharpCompilation.Create(repositoryImplAssemblyName,new SyntaxTree[] { syntaxTree }, metaReferences, compilationOpt);
            using (var ms = new MemoryStream())
            {
                var emitResult = compilation.Emit(ms);
                if(!emitResult.Success)
                {
                    StringBuilder sbError = new StringBuilder();
                    foreach(var diag in emitResult.Diagnostics)
                    {
                        sbError.AppendLine(diag.GetMessage());
                    }
                    throw new Exception(sbError.ToString());
                }
                ms.Position = 0;
                Assembly asm = Assembly.Load(ms.ToArray());
                Type repositoryImplType = asm.GetType($"{repositoryInterfaceNamespace}.{repositoryImplName}");
                return (TRepository)Activator.CreateInstance(repositoryImplType,this.dbContextCreator);
            }
         }
        
        private string CreateFindMethod(MethodInfo method)
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

            //begin calculate the predicate
            string predicate;
            if(findMethodBaseInfo is FindByPredicateMethodInfo)
            {
                var predicateMethodInfo = (FindByPredicateMethodInfo)findMethodBaseInfo;
                predicate = predicateMethodInfo.Predicate;
            }
            else if (findMethodBaseInfo is FindWithoutByMethodInfo)
            {
                predicate = null;
            }
            else if (findMethodBaseInfo is FindByTwoPropertiesMethodInfo)
            {
                var twoPInfo = (FindByTwoPropertiesMethodInfo)findMethodBaseInfo;
                predicate = twoPInfo.PropertyName1+"=@0 "+twoPInfo.Operator+" "+twoPInfo.PropertyName2+"=@1";
            }
            else if (findMethodBaseInfo is FindByOnePropertyMethodInfo)
            {
                var findByOnePropertyMethodInfo = (FindByOnePropertyMethodInfo)findMethodBaseInfo;
                predicate = findByOnePropertyMethodInfo.PropertyName + "=@0";
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
                        break;
                    case PropertyVerb.Contains:
                        predicate = pvInfo.PropertyName + ".Contains(@0)";
                        break;
                    case PropertyVerb.EndsWith:
                        predicate = pvInfo.PropertyName + ".EndsWith(@0)";
                        break;
                    case PropertyVerb.Equals:
                        predicate = pvInfo.PropertyName + "=@0";
                        break;
                    case PropertyVerb.False:
                        predicate = pvInfo.PropertyName + "=false";
                        break;
                    case PropertyVerb.GreaterThan:
                        predicate = pvInfo.PropertyName + ">@0";
                        break;
                    case PropertyVerb.GreaterThanEqual:
                        predicate = pvInfo.PropertyName + ">=@0";
                        break;
                    case PropertyVerb.In:
                        predicate = pvInfo.PropertyName + " in @0";
                        break;
                    case PropertyVerb.IsNotNull:
                        predicate = pvInfo.PropertyName + "!=null";
                        break;
                    case PropertyVerb.IsNull:
                        predicate = pvInfo.PropertyName + "==null";
                        break;
                    case PropertyVerb.LessThan:
                        predicate = pvInfo.PropertyName + "<@0";
                        break;
                    case PropertyVerb.LessThanEqual:
                        predicate = pvInfo.PropertyName + "<=@0";
                        break;
                    case PropertyVerb.NotEquals:
                        predicate = pvInfo.PropertyName + "!=@0";
                        break;
                    case PropertyVerb.StartsWith:
                        predicate = pvInfo.PropertyName + ".StartsWith(@0)";
                        break;
                    case PropertyVerb.True:
                        predicate = pvInfo.PropertyName + "=true";
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

            //begin orgnize the parameters
            if (findMethodBaseInfo is FindByPredicateMethodInfo)
            {
                var predicateMethodInfo = (FindByPredicateMethodInfo)findMethodBaseInfo;
                predicate = predicateMethodInfo.Predicate;
            }
            else if (findMethodBaseInfo is FindWithoutByMethodInfo)
            {
                predicate = null;
            }
            else if (findMethodBaseInfo is FindByTwoPropertiesMethodInfo)
            {
                var twoPInfo = (FindByTwoPropertiesMethodInfo)findMethodBaseInfo;
                predicate = twoPInfo.PropertyName1 + "=@0 " + twoPInfo.Operator + " " + twoPInfo.PropertyName2 + "=@1";
            }
            else if (findMethodBaseInfo is FindByOnePropertyMethodInfo)
            {
                var findByOnePropertyMethodInfo = (FindByOnePropertyMethodInfo)findMethodBaseInfo;
                predicate = findByOnePropertyMethodInfo.PropertyName + "=@0";
            }
            else if (findMethodBaseInfo is FindByPropertyVerbMethodInfo)
            {
                var pvInfo = (FindByPropertyVerbMethodInfo)findMethodBaseInfo;
                //todo: Use FromSQL to support functions that are not supported by System.Linq.Dynamic.Core
                //such as 'like','not like','not in'
                switch (pvInfo.Verb)
                {
                    case PropertyVerb.Between:
                        predicate = pvInfo.PropertyName + ">@0 and " + pvInfo.PropertyName + "<@1";
                        break;
                    case PropertyVerb.Contains:
                        predicate = pvInfo.PropertyName + ".Contains(@0)";
                        break;
                    case PropertyVerb.EndsWith:
                        predicate = pvInfo.PropertyName + ".EndsWith(@0)";
                        break;
                    case PropertyVerb.Equals:
                        predicate = pvInfo.PropertyName + "=@0";
                        break;
                    case PropertyVerb.False:
                        predicate = pvInfo.PropertyName + "=false";
                        break;
                    case PropertyVerb.GreaterThan:
                        predicate = pvInfo.PropertyName + ">@0";
                        break;
                    case PropertyVerb.GreaterThanEqual:
                        predicate = pvInfo.PropertyName + ">=@0";
                        break;
                    case PropertyVerb.In:
                        predicate = pvInfo.PropertyName + " in @0";
                        break;
                    case PropertyVerb.IsNotNull:
                        predicate = pvInfo.PropertyName + "!=null";
                        break;
                    case PropertyVerb.IsNull:
                        predicate = pvInfo.PropertyName + "==null";
                        break;
                    case PropertyVerb.LessThan:
                        predicate = pvInfo.PropertyName + "<@0";
                        break;
                    case PropertyVerb.LessThanEqual:
                        predicate = pvInfo.PropertyName + "<=@0";
                        break;
                    case PropertyVerb.NotEquals:
                        predicate = pvInfo.PropertyName + "!=@0";
                        break;
                    case PropertyVerb.StartsWith:
                        predicate = pvInfo.PropertyName + ".StartsWith(@0)";
                        break;
                    case PropertyVerb.True:
                        predicate = pvInfo.PropertyName + "=true";
                        break;
                    default:
                        throw new ConventionException($"Unkown PropertyVerb {pvInfo.Verb}");
                }
            }
            else
            {
                throw new ApplicationException($"type of findMethodBaseInfo is unknown, {findMethodBaseInfo.GetType()}");
            }
            //end orgnize the parameters

            sbCode.AppendLine("{return this.Find(\""+predicate+"\");}");

            sbCode.AppendLine("}");
            return sbCode.ToString();
        }
    }
}
