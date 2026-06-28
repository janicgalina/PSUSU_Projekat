using System.Windows;

namespace ProjekatScada.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            UsernameTextBox.Focus();
        }

        public string Username { get; private set; }
        public bool LoginSuccessful { get; private set; }

        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(UsernameTextBox.Text))
            {
                ValidationTextBlock.Text = "Korisničko ime je obavezno.";
                return;
            }

            Username = UsernameTextBox.Text.Trim();
            LoginSuccessful = true;
            Close();
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            LoginSuccessful = false;
            Close();
        }
    }
}
