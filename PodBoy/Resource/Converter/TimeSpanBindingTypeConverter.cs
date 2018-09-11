using System;
using PodBoy.Extension;
using ReactiveUI;

namespace PodBoy.Resource.Converter
{
    public class TimeSpanBindingTypeConverter : IBindingTypeConverter
    {
        public int GetAffinityForObjects(Type fromType, Type toType)
        {
            throw new NotImplementedException();
        }

        public bool TryConvert(object @from, Type toType, object conversionHint, out object result)
        {
            result = string.Empty;
            if (@from is double)
            {
                @from = TimeSpan.FromSeconds((double) @from);
            }
            if (!(@from is TimeSpan))
            {
                return false;
            }

            var time = (TimeSpan) @from;

            var isOptionSet = conversionHint is bool;
            if (isOptionSet && (bool) conversionHint && time == TimeSpan.Zero)
            {
                return true;
            }

            result = time.ToShortFormat();

            return true;
        }
    }
}