using System;
using System.ComponentModel;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace Client.Net
{
    public class KeepaliveChannel
    {
        public NetworkErrorEventHandler DeadClient;
        private BackgroundWorker worker;
        private Socket socket;

        public int MaxTries
        {
            get;
            set;
        }

        public int TimeoutMs
        {
            set
            {
                socket.SendTimeout = value;
            }
        }

        public KeepaliveChannel(int port)
        {
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            TimeoutMs = 3000;
            socket.Connect(new IPEndPoint(MainWindow.serverIP, MainWindow.keepAliveRemotePort));
            int HeartBeatLength = 32;
            byte[] heartBeatBuffer = new byte[HeartBeatLength];
            while (!worker.CancellationPending)
            {
                try
                {
                    Thread.Sleep(2000);
                    Utility.SendBytes(socket, heartBeatBuffer, HeartBeatLength, SocketFlags.None);
                }
                catch (Exception)
                {
                    Console.WriteLine("Keep alive dead");
                    throw;
                }
            }
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Utility.ShutdownSocket(socket);
            if (e.Error != null && e.Error.GetType() == typeof(SocketException))
            {
                Console.WriteLine("Error keepalive");
                SocketException exception = e.Error as SocketException;
                if (DeadClient != null)
                {
                    DeadClient(exception);
                }
            }
        }

        public void Start()
        {
            if (!worker.IsBusy)
            {
                worker.RunWorkerAsync();
            }
        }

        public void Interrupt()
        {
            if (worker.IsBusy)
            {
                worker.CancelAsync();
            }
        }
    }
}
