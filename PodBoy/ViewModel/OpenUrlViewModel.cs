using System;
using PodBoy.Properties;
using PodBoy.Validation;
using ReactiveUI;

namespace PodBoy.ViewModel
{
    public class OpenUrlViewModel : ReactiveValidatedObject
    {
        private string urlInput;
        private bool isValid;

        public OpenUrlViewModel()
        {
            ValidationObservable.Subscribe(x => IsValid = !string.IsNullOrEmpty(UrlInput) && IsObjectValid());
        }

        [ValidatesViaMethod(Name = "IsUrlValid", AllowNullOrEmpty = true, ErrorMessageResourceType = typeof(Resources),
            ErrorMessageResourceName = "ValidationInvalidUrl")]
        public string UrlInput
        {
            get { return urlInput; }
            set { this.RaiseAndSetIfChanged(ref urlInput, value); }
        }

        public bool IsUrlValid(string url)
        {
            Uri uriResult;
            return Uri.TryCreate(urlInput, UriKind.Absolute, out uriResult)
                   && (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);
        }

        public bool IsValid
        {
            get { return isValid; }
            set { this.RaiseAndSetIfChanged(ref isValid, value); }
        }
    }
}