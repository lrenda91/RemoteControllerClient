using System;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Security.Cryptography;
using System.Windows;

namespace Client.Net
{
    public class Utility
    {
        public static void ShutdownSocket(Socket s)
        {
            if (s != null)
            {
                if (s.Connected)
                {
                    try
                    {
                        s.Shutdown(SocketShutdown.Both);
                    }
                    catch (Exception) { }
                }
                s.Close();
            }
        }

        public static bool ReceiveBytes(Socket socket, byte[] buffer, int bytes, SocketFlags flags)
        {
            int offset = 0;
            int toReceive = bytes;
            do
            {
                int read = socket.Receive(buffer, offset, toReceive, flags);
                if (read == 0)
                {
                    return false;
                }
                offset += read;
                toReceive -= read;
            }
            while (offset < bytes);
            return true;
        }

        public static void SendBytes(Socket socket, byte[] buffer, int bytes, SocketFlags flags)
        {
            int offset = 0;
            int toSend = bytes;
            do
            {
                int written = socket.Send(buffer, offset, toSend, flags);
                if (written == 0) {
                    break;
                }
                offset += written;
                toSend -= written;
            }
            while (offset < bytes);
        }
    }

    class Connection
    {
        public delegate void ClientConnectedHandler();

        public delegate void ClientDisconnectedHandler();

        public static ClientConnectedHandler ClientConnected;

        public static ClientDisconnectedHandler ClientDisconnected;


        public static Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private static String passwdDigest;

        public static String password
        {
            set
            {
                StringBuilder sb = new StringBuilder();
                MD5 md5 = MD5CryptoServiceProvider.Create();
                byte[] hash = md5.ComputeHash(Encoding.Unicode.GetBytes(value));
                foreach (byte b in hash)
                    sb.Append(b.ToString("x2"));
                passwdDigest = sb.ToString();
            }
        }

        public static void connectToServer()
        {
            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(new IPEndPoint(MainWindow.serverIP, MainWindow.connectRemotePort));

                byte[] bufferToSend = Encoding.Unicode.GetBytes(passwdDigest);
                Utility.SendBytes(socket, bufferToSend, bufferToSend.Length, SocketFlags.None);

                byte[] response = new byte[4];
                if (!Utility.ReceiveBytes(socket, response, response.Length, SocketFlags.None))
                {
                    throw new Exception();
                }

                if (Encoding.Unicode.GetString(response).Equals("OK"))
                {
                    MessageBox.Show("Connected");
                    InputEventTransfer.Target = new IPEndPoint(MainWindow.serverIP, MainWindow.inputRemotePort);
                    ClipboardNetworkChannel.StartService(MainWindow.clipboardLocalPort);
                    ClipboardTrasfer.Target = new IPEndPoint(MainWindow.serverIP, MainWindow.clipboardRemotePort);

                    OnClientConnected();
                }
                else
                {
                    MessageBox.Show("Wrong password");
                    ConnectWindow.worker.ReportProgress(70);
                    Utility.ShutdownSocket(socket);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(
                    e.Message);
                ConnectWindow.worker.ReportProgress(50);
                Utility.ShutdownSocket(socket);
                ClipboardTrasfer.StopService();
                ClipboardNetworkChannel.StopService();
                InputEventTransfer.StopService();
            }
        }

        private static void OnClientConnected()
        {
            if (ClientConnected != null)
            {
                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    ClientConnected();
                })
              );
            }
        }

        private static void OnClientDisconnected()
        {
            if (ClientDisconnected != null)
            {
                ClientDisconnected();
            }
        }

        public static void disconnectFromServer()
        {
            try
            {
                byte[] bufferBye = Encoding.Unicode.GetBytes("BYE");
                Utility.SendBytes(socket, bufferBye, bufferBye.Length, SocketFlags.None);
            }
            catch (Exception)
            {
                Utility.ShutdownSocket(socket);
            }
            finally
            {
                MainWindow.keep.DeadClient = null;
                OnClientDisconnected();
                InputEventTransfer.StopService();
                ClipboardNetworkChannel.StopService();
                ClipboardTrasfer.StopService();
                Utility.ShutdownSocket(socket);
                MessageBox.Show("Disconnected");
            }
        }
    }
}