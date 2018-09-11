using System.Windows;
using System.Windows.Controls.Primitives;

namespace PodBoy.Resource.Control
{
    public class VolumeBar : ToggleButton
    {
        public static readonly DependencyProperty VolumeValueProperty = DependencyProperty.Register("VolumeValue",
            typeof(double), typeof(VolumeBar), new UIPropertyMetadata(default(double)));

        public double VolumeValue
        {
            get { return (double) GetValue(VolumeValueProperty); }
            set { SetValue(VolumeValueProperty, value); }
        }
    }
}