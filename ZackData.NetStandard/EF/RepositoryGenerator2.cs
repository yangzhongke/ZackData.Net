using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Linq.Dynamic.Core;

namespace ZackData.NetStandard.EF
{
    public class RepositoryGenerator2
    {
        private Func<DbContext> dbContextCreator;
        public RepositoryGenerator2(Func<DbContext> dbContextCreator)
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

            sbCode.AppendLine(@"
                }
            }");
            string repositoryImplAssemblyName = repositoryInterfaceNamespace + "." + repositoryImplName;

            var currentAssemblies = AppDomain.CurrentDomain.GetAssemblies();
            List<MetadataReference> metaReferences = new List<MetadataReference>();
            foreach(var asm in currentAssemblies)
            {
                if (asm.IsDynamic) continue;
                metaReferences.Add(MetadataReference.CreateFromFile(asm.Location));
            }
            metaReferences.Add(MetadataReference.CreateFromFile(typeof(DynamicQueryableExtensions).Assembly.Location));

            var syntaxTree = SyntaxFactory.ParseSyntaxTree(sbCode.ToString());
            var compilation = CSharpCompilation.Create(repositoryImplAssemblyName,new SyntaxTree[] { syntaxTree }, metaReferences);
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
            }
            
            return null;
         }
    }
}
