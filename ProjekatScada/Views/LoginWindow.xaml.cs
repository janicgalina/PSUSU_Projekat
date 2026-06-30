using System.Windows;
using ProjekatScada.ViewModels;

namespace ProjekatScada.Views
{
    public partial class LoginWindow : Window
    {
        private readonly LoginViewModel _viewModel;

        public LoginWindow()
        {
            InitializeComponent();
            _viewModel = new LoginViewModel
            {
                PasswordBox = PasswordInput
            };
            _viewModel.RequestClose += (sender, args) => Close();
            DataContext = _viewModel;
        }

        public bool LoginSuccessful
        {
            get { return _viewModel.LoginSuccessful; }
        }

        public Models.UserSession Session
        {
            get { return _viewModel.Session; }
        }
    }
}
