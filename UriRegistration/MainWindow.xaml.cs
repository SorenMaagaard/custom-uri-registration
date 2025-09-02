using Microsoft.Win32;
using System.ComponentModel;
using System.Windows;

namespace UriRegistration
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const string UriScheme = "com.awesome.myapp";
        const string FriendlyName = "My awesome App";


        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
           
        }

      
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            RegisterUriScheme();
        }

        public void RegisterUriScheme()
        {
            using var key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Classes\\" + UriScheme);
            // Replace typeof(App) by the class that contains the Main method or any class located in the project that produces the exe.
            // or replace typeof(App).Assembly.Location by anything that gives the full path to the exe
            string applicationLocation = typeof(App).Assembly.Location;

            if (applicationLocation.EndsWith("dll"))
            {
                applicationLocation = applicationLocation.Replace("dll", "exe");
            }

            key.SetValue("", "URL:" + FriendlyName);
            key.SetValue("URL Protocol", "");

            using (var defaultIcon = key.CreateSubKey("DefaultIcon"))
            {
                defaultIcon.SetValue("", applicationLocation + ",1");
            }

            using (var commandKey = key.CreateSubKey(@"shell\open\command"))
            {
                commandKey.SetValue("", "\"" + applicationLocation + "\" \"%1\"");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}