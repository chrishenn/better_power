﻿using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;


namespace better_power
{
    public enum RenameResult
    {
        RenameSuccess,
        RenameCancel
    }

    public sealed partial class RenameDialog : ContentDialog
    {
        public RenameResult result { get; private set; }
        public string new_name { get; private set; }
        private string curr_name;

        public RenameDialog(string curr_name)
        {
            this.InitializeComponent();
            this.curr_name = curr_name;

            this.Opened += RenameDialog_Opened;
        }

        private void RenameDialog_Opened(ContentDialog sender, ContentDialogOpenedEventArgs args)
        {
            RenameDialogEditBox.Text = this.curr_name;
            RenameDialogEditBox.SelectAll();
        }

        private void RenameDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // If rename box is empty, set args.Cancel = true to keep the dialog open.
            if (string.IsNullOrEmpty(RenameDialogEditBox.Text))
            {
                args.Cancel = true;
                RenameDialogErrorMessageBox.Text = "New scheme name cannot be blank";
            }
            else
            {
                this.new_name = RenameDialogEditBox.Text;
                this.result = RenameResult.RenameSuccess;
            }
        }

        private void RenameDialog_CloseButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // User clicked Cancel, ESC, or the system back button.
            this.result = RenameResult.RenameCancel;
        }

    }

}
