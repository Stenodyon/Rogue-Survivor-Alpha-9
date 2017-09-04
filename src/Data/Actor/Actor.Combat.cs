using System.Collections.Generic;

namespace djack.RogueSurvivor.Data
{
    /* This part of Actor manages combat */
    partial class Actor
    {
        Attack m_CurrentMeleeAttack;
        Attack m_CurrentRangedAttack;
        Defence m_CurrentDefence;

        List<Actor> m_AggressorOf = null;
        List<Actor> m_SelfDefenceFrom = null;

        int m_KillsCount;
        int m_MurdersCounter;

        public Attack CurrentMeleeAttack
        {
            get { return m_CurrentMeleeAttack; }
            set { m_CurrentMeleeAttack = value; }
        }

        public Attack CurrentRangedAttack
        {
            get { return m_CurrentRangedAttack; }
            set { m_CurrentRangedAttack = value; }
        }

        public Defence CurrentDefence
        {
            get { return m_CurrentDefence; }
            set { m_CurrentDefence = value; }
        }

        public int KillsCount
        {
            get { return m_KillsCount; }
            set { m_KillsCount = value; }
        }

        public IEnumerable<Actor> AggressorOf
        {
            get { return m_AggressorOf; }
        }

        public int CountAggressorOf
        {
            get
            {
                if (m_AggressorOf == null)
                    return 0;
                return m_AggressorOf.Count;
            }
        }

        public IEnumerable<Actor> SelfDefenceFrom
        {
            get { return m_SelfDefenceFrom; }
        }

        public int CountSelfDefenceFrom
        {
            get
            {
                if (m_SelfDefenceFrom == null)
                    return 0;
                return m_SelfDefenceFrom.Count;
            }
        }

        public int MurdersCounter
        {
            get { return m_MurdersCounter; }
            set { m_MurdersCounter = value; }
        }

        public void MarkAsAgressorOf(Actor other)
        {
            if (other == null || other.IsDead)
                return;

            if (m_AggressorOf == null)
                m_AggressorOf = new List<Actor>(1);
            else if (m_AggressorOf.Contains(other))
                return;
            m_AggressorOf.Add(other);
        }

        public void MarkAsSelfDefenceFrom(Actor other)
        {
            if (other == null || other.IsDead)
                return;

            if (m_SelfDefenceFrom == null)
                m_SelfDefenceFrom = new List<Actor>(1);
            else if (m_SelfDefenceFrom.Contains(other))
                return;
            m_SelfDefenceFrom.Add(other);
        }

        public bool IsAggressorOf(Actor other)
        {
            if (m_AggressorOf == null)
                return false;
            return m_AggressorOf.Contains(other);
        }

        public bool IsSelfDefenceFrom(Actor other)
        {
            if (m_SelfDefenceFrom == null)
                return false;
            return m_SelfDefenceFrom.Contains(other);
        }

        public void RemoveAggressorOf(Actor other)
        {
            if (m_AggressorOf == null)
                return;
            m_AggressorOf.Remove(other);
            if (m_AggressorOf.Count == 0)
                m_AggressorOf = null;
        }

        public void RemoveSelfDefenceFrom(Actor other)
        {
            if (m_SelfDefenceFrom == null)
                return;
            m_SelfDefenceFrom.Remove(other);
            if (m_SelfDefenceFrom.Count == 0)
                m_SelfDefenceFrom = null;
        }

        public void RemoveAllAgressorSelfDefenceRelations()
        {
            while (m_AggressorOf != null)
            {
                Actor other = m_AggressorOf[0];
                RemoveAggressorOf(other);
                other.RemoveSelfDefenceFrom(this);
            }
            while (m_SelfDefenceFrom != null)
            {
                Actor other = m_SelfDefenceFrom[0];
                RemoveSelfDefenceFrom(other);
                other.RemoveAggressorOf(this);
            }
        }

        public bool AreDirectEnemies(Actor other)
        {
            if (other == null || other.IsDead)
                return false;

            if (m_AggressorOf != null)
            {
                if (m_AggressorOf.Contains(other))
                    return true;
            }
            if (m_SelfDefenceFrom != null)
            {
                if (m_SelfDefenceFrom.Contains(other))
                    return true;
            }

            if (other.IsAggressorOf(this))
                return true;
            if (other.IsSelfDefenceFrom(this))
                return true;

            // nope.
            return false;
        }

