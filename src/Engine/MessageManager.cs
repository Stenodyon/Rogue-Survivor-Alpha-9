using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using djack.RogueSurvivor.Data;

namespace djack.RogueSurvivor.Engine
{
    class MessageManager
    {
        #region Fields
        readonly List<Message> m_Messages = new List<Message>();
        readonly List<Message> m_History;
        int m_HistorySize;
        #endregion

        #region Properties
        public int Count
        {
            get { return m_Messages.Count; }
        }

        public IEnumerable<Message> Messages
        {
            get { return m_Messages; }
        }

        public IEnumerable<Message> History
        {
            get { return m_History; }
        }
        #endregion

        #region Init
        public MessageManager(int historySize)
        {
            m_HistorySize = historySize;
            m_History = new List<Message>(historySize);
        }
        #endregion

        #region Managing messages
        public void Clear()
        {
            m_Messages.Clear();
        }

        public void ClearHistory()
        {
            m_History.Clear();
        }

        public void Add(Message msg)
        {
            m_Messages.Add(msg);
            m_History.Add(msg);
            if (m_History.Count > m_HistorySize)
            {
                m_History.RemoveAt(0);
            }
        }

        public void RemoveLastMessage()
        {
            if (m_Messages.Count == 0)
                return;
            m_Messages.RemoveAt(m_Messages.Count - 1);
        }
        #endregion
    }
}
