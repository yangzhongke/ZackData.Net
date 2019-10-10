using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;

namespace ZackData.NetStandard.EF
{
    static class Helper
    {
        public static void GetAllParentInterface(Type type,List<Type> interfaces)
        {
            interfaces.AddRange(type.GetInterfaces());
            foreach(var intyptf in type.GetInterfaces())
            {
                GetAllParentInterface(intyptf, interfaces);
            }
        }

        public static string CreateCodeFromType(Type type)
        {
            //IEnumerable`1  --> IEnumerable
            StringBuilder sbCode = new StringBuilder(type.Namespace+"."+type.Name.Split('`')[0]);

            if (type.IsArray)
            {
                sbCode.Append("[]");
            }
            var genericTypeNames = type.GenericTypeArguments.Select(t=>t.Namespace+"."+t.Name);
            if(genericTypeNames.Any())
            {
                sbCode.Append("<").Append(string.Join(",", genericTypeNames)).Append(">");
            }
            return sbCode.ToString();
        }

        public static string CreateCodeFromMethodDelaration(MethodInfo method)
        {
            StringBuilder sbCode = new StringBuilder();
            sbCode.Append("public ").Append(CreateCodeFromType(method.ReturnType)).Append(" ").Append(method.Name);
            sbCode.Append("(");
            List<string> argsCode = new List<string>();
            foreach(var parameter in method.GetParameters())
            {
                argsCode.Add(CreateCodeFromType(parameter.ParameterType)+" "+parameter.Name);
            }
            sbCode.Append(string.Join(",", argsCode));
            sbCode.Append(")");
            return sbCode.ToString();
        }
    }
}
