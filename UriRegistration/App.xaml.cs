using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.Windows;
using System.Threading;

namespace UriRegistration
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private Mutex? _mtx;
        public const string AppMtxName = "com.awesome.myapp";

        private NamedPipeServer? _server;

        protected override void OnStartup(StartupEventArgs e)
        {
            //Debugger.Launch();
            // Try to open an existing mutex to detect an already running instance
            var alreadyOpened = Mutex.TryOpenExisting(AppMtxName, out _);

            if (alreadyOpened)
            {
                // We are the client instance -> forward args to the main instance via pipe and exit
                try
                {
                    using var client = new NamedPipeClient(AppMtxName);
                    var commandLine = string.Join(';',e.Args);

                    client.SendMessage(commandLine);
                }
                finally
                {
                    Shutdown();
                }
            }
            else
            {
                // We are the first instance -> create mutex and start server listener
                _mtx = new Mutex(true, AppMtxName);
                StartPipeServer();

                // Ensure MainWindow exists before processing the initial message
                var window = new MainWindow();
                MainWindow = window;
                window.Show();

                ProcessMessage(string.Join(';', e.Args));
            }

            base.OnStartup(e);
        }

        private void StartPipeServer()
        {
            _server = new NamedPipeServer(AppMtxName);
            _server.ReceivedMessage += HandleMessageReceived;
        }

        private void HandleMessageReceived(object? sender, string e) => ProcessMessage(e);

        private void ProcessMessage(string args)
        {
            // Ensure UI-thread handling
            Dispatcher.Invoke(() =>
            {
                // TODO: handle incoming args in the main instance (e.g., bring window to front, process URI)
                // For now, just ensure the main window exists
                if (Current.MainWindow != null)
                {
                    ((MainWindow)Current.MainWindow).Text.Text += Environment.NewLine + args;

                    if (Current.MainWindow.WindowState == WindowState.Minimized)
                        Current.MainWindow.WindowState = WindowState.Normal;
                    Current.MainWindow.Activate();
                    Current.MainWindow.Topmost = true;  // temporarily force topmost
                    Current.MainWindow.Topmost = false;
                    Current.MainWindow.Focus();
                }
            });
        }
    }
}
