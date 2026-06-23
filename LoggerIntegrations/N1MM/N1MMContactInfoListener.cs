using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Xml.Serialization;

namespace wtKST
{
    [XmlRootAttribute("contactinfo", IsNullable = false)]
    public class N1MMContactInfo
    {
        public string app;
        public string contestname;
        public int contestnr;

        [XmlIgnore]
        public DateTime timestamp { get; set; }

        [XmlElement("timestamp")]
        public string timestampString
        {
            get { return timestamp.ToString("yyyy-MM-dd HH:mm:ss"); }
            set
            {
                DateTime parsed;
                timestamp = DateTime.TryParse(value, out parsed) ? parsed : DateTime.Now;
            }
        }

        public string mycall;
        public string band;
        public int rxfreq;
        public int txfreq;
        [XmlElement(ElementName = "operator")]
        public string op;
        public string mode;
        public string call;
        public string countryprefix;
        public string wpxprefix;
        public string stationprefix;
        public string continent;
        public string snt;
        public string sntnr;
        public string rcv;
        public string rcvnr;
        public string gridsquare;
        public string exchange1;
        public string section;
        public string comment;
        public string qth;
        public string zone;
        public string prec;
        public string ck;
        public string ismultiplier1;
        public string ismultiplier2;
        public string ismultiplier3;
        public string dbname;
        public string radionr;
        public string run1run2;
        public string RoverLocation;
        public string RadioInterfaced;
        public string NetworkedCompNr;
        public string IsOriginal;
        public string NetbiosName;
        public string IsRunQSO;
        public string ID;
        public string IsClaimedQso;
        public string oldtimestamp;
        public string oldcall;
        public string SentExchange;
        public string name;
        public string power;
        public string misctext;
        public string StationName;
        public int points;
    }

    public class N1MMContactInfoListener
    {
        private volatile bool listen;
        private UdpClient udpClient;

        public N1MMContactInfoListener(int udpPort)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, udpPort);
            udpClient = new UdpClient();
            udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
            udpClient.Client.Bind(ep);
            listen = true;

            Thread listener = new Thread(() =>
            {
                XmlSerializer serializer = new XmlSerializer(typeof(N1MMContactInfo));
                while (listen)
                {
                    try
                    {
                        byte[] data = udpClient.Receive(ref ep);
                        if (data.Length == 0)
                            continue;

                        using (var stream = new System.IO.MemoryStream(data))
                        {
                            N1MMContactInfo contactInfo = (N1MMContactInfo)serializer.Deserialize(stream);
                            ContactInfoReceived?.Invoke(this, new N1MMContactInfoEventArgs(contactInfo));
                        }
                    }
                    catch (SocketException)
                    {
                        break;
                    }
                    catch (InvalidOperationException ex)
                    {
                        ErrorReceived?.Invoke(this, new N1MMListenerErrorEventArgs(ex.Message));
                    }
                }
            });
            listener.IsBackground = true;
            listener.Start();
        }

        public event EventHandler<N1MMContactInfoEventArgs> ContactInfoReceived;
        public event EventHandler<N1MMListenerErrorEventArgs> ErrorReceived;

        public void Close()
        {
            listen = false;
            if (udpClient != null)
            {
                udpClient.Close();
                udpClient = null;
            }
        }
    }

    public class N1MMContactInfoEventArgs : EventArgs
    {
        public N1MMContactInfoEventArgs(N1MMContactInfo contactInfo)
        {
            ContactInfo = contactInfo;
        }

        public N1MMContactInfo ContactInfo { get; private set; }
    }

    public class N1MMListenerErrorEventArgs : EventArgs
    {
        public N1MMListenerErrorEventArgs(string message)
        {
            Message = message;
        }

        public string Message { get; private set; }
    }
}
