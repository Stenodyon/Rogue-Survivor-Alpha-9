using System;

namespace djack.RogueSurvivor.Data
{
    /* All the kinds of stats */
    public enum Stat
    {
        HP = 0,
        STAMINA,
        FOOD,
        SLEEP,
        SANITY,
        _END
    }

    /* Keeps a record of all the stats (current value and previous vaue) */
    [Serializable]
    public class ActorStats
    {
        private const int stat_count = (int)Stat._END;

        /// Storage of the values
        private int[] stats;

        public ActorStats()
        {
            stats = new int[stat_count * 2];
        }

        /// Sets the current value of the given stat
        public void SetStat(Stat stat, int value)
        {
            stats[(int)stat * 2] = value;
        }

        /// Returns the current value of the given stat
        public int GetStat(Stat stat)
        {
            return stats[(int)stat * 2];
        }

        /// Sets the previous value of the stat
        public void SetPrevious(Stat stat, int value)
        {
            stats[(int)stat * 2 + 1] = value;
        }

        /// Returns the previous value of the stat
        public int GetPrevious(Stat stat)
        {
            return stats[(int)stat * 2 + 1];
        }
    }
}