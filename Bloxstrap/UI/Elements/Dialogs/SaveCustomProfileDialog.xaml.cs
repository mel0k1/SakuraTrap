using System.Windows;

namespace Bloxstrap.UI.Elements.Dialogs
{
    public partial class SaveCustomProfileDialog
    {
        public MessageBoxResult Result = MessageBoxResult.Cancel;

        public string ProfileName => NameTextBox.Text;

        public SaveCustomProfileDialog()
        {
            InitializeComponent();
            NameTextBox.Focus();
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            if (String.IsNullOrWhiteSpace(NameTextBox.Text))
                return;

            Result = MessageBoxResult.OK;
            Close();
        }
    }
}
