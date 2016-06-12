using System;
using System.Collections.Generic;
using System.Net;
using System.Windows;
using Client.Windows;
using System.Net.Sockets;

namespace Client.Net
{
    public delegate void ClipboardNoticeCaller(IDataObject obj);

    public delegate void InputNoticeCaller(INPUT input);

    public delegate void NetworkErrorEventHandler(SocketException exception);

    class ClientManager
    {
        static IEnumerable<IPEndPoint> targets = new LinkedList<IPEndPoint>(); //list of server

        private static ClipboardNoticeCaller ClipboardCaller = new ClipboardNoticeCaller(ClipboardNetworkChannel.SendClipboardNotice);

        private static InputNoticeCaller InputCaller = new InputNoticeCaller(ClipboardNetworkChannel.SendInputNotice);

        public static void NotifyClipboardAsynch()
        {
            IAsyncResult result = ClipboardCaller.BeginInvoke(Clipboard.GetDataObject(), new AsyncCallback(clipboardCallback), null);
        }

        public static void clipboardCallback(IAsyncResult res)
        {
        }

        public static void NotifyInputAsynch(INPUT input)
        {
            IAsyncResult result = InputCaller.BeginInvoke(input, new AsyncCallback(inputCallback), null);
        }

        public static void inputCallback(IAsyncResult res)
        {
        }       
    }
}
