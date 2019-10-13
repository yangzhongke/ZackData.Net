using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using ZackData.NetStandard;
using ZackData.NetStandard.EF;
using ZackData.NetStandard.Exceptions;


namespace ZackData.NetStandard.Parsers
{
    class FindMethodNameParser
    {
        private static RegexOptions reOptions = RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled;
        //if use PredicateAttribute,the 'ByXXX'  should be ignored by parser
        private static readonly Regex reFindMethod = new Regex("^Find(By(?<PropertyPart>.+?))(OrderBy(?<OrderByName>.+)(?<OrderingRule>Asc|Desc)?)$", reOptions);

        //Name1,Name1AndName2,Name1OrName2
        private static readonly Regex reFindMethod_ByProperties = new Regex("^(?<Property1>.+?)(?<Operator>(And|Or)?)((?<Property2>.+)?)$", reOptions);//don't support more than 2 columnNames, because too long to be supported
        //Name1GreaterThan
        private static readonly Regex reFindMethod_PropertyTerm = new Regex("^(.+?)(Between|LessThan|LessThanEqual|GreaterThan|GreaterThanEqual|After|Before|IsNull|IsNotNull|Like|NotLike|StartingWith|EndingWith|Containing|Not|In|NotIn|True|False)$", reOptions);

        public static FindMethodBaseInfo Parse(MethodInfo findMethod)
        {
            PredicateAttribute predicateAttr = findMethod.GetCustomAttribute<PredicateAttribute>();
            string findMethodName = findMethod.Name;
            FindMethodBaseInfo findMethodBaseInfo;
            Match matchFindMethod = reFindMethod.Match(findMethodName);
            if (!matchFindMethod.Success)
            {
                throw new ConventionException($"Method {findMethodName} not comply with the pattern {reFindMethod}");
            }
            //begin analyse the methodName
            var groupsFindMethod = matchFindMethod.Groups;
            var groupPropertyPart = groupsFindMethod["PropertyPart"];

            if (predicateAttr!=null)
            {
                //if Find** with PredicateAttribute, the 'ByXXX'  should be ignored, but OrderBy,PageRequest,and Sort should not be ignored,
                findMethodBaseInfo = new FindByPredicateMethodInfo();
                ((FindByPredicateMethodInfo)findMethodBaseInfo).Predicate = predicateAttr.Predicate;
            }
            else if(!groupPropertyPart.Success)//no 'By', like FindOrderByPrice
            {
                findMethodBaseInfo = new FindWithoutByMethodInfo();
            }
            else
            {
                var matchFindMethod_ByProperties = reFindMethod_ByProperties.Match(groupPropertyPart.Value);
                var matchFindMethod_PropertyTerm = reFindMethod_PropertyTerm.Match(groupPropertyPart.Value);
                if(matchFindMethod_ByProperties.Success)//NameOrAge,NameAndAge,Name,Age
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
                else if(matchFindMethod_PropertyTerm.Success)//NameLike, AgeGreaterThan
                {
                    var groupPropertyName = matchFindMethod_PropertyTerm.Groups[1];
                    var groupPropertyVerb = matchFindMethod_PropertyTerm.Groups[2];
                    var propVerbMethodInfo = new FindByPropertyVerbMethodInfo();
                    propVerbMethodInfo.PropertyName = groupPropertyName.Value;
                    propVerbMethodInfo.Verb = ParserHelper.ParsePropertyVerb(groupPropertyVerb.Value);
                    findMethodBaseInfo = propVerbMethodInfo;
                }
                else
                {
                    throw new ConventionException($"{groupPropertyPart.Value} not comply with the pattern {reFindMethod_ByProperties} nor {reFindMethod_PropertyTerm}");
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


            ParameterInfo pageRequestParameter = Helper.FindSingleParameterOfType(findMethod, typeof(PageRequest));

            return findMethodBaseInfo;
        }
    }
}
