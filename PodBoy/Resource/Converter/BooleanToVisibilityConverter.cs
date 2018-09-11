using System.Windows;

namespace PodBoy.Resource.Converter
{
    public sealed class BooleanToVisibilityConverter : BooleanConverter<Visibility>
    {
        public BooleanToVisibilityConverter()
            : base(Visibility.Visible, Visibility.Collapsed) {}

        public BooleanToVisibilityConverter(Visibility trueValue, Visibility falseValue)
            : base(trueValue, falseValue) {}
    }
}