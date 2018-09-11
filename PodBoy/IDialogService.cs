using PodBoy.Resource.Control;

namespace PodBoy
{
    public interface IDialogService
    {
        IConfirmDeleteDialog RequestDeleteDialog(string text);
    }
}