        public bool AreIndirectEnemies(Actor other)
        {
            if (other == null || other.IsDead)
                return false;

            // check my leader and my mates, if any.
            if (this.HasLeader)
            {
                // my leader.
                if (m_Leader.AreDirectEnemies(other))
                    return true;
                if (other.HasLeader && m_Leader.AreDirectEnemies(other.Leader))
                    return true;
                // my mates = my leader followers.
                foreach (Actor mate in m_Leader.Followers)
                    if (mate != this && mate.AreDirectEnemies(other))
                        return true;
            }

            // check my followers, if any.
            if (this.CountFollowers > 0)
            {
                foreach (Actor fo in m_Followers)
                    if (fo.AreDirectEnemies(other))
                        return true;
            }

            // check their leader and mates.
            if (other.HasLeader)
            {
                // his leader.
                if (other.Leader.AreDirectEnemies(this))
                    return true;
                if (HasLeader && other.Leader.AreDirectEnemies(m_Leader))
                    return true;
                // his mates = his leader followers.
                foreach (Actor mate in other.Leader.Followers)
                    if (mate != other && mate.AreDirectEnemies(this))
                        return true;
            }

            // nope.
            return false;
        }

#if false
        /// <summary>
        /// Make sure another actor is added to the list of personal enemies.
        /// </summary>
        /// <param name="e"></param>
        public void MarkAsPersonalEnemy(Actor e)
        {
            if (e == null || e.IsDead)
                return;

            if (m_PersonalEnemies == null)
                m_PersonalEnemies = new List<Actor>(1);
            else if (m_PersonalEnemies.Contains(e))
                return;

            m_PersonalEnemies.Add(e);
        }

        public void RemoveAsPersonalEnemy(Actor e)
        {
            if (m_PersonalEnemies == null)
                return;

            m_PersonalEnemies.Remove(e);

            // minimize data size.
            if (m_PersonalEnemies.Count == 0)
                m_PersonalEnemies = null; 
        }

        public void RemoveAllPersonalEnemies()
        {
            if (m_PersonalEnemies == null)
                return;

            while (m_PersonalEnemies.Count > 0)
            {
                Actor e = m_PersonalEnemies[0];
                e.RemoveAsPersonalEnemy(this);
                m_PersonalEnemies.Remove(e);
            }
        }

        /// <summary>
        /// Checks for own personal enemy as well as our band (leader & followers).
        /// So in a band we share all personal enemies.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        public bool HasActorAsPersonalEnemy(Actor other)
        {
            if (other == null || other.IsDead)
                return false;

            // first check personal list.
            if (m_PersonalEnemies != null)
            {
                if (m_PersonalEnemies.Contains(other))
                    return true;
            }

            // check my leader and my mates, if any.
            if (this.HasLeader)
            {
                // my leader.
                if (m_Leader.HasDirectPersonalEnemy(other))
                    return true;
                // my mates = my leader followers.
                foreach (Actor mate in m_Leader.Followers)
                    if (mate != this && mate.HasDirectPersonalEnemy(other))
                        return true;
            }

            // check my followers, if any.
            if (this.CountFollowers > 0)
            {
                foreach (Actor fo in m_Followers)
                    if (fo.HasDirectPersonalEnemy(other))
                        return true;
            }

            // nope.
            return false;
        }

        /// <summary>
        /// Checks only personal enemy list, do not check our band.
        /// </summary>
        /// <param name="other"></param>
        /// <returns></returns>
        bool HasDirectPersonalEnemy(Actor other)
        {
            if (m_PersonalEnemies == null)
                return false;
            return m_PersonalEnemies.Contains(other);
        }

        public void MarkAsSelfDefence(Actor e)
        {
            if (e == null || e.IsDead)
                return;

            if (m_SelfDefence == null)
                m_SelfDefence = new List<Actor>(1);
            else if (m_SelfDefence.Contains(e))
                return;

            m_SelfDefence.Add(e);
        }

        public void RemoveAsSelfDefence(Actor e)
        {
            if (m_SelfDefence == null)
                return;

            m_SelfDefence.Remove(e);

            // minimize data size.
            if (m_SelfDefence.Count == 0)
                m_SelfDefence = null;
        }

        public void RemoveAllSelfDefence()
        {
            if (m_SelfDefence == null)
                return;

            while (m_SelfDefence.Count > 0)
            {
                Actor e = m_SelfDefence[0];
                e.RemoveAsSelfDefence(this);
                m_SelfDefence.Remove(e);
            }
        }

        public bool HasDirectSelfDefence(Actor other)
        {
            if (m_SelfDefence == null)
                return false;
            return m_SelfDefence.Contains(other);
        }
#endif
    }
}