//#define DEBUG_PACKET_LOSS

using System;
using System.Data;
using System.Diagnostics.Eventing.Reader;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace WinTest
{
    /*
     * <?xml version="1.0" encoding="utf-8"?>
     *  <contactinfo>
     *  <app>N1MM</app>
     *  <contestname>CWOPS</contestname>
     *  <contestnr>73</contestnr>
     *  <timestamp>2020-01-17 16:43:38</timestamp>
     *  <mycall>W2XYZ</mycall>
     *  <band>3.5</band>
     *  <rxfreq>352519</rxfreq>
     *  <txfreq>352519</txfreq>
     *  <operator></operator>
     *  <mode>CW</mode>
     *  <call>WlAW</call>
     *  <countryprefix>K</countryprefix>
     *  <wpxprefix>Wl</wpxprefix>
     *  <stationprefix>W2XYZ</stationprefix>
     *  <continent>NA</continent>
     *  <snt>599</snt>
     *  <sntnr>5</sntnr>
     *  <rcv>599</rcv>
     *  <rcvnr>0</rcvnr>
     *  <gridsquare></gridsquare>
     *  <exchangel></exchangel>
     *  <section></section>
     *  <comment></comment>
     *  <qth></qth>
     *  <name></name>
     *  <power></power>
     *  <misctext></misctext>
     *  <zone>0</zone>
     *  <prec></prec>
     *  <ck>0</ck>
     *  <ismultiplierl>l</ismultiplierl>
     *  <ismultiplier2>0</ismultiplier2>
     *  <ismultiplier3>0</ismultiplier3>
     *  <points>l</points>
     *  <radionr>l</radionr>
     *  <run1run2>1<run1run2>
     *  <RoverLocation></RoverLocation>
     *  <RadioInterfaced>l</RadioInterfaced>
     *  <NetworkedCompNr>0</NetworkedCompNr>
     *  <IsOriginal>False</IsOriginal>
     *  <NetBiosName></NetBiosName>
     *  <IsRunQSO>0</IsRunQSO>
     *  <StationName>CONTEST-PC</StationName>
     *  <ID>f9ffac4fcd3e479ca86e137df1338531</ID>
     *  <IsClaimedQso>1</IsClaimedQso>
     *  <oldtimestamp>2020-01-17 16:43:38</oldtimestamp>
     *  <oldcall>W1AW</oldcall>
     *  <SentExchange>XYZ NY</SentExchange>
     *</contactinfo>
    */


    [XmlRootAttribute("contactinfo",
    IsNullable = false)]

    public class n1mmcontactinfo
    {
        public string app;
        public string contestname;
        public int contestnr;
        [XmlIgnore]
        public DateTime timestamp { get; set; }

        [XmlElement("timestamp")]
        public string timestampString
        {
            get { return this.timestamp.ToString("yyyy-MM-dd HH:mm:ss"); }
            set { this.timestamp = DateTime.Parse(value); }
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
    
    public class N1MMListener
    {
        private volatile bool Listen;
        private UdpClient u;

        private void serializer_UnknownNode
(object sender, XmlNodeEventArgs e)
        {
            Console.WriteLine("Unknown Node:" + e.Name + "\t" + e.Text);
        }

        private void serializer_UnknownAttribute
        (object sender, XmlAttributeEventArgs e)
        {
            System.Xml.XmlAttribute attr = e.Attr;
            Console.WriteLine("Unknown attribute " +
            attr.Name + "='" + attr.Value + "'");
        }

        public N1MMListener(int UDPPort)
        { 
            Console.WriteLine("Listening on UDP port:"+UDPPort);
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, UDPPort);
            u = new UdpClient();
            u.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
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
                            var ustream = new System.IO.MemoryStream(data);

                            string element = "contactinfo";
                            /*
                            XmlReader reader = XmlReader.Create(ustream);
                            while (reader.Read())
                            {
                                if (reader.NodeType == XmlNodeType.Element)
                                {
                                    element = reader.Name;
                                    break;
                                }
                            }
                            reader.Close();
                            */
                            if (element == "contactinfo")
                            {
                                XmlSerializer serializer = new XmlSerializer(typeof(n1mmcontactinfo));
                                /* If the XML document has been altered with unknown
                                nodes or attributes, handle them with the
                                UnknownNode and UnknownAttribute events.*/

                                serializer.UnknownNode += new XmlNodeEventHandler(serializer_UnknownNode);
                                serializer.UnknownAttribute += new XmlAttributeEventHandler(serializer_UnknownAttribute);
                                /* Use the Deserialize method to restore the object's state with
                                data from the XML document.  */
                                try
                                {
                                    n1mmcontactinfo ci;
                                    ci = (n1mmcontactinfo)serializer.Deserialize(ustream);
                                    Console.WriteLine("call: " + ci.call + " band: " + ci.band);
                                    if (QTMessageReceived != null)
                                    {
                                        QTMessageReceived(this, new QTMessageEventArgs(ci));
                                    }
                                }
                                catch (InvalidOperationException e)
                                {
                                    Console.WriteLine("Not a contactinfo? " + e.Message);
                                }
                            } 
                            else
                            {
                                Console.WriteLine("Got a " + element + " record");
                            }

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

        public event EventHandler<QTMessageEventArgs> QTMessageReceived;
        public class QTMessageEventArgs : EventArgs
        {
            /// <summary>
            /// N1MM message
            /// </summary>
            public QTMessageEventArgs(n1mmcontactinfo ci)
            {
                this.ci = ci;
            }
            public n1mmcontactinfo ci { get; private set; }
        }

        public void close()
        {
            Listen = false;

            u.Client.Shutdown(SocketShutdown.Both);
            u.Close();
        }
    }

    public class N1MMLogSync : WinTestLogBase
    {
        private N1MMListener qtl;

        public N1MMLogSync(LogWriteMessageDelegate mylog) : base(mylog)
        {

            QSO.Columns.Add("TXNR");
            DataColumn[] QSOkeys = new DataColumn[]
            {
                QSO.Columns["TXNR"],
                QSO.Columns["BAND"]
            };
            QSO.PrimaryKey = QSOkeys;
        }

        public override void Dispose()
        {
            if (qtl != null)
                qtl.close();
        }


        public override string getStatus()
        {
            return QSO.Rows.Count.ToString();
        }

        public override void Get_QSOs(string wtname)
        {
            // start listener
            if (qtl == null)
            {
                QSO.Clear();
                Console.WriteLine("Creating listener");
                qtl = new N1MMListener(12060); // N1MM default UDP port
                qtl.QTMessageReceived += QTMessageReceivedHandler;
            }
        }

        private void QTMessageReceivedHandler(object sender, N1MMListener.QTMessageEventArgs e)
        {
            // QSO contactinfo 
            DataRow row = QSO.NewRow();
            row["CALL"] = e.ci.call;

            switch (e.ci.band)
            {
                case "50":
                    row["BAND"] = "50M";
                    break;
                case "70":
                    row["BAND"] = "70M";
                    break;
                case "144":
                    row["BAND"] = "144M";
                    break;
                case "420":
                    row["BAND"] = "432M";
                    break;
                case "1240":
                    row["BAND"] = "1_2G";
                    break;
                case "2300":
                    row["BAND"] = "2_3G";
                    break;
                case "3300":
                    row["BAND"] = "3_4G";
                    break;
                case "5650":
                    row["BAND"] = "5_7G";
                    break;
                case "10000":
                    row["BAND"] = "10G";
                    break;
                case "24000":
                    row["BAND"] = "24G";
                    break;
                case "47000":
                    row["BAND"] = "47G";
                    break;
                case "76000":
                    row["BAND"] = "76G";
                    break;
                default:
                    row["BAND"] = "";
                    break;
            }

            row["TIME"] = e.ci.timestamp.ToString("HH:mm"); // FIXME warum nicht als object? So fehlt das Datum
            row["TXNR"] = e.ci.sntnr;
            row["SENT"] = e.ci.snt;
            row["RCVD"] = e.ci.rcv;
            row["LOC"] = e.ci.gridsquare;
            Console.WriteLine("Added: time=" + row["TIME"] +" call=" + row["CALL"] + " band=" + row["BAND"] + " loc=" + row["LOC"] + " txnr=" + row["TXNR"]);
            try
            {
                var qso = QSO.Rows.Find(new object[]
                {
                                row["TXNR"],
                                row["BAND"]
                });

                if (qso != null)
                {
                    if (!(qso.ItemArray.SequenceEqual(row.ItemArray)))
                    {
                        qso.Delete();
                        QSO.Rows.Add(row);
                    }
                }
                else
                {
                    QSO.Rows.Add(row);
                }

            }
            catch (Exception ex) { Console.WriteLine("Final exception:" + ex.Message); }
        }
    }

}