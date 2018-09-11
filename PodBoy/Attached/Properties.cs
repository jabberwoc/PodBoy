using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;
using PodBoy.Behavior;
using PodBoy.Extension;
using PodBoy.Notification;

namespace PodBoy.Attached
{
    public class Properties
    {
        private static readonly DependencyProperty IsFadeOutProperty = DependencyProperty.RegisterAttached("IsFadeOut",
            typeof(bool), typeof(Properties), new PropertyMetadata(OnIsFadeOut));

        private static readonly DependencyProperty OffsetProperty = DependencyProperty.RegisterAttached("Offset",
            typeof(double), typeof(Properties));

        private static readonly DependencyProperty IsDragDropEnabledProperty =
            DependencyProperty.RegisterAttached("IsDragDropEnabled", typeof(bool), typeof(Properties),
                new PropertyMetadata(true, OnIsDragDropEnabled));

        private static void OnIsDragDropEnabled(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var listBox = d.FindAncestor<ListBox>();

            var behaviors = Interaction.GetBehaviors(listBox);
            var behavior = behaviors?.FirstOrDefault(_ => _ is IListBoxDragDropBehavior);

            if (behavior == null)
            {
                throw new InvalidOperationException($"{typeof(IListBoxDragDropBehavior)} not found on ListBox");
            }

            ((IListBoxDragDropBehavior) behavior).IsEnabled = (bool) e.NewValue;
        }

        private static void OnIsFadeOut(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var item = d as ListBoxItem;
            var element = item?.DataContext as NotificationElement;
            if (element == null || !(e.NewValue is bool))
            {
                return;
            }

            var isFadeOut = (bool) e.NewValue;
            if (isFadeOut)
            {
                element.IsActive = false;
            }
        }

        public static void SetIsFadeOut(DependencyObject obj, bool value)
        {
            obj.SetValue(IsFadeOutProperty, value);
        }

        public static bool GetIsFadeOut(DependencyObject obj)
        {
            return (bool) obj.GetValue(IsFadeOutProperty);
        }

        public static void SetOffset(DependencyObject obj, double value)
        {
            obj.SetValue(OffsetProperty, value);
        }

        public static double GetOffset(DependencyObject obj)
        {
            return (double) obj.GetValue(OffsetProperty);
        }

        public static void SetIsDragDropEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsDragDropEnabledProperty, value);
        }

        public static bool GetIsDragDropEnabled(DependencyObject obj)
        {
            return (bool) obj.GetValue(IsDragDropEnabledProperty);
        }
    }
}