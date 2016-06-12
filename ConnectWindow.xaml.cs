using System.Windows;
using System.Text.RegularExpressions;
using MahApps.Metro.Controls.Dialogs;
using MahApps.Metro.Controls;
using System.ComponentModel;
using Client.Net;

namespace Client
{
    public partial class ConnectWindow : MetroWindow
    {
        public static BackgroundWorker worker;

        public ConnectWindow()
        {
            worker = new BackgroundWorker();
            worker.WorkerSupportsCancellation = true;
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
            ipAddress.Text = MainWindow.serverIP.ToString();
        }

        private async void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e.ProgressPercentage == 50) // Connection failed
            {
                MessageDialogResult result = await this.ShowMessageAsync("Error", "Cannot connect to server", MessageDialogStyle.Affirmative, WPFProperties.metroDialogSettings);
                //System.Windows.MessageBox.Show("Cannot connect to server");
            }
            if (e.ProgressPercentage == 70) // Incorrect Password
            {
                MessageDialogResult result = await this.ShowMessageAsync("Error", "Wrong password", MessageDialogStyle.Affirmative, WPFProperties.metroDialogSettings);
                //System.Windows.MessageBox.Show("Wrong Password");
            }
            TestoProgresso.Visibility = System.Windows.Visibility.Hidden;
            loading.Visibility = System.Windows.Visibility.Hidden;
            this.Opacity = 1;
            this.Close();
        }

        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Connection.connectToServer();
            worker.ReportProgress(100);
        }

        private async void connect(object sender, RoutedEventArgs e)
        {
            if (ipAddress.Text != "")
            {
                Regex rIp = new Regex(@"^[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}$");
                if (!rIp.IsMatch(ipAddress.Text))
                {
                    MessageDialogResult result = await this.ShowMessageAsync("Error", "Insert a valid ip", MessageDialogStyle.Affirmative, WPFProperties.metroDialogSettings);
                    return;
                }
                if (password.Password.Length <= 0)
                {
                    MessageDialogResult result = await this.ShowMessageAsync("Error", "Password required", MessageDialogStyle.Affirmative, WPFProperties.metroDialogSettings);
                    return;
                }
                this.Opacity = 0.8;

                TestoProgresso.Text = "Connecting...";
                TestoProgresso.Visibility = Visibility.Visible;
                loading.Visibility = Visibility.Visible;

                MainWindow.serverIP = System.Net.IPAddress.Parse(ipAddress.Text);
                Connection.password = password.Password;

                worker.RunWorkerAsync(); // Runs the connecting thread in an asynchronous way 
            }
            else
            {
                MessageDialogResult result = await this.ShowMessageAsync("Error", "Insert an ip", MessageDialogStyle.Affirmative, WPFProperties.metroDialogSettings);
                return;
            }
        }
    }
}