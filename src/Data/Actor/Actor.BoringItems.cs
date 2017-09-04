using System.Collections.Generic;

namespace djack.RogueSurvivor.Data
{
    /* This part of actor manages boring items */
    partial class Actor
    {
        HashSet<Item> m_BoringItems = null;

        public void AddBoringItem(Item it)
        {
            if (m_BoringItems == null) m_BoringItems = new HashSet<Item>();
            m_BoringItems.Add(it);
        }

        public bool IsBoredOf(Item it)
        {
            if (m_BoringItems == null) return false;
            return m_BoringItems.Contains(it);
        }
    }
}