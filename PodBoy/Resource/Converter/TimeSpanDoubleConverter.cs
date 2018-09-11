using System;
using ReactiveUI;

namespace PodBoy.Resource.Converter
{
    public class TimeSpanDoubleConverter : IBindingTypeConverter
    {
        public int GetAffinityForObjects(Type fromType, Type toType)
        {
            throw new NotImplementedException();
        }

        public bool TryConvert(object @from, Type toType, object conversionHint, out object result)
        {
            if (toType == typeof(double))
            {
                result = default(double);
                if (!(@from is TimeSpan))
                {
                    return false;
                }

                var time = (TimeSpan) @from;

                result = time.TotalSeconds;
                return true;
            }

            if (toType == typeof(TimeSpan))
            {
                result = default(TimeSpan);
                if (!(@from is double))
                {
                    return false;
                }

                result = (double) @from;

                return true;
            }

            result = null;
            return false;
        }
    }
}