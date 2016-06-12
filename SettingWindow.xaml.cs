using System.Windows;
using MahApps.Metro.Controls;
using System.Text.RegularExpressions;
using MahApps.Metro.Controls.Dialogs;
using System.Windows.Forms;
using Client.Net;
using System.IO;
using System;
using System.Collections.Generic;

namespace Client
{
    public partial class SettingWindow : MetroWindow
    {
        public SettingWindow()
        {
            List<string> l = new List<string>();
            l.Add(String.Format("5 MB"));
            l.Add(String.Format("10 MB"));
            l.Add(String.Format("20 MB"));
            l.Add(String.Format("50 MB"));

            InitializeComponent();
            saveFolderPath.Text = Directory.GetCurrentDirectory();
            selectFileSize.ItemsSource = l;
        }

        private async void saveSetting(object sender, RoutedEventArgs e)
        {
            Regex rPort = new Regex(@"^[0-9]+$");
            if (!rPort.IsMatch(inputRemotePort.Text))
            {
                MessageDialogResult result = await this.ShowMessageAsync("Error", "Insert a valid port number [0-9]", MessageDialogStyle.Affirmative, WPFProperties.metroDialogSettings);
                return;
            }
            MainWindow.inputRemotePort = short.Parse(inputRemotePort.Text);

            if (!rPort.IsMatch(connectRemotePort.Text))
            {
                MessageDialogResult result = await this.ShowMessageAsync("Error", "Insert a valid port number [0-9]", MessageDialogStyle.Affirmative, WPFProperties.metroDialogSettings);
                return;
            }
            MainWindow.connectRemotePort = short.Parse(connectRemotePort.Text);

            if (!rPort.IsMatch(clipboardLocalPort.Text))
            {
                MessageDialogResult result = await this.ShowMessageAsync("Error", "Insert a valid port number [0-9]", MessageDialogStyle.Affirmative, WPFProperties.metroDialogSettings);
                return;
            }
            MainWindow.clipboardLocalPort = short.Parse(clipboardLocalPort.Text);

            if (!rPort.IsMatch(clipboardRemotePort.Text))
            {
                MessageDialogResult result = await this.ShowMessageAsync("Error", "Insert a valid port number [0-9]", MessageDialogStyle.Affirmative, WPFProperties.metroDialogSettings);
                return;
            }
            MainWindow.clipboardRemotePort = short.Parse(clipboardRemotePort.Text);

            if (!rPort.IsMatch(keepAliveRemotePort.Text))
            {
                MessageDialogResult result = await this.ShowMessageAsync("Error", "Insert a valid port number [0-9]", MessageDialogStyle.Affirmative, WPFProperties.metroDialogSettings);
                return;
            }
            MainWindow.keepAliveRemotePort = short.Parse(keepAliveRemotePort.Text);

            short[] ports = new short[]{
                MainWindow.inputRemotePort,
                MainWindow.connectRemotePort,
                MainWindow.clipboardLocalPort,
                MainWindow.clipboardRemotePort,
                MainWindow.keepAliveRemotePort
            };
            HashSet<short> set = new HashSet<short>();
            foreach (short port in ports)
            {
                if (port < 1024)
                {
                    MessageDialogResult result = await this.ShowMessageAsync("Error", "Some port has no valid value", MessageDialogStyle.Affirmative, WPFProperties.metroDialogSettings);
                    return;
                }
                if (!set.Add(port))
                {
                    MessageDialogResult result = await this.ShowMessageAsync("Error", "Port " + port + " duplicated!Please choose another one", MessageDialogStyle.Affirmative, WPFProperties.metroDialogSettings);
                    return;
                }
            }

            ClipboardNetworkChannel.MAX_FILE_SIZE = long.Parse(((string)selectFileSize.SelectedItem).Split()[0]) * 1024 * 1024;

            this.Close();
        }

        private void chooseSaveFolder(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            folderDialog.SelectedPath = "C:\\";
            DialogResult result = folderDialog.ShowDialog();
            if (result.ToString() == "OK")
            {
                ClipboardNetworkChannel.TEMP_DIR = folderDialog.SelectedPath + '\\';
                saveFolderPath.Text = folderDialog.SelectedPath + '\\';
            }
        }
    }
}
