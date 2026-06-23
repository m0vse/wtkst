using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SQLite;
using WinTest;

namespace wtKST
{
    public class N1MMSQLiteLog : WinTestLogBase
    {
        public int ContestNR { get; set; } = -1;

        private string _dbPath = "";

        public N1MMSQLiteLog(WinTestLogBase.LogWriteMessageDelegate mylog) : base(mylog)
        {
            DataColumn[] keys = { QSO.Columns["CALL"], QSO.Columns["BAND"] };
            QSO.PrimaryKey = keys;
        }

        public override void Dispose() { }

        public override string getStatus()
        {
            return _dbPath + " ContestNR=" + ContestNR + " QSOs=" + QSO.Rows.Count;
        }

        public override void Get_QSOs(string dbPath)
        {
            if (string.IsNullOrEmpty(dbPath) || ContestNR < 0)
                return;

            _dbPath = dbPath;

            try
            {
                List<QsoRecord> records = new List<QsoRecord>();
                string connectionString = BuildConnectionString(dbPath);
                using (var conn = new SQLiteConnection(connectionString))
                {
                    conn.Open();
                    LogState = LOG_STATE.LOG_SYNCING;

                    using (var cmd = new SQLiteCommand(
                        "SELECT Call, Band, TS, SentNr, NR, GridSquare FROM DXLOG WHERE ContestNR = @nr",
                        conn))
                    {
                        cmd.Parameters.AddWithValue("@nr", ContestNR);
                        using (var reader = cmd.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                string call = reader["Call"].ToString().Trim();
                                double bandMhz = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                                string band = BandFromMhz(bandMhz);
                                string ts = "";
                                if (!reader.IsDBNull(2))
                                {
                                    DateTime dt;
                                    if (DateTime.TryParse(reader["TS"].ToString(), out dt))
                                        ts = dt.ToString("HH:mm");
                                }
                                string sent = reader.IsDBNull(3) ? "" : reader.GetInt32(3).ToString();
                                string rcvd = reader.IsDBNull(4) ? "" : reader.GetInt32(4).ToString();
                                string loc = reader.IsDBNull(5) ? "" : reader["GridSquare"].ToString().Trim();

                                if (string.IsNullOrEmpty(call) || string.IsNullOrEmpty(band))
                                    continue;

                                records.Add(new QsoRecord(call, band, ts, sent, rcvd, loc));
                            }
                        }
                    }

                    MergeDatabaseRecords(records);
                    LogState = LOG_STATE.LOG_IN_SYNC;
                }
            }
            catch (Exception ex)
            {
                Error("(" + dbPath + "): " + ex.Message);
                LogState = LOG_STATE.LOG_INACTIVE;
            }
        }

        private void MergeDatabaseRecords(List<QsoRecord> records)
        {
            HashSet<string> databaseKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (QsoRecord record in records)
                databaseKeys.Add(record.Key);

            lock (QSOlock)
            {
                for (int i = QSO.Rows.Count - 1; i >= 0; i--)
                {
                    DataRow row = QSO.Rows[i];
                    string key = BuildKey(row["CALL"].ToString(), row["BAND"].ToString());
                    if (!databaseKeys.Contains(key) && !KeepRowMissingFromDatabase(row))
                        QSO.Rows.RemoveAt(i);
                }

                foreach (QsoRecord record in records)
                {
                    try
                    {
                        DataRow row = QSO.NewRow();
                        row["CALL"] = record.Call;
                        row["BAND"] = record.Band;
                        row["TIME"] = record.Time;
                        row["SENT"] = record.Sent;
                        row["RCVD"] = record.Rcvd;
                        row["LOC"] = record.Loc;

                        DataRow existing = QSO.Rows.Find(new object[] { record.Call, record.Band });
                        if (existing != null)
                            existing.ItemArray = row.ItemArray;
                        else
                            QSO.Rows.Add(row);
                        DatabaseRowMerged(record.Key);
                    }
                    catch (Exception ex)
                    {
                        Error("(" + record.Call + "): " + ex.Message);
                    }
                }
            }
        }

        protected virtual bool KeepRowMissingFromDatabase(DataRow row)
        {
            return false;
        }

        protected virtual void DatabaseRowMerged(string key)
        {
        }

        protected static string BuildKey(string call, string band)
        {
            return (call ?? "").Trim() + "\u001f" + (band ?? "").Trim();
        }

        private static string BandFromMhz(double mhz)
        {
            if (mhz == 50.0) return "50M";
            if (mhz == 70.0) return "70M";
            if (mhz == 144.0) return "144M";
            if (mhz == 420.0) return "432M";
            if (mhz == 1240.0) return "1_2G";
            if (mhz == 2300.0) return "2_3G";
            if (mhz == 3300.0) return "3_4G";
            if (mhz == 5650.0) return "5_7G";
            if (mhz == 10000.0) return "10G";
            if (mhz == 24000.0) return "24G";
            if (mhz == 47000.0) return "47G";
            if (mhz == 76000.0) return "76G";
            return "";
        }

        public static List<ContestEntry> LoadContests(string dbPath)
        {
            var result = new List<ContestEntry>();
            string connectionString = BuildConnectionString(dbPath);
            using (var conn = new SQLiteConnection(connectionString))
            {
                conn.Open();
                using (var cmd = new SQLiteCommand(
                    "SELECT ContestNR, ContestName, StartDate FROM ContestInstance ORDER BY StartDate DESC",
                    conn))
                {
                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            int nr = reader.IsDBNull(0) ? -1 : reader.GetInt32(0);
                            string name = reader.IsDBNull(1) ? "" : reader["ContestName"].ToString();
                            string dateStr = reader.IsDBNull(2) ? "" : reader["StartDate"].ToString();
                            result.Add(new ContestEntry { ContestNR = nr, ContestName = name, StartDate = dateStr });
                        }
                    }
                }
            }
            return result;
        }

        private static string BuildConnectionString(string dbPath)
        {
            var builder = new SQLiteConnectionStringBuilder
            {
                DataSource = dbPath,
                Version = 3,
                ReadOnly = true
            };
            return builder.ConnectionString;
        }

        protected class QsoRecord
        {
            public QsoRecord(string call, string band, string time, string sent, string rcvd, string loc)
            {
                Call = call;
                Band = band;
                Time = time;
                Sent = sent;
                Rcvd = rcvd;
                Loc = loc;
                Key = BuildKey(call, band);
            }

            public string Call { get; private set; }
            public string Band { get; private set; }
            public string Time { get; private set; }
            public string Sent { get; private set; }
            public string Rcvd { get; private set; }
            public string Loc { get; private set; }
            public string Key { get; private set; }
        }

        public class ContestEntry
        {
            public int ContestNR;
            public string ContestName;
            public string StartDate;
            public override string ToString() => ContestName + " \u2014 " + StartDate;
        }
    }
}
