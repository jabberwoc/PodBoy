using System.Reactive.Concurrency;
using System.Windows;
using FluentAssertions;
using ReactiveUI;
using Splat;
using Xunit;

namespace Tests
{
    public class AppBootstrapperTest
    {
        [Fact]
        public void ShouldLocateViewForViewModel()
        {
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;

            Locator.CurrentMutable.Register(() => new DummyView(), typeof(IViewFor<DummyViewModel>));
            Locator.CurrentMutable.InitializeSplat();
            Locator.CurrentMutable.InitializeReactiveUI();
            RxApp.MainThreadScheduler = Scheduler.CurrentThread;

            var viewLocator = Locator.Current.GetService<IViewLocator>();

            var viewModel = new DummyViewModel();
            var view = viewLocator.ResolveView(viewModel);

            view.Should().NotBeNull().And.BeOfType<DummyView>();
        }

        public class DummyViewModel {}

        public class DummyView : DependencyObject, IViewFor<DummyViewModel>
        {
            public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register("ViewModel",
                typeof(DummyViewModel), typeof(DummyView));

            object IViewFor.ViewModel
            {
                get { return ViewModel; }
                set { ViewModel = (DummyViewModel) value; }
            }

            public DummyViewModel ViewModel
            {
                get { return (DummyViewModel) GetValue(ViewModelProperty); }
                set { SetValue(ViewModelProperty, value); }
            }
        }
    }
}