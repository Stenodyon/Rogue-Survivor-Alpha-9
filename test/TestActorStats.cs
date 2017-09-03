using Tester;

using djack.RogueSurvivor.Data;

namespace Test.RogueSurvivor.Data
{
    public class TestActorStats : TestFixture
    {
        public void test_set_hp()
        {
            ActorStats stats = new ActorStats();
            stats.SetStat(Stat.HP, 13);
            int val = stats.GetStat(Stat.HP);
            Assert.Equal(13, val);
        }

        public void test_set_stamina()
        {
            ActorStats stats = new ActorStats();
            stats.SetStat(Stat.STAMINA, 42);
            int val = stats.GetStat(Stat.STAMINA);
            Assert.Equal(42, val);
        }

        public void test_set_food()
        {
            ActorStats stats = new ActorStats();
            stats.SetStat(Stat.FOOD, 32);
            int val = stats.GetStat(Stat.FOOD);
            Assert.Equal(32, val);
        }

        public void test_set_sleep()
        {
            ActorStats stats = new ActorStats();
            stats.SetStat(Stat.SLEEP, 66);
            int val = stats.GetStat(Stat.SLEEP);
            Assert.Equal(66, val);
        }

        public void test_set_sanity()
        {
            ActorStats stats = new ActorStats();
            stats.SetStat(Stat.SANITY, 999);
            int val = stats.GetStat(Stat.SANITY);
            Assert.Equal(999, val);
        }

        public void test_set_previous_hp()
        {
            ActorStats stats = new ActorStats();
            stats.SetPrevious(Stat.HP, 13);
            int val = stats.GetPrevious(Stat.HP);
            Assert.Equal(13, val);
        }

        public void test_set_previous_stamina()
        {
            ActorStats stats = new ActorStats();
            stats.SetPrevious(Stat.STAMINA, 42);
            int val = stats.GetPrevious(Stat.STAMINA);
            Assert.Equal(42, val);
        }

        public void test_set_previous_food()
        {
            ActorStats stats = new ActorStats();
            stats.SetPrevious(Stat.FOOD, 32);
            int val = stats.GetPrevious(Stat.FOOD);
            Assert.Equal(32, val);
        }

        public void test_set_previous_sleep()
        {
            ActorStats stats = new ActorStats();
            stats.SetPrevious(Stat.SLEEP, 66);
            int val = stats.GetPrevious(Stat.SLEEP);
            Assert.Equal(66, val);
        }

        public void test_set_previous_sanity()
        {
            ActorStats stats = new ActorStats();
            stats.SetPrevious(Stat.SANITY, 999);
            int val = stats.GetPrevious(Stat.SANITY);
            Assert.Equal(999, val);
        }
    }
}