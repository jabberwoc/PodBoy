using PodBoy.Resource.Control;
using Splat;

namespace PodBoy
{
    public class DialogService : IDialogService
    {
        public IConfirmDeleteDialog RequestDeleteDialog(string text)
        {
            var dialog = Locator.Current.GetService<IConfirmDeleteDialog>();
            dialog.Text = text;
            return dialog;
        }
    }
}