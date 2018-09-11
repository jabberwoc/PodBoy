using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PodBoy.Layout.Adorners
{
    public sealed class DragAdorner : Adorner
    {
        private readonly UIElement child;
        private readonly double xCenter;
        private readonly double yCenter;
        private double leftOffset;
        private double topOffset;

        public DragAdorner(UIElement owner, UIElement child, bool useVisualBrush, double opacity)
            : base(owner)
        {
            if (!useVisualBrush)
            {
                this.child = child;
            }
            else
            {
                var size = GetRealSize(child);
                xCenter = size.Width / 2;
                yCenter = size.Height / 2;

                //this.child = content;

                var border = new Border();
                var containerBackground = Application.Current.TryFindResource("GrayBrush") as Brush;
                if (containerBackground != null)
                {
                    border.Background = containerBackground;
                    //border.BorderBrush = Application.Current.TryFindResource("DarkerGrayBrush") as Brush;
                    //border.BorderThickness = new Thickness(1);
                    //border.Padding = new Thickness(3);
                }

                border.Child = new Rectangle
                {
                    RadiusX = 3,
                    RadiusY = 3,
                    Width = size.Width,
                    Height = size.Height,
                    Fill = new VisualBrush(child)
                    {
                        Opacity = opacity,
                        AlignmentX = AlignmentX.Left,
                        AlignmentY = AlignmentY.Top,
                        Stretch = Stretch.None,
                    },
                };

                this.child = border;
            }
        }

        protected override int VisualChildrenCount => 1;

        public double LeftOffset
        {
            get { return leftOffset + xCenter; }
            set
            {
                leftOffset = value - xCenter;
                UpdatePosition();
            }
        }

        public double TopOffset
        {
            get { return topOffset + yCenter; }
            set
            {
                topOffset = value - yCenter;
                UpdatePosition();
            }
        }

        private static Size GetRealSize(UIElement child)
        {
            return child?.RenderSize ?? Size.Empty;
        }

        public void UpdatePosition(Point point)
        {
            leftOffset = point.X;
            topOffset = point.Y;
            UpdatePosition();
        }

        public void UpdatePosition()
        {
            var adorner = Parent as AdornerLayer;
            adorner?.Update(AdornedElement);
        }

        protected override Visual GetVisualChild(int index)
        {
            if (0 != index)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            return child;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            child.Measure(availableSize);
            return child.DesiredSize;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            child.Arrange(new Rect(child.DesiredSize));
            return finalSize;
        }

        public override GeneralTransform GetDesiredTransform(GeneralTransform transform)
        {
            var result = new GeneralTransformGroup();
            result.Children.Add(new TranslateTransform(leftOffset, topOffset));

            var baseTransform = base.GetDesiredTransform(transform);
            if (baseTransform != null)
            {
                result.Children.Add(baseTransform);
            }

            return result;
        }

        public void Destroy()
        {
            var adorner = Parent as AdornerLayer;
            adorner?.Remove(this);
        }
    }
}