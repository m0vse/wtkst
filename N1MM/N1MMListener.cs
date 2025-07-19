using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Xml;

namespace N1MM
{
    public class N1MMListener
    {
        private volatile bool Listen;
        private UdpClient u;

        public N1MMListener(int UDPPort)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, UDPPort);
            u = new UdpClient();
            u.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            Console.WriteLine("Listening for N1MM data on: " + UDPPort);
            //u.Client.ReceiveTimeout = 1000;
            u.Client.Bind(ep);
            Listen = true;

            Thread listener = new Thread(() =>
            {
                while (Listen == true)
                {
                    try
                    {
                        byte[] data = u.Receive(ref ep);
                        if (data.Length > 0)
                        {
                            XmlDocument doc = new XmlDocument();
                            string xml = Encoding.UTF8.GetString(data);
                            doc.LoadXml(xml);
                            Console.WriteLine("RX: "+Encoding.Default.GetString(data));
                            //N1MMMessage Msg = new N1MMMessage(data);
                            //Console.WriteLine("N1MM Msg " + Msg.Msg + " src " + Msg.Src + " dest " + Msg.Dst + " data " + Msg.Data + " checksum " + Msg.HasChecksum);
                            //if (N1MMMessageReceived != null)
                            //{
                            //    N1MMMessageReceived(this, new N1MMMessageEventArgs(Msg));
                            //}
                        }
                    }
                    catch (SocketException)
                    {
                        break; // exit the while loop
                    }
                }
                //Console.WriteLine("listener done");
            });
            listener.IsBackground = true;
            listener.Start();
        }

        public event EventHandler<N1MMMessageEventArgs> N1MMMessageReceived;
        public class N1MMMessageEventArgs : EventArgs
        {
            /// <summary>
            /// Win-Test message
            /// </summary>
            public N1MMMessageEventArgs(N1MMMessage Msg)
            {
                this.Msg = Msg;
            }
            public N1MMMessage Msg { get; private set; }
        }

        public void close()
        {
            Listen = false;

            u.Client.Shutdown(SocketShutdown.Both);
            u.Close();
        }
    }
}
