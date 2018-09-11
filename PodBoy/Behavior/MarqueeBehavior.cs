using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using System.Windows.Media;
using System.Windows.Media.Animation;
using ReactiveUI;

namespace PodBoy.Behavior
{
    public class MarqueeBehavior : Behavior<TextBlock>
    {
        public static readonly DependencyProperty CanvasProperty = DependencyProperty.Register("Canvas", typeof(Canvas),
            typeof(MarqueeBehavior), new UIPropertyMetadata());

        public Canvas Canvas
        {
            get { return (Canvas) GetValue(CanvasProperty); }
            set { SetValue(CanvasProperty, value); }
        }

        public Storyboard Storyboard { get; set; }

        public DoubleAnimation Animation { get; set; }

        private void Init()
        {
            StopStoryboard();
            SetTextAndAnimationPosition();
            if (ShouldAnimate)
            {
                StartAnimation();
            }
        }

        private void StopStoryboard()
        {
            Storyboard?.Stop(AssociatedObject);
        }

        public bool ShouldAnimate => AssociatedObject.ActualWidth >= Canvas.ActualWidth;

        public double AnimationOffset => Canvas.ActualWidth - AssociatedObject.ActualWidth;

        private void InitStoryboard()
        {
            if (Storyboard == null)
            {
                Animation = new DoubleAnimation(0, AnimationOffset,
                    new Duration(TimeSpan.FromSeconds(Math.Abs(AnimationOffset) / 10)))
                {
                    FillBehavior = FillBehavior.Stop
                };

                Storyboard.SetTarget(Animation, AssociatedObject);
                var transform = new TranslateTransform();
                AssociatedObject.RenderTransform = transform;
                Storyboard.SetTargetProperty(Animation, new PropertyPath("RenderTransform.(TranslateTransform.X)"));

                Storyboard = new Storyboard();
                Storyboard.Children.Add(Animation);
            }
        }

        private void StartAnimation()
        {
            InitStoryboard();
            StopStoryboard();
            Storyboard.Begin(AssociatedObject, true);
        }

        private void RunAnimation()
        {
            if (ShouldAnimate)
            {
                InitStoryboard();
                if (Storyboard.GetCurrentState(AssociatedObject) != ClockState.Active)
                {
                    Storyboard.Stop(AssociatedObject);
                    Storyboard.Begin(AssociatedObject, true);
                }
            }
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            Canvas.Events()
                .IsMouseDirectlyOverChanged.Select(_ => (bool) _.NewValue)
                .Where(_ => _)
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => RunAnimation());

            var textChanged =
                this.WhenAnyValue(_ => _.AssociatedObject.Text).DistinctUntilChanged().Select(_ => Unit.Default);
            textChanged.Subscribe(_ => AssociatedObject.Visibility = Visibility.Hidden);

            var textBlockSizeChanged =
                Canvas.Events()
                    .SizeChanged.Merge(AssociatedObject.Events().SizeChanged)
                    .Where(_ => Canvas.ActualWidth > 0)
                    .Select(_ => Unit.Default);

            textChanged.Merge(textBlockSizeChanged)
                .Throttle(TimeSpan.FromSeconds(.2))
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(_ => Init());
        }

        private void SetTextAndAnimationPosition()
        {
            if (AssociatedObject.ActualWidth >= Canvas.ActualWidth)
            {
                Canvas.SetLeft(AssociatedObject, 0);
                if (Animation != null)
                {
                    Animation.To = Canvas.ActualWidth - AssociatedObject.ActualWidth;
                    Animation.Duration = new Duration(TimeSpan.FromSeconds(Math.Abs(AnimationOffset) / 10));
                }
            }
            else
            {
                Canvas.SetLeft(AssociatedObject, Canvas.ActualWidth / 2 - AssociatedObject.ActualWidth / 2);
            }

            // show
            AssociatedObject.Visibility = Visibility.Visible;
        }
    }
}