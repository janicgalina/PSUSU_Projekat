using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace ProjekatScada.Services
{
    public class AdminInactivityMonitor
    {
        private readonly Window _window;
        private readonly DispatcherTimer _timer;
        private DateTime _lastActivityUtc;

        public AdminInactivityMonitor(Window window, TimeSpan timeout)
        {
            _window = window;
            Timeout = timeout;
            _lastActivityUtc = DateTime.UtcNow;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(30) };
            _timer.Tick += Timer_Tick;

            _window.PreviewMouseMove += Window_Activity;
            _window.PreviewMouseDown += Window_Activity;
            _window.PreviewKeyDown += Window_Activity;
            _window.PreviewMouseWheel += Window_Activity;
        }

        public TimeSpan Timeout { get; private set; }

        public event EventHandler SessionExpired;

        public void Start()
        {
            ResetActivity();
            _timer.Start();
        }

        public void Stop()
        {
            _timer.Stop();
            _window.PreviewMouseMove -= Window_Activity;
            _window.PreviewMouseDown -= Window_Activity;
            _window.PreviewKeyDown -= Window_Activity;
            _window.PreviewMouseWheel -= Window_Activity;
        }

        private void Window_Activity(object sender, InputEventArgs e)
        {
            ResetActivity();
        }

        private void ResetActivity()
        {
            _lastActivityUtc = DateTime.UtcNow;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            if (DateTime.UtcNow - _lastActivityUtc >= Timeout)
            {
                _timer.Stop();
                var handler = SessionExpired;
                if (handler != null)
                {
                    handler(this, EventArgs.Empty);
                }
            }
        }
    }
}
