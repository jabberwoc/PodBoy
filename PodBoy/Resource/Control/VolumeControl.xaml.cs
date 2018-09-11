using System;
using System.Windows;

namespace PodBoy.Resource.Control
{
    /// <summary>
    /// Interaction logic for Volume.xaml
    /// </summary>
    public partial class VolumeControl
    {
        public static readonly DependencyProperty CurrentVolumeProperty = DependencyProperty.Register("CurrentVolume",
            typeof(double), typeof(VolumeControl), new UIPropertyMetadata(default(double), OnCurrentVolumeChanged));

        private static void OnCurrentVolumeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (VolumeControl) d;

            control.SetBarsChecked();
        }

        public double CurrentVolume
        {
            get { return (double) GetValue(CurrentVolumeProperty); }
            set { SetValue(CurrentVolumeProperty, value); }
        }

        public VolumeControl()
        {
            InitializeComponent();
        }

        private void SetBarsChecked()
        {
            var relativeVolume = CurrentVolume * VolumeCanvas.Height;
            foreach (VolumeBar bar in BarPannel.Children)
            {
                bar.IsChecked = bar.VolumeValue <= relativeVolume;
            }
        }

        private void OnVolumeBarClick(object sender, RoutedEventArgs e)
        {
            var volumeBar = sender as VolumeBar;
            if (volumeBar == null)
            {
                throw new ArgumentException(@"sender must be of type ToggleButton", "sender");
            }

            SetCurrentVolume(volumeBar.VolumeValue);
        }

        private void SetCurrentVolume(double relativeValue)
        {
            CurrentVolume = relativeValue / VolumeCanvas.Height;
        }
    }
}