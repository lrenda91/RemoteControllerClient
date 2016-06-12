using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Windows;

namespace Client.Net
{
    class ClipboardTrasfer
    {
        internal const int BUFFER_SIZE = 8 * 1024 - (1 + 8 + 256 + 4);    // 8KB per transfer

        private const byte TEXT_TYPE = 0;
        private const byte FILE_TYPE = 1;
        private const byte DIRECTORY_TYPE = 2;
        private const byte BITMAP_TYPE = 3;
        private const byte UPDATE_TYPE = 4;
        private const byte SETDROPLIST_TYPE = 6;

        private static Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        public static IPEndPoint Target
        {
            set
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(value);
            }
        }

        public static void StopService()
        {
            if (socket != null && socket.Connected)
            {
                Utility.ShutdownSocket(socket);
            }
        }

        public static void SendFile(string fileToSend, ref string rootDir)
        {
            int readBytes = 0;
            long bytesToSend = new FileInfo(fileToSend).Length;

            ClipboardPacket packet = new ClipboardPacket();
            packet.type = FILE_TYPE;
            String filename = fileToSend.Substring(rootDir.Length);
            packet.name = filename;
            packet.totalLength = bytesToSend;

            FileStream fileStream = new FileStream(fileToSend, FileMode.Open);
            byte[] buffer = new byte[BUFFER_SIZE];
            try
            {
                do {
                    readBytes = fileStream.Read(buffer, 0, BUFFER_SIZE);
                    bytesToSend -= readBytes;

                    packet.length = readBytes;
                    packet.data = new byte[BUFFER_SIZE];
                    Array.Copy(buffer, packet.data, BUFFER_SIZE);

                    byte[] toSend = Serialization.GetClipboardBytes(packet);
                    Utility.SendBytes(socket, toSend, toSend.Length, SocketFlags.None);
                }
                while (bytesToSend > 0);
                Console.WriteLine("trasferimento terminato");
            }
            catch (Exception)
            {
                Console.WriteLine("Errore nel trasferimento");
            }
            finally
            {
                fileStream.Close();
            }
        }

        public static void SendBitmap(System.Windows.Media.Imaging.BitmapSource b)
        {
            ClipboardPacket packet = new ClipboardPacket();
            packet.type = BITMAP_TYPE;
            MemoryStream ms = new MemoryStream();
            System.Windows.Media.Imaging.BmpBitmapEncoder encoder = new System.Windows.Media.Imaging.BmpBitmapEncoder();

            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(b));
            encoder.Save(ms);

            byte[] getBitmapData = ms.ToArray();
            packet.length = BUFFER_SIZE;
            packet.totalLength = ms.Length;
            packet.name = String.Empty;
            long bytesToSend = ms.Length;
            while (bytesToSend > 0)
            {
                if (bytesToSend < BUFFER_SIZE)
                {
                    packet.length = (int)bytesToSend;
                }
                packet.data = new byte[BUFFER_SIZE];
                Array.Copy(getBitmapData, ms.Length - bytesToSend, packet.data, 0, packet.length);
                bytesToSend -= packet.length;
                byte[] toSend = Serialization.GetClipboardBytes(packet);

                Utility.SendBytes(socket, toSend, toSend.Length, SocketFlags.None);
            }
            ms.Close();
        }

        public static void SendNewFolder(string folder, ref string rootDir)
        {
            ClipboardPacket packet = new ClipboardPacket();
            packet.type = DIRECTORY_TYPE;
            packet.name = folder.Substring(rootDir.Length);
            packet.length = 0;
            packet.totalLength = 0;

            byte[] toSend = Serialization.GetClipboardBytes(packet);
            Console.WriteLine("Folder: sending {0} bytes to server...", toSend.Length);

            Utility.SendBytes(socket, toSend, toSend.Length, SocketFlags.None);
        }

        public static void SendText(string text)
        {
            ClipboardPacket packet = new ClipboardPacket();
            packet.type = TEXT_TYPE;
            packet.data = System.Text.Encoding.Unicode.GetBytes(text);
            packet.length = packet.data.Length;
            packet.totalLength = 0;
            packet.name = String.Empty;

            byte[] toSend = Serialization.GetClipboardBytes(packet);
            Console.WriteLine("Text: sending {0} bytes to server...", toSend.Length);

            Utility.SendBytes(socket, toSend, toSend.Length, SocketFlags.None);
        }

        public static void SendUpdateRequest()
        {
            /** Richiedo un aggiornamento della mia clipboard. Il server mi invierà il contenuto della sua clipboard **/
            ClipboardPacket packet = new ClipboardPacket();
            packet.data = new byte[0];
            packet.length = packet.data.Length;
            packet.totalLength = 0;
            packet.name = String.Empty;
            packet.type = UPDATE_TYPE;

            byte[] toSend = Serialization.GetClipboardBytes(packet);
            Console.WriteLine("Sending update packet");
            Utility.SendBytes(socket, toSend, toSend.Length, SocketFlags.None);
        }

        public static void SendPathDropList(string path)
        {
            ClipboardPacket packet = new ClipboardPacket();
            packet.type = SETDROPLIST_TYPE;
            packet.data = System.Text.Encoding.Unicode.GetBytes(path);
            packet.length = packet.data.Length;
            packet.totalLength = 0;
            packet.name = String.Empty;

            byte[] toSend = Serialization.GetClipboardBytes(packet);
            Console.WriteLine("Text: sending path" + path);
            Utility.SendBytes(socket, toSend, toSend.Length, SocketFlags.None);
        }

        public static void ShowErrorMessage(Exception e)
        {
            MessageBoxResult result = MessageBox.Show(
                    e.Message,
                    e.TargetSite.Name + " exception!!",
                    MessageBoxButton.OK
                    );
        }
    }
}