using System;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using PodBoy.Entity;
using PodBoy.Extension;
using ReactiveUI;

namespace PodBoy.Behavior
{
    public class OrderedValueTextBoxBehavior<T> : Behavior<TextBox> where T : IHasOrder
    {
        public static readonly DependencyProperty ListManagerProperty = DependencyProperty.Register("ListManager",
            typeof(IOrderedListManager<T>), typeof(OrderedValueTextBoxBehavior<T>), new UIPropertyMetadata());

        public static readonly DependencyProperty ItemProperty = DependencyProperty.Register("Item", typeof(T),
            typeof(OrderedValueTextBoxBehavior<T>), new UIPropertyMetadata());

        public IOrderedListManager<T> ListManager
        {
            get { return (IOrderedListManager<T>) GetValue(ListManagerProperty); }
            set { SetValue(ListManagerProperty, value); }
        }

        public T Item
        {
            get { return (T) GetValue(ItemProperty); }
            set { SetValue(ItemProperty, value); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            this.WhenAnyValue(_ => _.ListManager).NotNull().Subscribe(_ => Init());
        }

        private void Init()
        {
            AssociatedObject.Events()
                .LostKeyboardFocus.Select(_ => AssociatedObject.Text)
                .DistinctUntilChanged()
                .Subscribe(_ =>
                {
                    // TODO validate text (as int)
                    int value;
                    if (int.TryParse(_, out value))
                    {
                        if (value != Item.OrderNumber)
                        {
                            ListManager.MoveItemToIndex(Item, value);
                        }
                    }
                    else
                    {
                        AssociatedObject.Text = Item.OrderNumber.ToString();
                    }
                });
        }
    }
}