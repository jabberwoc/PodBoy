using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows.Data;
using ReactiveUI;

namespace PodBoy.Resource.Converter
{
    public class BooleanConverter<T> : IValueConverter, IBindingTypeConverter
    {
        public BooleanConverter(T trueValue, T falseValue)
        {
            True = trueValue;
            False = falseValue;
        }

        public T True { get; set; }
        public T False { get; set; }

        public virtual object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool && ((bool) value) ? True : False;
        }

        public virtual object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is T && EqualityComparer<T>.Default.Equals((T) value, True);
        }

        public int GetAffinityForObjects(Type fromType, Type toType)
        {
            throw new NotImplementedException();
        }

        public bool TryConvert(object @from, Type toType, object conversionHint, out object result)
        {
            result = Convert(@from, toType, conversionHint, CultureInfo.InvariantCulture);
            return true;
        }
    }
}