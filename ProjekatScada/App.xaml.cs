using System;
using System.IO;
using System.Windows;
using ProjekatScada.Views;

namespace ProjekatScada
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            AppDomain.CurrentDomain.SetData("DataDirectory", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "App_Data"));
            Directory.CreateDirectory((string)AppDomain.CurrentDomain.GetData("DataDirectory"));

            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            ShowLoginAndMainLoop();
        }

        private void ShowLoginAndMainLoop()
        {
            var loginWindow = new LoginWindow();
            loginWindow.ShowDialog();

            if (!loginWindow.LoginSuccessful || loginWindow.Session == null)
            {
                Shutdown();
                return;
            }

            var mainWindow = new MainWindow();
            mainWindow.SessionExpired += MainWindow_SessionExpired;
            mainWindow.InitializeSession(loginWindow.Session);
            MainWindow = mainWindow;
            mainWindow.Show();
        }

        private void MainWindow_SessionExpired(object sender, EventArgs e)
        {
            var mainWindow = sender as MainWindow;
            if (mainWindow != null)
            {
                mainWindow.SessionExpired -= MainWindow_SessionExpired;
                mainWindow.Close();
            }

            LogSessionExpired();
            ShowLoginAndMainLoop();
        }

        private static void LogSessionExpired()
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "system.log");
            var line = string.Format("{0:yyyy-MM-dd HH:mm:ss} | Admin je automatski odjavljen zbog neaktivnosti.", DateTime.Now);
            File.AppendAllLines(logPath, new[] { line });
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            LogUnhandledException(e.Exception);
            MessageBox.Show(e.Exception.Message, "Greška", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var exception = e.ExceptionObject as Exception;
            if (exception != null)
            {
                LogUnhandledException(exception);
            }
        }

        private static void LogUnhandledException(Exception exception)
        {
            var logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "system.log");
            var line = string.Format("{0:yyyy-MM-dd HH:mm:ss} | ERROR | Neočekivana greška: {1}", DateTime.Now, exception.Message);
            File.AppendAllLines(logPath, new[] { line });
        }
    }
}
