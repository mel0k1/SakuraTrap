using System.Windows;

namespace Bloxstrap.UI.Elements.Dialogs
{
    public partial class SaveCustomProfileDialog
    {
        public MessageBoxResult Result = MessageBoxResult.Cancel;

        public string ProfileName => NameTextBox.Text;

        public SaveCustomProfileDialog() : this("") { }

        public SaveCustomProfileDialog(string title) : this(title, "") { }

        public SaveCustomProfileDialog(string title, string initialName)
        {
            InitializeComponent();

            if (!String.IsNullOrEmpty(title))
            {
                Title = title;
                DialogTitleBar.Title = title;
            }

            if (!String.IsNullOrEmpty(initialName))
                NameTextBox.Text = initialName;

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
