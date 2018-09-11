using System;
using System.Linq.Expressions;
using ReactiveUI;

namespace PodBoy.Extension
{
    public static class ReactiveObjectExtensions
    {
        public static void RaisePropertyChanged<T>(this IReactiveObject reactiveObject,
            Expression<Func<T>> propertyExpression)
        {
            var memberExpr = propertyExpression.Body as MemberExpression;
            if (memberExpr == null)
            {
                throw new ArgumentException("propertyExpression should represent access to a member");
            }
            string memberName = memberExpr.Member.Name;
            // ReSharper disable once ExplicitCallerInfoArgument
            reactiveObject.RaisePropertyChanged(memberName);
        }
    }
}