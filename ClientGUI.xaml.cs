using System;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Input;
using System.Runtime.InteropServices;
using System.Net;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Client.Net;
using Client.Windows;
using System.Collections.Generic;
using System.Net.Sockets;

namespace Client
{
    public partial class MainWindow : MetroWindow
    {
        public static short connectRemotePort;
        public static short clipboardLocalPort;
        public static short clipboardRemotePort;
        public static short keepAliveRemotePort = 5000;
        public static short inputRemotePort;

        public static IPAddress serverIP = new IPAddress(new byte[] { 192, 168, 1, 135 });

        public static List<IpAddress> listIp = new List<IpAddress>();

        private LowLevelKeyboardListener _listener;
        private LowLevelMouseListener _mouseListener;

        public static KeepaliveChannel keep = new KeepaliveChannel(keepAliveRemotePort);

        public MainWindow()
        {
            InitializeComponent();
        }

        private void updateButtons()
        {
            connectButton.Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(delegate ()
            {
                connectButton.IsEnabled = !connectButton.IsEnabled;
                disconnectButton.IsEnabled = !disconnectButton.IsEnabled;
            }));
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            inputRemotePort = 9000;
            connectRemotePort = 7001;
            clipboardLocalPort = 8000;
            clipboardRemotePort = 8001;
            keepAliveRemotePort = 5000;

            Connection.ClientConnected += registerListeners;
            Connection.ClientConnected += updateButtons;
            Connection.ClientConnected += startKeepAlive;

            Connection.ClientConnected += fullScreen;

            Connection.ClientDisconnected += deregisterListeners;
            Connection.ClientDisconnected += updateButtons;
            Connection.ClientDisconnected += stopKeepAlive;

            Connection.ClientDisconnected += resizeScreen;

            keep.DeadClient += onKeepAliveError;

            _listener = new LowLevelKeyboardListener();
            _mouseListener = new LowLevelMouseListener();
        }

        private void resizeScreen()
        {
            barra.Visibility = Visibility.Visible;
            ResizeMode = ResizeMode.CanResize;
            UseNoneWindowStyle = false;
            WindowStyle = WindowStyle.None;
        }

        private void fullScreen()
        {
            barra.Visibility = Visibility.Hidden;
            ResizeMode = ResizeMode.NoResize;
            UseNoneWindowStyle = true;

        }

        private void onKeepAliveError(SocketException exception)
        {
            Connection.disconnectFromServer();
        }

        private void stopKeepAlive()
        {
            keep.Interrupt();
        }

        private void startKeepAlive()
        {
            keep.Start();
        }

        private static void _mouseListener_MouseEvent(object sender, INPUT e)
        {
            ClientManager.NotifyInputAsynch(e);
        }

        private static void _listener_KeyUnicode(object sender, INPUT[] e)
        {
            foreach (INPUT i in e)
            {
                ClientManager.NotifyInputAsynch(i);
            }
        }

        private static void _listener_KeyUp(object sender, INPUT e)
        {
            ClientManager.NotifyInputAsynch(e);
        }

        private async void _listener_OnKeyPressed(object sender, INPUT e)
        {
            bool error = false; string message = "";
            try
            {
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    Key k = KeyInterop.KeyFromVirtualKey(e.ki.wVk);
                    int virtualKey = KeyInterop.VirtualKeyFromKey(k);
                    if (k == Key.N)
                    {
                        // Invio della clipboard del client
                        ClientManager.NotifyClipboardAsynch();
                    }
                    else if (k == Key.M)
                    {
                        // Richiesta della clipboard al server
                        ClipboardTrasfer.SendUpdateRequest();
                    }
                    else if (virtualKey >= (int)VirtualKeyCode.VK_1 && virtualKey <= (int)VirtualKeyCode.VK_9) // Switch
                    {
                        virtualKey -= (int)VirtualKeyCode.VK_0;
                        if (virtualKey <= listIp.Count)
                        {
                            serverIP = IPAddress.Parse(listIp[virtualKey - 1].ipAddress);
                            Connection.disconnectFromServer();
                            ConnectWindow c = new ConnectWindow();
                            c.Show();
                        }
                    }
                    else if (virtualKey == (int)VirtualKeyCode.VK_0) // Disconnect
                    {
                        MessageDialogResult result = await this.ShowMessageAsync("Exit", "Are you sure you want to disconnect?", MessageDialogStyle.AffirmativeAndNegative, WPFProperties.metroDialogSettings);
                        if (result == MessageDialogResult.Affirmative)
                        {
                            if (disconnectButton.IsEnabled)
                            {
                                Connection.disconnectFromServer();
                            }
                        }
                    }
                    else
                    {
                        ClientManager.NotifyInputAsynch(e);
                    }
                }
                else
                {
                    ClientManager.NotifyInputAsynch(e);
                }
            }
            catch (Exception ex)
            {
                error = true;
                message = ex.Message;
            }
            if (error) {
                MessageDialogResult result = await this.ShowMessageAsync("Error: " + message, "You are now disconnected from server", MessageDialogStyle.Affirmative, WPFProperties.metroDialogSettings);
            }
        }

        private void connect(object sender, RoutedEventArgs e)
        {
            ConnectWindow c = new ConnectWindow();
            c.Show();
        }

        private void addServer(object sender, RoutedEventArgs e)
        {
            AddServerWindow asw = new AddServerWindow();
            asw.Show();
        }

        private void showServer(object sender, RoutedEventArgs e)
        {
            ShowServerWindow ssw = new ShowServerWindow();
            ssw.Show();
        }

        private void information(object sender, RoutedEventArgs e)
        {
            InformationWindow iw = new InformationWindow();
            iw.Show();
        }

        private void setSettings(object sender, RoutedEventArgs e)
        {
            SettingWindow sw = new SettingWindow();
            sw.Show();
        }

        private void Disconnect_Click(object sender, RoutedEventArgs e)
        {
            Connection.disconnectFromServer();
        }

        private async void exit(object sender, RoutedEventArgs e)
        {
            MessageDialogResult result = await this.ShowMessageAsync("Exit", "Are you sure you want to quit?", MessageDialogStyle.AffirmativeAndNegative, WPFProperties.metroDialogSettings);
            if (result == MessageDialogResult.Affirmative)
            {
                /* if (disconnectButton.IsEnabled)
                 {
                     Console.WriteLine("Disconnetto ed esco");
                     Disconnect_Click(sender, e);
                 }
                 else
                 {*/
                this.Close();
                Application.Current.Shutdown();
                //}
            }
        }

        public void registerListeners()
        {
            Console.WriteLine("Register callbacks");

            _listener.HookKeyboard();
            _mouseListener.HookMouse();

            _listener.KeyDown += _listener_OnKeyPressed;
            _listener.KeyUp += _listener_KeyUp;
            _listener.KeyUnicode += _listener_KeyUnicode;

            _mouseListener.MouseEvent += _mouseListener_MouseEvent;
        }

        public void deregisterListeners()
        {
            if (_listener != null && _mouseListener != null)
            {
                Console.WriteLine("deregister callbacks");
                _listener.KeyDown -= _listener_OnKeyPressed;
                _listener.KeyUp -= _listener_KeyUp;
                _listener.KeyUnicode -= _listener_KeyUnicode;

                _mouseListener.MouseEvent -= _mouseListener_MouseEvent;

                _listener.UnHookKeyboard();
                _mouseListener.UnHookMouse();
            }
        }

        private void MetroWindow_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void MetroWindow_PreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }
    }
}