﻿using System;
using System.Data;
using System.Reflection;

namespace N1MM
{
    public abstract class WtLogEventArgs : EventArgs
    {
        public DataRow QSO { get; }
    }
    public abstract class N1MMLogBase : IDisposable
    {
        public DataTable QSO { get; protected set; }
        public string MyLoc { get; protected set; }

        public delegate bool LogWriteMessageDelegate(string s);
        protected LogWriteMessageDelegate LogWrite;

        protected void Error(string Text)
        {
            if (LogWrite != null)
                LogWrite("Error - <" + (new System.Diagnostics.StackTrace()).GetFrame(1).GetMethod().Name + "> " + Text);
        }

        protected void Debug(string Text)
        {
#if DEBUG
            Console.WriteLine(Text);
            if (LogWrite != null)
                LogWrite((new System.Diagnostics.StackTrace()).GetFrame(1).GetMethod().Name + " - " + Text);
#endif
        }

        public void Clear_QSOs()
        {
            QSO.Clear();
            LogState = LOG_STATE.LOG_INACTIVE;
        }

        public abstract string getStatus();

        public abstract void Get_QSOs(string tmp);

        public abstract void Dispose();

        class WtLogEventArgs : EventArgs
        {
            /// <summary>
            /// Win-Test message
            /// </summary>
            public WtLogEventArgs(DataRow QSO)
            {
                this.QSO = QSO;
            }
            public DataRow QSO { get; private set; }
        }

        public event EventHandler AddQSO;

        public enum LOG_STATE
        {
            LOG_INACTIVE,
            LOG_SYNCING,
            LOG_IN_SYNC,
        };

        private LOG_STATE logState = LOG_STATE.LOG_INACTIVE;

        public LOG_STATE LogState {
            get { return logState; }
            protected set
            {
                logState = value;
                if (LogStateChanged != null)
                {
                    LogStateChanged(this, new LogStateEventArgs(logState));
                }
            }
        }

        public event EventHandler<LogStateEventArgs> LogStateChanged;
        public class LogStateEventArgs : EventArgs
        {
            /// <summary>
            /// called when LogState changes
            /// </summary>
            public LogStateEventArgs(LOG_STATE LogState)
            {
                this.LogState = LogState;
            }
            public LOG_STATE LogState { get; private set; }
        }

        public N1MMLogBase(LogWriteMessageDelegate mylog)
        {
            QSO = new DataTable("QSO");
            QSO.Columns.Add("CALL");
            QSO.Columns.Add("BAND");
            QSO.Columns.Add("TIME");
            QSO.Columns.Add("SENT");
            QSO.Columns.Add("RCVD");
            QSO.Columns.Add("LOC");
            LogWrite = mylog;
        }
    }
}
