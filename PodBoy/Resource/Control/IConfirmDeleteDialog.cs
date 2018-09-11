using System.Windows;

namespace PodBoy.Resource.Control
{
    public interface IConfirmDeleteDialog
    {
        bool? ShowDialog();
        MessageBoxResult MessageBoxResult { get; }
        string Text { get; set; }
    }
}