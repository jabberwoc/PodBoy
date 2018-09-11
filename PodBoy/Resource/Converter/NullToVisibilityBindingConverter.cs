using System;
using System.Windows;
using ReactiveUI;

namespace PodBoy.Resource.Converter
{
    public class NullToVisibilityBindingConverter : IBindingTypeConverter
    {
        public int GetAffinityForObjects(Type fromType, Type toType)
        {
            throw new NotImplementedException();
        }

        public bool TryConvert(object @from, Type toType, object conversionHint, out object result)
        {
            result = Visibility.Collapsed;
            if (@from == null)
            {
                result = Visibility.Visible;
            }

            return true;
        }
    }
}