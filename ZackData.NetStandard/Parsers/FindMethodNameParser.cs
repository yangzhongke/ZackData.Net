using System;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ZackData.NetStandard.EF;
using ZackData.NetStandard.Exceptions;


namespace ZackData.NetStandard.Parsers
{
    class FindMethodNameParser
    {
        private static RegexOptions reOptions = RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled;
        //if use PredicateAttribute,the 'ByXXX'  should be ignored by parser
        private static readonly Regex reFindMethod = new Regex("^Find(By(?<PropertyPart>.+?))?(OrderBy(?<OrderByName>.+?)(?<OrderingRule>(Asc|Desc))?)?$", reOptions);

        //Name1,Name1AndName2,Name1OrName2
        private static readonly Regex reByProperties = new Regex("^(?<Property1>.+?)((?<Operator>And|Or)(?<Property2>.+))?$", reOptions);//don't support more than 2 columnNames, because too long to be supported
        //Name1GreaterThan
        private static readonly Regex rePropertyTerm = new Regex("^(.+?)(Between|Equals|NotEquals|LessThan|LessThanEqual|GreaterThan|GreaterThanEqual|IsNull|IsNotNull|StartsWith|EndsWith|Contains|In|True|False)$", reOptions);

        public static FindMethodBaseInfo Parse(MethodInfo findMethod)
        {
            PredicateAttribute predicateAttr = findMethod.GetCustomAttribute<PredicateAttribute>();
            string findMethodName = findMethod.Name;
            FindMethodBaseInfo findMethodBaseInfo;
            Match matchFindMethod = reFindMethod.Match(findMethodName);
            //begin analyse the methodName
            var groupsFindMethod = matchFindMethod.Groups;
            var groupPropertyPart = groupsFindMethod["PropertyPart"];

            if (predicateAttr!=null)
            {
                //if Find** with PredicateAttribute, the 'ByXXX'  should be ignored, but OrderBy,PageRequest,and Sort should not be ignored,
                var predicatMethodInfo = new FindByPredicateMethodInfo();
                predicatMethodInfo.Predicate = predicateAttr.Predicate;
                findMethodBaseInfo = predicatMethodInfo;                
            }
            else
            {
                if (!matchFindMethod.Success)
                {
                    throw new ConventionException($"Method {findMethodName} not comply with the pattern {reFindMethod}");
                }
                if (!groupPropertyPart.Success)//no 'By', like FindOrderByPrice
                {
                    findMethodBaseInfo = new FindWithoutByMethodInfo();
                }
                var matchFindMethod_ByProperties = reByProperties.Match(groupPropertyPart.Value);
                var matchFindMethod_PropertyTerm = rePropertyTerm.Match(groupPropertyPart.Value);

                //matchFindMethod_PropertyTerm has a higher priority than matchFindMethod_ByProperties
                //like : FindByPriceIsNull
                if (matchFindMethod_PropertyTerm.Success)//NameLike, AgeGreaterThan
                {
                    var groupPropertyName = matchFindMethod_PropertyTerm.Groups[1];
                    var groupPropertyVerb = matchFindMethod_PropertyTerm.Groups[2];
                    var propVerbMethodInfo = new FindByPropertyVerbMethodInfo();
                    propVerbMethodInfo.PropertyName = groupPropertyName.Value;
                    propVerbMethodInfo.Verb = ParserHelper.ParsePropertyVerb(groupPropertyVerb.Value);
                    findMethodBaseInfo = propVerbMethodInfo;
                }
                else if(matchFindMethod_ByProperties.Success)//NameOrAge,NameAndAge,Name,Age
                {
                    var groupProperty1 = matchFindMethod_ByProperties.Groups["Property1"];
                    var groupOperator = matchFindMethod_ByProperties.Groups["Operator"];
                    var groupProperty2 = matchFindMethod_ByProperties.Groups["Property2"];
                    string property1Name = groupProperty1.Value;
                    if(groupOperator.Success) //NameOrAge,NameAndAge
                    {
                        if (!groupProperty2.Success)
                        {
                            throw new ConventionException($"Method {findMethodName} has {groupOperator.Value} Operator, but doesn't have Property2");
                        }
                        var twoPropsMethodInfo = new FindByTwoPropertiesMethodInfo();
                        twoPropsMethodInfo.PropertyName1 = property1Name;
                        twoPropsMethodInfo.Operator = ParserHelper.ParseOperatorType(groupOperator.Value);
                        twoPropsMethodInfo.PropertyName2 = groupProperty2.Value;
                        findMethodBaseInfo = twoPropsMethodInfo;
                    }
                    else//Name
                    {
                        var onePropMethodInfo = new FindByOnePropertyMethodInfo();
                        onePropMethodInfo.PropertyName = property1Name;
                        findMethodBaseInfo = onePropMethodInfo;
                    }
                }

                else
                {
                    throw new ConventionException($"{groupPropertyPart.Value} not comply with the pattern {reByProperties} nor {rePropertyTerm}");
                }                
            }

            //end analyse the methodName

            //begin analyse the Order
            ParameterInfo ordersParameter = Helper.FindSingleParameterOfType(findMethod, typeof(Order[]));
            ParameterInfo orderParameter = Helper.FindSingleParameterOfType(findMethod, typeof(Order));
            var groupOrderByName = groupsFindMethod["OrderByName"];
            var groupOrderingRule = groupsFindMethod["OrderingRule"];
            if(ordersParameter!=null&& orderParameter!=null)
            {
                throw new ConventionException($"Order and Order[] cannot exist at the same method {findMethod}");
            }
            if((ordersParameter!=null|| orderParameter!=null)&& groupOrderByName.Success)
            {
                throw new ConventionException($"Order or Order[] cannot be used in a method whose name contains 'OrderBy' {findMethod}");
            }
            if(orderParameter != null)
            {
                findMethodBaseInfo.OrderParameter = orderParameter;
            }
            else if(ordersParameter!=null)
            {
                findMethodBaseInfo.OrdersParameter = ordersParameter;
            }
            else if(groupOrderByName.Success)
            {
                string orderByPropertyName = groupOrderByName.Value;
                string orderingRule = groupOrderingRule.Value;
                Order order = new Order(orderByPropertyName);
                order.Ascending = orderingRule.EqualsIgnoreCase("Asc");
                findMethodBaseInfo.OrderInMethodName = order;
            }
            else
            {
                //do nothing, no order settings
            }
            //end analyse the Order

            findMethodBaseInfo.PageRequestParameter= Helper.FindSingleParameterOfType(findMethod, typeof(PageRequest));
            findMethodBaseInfo.MethodName = findMethodName;
            findMethodBaseInfo.ReturnType = findMethod.ReturnType;
            var specialParamTypes = new Type[] { typeof(PageRequest), typeof(Order), typeof(Order[]) };
            findMethodBaseInfo.PlainParameters = findMethod.GetParameters()
                .Where(t => !specialParamTypes.Contains(t.ParameterType)).ToArray();
            return findMethodBaseInfo;
        }
    }
}
