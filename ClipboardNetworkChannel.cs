using System;
using System.Windows;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Collections.Specialized;
using System.Windows.Media.Imaging;
using Client.Windows;

namespace Client.Net
{

    public enum ClipboardPacketType : byte
    {
        TEXT = 0,
        FILE = 1,
        DIRECTORY = 2,
        BITMAP = 3,
        UPDATE = 4,
        CANCEL = 5,
        SET_DROPLIST = 6
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct ClipboardPacket
    {
        public byte type;   //establish whether clipboard update notification refers to plain text, file or directory
        public long totalLength; //a platform-specific long specifying total length of the fragmented file
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string name; //file name (only if clipboard notification refers to a file or directory)
        public int length; //a platform-specific integer specifying 'data' array's length
        public byte[] data; //real data to transfer
    }

    class ClipboardNetworkChannel
    {
        public static long MAX_FILE_SIZE = 5 * 1024 * 1024; //50MB max

        public static int BYTES_PER_TRANSFER = 8 * 1024;    //8KB per file packet transfer
        public static string TEMP_DIR = Directory.GetCurrentDirectory() + "\\";

        private static string BITMAP_FILE;

        private static Thread thread;

        private static MemoryStream bitmapStream;
        private static AsynchFileReceiver bitmapDownload = null;
        private static AsynchFileReceiver currentDownload = null;
        private static string currentFileName;
        private static object objLock = new object();

        private static Socket listener;
        private static Socket client;


        public static void StartService(short port)
        {
            BITMAP_FILE = TEMP_DIR + "\\screen2.bmp";

            if (listener != null || thread != null)
            {
                throw new InvalidOperationException("Clipboard Receiving service already running");
            }
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(new IPEndPoint(IPAddress.Any, port));

            thread = new Thread(new ThreadStart(listenClipboard));
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
        }

        public static void StopService()
        {
            if (listener == null || thread == null)
            {
                //service is already stopped. No problems in this case
                return;
            }
            Utility.ShutdownSocket(client);
            listener.Close();
            try { thread.Join(); }
            catch (ThreadInterruptedException) { }
            thread = null;
            listener = null;
            Console.WriteLine("ClipboardReceiver service CLOSED!!");
        }


        static internal void downloadCompleted(AsynchFileReceiver download)
        {
            lock (objLock)
            {
                currentFileName = null;
                if (download == currentDownload) currentDownload = null;
                if (download == bitmapDownload) bitmapDownload = null;
                Monitor.PulseAll(objLock);
            }
        }


        private static void ReceivePacket(ref Socket client, ref byte[] buffer)
        {
            int offset = 0;
            int toReceive = BYTES_PER_TRANSFER;
            do
            {
                int read = client.Receive(buffer, offset, toReceive, SocketFlags.None);
                if (read == 0)  //socket closed by StopService()
                {
                    throw new SocketException();
                }
                offset += read;
                toReceive -= read;
            }
            while (offset < BYTES_PER_TRANSFER);
        }

        private static void listenClipboard()
        {
            try
            {
                listener.Listen(0);
                client = listener.Accept();
            }
            catch (Exception e)
            {
                listener.Close();
                return;
            }

            byte[] buffer = new byte[BYTES_PER_TRANSFER];

            while (true)
            {
                try
                {
                    ReceivePacket(ref client, ref buffer);

                    ClipboardPacket packet = Serialization.FromClipboardBytes(buffer);

                    switch ((ClipboardPacketType)packet.type)
                    {
                        case ClipboardPacketType.TEXT:
                            Clipboard.SetText(Encoding.Unicode.GetString(packet.data));
                            break;

                        case ClipboardPacketType.DIRECTORY:
                            Clipboard.Clear();
                            string dir = TEMP_DIR + packet.name;
                            Directory.CreateDirectory(dir);
                            break;

                        case ClipboardPacketType.FILE:
                            string file = TEMP_DIR + packet.name;

                            lock (objLock)
                            {
                                while (currentDownload != null && !packet.name.Equals(currentFileName))
                                {
                                    Monitor.Wait(objLock);
                                }
                                if (currentDownload == null)
                                {
                                    currentFileName = packet.name;
                                    currentDownload = new AsynchFileReceiver(file, packet.totalLength);
                                    Console.WriteLine("START " + file);
                                    currentDownload.Start();
                                }
                                currentDownload.newFragmentAvailable(ref packet.data);
                            }
                            break;

                        case ClipboardPacketType.BITMAP:
                            lock (objLock)
                            {
                                if (bitmapStream == null)
                                {
                                    bitmapStream = new MemoryStream((int)packet.totalLength);
                                }
                                bitmapStream.Write(packet.data, 0, packet.data.Length);
                                if (bitmapStream.Position == packet.totalLength)
                                {
                                    var bitmap = new BitmapImage();
                                    bitmap.BeginInit();
                                    bitmap.StreamSource = bitmapStream;
                                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                                    bitmap.EndInit();
                                    bitmap.Freeze();
                                    Clipboard.SetImage(bitmap);
                                    bitmapStream.Dispose();
                                    bitmapStream = null;
                                }
                            }
                            break;

                        case ClipboardPacketType.UPDATE:
                            SendClipboardNotice(Clipboard.GetDataObject());
                            break;

                        case ClipboardPacketType.SET_DROPLIST:
                            string fileNames = Encoding.Unicode.GetString(packet.data);
                            string[] filesArray = fileNames.Split('|');
                            StringCollection sc = new StringCollection();
                            foreach (string s in filesArray)
                            {
                                sc.Add(TEMP_DIR + s);
                            }
                            try
                            {
                                Thread t = new Thread(new ThreadStart(() =>
                                {
                                    Clipboard.SetFileDropList(sc);
                                }
                                ));
                                t.SetApartmentState(ApartmentState.STA);
                                t.Start();
                            }
                            catch (Exception e)
                            {

                            }
                            break;
                    }
                }
                catch (SocketException)
                {
                    break;
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    break;
                }
            }
            client.Close();
            listener.Close();
        }

