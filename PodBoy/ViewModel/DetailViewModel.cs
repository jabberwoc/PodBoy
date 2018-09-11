using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Navigation;
using System.Xml;
using HTMLConverter;
using PodBoy.Entity;
using PodBoy.Extension;
using Reactive.EventAggregator;
using ReactiveUI;

namespace PodBoy.ViewModel
{
    public class DetailViewModel : ReactiveViewModel, IProvidesImage
    {
        private IDetailEntity detailEntity;
        private string title;
        private string description;
        private Uri imageUri;
        private FlowDocument document;
        private const int DetailImagePixelWidth = 60;

        public DetailViewModel(IEventAggregator eventAggregator)
            : base(eventAggregator)
        {
            // image provider
            this.WhenAnyValue(_ => _.ImageUri).BindTo(ImageProvider, _ => _.ImageUri);
            this.WhenAnyValue(_ => _.DetailEntity).NotNull().DistinctUntilChanged().Subscribe(OnDetailChanged);
            this.WhenAnyValue(_ => _.Description).NotNull().DistinctUntilChanged().Subscribe(SetDocument);
        }

        private void SetDocument(string html)
        {
            var xaml = HtmlToXamlConverter.ConvertHtmlToXaml(html, true);
            var stringReader = new StringReader(xaml);
            var xmlReader = XmlReader.Create(stringReader);
            var xamlReader = new XamlReader();
            var flowDocument = xamlReader.LoadAsync(xmlReader);
            var doc = flowDocument as FlowDocument;

            if (doc != null)
            {
                SubscribeToAllHyperlinks(doc);
                doc.PagePadding = new Thickness(0d);
            }
            Document = doc;
        }

        public FlowDocument Document
        {
            get { return document; }
            set { this.RaiseAndSetIfChanged(ref document, value); }
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

        private void OnDetailChanged(IDetailEntity detail)
        {
            Title = detail.Title;
            Description = detail.Description;
            ImageUri = detail.ImageUri;
            ImageProvider.ResetImage();
        }

        public IDetailEntity DetailEntity
        {
            get { return detailEntity; }
            set { this.RaiseAndSetIfChanged(ref detailEntity, value); }
        }

        public string Title
        {
            get { return title; }
            set { this.RaiseAndSetIfChanged(ref title, value); }
        }

        public string Description
        {
            get { return description; }
            set { this.RaiseAndSetIfChanged(ref description, value); }
        }

        public Uri ImageUri
        {
            get { return imageUri; }
            set { this.RaiseAndSetIfChanged(ref imageUri, value); }
        }

        public int DecodePixelWidth => DetailImagePixelWidth;

        public IImageProvider ImageProvider { get; } = new ImageProvider(DetailImagePixelWidth);
    }
}