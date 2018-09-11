using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interactivity;
using System.Windows.Shapes;
using PodBoy.Entity;
using PodBoy.Layout.Adorners;
using PodBoy.Extension;

namespace PodBoy.Behavior
{
    public class ListBoxDragDropBehavior<T> : Behavior<ListBox>, IListBoxDragDropBehavior where T : class, IHasOrder
    {
        public static readonly DependencyProperty ListManagerProperty = DependencyProperty.Register("ListManager",
            typeof(IOrderedListManager<T>), typeof(ListBoxDragDropBehavior<T>), new UIPropertyMetadata());

        public static readonly DependencyProperty ClickThroughProperty = DependencyProperty.Register("ClickThrough",
            typeof(bool), typeof(ListBoxDragDropBehavior<T>), new UIPropertyMetadata());

        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.Register("IsEnabled",
            typeof(bool), typeof(ListBoxDragDropBehavior<T>), new UIPropertyMetadata());

        private const string DropAreaBottomName = "DropAreaBottom";

        private const string DropAreaTopName = "DropAreaTop";

        private const string ItemContentContainer = "ItemContentContainer";

        private DragAdorner adorner;

        private ListBoxItem draggedItem;

        private Point dragStartPoint;

        private Rectangle dropAreaOpen;

        private string typeName;

        public IOrderedListManager<T> ListManager
        {
            get { return (IOrderedListManager<T>) GetValue(ListManagerProperty); }
            set { SetValue(ListManagerProperty, value); }
        }

        public bool ClickThrough
        {
            get { return (bool) GetValue(ClickThroughProperty); }
            set { SetValue(ClickThroughProperty, value); }
        }

        public bool IsEnabled
        {
            get { return (bool) GetValue(IsEnabledProperty); }
            set { SetValue(IsEnabledProperty, value); }
        }

        public UIElement GetItemContainerFromItemsControl(T item)
        {
            UIElement container;
            if (AssociatedObject != null && AssociatedObject.Items.Count > 0)
            {
                container = AssociatedObject.ItemContainerGenerator.ContainerFromItem(item) as UIElement;
            }
            else
            {
                container = AssociatedObject;
            }
            return container;
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            typeName = typeof(T).Name;

            AssociatedObject.Loaded += OnLoaded;
        }

        private void DetachDragAdorner()
        {
            if (adorner != null)
            {
                adorner.Destroy();
                adorner = null;
            }
        }

        private T FindListBoxItem(object e)
        {
            var item = ((DependencyObject) e).FindAncestor<ListBoxItem>();

            if (item == null)
            {
                return null;
            }

            return AssociatedObject.ItemContainerGenerator.ItemFromContainer(item) as T;
        }

        private void InitializeDragAdorner(Point startPosition)
        {
            if (adorner != null)
            {
                return;
            }
            if (draggedItem == null)
            {
                return;
            }
            var contentContainer = draggedItem.FindChild<Grid>(ItemContentContainer);

            adorner = new DragAdorner(AssociatedObject, contentContainer, true, 1);
            var adornerLayer = AdornerLayer.GetAdornerLayer(AssociatedObject);
            adornerLayer.Add(adorner);
            adorner.UpdatePosition(startPosition);
        }

        private bool IsDragging(Point startPoint, MouseEventArgs e)
        {
            var diff = e.GetPosition(null) - startPoint;
            return e.LeftButton == MouseButtonState.Pressed
                   && (Math.Abs(diff.X) > SystemParameters.MinimumHorizontalDragDistance
                       || Math.Abs(diff.Y) > SystemParameters.MinimumVerticalDragDistance);
        }

        private async Task OnDrop(DragEventArgs e)
        {
            DetachDragAdorner();

            if (e.Data.GetDataPresent(typeName))
            {
                var source = e.Data.GetData(typeName) as T;
                if (source == null)
                {
                    return;
                }

                var item = AssociatedObject.TryFindFromPoint<ListBoxItem>(e.GetPosition(AssociatedObject));
                if (item == null)
                {
                    return;
                }

                var target = AssociatedObject.ItemContainerGenerator.ItemFromContainer(item) as T;
                if (target == null)
                {
                    return;
                }

                if (dropAreaOpen == null)
                {
                    return;
                }

                // determine target index
                var targetIndex = target.OrderNumber;
                if (Equals(dropAreaOpen.Name, DropAreaTopName))
                {
                    if (source.OrderNumber < targetIndex)
                    {
                        targetIndex--;
                    }
                    targetIndex = Math.Max(1, targetIndex);
                }
                else if (Equals(dropAreaOpen.Name, DropAreaBottomName))
                {
                    if (source.OrderNumber > targetIndex)
                    {
                        targetIndex++;
                    }
                    targetIndex = Math.Min(AssociatedObject.Items.Count, targetIndex);
                }

                ResetDropArea();

                var listBoxItem = GetItemContainerFromItemsControl(source);
                if (listBoxItem != null)
                {
                    listBoxItem.Visibility = Visibility.Visible;
                }

                await ListManager.MoveItemToIndex(source, targetIndex);
            }
        }

        private void OnItemDragEnter(ListBoxItem item, DragEventArgs e)
        {
            if (item == null)
            {
                return;
            }

            AssociatedObject.AllowDrop = false;

            OpenDropArea(item, e);

            InitializeDragAdorner(e.GetPosition(AssociatedObject));

            e.Handled = true;
        }

