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
            PredicateAttribute predicateAttr = method.GetCustomAttribute<PredicateAttribute>();
            ParameterInfo pageRequestParameter = Helper.FindSingleParameterOfType(method, typeof(PageRequest));
            //todo: cache the RegExp
            
            //ReturnType can be Page<T>,IEnumerable<T>,IQueryable<T> or T(single item)

            //If there is a parameter of type PageRequest<T>, return type must be Page<T>
            if(pageRequestParameter!=null)
            {
                if(!method.ReturnType.IsGenericType
                    ||method.ReturnType.GetGenericTypeDefinition()!=typeof(Page<>))
                {
                    throw new ConventionException($"since there is a parameter '{pageRequestParameter.Name}' of type PageRequest, the return type of {method} must be Page<T>");
                }
            }

            string predicate = "";
            StringBuilder sbCode = new StringBuilder();
            sbCode.AppendLine(Helper.CreateCodeFromMethodDelaration(method));
            sbCode.AppendLine("{");

            if (predicateAttr!=null)//if Find** with PredicateAttribute, the method name should be ignored,
                                   //and only use the predicate of PredicateAttribute
            {

                predicate = predicateAttr.Predicate;
            }
            sbCode.AppendLine("}");
            return sbCode.ToString();
        }
    }
}
