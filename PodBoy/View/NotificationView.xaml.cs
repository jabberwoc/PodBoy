using System.Windows;
using PodBoy.Notification;
using ReactiveUI;
using Splat;

namespace PodBoy.View
{
    /// <summary>
    /// Interaction logic for NotificationView.xaml
    /// </summary>
    public partial class NotificationView : IViewFor<NotificationViewModel>
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register("ViewModel",
            typeof(NotificationViewModel), typeof(NotificationView), new UIPropertyMetadata(null));

        public NotificationView()
        {
            InitializeComponent();

            ViewModel = Locator.Current.GetService<NotificationViewModel>();
            DataContext = ViewModel;
            this.WhenActivated(d =>
            {
                d(this.OneWayBind(ViewModel, vm => vm.NotificationView, v => v.NotificationList.ItemsSource));
                d(this.OneWayBind(ViewModel, vm => vm.IsVisible, v => v.NotificationList.Visibility));
                d(this.WhenAnyValue(_ => _.NotificationList.SelectedItem).BindTo(ViewModel, _ => _.SelectedElement));
            });
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (NotificationViewModel) value; }
        }

        public NotificationViewModel ViewModel
        {
            get { return (NotificationViewModel) GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
    }
}