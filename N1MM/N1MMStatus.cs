using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Timers;

namespace N1MM
{
    public class N1MMStatus
    {
        private N1MMListener N1MMl;

        public class N1MMStat
        {
            public string from { get; set; }
            public string band;
            public string mode;
            public ulong freq; // current radio frequency in Hz
            public ulong passfreq; // current passfrequency in Hz
            public DateTime timestamp;

            public N1MMStat(string from, string band, string mode, ulong freq, ulong passfreq)
            {
                this.from = from; this.band = band; this.mode = mode;
                this.freq = freq; this.passfreq = passfreq;
                this.timestamp = DateTime.Now;
            }
        };

        //private BindingList<N1MMStat> _N1MMStatusList = new BindingList<N1MMStat>();
        //public BindingList<N1MMStat> N1MMStatusList { get { return _N1MMStatusList; } }
        public readonly BindingList<N1MMStat> N1MMStatusList;

        private readonly SynchronizationContext _context = SynchronizationContext.Current;
        private System.Timers.Timer ti_check_timestamps;

        public N1MMStatus()
        {
            N1MMl = new N1MMListener(N1MM.N1MMDefaultPort);
            N1MMl.N1MMMessageReceived += N1MMMessageReceivedHandler;
            N1MMStatusList = new BindingList<N1MMStat>();

            ti_check_timestamps = new System.Timers.Timer();
            ti_check_timestamps.Enabled = true;
            ti_check_timestamps.Interval = 60000; // check every minute
            ti_check_timestamps.Elapsed += new System.Timers.ElapsedEventHandler(this.ti_check_timestamps_Tick);
        }

        private void N1MMMessageReceivedHandler(object sender, N1MMListener.N1MMMessageEventArgs e)
        {
            //Console.WriteLine("N1MM Msg " + e.Msg.Msg + " src " +
            //e.Msg.Src + " dest " + e.Msg.Dst + " data " + e.Msg.Data + " checksum "
            //+ e.Msg.HasChecksum);
            /* STATUS "mm" "" 0 12 0 0 0 "0" 0 "1" 1440400 ""
             * 
             *                  band
             *                    mode
             *                       radio1 is primary
             *                         radio1 freq
             *                           ?
             *                               radio2 freq
             *                                 ?
             *                                         pass_freq
             *                                             ?
             */
            if (e.Msg.Msg == N1MMMESSAGES.STATUS && e.Msg.HasChecksum)
            {
                string[] data = e.Msg.Data.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                if (data.Length == 10 || data.Length == 9)
                {
                    string band;
                    try
                    {
                        band = Enum.Parse(typeof(N1MMBANDS), data[1]).ToString().Replace("Band", "");
                    }
                    catch
                    {
                        band = "";
                    }

                    string mode;
                    try
                    {
                        mode = Enum.Parse(typeof(N1MMMODE), data[2]).ToString().Replace("Mode", "");
                    }
                    catch
                    {
                        mode = "";
                    }

                    //Console.WriteLine("STATUS: from " + e.Msg.Src + " band " + band + " mode " + mode +
                    //    " freq " + data[4] + " pass " + data[8]);

                    ulong freq, passfreq;
                    if (!UInt64.TryParse(data[4], out freq))
                        freq = 0;
                    if (!UInt64.TryParse(data[8], out passfreq))
                        passfreq = 0;
                    N1MMStat w = new N1MMStat(e.Msg.Src, band, mode, freq * 100UL, passfreq * 100UL);
                    // we need to make sure, N1MMStatusList is updated in the UI thread, otherwise the ListChanged event
                    // does not reach the UI elements
                    var th = new Thread(() =>
                    {
                        _context.Send(o => N1MMStatusListAdd_UIthread(e.Msg.Src, w), null);
                    })
                    { IsBackground = true };
                    th.Start();
                }
            }
        }

        private void N1MMStatusListAdd_UIthread(string src, N1MMStat w)
        {
            var el = N1MMStatusList.SingleOrDefault(x => x.from == src);

            if (el != null)
            {
                el = w;
            }
            else
            {
                N1MMStatusList.Add(w);
            }
        }
        private void ti_check_timestamps_Tick(object sender, ElapsedEventArgs e)
        {
            var th = new Thread(() =>
            {
                _context.Send(o => N1MMStatusListRemoveExpired_UIthread(), null);
            })
            { IsBackground = true };
            th.Start();
        }

        private void N1MMStatusListRemoveExpired_UIthread()
        {
            for (int i = 0; i < N1MMStatusList.Count; i++)
            {
                var el = N1MMStatusList[i];
                if (el != null)
                {
                    if (DateTime.Now.Subtract(el.timestamp).TotalMinutes > 3) // 3 min timeout
                    {
                        N1MMStatusList.Remove(el);
                    }
                }
            }
        }
    }
}
