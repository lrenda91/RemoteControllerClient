using System;
using System.Net;
using System.Net.Sockets;
using System.Windows;
using Client.Windows;

namespace Client.Net
{
    class InputEventTransfer
    {
        private static Socket socket;

        public static IPEndPoint Target
        {
            set
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                //socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
               
                //socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 3000);
                socket.SendTimeout = 3000;
                socket.Connect(value);
            }
        }

        public static void SendInputEvent(INPUT input)
        {
            try
            {
                byte[] b = Serialization.getBytes(input);
                Utility.SendBytes(socket, b, b.Length, SocketFlags.None);
            }
            catch (Exception e)
            {
                MessageBox.Show("Error during send input");
            }
        }

        public static void StopService()
        {
            if (socket != null && socket.Connected)
            {
                Utility.ShutdownSocket(socket);
            }
        }
    }
}
