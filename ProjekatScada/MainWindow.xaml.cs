using System;
using System.Windows;
using ProjekatScada.Models;
using ProjekatScada.Services;
using ProjekatScada.ViewModels;

namespace ProjekatScada
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel _viewModel;
        private AdminInactivityMonitor _inactivityMonitor;

        public MainWindow()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            DataContext = _viewModel;
        }

        public event EventHandler SessionExpired;
        public event EventHandler LogoutRequested;

        public void InitializeSession(UserSession session)
        {
            _viewModel.InitializeAfterLogin(session);
            _viewModel.LogoutRequested += ViewModel_LogoutRequested;

            if (session.CanWrite)
            {
                _inactivityMonitor = new AdminInactivityMonitor(this, TimeSpan.FromMinutes(5));
                _inactivityMonitor.SessionExpired += InactivityMonitor_SessionExpired;
                _inactivityMonitor.Start();
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _viewModel.LogoutRequested -= ViewModel_LogoutRequested;
            _viewModel.Shutdown();

            if (_inactivityMonitor != null)
            {
                _inactivityMonitor.Stop();
                _inactivityMonitor.SessionExpired -= InactivityMonitor_SessionExpired;
            }

            base.OnClosed(e);
        }

        private void InactivityMonitor_SessionExpired(object sender, EventArgs e)
        {
            MessageBox.Show(
                "Admin sesija je istekla zbog neaktivnosti (5 minuta).",
                "Auto logout",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            var handler = SessionExpired;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void ViewModel_LogoutRequested(object sender, EventArgs e)
        {
            var handler = LogoutRequested;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }
    }
}
