using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Navigation;
using HTMLConverter;

namespace PodBoy.Resource.Converter
{
    public class HtmlToFlowDocConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var xaml = HtmlToXamlConverter.ConvertHtmlToXaml((string) value, true);
            var flowDocument = XamlReader.Parse(xaml);
            var document = flowDocument as FlowDocument;

            if (document != null)
            {
                SubscribeToAllHyperlinks(document);
                document.PagePadding = new Thickness(0d);
            }
            return flowDocument;
        }

        private void SubscribeToAllHyperlinks(FlowDocument flowDocument)
        {
            var hyperlinks = GetVisuals(flowDocument).OfType<Hyperlink>();
            foreach (var link in hyperlinks)
            {
                link.RequestNavigate += LinkRequestNavigate;
            }
        }

        private static IEnumerable<DependencyObject> GetVisuals(DependencyObject root)
        {
            foreach (var child in
                LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            {
                yield return child;
                foreach (var descendants in GetVisuals(child))
                {
                    yield return descendants;
                }
            }
        }

        private void LinkRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}