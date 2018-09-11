using System.Windows.Controls;
using FirstFloor.ModernUI.Windows.Controls;
using PodBoy.Properties;

namespace PodBoy.Resource.Control
{
    public class ConfirmDeleteDialog : ModernDialog, IConfirmDeleteDialog
    {
        public ConfirmDeleteDialog()
        {
            Buttons = new[]
            {
                YesButton,
                NoButton
            };
            NoButton.IsCancel = true;

            Title = Messages.CONFIRM_DELETE;
        }

        public string Text { get; set; }

        private void SetText()
        {
            ((ContentControl) Content).Content = Text;
        }

        public override void OnApplyTemplate()
        {
            base.OnApplyTemplate();
            SetText();
        }
    }
}