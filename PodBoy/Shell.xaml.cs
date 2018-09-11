using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;
using PodBoy.Properties;
using ReactiveUI;

namespace PodBoy
{
    /// <summary>
    /// Interaction logic for Shell.xaml
    /// </summary>
    public partial class Shell : IViewFor<ShellViewModel>
    {
        public static readonly DependencyProperty ViewModelProperty;

        static Shell()
        {
            ViewModelProperty = DependencyProperty.Register("ViewModel", typeof(ShellViewModel), typeof(Shell));
        }

        public Shell(ShellViewModel viewModel)
        {
            InitializeComponent();

            if (Settings.Default.UpgradeRequired)
            {
                Settings.Default.Upgrade();
                Settings.Default.UpgradeRequired = false;
                Settings.Default.Save();
            }

            SetWindowProperties();

            ViewModel = viewModel;
            DataContext = ViewModel;

            this.WhenActivated(d =>
            {
                d(this.Bind(ViewModel, vm => vm.Router, v => v.ViewHost.Router));

                d(this.BindCommand(ViewModel, vm => vm.NavigateToPodcasts, v => v.NavigateToList));

                d(this.BindCommand(ViewModel, vm => vm.NavigateToPlaylist, v => v.NavigateToPlaylist));

                d(this.Events().Closing.Subscribe(_ =>
                {
                    ViewModel.DeactivateViewModels();
                    SaveWindowProperties();
                }));

                d(
                    this.Events()
                        .KeyUp.Where(x => x.Key == Key.Space)
                        .Subscribe(_ => ViewModel.RaiseKeyboardEvent(ShortcutCommandType.TogglePlay)));

                d(this.OneWayBind(ViewModel, vm => vm.IsBusy, x => x.BusyOverlay.Visibility));
            });
        }

        private void SetWindowProperties()
        {
            Top = Settings.Default.Top;
            Left = Settings.Default.Left;
            Height = Settings.Default.Height;
            Width = Settings.Default.Width;
            WindowState = Settings.Default.WindowState;
        }

        private void SaveWindowProperties()
        {
            Settings.Default.Top = Top;
            Settings.Default.Left = Left;
            Settings.Default.Height = Height;
            Settings.Default.Width = Width;
            Settings.Default.WindowState = WindowState == WindowState.Minimized ? WindowState.Normal : WindowState;

            Settings.Default.Save();
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (ShellViewModel) value; }
        }

        public ShellViewModel ViewModel
        {
            get { return (ShellViewModel) GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
    }
}