        private void OnItemDragLeave(DragEventArgs e)
        {
            ResetDropArea();
            DetachDragAdorner();

            e.Handled = true;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Events().PreviewMouseMove.Subscribe(OnPreviewMouseMove);
            AssociatedObject.Events().PreviewMouseLeftButtonDown.Subscribe(OnPreviewMouseLeftButtonDown);
            AssociatedObject.Events().Drop.Subscribe(async _ => await OnDrop(_));
            AssociatedObject.Events().MouseLeftButtonUp.Subscribe(OnMouseLeftButtonUp);

            AssociatedObject.Events().PreviewDragOver.Where(_ => _.Data.GetDataPresent(typeName)).Select(_ => new
            {
                Item = TargetItemFromEvent(_),
                Event = _
            }).Subscribe(_ => OnPreviewDragOver(_.Item, _.Event));

            AssociatedObject.Events().PreviewDragEnter.Where(_ => _.Data.GetDataPresent(typeName)).Select(_ => new
            {
                Item = TargetItemFromEvent(_),
                Event = _
            }).Subscribe(_ => OnItemDragEnter(_.Item, _.Event));
            AssociatedObject.Events()
                .PreviewDragLeave.Where(_ => _.Data.GetDataPresent(typeName))
                .Subscribe(OnItemDragLeave);

            AssociatedObject.Loaded -= OnLoaded;
        }

        private void OnMouseLeftButtonUp(MouseButtonEventArgs e)
        {
            if (!IsEnabled)
            {
                return;
            }

            var item = FindListBoxItem(e.OriginalSource);

            if (item != null)
            {
                AssociatedObject.SelectedItem = item;
            }
        }

        private void OnPreviewDragOver(ListBoxItem item, DragEventArgs e)
        {
            if (item != null && e.Data.GetDataPresent(typeName))
            {
                UpdateDragAdorner(e.GetPosition(AssociatedObject));

                UpdateDropArea(item, e);
            }
            e.Handled = true;
        }

        private void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            if (!IsEnabled)
            {
                return;
            }

            dragStartPoint = e.GetPosition(null);

            if (!ClickThrough)
            {
                e.Handled = true;
            }
        }

        private void OnPreviewMouseMove(MouseEventArgs e)
        {
            if (!IsEnabled)
            {
                return;
            }

            if (AssociatedObject.Items.Count > 1 && IsDragging(dragStartPoint, e))
            {
                var listBoxItem = ((DependencyObject) e.OriginalSource).FindAncestor<ListBoxItem>();

                if (listBoxItem == null)
                {
                    return;
                }

                draggedItem = listBoxItem;
                // hide current list box item
                SetDragTargetVisibility(false);

                var item = AssociatedObject.ItemContainerGenerator.ItemFromContainer(listBoxItem);

                // drag & drop
                var dragData = new DataObject(typeName, item);
                var result = DragDrop.DoDragDrop(listBoxItem, dragData, DragDropEffects.Move);

                if (result == DragDropEffects.None)
                {
                    SetDragTargetVisibility(true);
                }
                draggedItem = null;

                e.Handled = true;
            }
        }

        private void OpenDropArea(ListBoxItem item, DragEventArgs e)
        {
            var itemContent = item.FindChild<Grid>(ItemContentContainer);

            double mousePointY = e.GetPosition(itemContent).Y;
            double itemCenter = itemContent.ActualHeight / 2;

            // set active drop area
            var activeDropArea =
                item.FindChild<Rectangle>(mousePointY <= itemCenter ? DropAreaTopName : DropAreaBottomName);

            if (dropAreaOpen != null)
            {
                dropAreaOpen.Visibility = Visibility.Collapsed;
            }
            activeDropArea.Visibility = Visibility.Visible;
            dropAreaOpen = activeDropArea;
        }

        private void UpdateDropArea(ListBoxItem item, DragEventArgs e)
        {
            var itemContent = item.FindChild<Grid>(ItemContentContainer);

            double mousePointY = e.GetPosition(itemContent).Y;
            double itemCenter = itemContent.ActualHeight / 2;

            var activeDropArea =
                item.FindChild<Rectangle>(mousePointY <= itemCenter ? DropAreaTopName : DropAreaBottomName);

            if (!Equals(dropAreaOpen, activeDropArea))
            {
                dropAreaOpen.Visibility = Visibility.Collapsed;
                activeDropArea.Visibility = Visibility.Visible;

                dropAreaOpen = activeDropArea;
            }

            //else if (!Equals(dropAreaOpen, activeDropArea))
            //{
            //    dropAreaOpen.Visibility = Visibility.Collapsed;
            //    activeDropArea.Visibility = Visibility.Visible;

            //    dropAreaOpen = activeDropArea;
            //}
        }

        private void ResetDropArea()
        {
            if (dropAreaOpen != null)
            {
                dropAreaOpen.Visibility = Visibility.Collapsed;
                dropAreaOpen = null;
            }
        }

        private void SetDragTargetVisibility(bool isVisible)
        {
            if (draggedItem == null)
            {
                return;
            }
            draggedItem.Visibility = isVisible ? Visibility.Visible : Visibility.Collapsed;
        }

        private ListBoxItem TargetItemFromEvent(DragEventArgs dragEventArgs)
        {
            return AssociatedObject.TryFindFromPoint<ListBoxItem>(dragEventArgs.GetPosition(AssociatedObject));
        }

        private void UpdateDragAdorner(Point currentPosition)
        {
            adorner?.UpdatePosition(currentPosition);
        }
    }
}