using System;
using System.Reactive.Linq;
using System.Windows;
using PodBoy.Entity;
using PodBoy.ViewModel;
using ReactiveUI;
using Splat;

namespace PodBoy.View
{
    /// <summary>
    /// Interaction logic for DetailView.xaml
    /// </summary>
    public partial class DetailView : IViewFor<DetailViewModel>
    {
        public static readonly DependencyProperty DetailEntityProperty = DependencyProperty.Register("DetailEntity",
            typeof(IDetailEntity), typeof(DetailView), new PropertyMetadata(null, OnDetailEntityChanged));

        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register("ViewModel",
            typeof(DetailViewModel), typeof(DetailView));

        public DetailView()
        {
            InitializeComponent();

            ViewModel = Locator.Current.GetService<DetailViewModel>();

            this.WhenActivated(d =>
            {
                d(this.WhenAnyValue(_ => _.ViewModel).BindTo(this, _ => _.DataContext));
                d(
                    this.WhenAnyValue(_ => _.Visibility)
                        .Where(_ => _ == Visibility.Visible)
                        .Subscribe(_ => SetDetailEntity()));
            });
        }

        public IDetailEntity DetailEntity
        {
            get { return (IDetailEntity) GetValue(DetailEntityProperty); }
            set { SetValue(DetailEntityProperty, value); }
        }

        object IViewFor.ViewModel
        {
            get { return ViewModel; }
            set { ViewModel = (DetailViewModel) value; }
        }

        public DetailViewModel ViewModel
        {
            get { return (DetailViewModel) GetValue(ViewModelProperty); }
            set
            {
                SetValue(ViewModelProperty, value);
                DataContext = value;
            }
        }

        private static void OnDetailEntityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var control = (DetailView) d;
            control.SetDetailEntity();
        }

        private void SetDetailEntity()
        {
            if (Visibility == Visibility.Visible)
            {
                ViewModel.DetailEntity = DetailEntity;
            }
        }
    }
}