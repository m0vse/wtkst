using System;
using System.Collections.Generic;
using System.Data;
using WinTest;

namespace wtKST
{
    public class N1MMLiveSQLiteLog : N1MMSQLiteLog
    {
        private N1MMContactInfoListener listener;
        private readonly HashSet<string> liveKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public N1MMLiveSQLiteLog(WinTestLogBase.LogWriteMessageDelegate mylog) : base(mylog)
        {
            StartListener();
        }

        public override void Dispose()
        {
            if (listener != null)
            {
                listener.ContactInfoReceived -= ContactInfoReceivedHandler;
                listener.ErrorReceived -= ListenerErrorReceivedHandler;
                listener.Close();
                listener = null;
            }
        }

        public override string getStatus()
        {
            return base.getStatus() + " Live UDP";
        }

        public override void Get_QSOs(string dbPath)
        {
            base.Get_QSOs(dbPath);
        }

        private void StartListener()
        {
            if (listener != null)
                return;

            try
            {
                listener = new N1MMContactInfoListener(12060);
                listener.ContactInfoReceived += ContactInfoReceivedHandler;
                listener.ErrorReceived += ListenerErrorReceivedHandler;
            }
            catch (Exception ex)
            {
                Error("N1MM UDP listener: " + ex.Message);
            }
        }

        private void ContactInfoReceivedHandler(object sender, N1MMContactInfoEventArgs e)
        {
            string call = (e.ContactInfo.call ?? "").Trim();
            string band = BandFromN1MMContactInfo(e.ContactInfo.band);
            if (string.IsNullOrEmpty(call) || string.IsNullOrEmpty(band))
                return;

            DataRow row = QSO.NewRow();
            row["CALL"] = call;
            row["BAND"] = band;
            row["TIME"] = e.ContactInfo.timestamp.ToString("HH:mm");
            row["SENT"] = e.ContactInfo.snt ?? "";
            row["RCVD"] = e.ContactInfo.rcv ?? "";
            row["LOC"] = e.ContactInfo.gridsquare ?? "";

            try
            {
                lock (QSOlock)
                {
                    DataRow existing = QSO.Rows.Find(new object[] { call, band });
                    if (existing != null)
                        existing.ItemArray = row.ItemArray;
                    else
                        QSO.Rows.Add(row);

                    liveKeys.Add(BuildKey(call, band));
                }
            }
            catch (Exception ex)
            {
                Error("(" + call + "): " + ex.Message);
            }
        }

        private void ListenerErrorReceivedHandler(object sender, N1MMListenerErrorEventArgs e)
        {
            Error("N1MM UDP parse error: " + e.Message);
        }

        protected override bool KeepRowMissingFromDatabase(DataRow row)
        {
            return liveKeys.Contains(BuildKey(row["CALL"].ToString(), row["BAND"].ToString()));
        }

        protected override void DatabaseRowMerged(string key)
        {
            liveKeys.Remove(key);
        }

        private static string BandFromN1MMContactInfo(string band)
        {
            switch (band)
            {
                case "50": return "50M";
                case "70": return "70M";
                case "144": return "144M";
                case "420": return "432M";
                case "1240": return "1_2G";
                case "2300": return "2_3G";
                case "3300": return "3_4G";
                case "5650": return "5_7G";
                case "10000": return "10G";
                case "24000": return "24G";
                case "47000": return "47G";
                case "76000": return "76G";
                default: return "";
            }
        }
    }
}
