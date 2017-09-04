using System;
using System.Collections.Generic;

namespace djack.RogueSurvivor.Data
{
    /* Associates a trust value to an actor */
    [Serializable]
    class TrustRecord
    {
        public Actor Actor { get; set; }
        public int Trust { get; set; }
    }

    /* This part of Actor manages leaders, followers and trust */
    partial class Actor
    {
        /// This actor's leader
        Actor m_Leader;
        /// This actor's followers
        List<Actor> m_Followers = null;
        /// List of trust values towards other actors
        List<TrustRecord> m_TrustList = null;

        /// Trust value in the leader
        public int TrustInLeader;

        public Actor Leader
        {
            get { return m_Leader; }
        }

        /// Gets if has a leader and he is alive.
        public bool HasLeader
        {
            get { return m_Leader != null && !m_Leader.IsDead; }
        }

        public IEnumerable<Actor> Followers
        {
            get { return m_Followers; }
        }

        /// Returns the number of followers
        public int CountFollowers
        {
            get
            {
                if (m_Followers == null)
                    return 0;
                return m_Followers.Count;
            }
        }

        public void AddFollower(Actor other)
        {
            if (other == null)
                throw new ArgumentNullException("other");
            if (m_Followers != null && m_Followers.Contains(other))
                throw new ArgumentException("other is already a follower");

            if (m_Followers == null)
                m_Followers = new List<Actor>(1);
            m_Followers.Add(other);

            if (other.Leader != null)
                other.Leader.RemoveFollower(other);
            other.m_Leader = this;
        }

        public void RemoveFollower(Actor other)
        {
            if (other == null)
                throw new ArgumentNullException("other");
            if (m_Followers == null)
                throw new InvalidOperationException("no followers");

            m_Followers.Remove(other);
            if (m_Followers.Count == 0)
                m_Followers = null;

            other.m_Leader = null;

            // reset directives & order.
            AIController ai = other.Controller as AIController;
            if (ai != null)
            {
                ai.Directives.Reset();
                ai.SetOrder(null);
            }
        }

        public void RemoveAllFollowers()
        {
            while (m_Followers != null && m_Followers.Count > 0)
            {
                RemoveFollower(m_Followers[0]);
            }
        }

        /// Set the trust value in someone
        public void SetTrustIn(Actor other, int trust)
        {
            if (m_TrustList == null)
            {
                m_TrustList = new List<TrustRecord>(1) { new TrustRecord() { Actor = other, Trust = trust } };
                return;
            }

            foreach (TrustRecord r in m_TrustList)
            {
                if (r.Actor == other)
                {
                    r.Trust = trust;
                    return;
                }
            }

            m_TrustList.Add(new TrustRecord() { Actor = other, Trust = trust });
        }


        public void AddTrustIn(Actor other, int amount)
        {
            SetTrustIn(other, GetTrustIn(other) + amount);
        }

        public int GetTrustIn(Actor other)
        {
            if (m_TrustList == null) return 0;
            foreach (TrustRecord r in m_TrustList)
            {
                if (r.Actor == other)
                    return r.Trust;
            }
            return 0;
        }
    }
}