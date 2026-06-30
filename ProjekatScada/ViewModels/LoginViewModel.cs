using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ProjekatScada.Infrastructure;
using ProjekatScada.Models;
using ProjekatScada.Models.Enums;
using ProjekatScada.Services;

namespace ProjekatScada.ViewModels
{
    public class LoginViewModel : ObservableObject
    {
        private readonly AuthenticationService _authenticationService = new AuthenticationService();
        private UserRole _selectedRole = UserRole.Operator;
        private string _username;
        private string _validationMessage;

        public LoginViewModel()
        {
            Roles = new ObservableCollection<UserRole>
            {
                UserRole.Admin,
                UserRole.Operator,
                UserRole.Student,
                UserRole.Teacher
            };

            LoginCommand = new RelayCommand(_ => Login(GetPassword()));
            RegisterCommand = new RelayCommand(_ => Register(GetPassword()));
            ExitCommand = new RelayCommand(_ => Cancel());
        }

        public ObservableCollection<UserRole> Roles { get; private set; }

        public ICommand LoginCommand { get; private set; }
        public ICommand RegisterCommand { get; private set; }
        public ICommand ExitCommand { get; private set; }

        public PasswordBox PasswordBox { get; set; }

        public UserSession Session { get; private set; }
        public bool LoginSuccessful { get; private set; }

        public string Username
        {
            get { return _username; }
            set { SetProperty(ref _username, value); }
        }

        public UserRole SelectedRole
        {
            get { return _selectedRole; }
            set { SetProperty(ref _selectedRole, value); }
        }

        public string ValidationMessage
        {
            get { return _validationMessage; }
            set { SetProperty(ref _validationMessage, value); }
        }

        public event EventHandler RequestClose;

        private string GetPassword()
        {
            return PasswordBox != null ? PasswordBox.Password : string.Empty;
        }

        private void Login(string password)
        {
            try
            {
                ValidationMessage = string.Empty;
                Session = _authenticationService.Login(Username, password, SelectedRole);
                LoginSuccessful = true;
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ValidationMessage = ex.Message;
            }
        }

        private void Register(string password)
        {
            try
            {
                ValidationMessage = string.Empty;
                Session = _authenticationService.Register(Username, password, SelectedRole);
                LoginSuccessful = true;
                MessageBox.Show(
                    "Nalog je uspešno kreiran.",
                    "Registracija",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                RequestClose?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                ValidationMessage = ex.Message;
            }
        }

        private void Cancel()
        {
            LoginSuccessful = false;
            RequestClose?.Invoke(this, EventArgs.Empty);
        }
    }
}
