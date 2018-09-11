using System.Windows;
using PodBoy.ViewModel;
using ReactiveUI;
using Splat;

namespace PodBoy.View
{
    /// <summary>
    /// Interaction logic for OpenUrlView.xaml
    /// </summary>
    public partial class OpenUrlView : IViewFor<OpenUrlViewModel>
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register("ViewModel",
            typeof(OpenUrlViewModel), typeof(OpenUrlView), new UIPropertyMetadata(null));

        public OpenUrlView()
        {
            InitializeComponent();

            this.WhenActivated(d => { d(this.WhenAnyValue(_ => _.ViewModel).BindTo(this, _ => _.DataContext)); });
            ViewModel = Locator.Current.GetService<OpenUrlViewModel>();
            DataContext = ViewModel; // prevent initial binding error
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (OpenUrlViewModel) value; }
        }

        public OpenUrlViewModel ViewModel
        {
            get { return (OpenUrlViewModel) GetValue(ViewModelProperty); }
            set { SetValue(ViewModelProperty, value); }
        }
    }
}