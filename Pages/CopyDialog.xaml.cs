using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;


namespace better_power
{
    public enum CopyResult
    {
        CopySuccess,
        CopyCancel
    }

    public sealed partial class CopyDialog : ContentDialog
    {
        public CopyResult result { get; private set; }
        public string new_name { get; private set; }
        private string curr_name;

        public CopyDialog(string curr_name)
        {
            this.InitializeComponent();
            this.curr_name = curr_name;

            this.Opened += CopyDialog_Opened;
        }

        private void CopyDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            CopyDialogEditBox.Text = this.curr_name + " - Copy";
            CopyDialogEditBox.SelectAll();
        }

        private void CopyDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // If rename box is empty, set args.Cancel = true to keep the dialog open.
            if (string.IsNullOrEmpty(CopyDialogEditBox.Text))
            {
                args.Cancel = true;
                CopyDialogErrorMessageBox.Text = "New scheme name cannot be blank";
            }
            else
            {
                this.new_name = CopyDialogEditBox.Text;
                this.result = CopyResult.CopySuccess;
            }
        }

        private void CopyDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // User clicked Cancel, ESC, or the system back button.
            this.result = CopyResult.CopyCancel;
        }
    }

}
