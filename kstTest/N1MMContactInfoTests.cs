using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Xml.Serialization;
using wtKST;

namespace TestClass.kstTest
{
    [TestClass]
    public sealed class N1MMContactInfoTests
    {
        [TestMethod]
        public void DeserializeContactInfoMapsN1MMXmlFields()
        {
            N1MMContactInfo contact = DeserializeContactInfo(SampleContactInfoXml());

            Assert.AreEqual("N1MM Logger+", contact.app);
            Assert.AreEqual("M0VSE", contact.mycall);
            Assert.AreEqual("G4CLA", contact.call);
            Assert.AreEqual("144", contact.band);
            Assert.AreEqual("JO02OB", contact.gridsquare);
            Assert.AreEqual("OP1", contact.op);
            Assert.AreEqual(new DateTime(2026, 6, 23, 19, 42, 15), contact.timestamp);
        }

        [TestMethod]
        public void InvalidTimestampFallsBackToCurrentTime()
        {
            DateTime before = DateTime.Now.AddSeconds(-1);

            N1MMContactInfo contact = DeserializeContactInfo(SampleContactInfoXml("not-a-date"));

            DateTime after = DateTime.Now.AddSeconds(1);
            Assert.IsTrue(contact.timestamp >= before && contact.timestamp <= after,
                "Invalid timestamps should not throw during XML deserialization.");
        }

        [TestMethod]
        public void ListenerRaisesContactInfoReceivedForValidUdpPacket()
        {
            int port = GetFreeUdpPort();
            using ManualResetEventSlim received = new ManualResetEventSlim(false);
            N1MMContactInfo? contact = null;
            N1MMContactInfoListener listener = new N1MMContactInfoListener(port);

            try
            {
                listener.ContactInfoReceived += (sender, args) =>
                {
                    contact = args.ContactInfo;
                    received.Set();
                };

                SendUdp(port, SampleContactInfoXml());

                Assert.IsTrue(received.Wait(TimeSpan.FromSeconds(2)), "Listener did not receive the UDP packet.");
                Assert.IsNotNull(contact);
                Assert.AreEqual("G4CLA", contact!.call);
                Assert.AreEqual("144", contact.band);
            }
            finally
            {
                listener.Close();
            }
        }

        [TestMethod]
        public void ListenerRaisesErrorForInvalidUdpPacket()
        {
            int port = GetFreeUdpPort();
            using ManualResetEventSlim received = new ManualResetEventSlim(false);
            string? error = null;
            N1MMContactInfoListener listener = new N1MMContactInfoListener(port);

            try
            {
                listener.ErrorReceived += (sender, args) =>
                {
                    error = args.Message;
                    received.Set();
                };

                SendUdp(port, "<not-contactinfo>");

                Assert.IsTrue(received.Wait(TimeSpan.FromSeconds(2)), "Listener did not report the invalid UDP packet.");
                Assert.IsFalse(string.IsNullOrWhiteSpace(error));
            }
            finally
            {
                listener.Close();
            }
        }

        private static N1MMContactInfo DeserializeContactInfo(string xml)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(N1MMContactInfo));
            using MemoryStream stream = new MemoryStream(Encoding.UTF8.GetBytes(xml));
            return (N1MMContactInfo)serializer.Deserialize(stream)!;
        }

        private static string SampleContactInfoXml(string timestamp = "2026-06-23 19:42:15")
        {
            return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<contactinfo>
  <app>N1MM Logger+</app>
  <contestname>RSGB UKAC</contestname>
  <contestnr>12</contestnr>
  <timestamp>{timestamp}</timestamp>
  <mycall>M0VSE</mycall>
  <band>144</band>
  <rxfreq>144300</rxfreq>
  <txfreq>144300</txfreq>
  <operator>OP1</operator>
  <mode>SSB</mode>
  <call>G4CLA</call>
  <snt>59</snt>
  <rcv>59</rcv>
  <gridsquare>JO02OB</gridsquare>
</contactinfo>";
        }

        private static int GetFreeUdpPort()
        {
            using UdpClient udpClient = new UdpClient(new IPEndPoint(IPAddress.Loopback, 0));
            return ((IPEndPoint)udpClient.Client.LocalEndPoint).Port;
        }

        private static void SendUdp(int port, string payload)
        {
            byte[] data = Encoding.UTF8.GetBytes(payload);
            using UdpClient udpClient = new UdpClient();
            udpClient.Send(data, data.Length, new IPEndPoint(IPAddress.Loopback, port));
        }
    }
}
