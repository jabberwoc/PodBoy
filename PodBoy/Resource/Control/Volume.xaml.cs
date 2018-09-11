using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using ReactiveUI;

namespace PodBoy.Resource.Control
{
    /// <summary>
    /// Interaction logic for Volume.xaml
    /// </summary>
    public partial class Volume
    {
        public static readonly DependencyProperty CurrentVolumeProperty = DependencyProperty.Register("CurrentVolume",
            typeof(double), typeof(Volume), new UIPropertyMetadata(1d));

        private const double DefaultScrollUnit = 120d;

        public Volume()
        {
            InitializeComponent();

            this.WhenAnyValue(_ => _.IsMouseOver).CombineLatest(this.WhenAnyValue(_ => _.Popup.IsMouseOver),
                (c, p) => c || p).BindTo(this, _ => _.Popup.IsOpen);

            this.Events().MouseWheel.Where(_ => Popup.IsOpen).Subscribe(ChangeVolume);
        }

        public double CurrentVolume
        {
            get { return (double) GetValue(CurrentVolumeProperty); }
            set { SetValue(CurrentVolumeProperty, value); }
        }

        private void ChangeVolume(MouseWheelEventArgs e)
        {
            var scrollUnit = Math.Abs(e.Delta) / DefaultScrollUnit * 0.1;

            CurrentVolume = e.Delta > 0
                ? Math.Min(1d, CurrentVolume + scrollUnit)
                : Math.Max(0d, CurrentVolume - scrollUnit);
        }
    }
}