using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interactivity;
using PodBoy.Extension;
using ReactiveUI;
using TextBox = System.Windows.Controls.TextBox;

namespace PodBoy.Behavior
{
    public class ActivatedTextBoxBehavior : Behavior<TextBox>
    {
        public static readonly DependencyProperty ParentProperty = DependencyProperty.Register("Parent",
            typeof(ContentControl), typeof(ActivatedTextBoxBehavior), new UIPropertyMetadata());

        public ContentControl Parent
        {
            get { return (ContentControl) GetValue(ParentProperty); }
            set { SetValue(ParentProperty, value); }
        }

        protected override void OnAttached()
        {
            base.OnAttached();

            this.WhenAnyValue(_ => _.Parent).NotNull().Subscribe(_ => Init());
        }

        private void Init()
        {
            Parent.Events().MouseDoubleClick.Subscribe(OnMouseDoubleClick);

            AssociatedObject.Events()
                .KeyDown.Where(_ => _.Key == Key.Enter)
                .Select(_ => Unit.Default)
                .Merge(AssociatedObject.Events().LostKeyboardFocus.Select(_ => Unit.Default))
                .Subscribe(_ => OnKeyboardFocusLost());
        }

        private void OnKeyboardFocusLost()
        {
            AssociatedObject.IsEnabled = false;
            Keyboard.ClearFocus();
        }

        private void OnMouseDoubleClick(MouseButtonEventArgs e)
        {
            AssociatedObject.IsEnabled = true;
            AssociatedObject.Focus();
            AssociatedObject.SelectAll();
            e.Handled = true;
        }
    }
}