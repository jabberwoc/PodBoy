using System.Windows;

namespace PodBoy.Extension
{
    // ReSharper disable once InconsistentNaming
    public static class UIElementExtensions
    {
        public static T TryFindFromPoint<T>(this UIElement reference, Point point) where T : DependencyObject
        {
            DependencyObject element = reference.InputHitTest(point) as DependencyObject;
            if (element == null)
            {
                return null;
            }
            if (element is T)
            {
                return (T) element;
            }
            return element.FindAncestor<T>();
        }
    }
}