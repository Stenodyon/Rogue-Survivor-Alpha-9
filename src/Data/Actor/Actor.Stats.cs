using System;

namespace djack.RogueSurvivor.Data
{
    /* Part of Actor that manages stats such as HP, Food, etc */
    partial class Actor
    {
        ActorStats stats = new ActorStats();

        public ActorStats Stats
        {
            get { return stats; }
        }

        public int HitPoints
        {
            get { return stats.GetStat(Stat.HP); }
            set { stats.SetStat(Stat.HP, value); }
        }

        public int PreviousHitPoints
        {
            get { return stats.GetPrevious(Stat.HP); }
            set { stats.SetPrevious(Stat.HP, value); }
        }

        public int StaminaPoints
        {
            get { return stats.GetStat(Stat.STAMINA); }
            set { stats.SetStat(Stat.STAMINA, value); }
        }

        public int PreviousStaminaPoints
        {
            get { return stats.GetPrevious(Stat.STAMINA); }
            set { stats.SetPrevious(Stat.STAMINA, value); }
        }

        public int FoodPoints
        {
            get { return stats.GetStat(Stat.FOOD); }
            set { stats.SetStat(Stat.FOOD, value); }
        }

        public int PreviousFoodPoints
        {
            get { return stats.GetPrevious(Stat.FOOD); }
            set { stats.SetPrevious(Stat.FOOD, value); }
        }

        public int SleepPoints
        {
            get { return stats.GetStat(Stat.SLEEP); }
            set { stats.SetStat(Stat.SLEEP, value); }
        }

        public int PreviousSleepPoints
        {
            get { return stats.GetPrevious(Stat.SLEEP); }
            set { stats.SetPrevious(Stat.SLEEP, value); }
        }

        public int Sanity
        {
            get { return stats.GetStat(Stat.SANITY); }
            set { stats.SetStat(Stat.SANITY, value); }
        }

        public int PreviousSanity
        {
            get { return stats.GetPrevious(Stat.SANITY); }
            set { stats.SetPrevious(Stat.SANITY, value); }
        }
    }
}