        public static void SendClipboardNotice(IDataObject obj)
        {
            if (obj.GetDataPresent(DataFormats.Text))
            {
                string text = (string)obj.GetData(DataFormats.Text);
                ClipboardTrasfer.SendText(text);
            }
            else if (obj.GetDataPresent(DataFormats.FileDrop))
            {
                /* get the list of absolute file paths actually inside Windows Clipboard */
                var dropList = (string[])obj.GetData(DataFormats.FileDrop, false);

                if (!ConfirmSend(dropList))
                {
                    return;
                }

                /* get parent folder, i.e. the folder in which Windows Clipboard was changed */
                int lastDirSeparatorIndex = (dropList[0].LastIndexOf('\\') + 1);
                string parentDir = dropList[0].Remove(lastDirSeparatorIndex);

                string path = "";
                foreach (string s in dropList)
                {
                    path += s.Substring(parentDir.Length);
                    path += "|";
                }
                path = path.Remove(path.Length - 1);

                foreach (string absoluteFilePath in dropList)
                {
                    /* 
                     * Check if current absolute file path inside the Clipboard represents 
                     * a Directory and (if greater than MAX_SIZE) user confirmed its transfer
                     */
                    if (Directory.Exists(absoluteFilePath))
                    {
                        /* First, send to client the current folder... */
                        ClipboardTrasfer.SendNewFolder(absoluteFilePath, ref parentDir);

                        /* ...and all its subfolders */
                        string[] subDirs = Directory.GetDirectories(absoluteFilePath, "*.*", SearchOption.AllDirectories);
                        foreach (string dir in subDirs)
                        {
                            ClipboardTrasfer.SendNewFolder(dir, ref parentDir);
                        }
                        /* finally, send to client all subfiles in order to 'fill' all previously sent folders */
                        string[] subFiles = Directory.GetFiles(absoluteFilePath, "*.*", System.IO.SearchOption.AllDirectories);
                        foreach (string file in subFiles)
                        {
                            ClipboardTrasfer.SendFile(file, ref parentDir);
                        }
                    }

                    /* 
                     * Check if current absolute file path inside the Clipboard represents 
                     * a File and (if greater than MAX_SIZE) user confirmed its transfer
                     */
                    else if (File.Exists(absoluteFilePath))
                    {
                        ClipboardTrasfer.SendFile(absoluteFilePath, ref parentDir);
                    }
                }
                /*
                 * Finally, send the path drop list, so that Clipboard could change for the counterpart
                 */
                ClipboardTrasfer.SendPathDropList(path);

            }
            else if (obj.GetDataPresent(DataFormats.Bitmap))
            {
                BitmapSource bitmap = (BitmapSource)obj.GetData(DataFormats.Bitmap);
                ClipboardTrasfer.SendBitmap(bitmap);
            }
        }

        public static void SendInputNotice(INPUT input)
        {
            try
            {
                InputEventTransfer.SendInputEvent(input);
            }
            catch (Exception e)
            {
                ShowErrorMessage(ref e);
            }
        }

        public static void ShowErrorMessage(ref Exception e)
        {
            MessageBoxResult result = MessageBox.Show(
                    e.Message,
                    e.TargetSite.Name + " exception!!",
                    MessageBoxButton.OK
                    );
        }

        private static bool ConfirmSend(string[] path)
        {
            long total = 0;
            foreach (string absoluteFilePath in path)
            {
                if (Directory.Exists(absoluteFilePath))
                {
                    total += GetFolderSize(absoluteFilePath);
                }
                else if (File.Exists(absoluteFilePath))
                {
                    total += new FileInfo(absoluteFilePath).Length;
                }
            }
            if (total > MAX_FILE_SIZE)
            {
                MessageBoxResult result = MessageBox.Show(
                    "Selected files size is " + (total / 1024 / 1024) +
                    " MB.\nDo you want to continue?",
                    "Updating Remote Clipboard",
                    MessageBoxButton.YesNo);
                return result == MessageBoxResult.Yes;
            }
            return true;
        }

        private static long GetFolderSize(string folder)
        {
            string[] files = Directory.GetFiles(folder, "*.*");
            long filesSize = 0;
            foreach (string f in files)
            {
                filesSize += new FileInfo(f).Length;
            }
            return filesSize;
        }
    }
}
