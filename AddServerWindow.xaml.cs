using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using MahApps.Metro.Controls;
using System.Text.RegularExpressions;
using MahApps.Metro.Controls.Dialogs;

namespace Client
{
    public partial class AddServerWindow : MetroWindow
    {
        public AddServerWindow()
        {
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterScreen;
            InitializeComponent();
        }

        private async void addServer(object sender, RoutedEventArgs e)
        {
            String ip = ipAddress.Text;
            if (ip != "")
            {
                Regex r = new Regex(@"^[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}$");
                if (!r.IsMatch(ip))
                {
                    MessageDialogResult result = await this.ShowMessageAsync("Error", "Invalid ip address", MessageDialogStyle.Affirmative, WPFProperties.metroDialogSettings);
                }
                else
                {
                    ShowServerWindow.AddServerToList(ip);
                    MessageDialogResult result = await this.ShowMessageAsync("Success", "Ip address successfully added", MessageDialogStyle.Affirmative, WPFProperties.metroDialogSettings);
                    if (result == MessageDialogResult.Affirmative)
                    {
                        this.Close();
                    }
                }
            }
            else
            {
                MessageDialogResult result = await this.ShowMessageAsync("Error", "Insert an ip address", MessageDialogStyle.Affirmative, WPFProperties.metroDialogSettings);
                return;
            }
        }        
    }
}