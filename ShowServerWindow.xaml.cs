using System;
using System.Windows;
using System.Windows.Input;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;


namespace Client
{
    public partial class ShowServerWindow : MetroWindow
    {
        //public static List<IpAddress> listIp = new List<IpAddress>();

        public ShowServerWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
            DataGridServer.ItemsSource = MainWindow.listIp;
            this.DataGridServer.GotMouseCapture += DataGridServer_GotMouseCapture;
        }

        /** Questo metodo intercetta l'input del mouse sulla cella contenente l'indirizzo ip del server **/
        void DataGridServer_GotMouseCapture(object sender, MouseEventArgs e)
        {
            int item = DataGridServer.SelectedIndex;
            if (item < MainWindow.listIp.Count) // Ho cliccato su una riga valorizzata
            {
                IpAddress i = (IpAddress)DataGridServer.SelectedItem;
                if (i != null)
                {
                    ConnectWindow c = new ConnectWindow();
                    c.ipAddress.Text = i.ipAddress;
                    MainWindow.serverIP = System.Net.IPAddress.Parse(i.ipAddress);
                    c.Show();
                    this.Close();
                }
            }
        }

        /** QUESTO METODO PUO' ESSERE UTILE PER IL LOGIN **/
        private async void ShowLoginDialog(object sender, RoutedEventArgs e)
        {
            LoginDialogData result = await this.ShowLoginAsync("Authentication", "Enter your credentials", new LoginDialogSettings { ColorScheme = this.MetroDialogOptions.ColorScheme, InitialUsername = "MahApps" });
            if (result == null)
            {
                MessageDialogResult messageResult = await this.ShowMessageAsync("Authentication Information", "ciao", MessageDialogStyle.Affirmative, null);
            }
            else
            {
                MessageDialogResult messageResult = await this.ShowMessageAsync("Authentication Information", String.Format("Username: {0}\nPassword: {1}", result.Username, result.Password));
            }
        }

        public static void AddServerToList(String s)
        {
            IpAddress i = new IpAddress();
            i.idx = MainWindow.listIp.Count + 1;
            i.ipAddress = s;
            MainWindow.listIp.Add(i);

        }
    }
}