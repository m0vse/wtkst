using System;
using System.Net;
using System.Net.Sockets;

namespace N1MM
{
    public class N1MMSked
    {
        private IPAddress localbroadcastIP;

        public N1MMSked()
        {
            localbroadcastIP = N1MM.GetIpIFBroadcastAddress();
        }

        private const string my_N1MMname = "KST"; // FIXME!!!

        private void send(N1MMMessage Msg)
        {
            try
            {
                UdpClient client = new UdpClient();
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, 1);
                client.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontRoute, 1);
                client.Client.ReceiveTimeout = 10000;
                IPEndPoint groupEp = new IPEndPoint(localbroadcastIP, N1MM.N1MMDefaultPort);
                client.Connect(groupEp);
                Console.WriteLine("send: " + Msg.Data);
                byte[] b = Msg.ToBytes();
                client.Send(b, b.Length);
                client.Close();
            }
            catch
            {
            }
        }
        /*
         * 'N1MM Msg LOCKSKED src N1MMKST dest  data N1MMKST checksum True
N1MM Msg UNLOCKSKED src N1MMKST dest  data N1MMKST checksum True
N1MM Msg STATUS src STN1 dest  data 0 12 0 0 1441124 1 0 1  1440400  checksum True
STATUS: from STN1 band 144MHz mode 0 freq 1441124 pass 1440400
N1MM Msg LOCKSKED src STN1 dest  data STN1 checksum True
N1MM Msg UPDATESKED src STN1 dest  data 1506630300 1440400 G3XDY 1506630600 1440400 12 1 G3XDY [JO02OB - 279\260] checksum True
N1MM Msg UNLOCKSKED src STN1 dest  data STN1 checksum True
N1MM Msg STATUS src STN1 dest  data 0 12 0 0 1441124 1 0 1  1440400  checksum True
STATUS: from STN1 band 144MHz mode 0 freq 1441124 pass 1440400
N1MM Msg LOCKSKED src STN1 dest  data STN1 checksum True
N1MM Msg DELETESKED src STN1 dest  data 1506630600 1440400 G3XDY checksum True
N1MM Msg UNLOCKSKED src STN1 dest  data STN1 checksum True

N1MM Msg LOCKSKED src STN1 dest  data STN1 checksum True
N1MM Msg ADDSKED src STN1 dest  data 1506630660 1441124 12 1 G3XDY [JO02OB - 279\260] AS in 2min checksum True
ich
N1MM Msg ADDSKED src N1MMKST dest  data 1506639652 1441424 12 0 DL8AAI [JO02OB - 279?] AS in 2min checksum True
N1MM Msg UNLOCKSKED src STN1 dest  data STN1 checksum True

*/
        public void send_locksked(string target_N1MM)
        {
            N1MMMessage Msg = new N1MMMessage(N1MMMESSAGES.LOCKSKED, my_N1MMname, "", "\"" + my_N1MMname + "\"");
            send(Msg);
        }

        public void send_unlocksked(string target_N1MM)
        {
            N1MMMessage Msg = new N1MMMessage(N1MMMESSAGES.UNLOCKSKED, my_N1MMname, "", "\"" + my_N1MMname + "\"");
            send(Msg);
        }

        public void send_addsked(string target_N1MM, DateTime t, uint qrg, N1MMBANDS band, N1MMMODE mode, string call, string notes)
        {
            N1MMMessage Msg = new N1MMMessage(N1MMMESSAGES.ADDSKED, my_N1MMname, "", string.Concat(new string[]
            {
                // the time stamp for Win-Test seems to be 1 minute off, so use 1.1.1970 00:01:00 as reference
                ((Int64)((t.ToUniversalTime() - new DateTime (1970, 1, 1, 0, 1, 0, DateTimeKind.Utc)).TotalSeconds)+60).ToString(),
                " ", (10*qrg).ToString(), " ", ((int)band).ToString(),
                " ", ((int)mode).ToString(), " \"", call, "\" \"", notes, "\""
            }));
            send(Msg);
        }

    }
}
