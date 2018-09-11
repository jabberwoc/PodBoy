using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace PodBoy.Feed
{
    public class RssCorrectionRule
    {
        private static string LengthNoneTarget = "/rss/channel/item/enclosure[@length = \"None\"]";
        private static readonly Action<XElement> LengthNoneAction = e => e.SetAttributeValue("length", default(int));

        public static RssCorrectionRule lengthNoneRule = new RssCorrectionRule(LengthNoneTarget, new[]
        {
            LengthNoneAction
        });

        public static IList<RssCorrectionRule> AllRules()
        {
            return new[]
            {
                lengthNoneRule
            };
        }

        public string Expression { get; }
        public IList<Action<XElement>> Actions { get; }

        public RssCorrectionRule(string expression, IList<Action<XElement>> actions)
        {
            Expression = expression;
            Actions = actions;
        }
    }
}