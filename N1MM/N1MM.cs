using System;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;

namespace N1MM
{
    public enum N1MMMESSAGES
    {
        NONE = 0,
        STATUS = 1,
        PKTRCVD = 2,

        NEEDQSO,
        IHAVE,
        ADDQSO,
        UPDQSO,
        BANDMAP,
        RCVDPKT,
        TIME,
        HELLO,
        SUMMARY,
        GAB,
        LOCKSKED,
        UNLOCKSKED,
        ADDSKED,
        DELETESKED,
        UPDATESKED,
        READAZIMUTH,
        VIOLATION,
        LOGIN,
        LOGOUT,
        SETAZIMUTH = 100,
        SETELEVATION = 101,
        ASWATCHLIST = 249,
        ASSETPATH = 252,
        ASSHOWPATH = 253,
        ASNEAREST = 254,
        UNKNOWN = 255
    }

    // unfortunately not in http://download.win-test.com/v4/lua/constants.N1MMs - reverse engineered
    public enum N1MMBANDS
    {
        Band50MHz = 10,
        Band70MHz = 11,
        Band144MHz = 12,
        Band432MHz = 14,
        Band1_2GHz = 16,
        Band2_3GHz = 17,
        Band3_4GHz = 18,
        Band5_7GHz = 19,
        Band10GHz = 20,
        Band24GHz = 21,
        Band47GHz = 22,
        Band76GHz = 23
    }

    public enum N1MMMODE
    {
        ModeCW = 0,
        ModeSSB = 1,
    }

    public class N1MMMessage
    {
        public N1MMMESSAGES Msg;
        public string Src;
        public string Dst;
        public string Data;
        public byte Checksum;
        public bool HasChecksum;

        public N1MMMessage(byte[] bytes)
        {
            Msg = N1MMMESSAGES.NONE;
            Src = "";
            Dst = "";
            Data = "";
            Checksum = 0;
            HasChecksum = false;
            this.FromBytes(bytes);
        }

        public N1MMMessage(N1MMMESSAGES MSG, string src, string dst, string data)
        {
            Msg = MSG;
            Src = src;
            Dst = dst;
            Data = data;
            Checksum = 0;
            HasChecksum = false;
        }

        public void FromBytes(byte[] bytes)
        {
            // convert bytes coded in UTF8 to text
            string text = Encoding.ASCII.GetString(bytes);
            string msg = text.Substring(0, text.IndexOf(": "));
            try
            {
                Msg = (N1MMMESSAGES)Enum.Parse(typeof(N1MMMESSAGES), msg);
            }
            catch
            {
                Msg = N1MMMESSAGES.UNKNOWN;
            }
            var Length = bytes.Length;
            // FIXME: maybe do this for anything but N1MMMESSAGES.ASNEAREST
            if (bytes[Length - 1] == 0)
            {
                if (Length > 1)
                    Length--; // skip trailing zero
                if (text.Length > 1)
                    text = text.Substring(0, text.Length - 1); // skip trailing zero
            }
            text = text.Remove(0, text.IndexOf(": ") + 2);
            Src = text.Substring(0, text.IndexOf(" ")).Replace("\"", "");
            text = text.Remove(0, text.IndexOf(" ") + 1);
            if (text.IndexOf(" ") > 0)
            {
                Dst = text.Substring(0, text.IndexOf(" ")).Replace("\"", "");
                text = text.Remove(0, text.IndexOf(" ") + 1);
            }
            else
                Dst = "";
            // Clean up the message --> scrub last byte
            text = text.Substring(0, text.Length - 1).Replace("\"", "");
            // convert bytes coded in UTF8 to text
            Data = text.Substring(0, text.Length);
            // get checksum
            Checksum = bytes[Length - 1];
            byte sum = 0;
            byte sum = 0;
            for (int i = 0; i < Length - 1; i++)
                sum += bytes[i];
            sum = (byte)(sum | 0x80);
            if (Checksum == sum)
                HasChecksum = true;
            else
                HasChecksum = false;
        }

        public byte[] ToBytes()
        {
            byte[] b = null;
            string s;
            try
            {
                switch (Msg)
                {
                    case N1MMMESSAGES.NONE:
                        break;
                    case N1MMMESSAGES.ASNEAREST:
                        // emulate old N1MM DLL for N1MMKST
                        // combine all fields to string incl. placeholder for checksum
                        s = Msg + ": " + "\"" + Src + "\" \"" + Dst + "\" \"" + Data + "\"?";
                        // translate into ASCII bytes
                        b = Encoding.ASCII.GetBytes(s);
                        // calculate checksum
                        byte sum = 0;
                        for (int i = 0; i < b.Length - 1; i++)
                            sum += b[i];
                        sum = (byte)(sum | (byte)0x80);
                        b[b.Length - 1] = sum;
                        break;
                    case N1MMMESSAGES.UNKNOWN:
                        break;
                    default:
                        Data = Data.Replace("°", "\\260");
                        // combine all fields to string incl. placeholder for checksum and a \0 at the end
                        s = Msg + ": " + "\"" + Src + "\" \"" + Dst + "\" " + Data + "?\0";
                        // translate into ASCII bytes
                        b = Encoding.ASCII.GetBytes(s);
                        // calculate checksum
                        sum = 0;
                        for (int i = 0; i < b.Length - 2; i++)
                            sum += b[i];
                        sum = (byte)(sum | (byte)0x80);
                        b[b.Length - 2] = sum;
                        break;
                }
            }
            catch
            {
                throw new ArgumentOutOfRangeException(Msg.ToString(), "Unknown Message.");
            }
            return b;
        }
    }

    public class N1MM
    {
        public const int N1MMDefaultPort = 12060;

        public static IPAddress GetIpIFBroadcastAddress()
        {
            var unicast = NetworkInterface
                .GetAllNetworkInterfaces()
                .Where(n => n.OperationalStatus == OperationalStatus.Up)
                .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
                .Where(n => n.GetIPProperties().GatewayAddresses.Count > 0) // only interfaces with a gateway
                .SelectMany(n => n.GetIPProperties()?.UnicastAddresses)
                .Where(g => g.Address.AddressFamily == AddressFamily.InterNetwork) // filter IPv4
                .FirstOrDefault(g => g != null);

            var address = unicast.Address;
            var mask = unicast.IPv4Mask;
            var addressInt = BitConverter.ToInt32(address.GetAddressBytes(), 0);
            var maskInt = BitConverter.ToInt32(mask.GetAddressBytes(), 0);
            var broadcastInt = addressInt | ~maskInt;
            var broadcastAddress = new IPAddress(BitConverter.GetBytes(broadcastInt));
            return broadcastAddress;
        }

    }
}
