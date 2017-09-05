using System;
using System.Drawing;
using System.Collections.Generic;

using djack.RogueSurvivor.Data;
using djack.RogueSurvivor.Engine;
using djack.RogueSurvivor.Engine.AI;
using djack.RogueSurvivor.Gameplay.AI.Sensors;

namespace djack.RogueSurvivor.Gameplay.AI
{
    /* This part of BaseAI provides filters */
    abstract partial class BaseAI
    {
        protected List<Percept> FilterSameMap(RogueGame game, List<Percept> percepts)
        {
            if (percepts == null || percepts.Count == 0)
                return null;

            List<Percept> list = null;
            Map map = m_Actor.Location.Map;
            foreach (Percept p in percepts)
            {
                if (p.Location.Map == map)
                {
                    if (list == null)
                        list = new List<Percept>(percepts.Count);
                    list.Add(p);
                }
            }

            return list;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="game"></param>
        /// <param name="percepts"></param>
        /// <returns>null if no enemies</returns>
        protected List<Percept> FilterEnemies(RogueGame game, List<Percept> percepts)
        {
            if (percepts == null || percepts.Count == 0)
                return null;

            List<Percept> list = null;

            foreach (Percept p in percepts)
            {
                Actor other = p.Percepted as Actor;
                if (other != null && other != m_Actor)
                {
                    if (game.Rules.IsEnemyOf(m_Actor, other))
                    {
                        if (list == null)
                            list = new List<Percept>(percepts.Count);
                        list.Add(p);
                    }
                }
            }

            return list;
        }

        protected List<Percept> FilterNonEnemies(RogueGame game, List<Percept> percepts)
        {
            if (percepts == null || percepts.Count == 0)
                return null;

            List<Percept> list = null;

            foreach (Percept p in percepts)
            {
                Actor other = p.Percepted as Actor;
                if (other != null && other != m_Actor)
                {
                    if (!game.Rules.IsEnemyOf(m_Actor, other))
                    {
                        if (list == null)
                            list = new List<Percept>(percepts.Count);
                        list.Add(p);
                    }
                }
            }

            return list;
        }

        protected List<Percept> FilterCurrent(RogueGame game, List<Percept> percepts)
        {
            if (percepts == null || percepts.Count == 0)
                return null;

            List<Percept> list = null;

            int turn = m_Actor.Location.Map.LocalTime.TurnCounter;
            foreach (Percept p in percepts)
            {
                if (p.Turn == turn)
                {
                    if (list == null)
                        list = new List<Percept>(percepts.Count);
                    list.Add(p);
                }
            }

            return list;
        }

        protected Percept FilterNearest(RogueGame game, List<Percept> percepts)
        {
            if (percepts == null || percepts.Count == 0)
                return null;

            Percept best = percepts[0];
            float nearest = game.Rules.StdDistance(m_Actor.Location.Position, percepts[0].Location.Position);

            for (int i = 1; i < percepts.Count; i++)
            {
                Percept p = percepts[i];
                float dist = game.Rules.StdDistance(m_Actor.Location.Position, p.Location.Position);
                if (dist < nearest)
                {
                    best = p;
                    nearest = dist;
                }
            }

            return best;
        }

        protected Percept FilterStrongestScent(RogueGame game, List<Percept> scents)
        {
            if (scents == null || scents.Count == 0)
                return null;

            Percept pBest = null;
            SmellSensor.AIScent best = null;
            foreach (Percept p in scents)
            {
                SmellSensor.AIScent aiScent = p.Percepted as SmellSensor.AIScent;
                if (aiScent == null)
                    throw new InvalidOperationException("percept not an aiScent");
                if (pBest == null || aiScent.Strength > best.Strength)
                {
                    best = aiScent;
                    pBest = p;
                }
            }

            return pBest;
        }

        protected List<Percept> FilterActorsModel(RogueGame game, List<Percept> percepts, ActorModel model)
        {
            if (percepts == null || percepts.Count == 0)
                return null;

            List<Percept> list = null;

            foreach (Percept p in percepts)
            {
                Actor a = p.Percepted as Actor;
                if (a != null && a.Model == model)
                {
                    if (list == null)
                        list = new List<Percept>(percepts.Count);
                    list.Add(p);
                }
            }

            return list;
        }

        protected List<Percept> FilterActors(RogueGame game, List<Percept> percepts, Predicate<Actor> predicateFn)
        {
            if (percepts == null || percepts.Count == 0)
                return null;

            List<Percept> list = null;

            foreach (Percept p in percepts)
            {
                Actor a = p.Percepted as Actor;
                if (a != null && predicateFn(a))
                {
                    if (list == null)
                        list = new List<Percept>(percepts.Count);
                    list.Add(p);
                }
            }

            return list;
        }

        protected List<Percept> FilterFireTargets(RogueGame game, List<Percept> percepts)
        {
            return Filter(game, percepts, 
                (p) =>
                {
                    Actor other = p.Percepted as Actor;
                    if (other == null)
                        return false;
                    return game.Rules.CanActorFireAt(m_Actor, other);
                });
        }

        protected List<Percept> FilterStacks(RogueGame game, List<Percept> percepts)
        {
            return Filter(game, percepts,
                (p) =>
                {
                    Inventory it = p.Percepted as Inventory;
                    if (it == null)
                        return false;
                    return true;
                });
        }

        protected List<Percept> FilterCorpses(RogueGame game, List<Percept> percepts)
        {
            return Filter(game, percepts,
                (p) =>
                {
                    List<Corpse> corpses = p.Percepted as List<Corpse>;
                    if (corpses == null)
                        return false;
                    return true;
                });
        }

        protected List<Percept> Filter(RogueGame game, List<Percept> percepts, Predicate<Percept> predicateFn)
        {
            if (percepts == null || percepts.Count == 0)
                return null;

            List<Percept> list = null;

            foreach (Percept p in percepts)
            {
                if (predicateFn(p))
                {
                    if (list == null)
                        list = new List<Percept>(percepts.Count);
                    list.Add(p);
                }
            }

            return list;
        }

        protected Percept FilterFirst(RogueGame game, List<Percept> percepts, Predicate<Percept> predicateFn)
        {
            if (percepts == null || percepts.Count == 0)
                return null;

            foreach (Percept p in percepts)
            {
                if (predicateFn(p))
                    return p;
            }

            return null;
        }

        protected List<Percept> FilterOut(RogueGame game, List<Percept> percepts, Predicate<Percept> rejectPredicateFn)
        {
            return Filter(game, percepts, (p) => !rejectPredicateFn(p));
        }

        /// <summary>
        /// Closest first.
        /// </summary>
        /// <param name="game"></param>
        /// <param name="percepts"></param>
        /// <returns></returns>
        protected List<Percept> SortByDistance(RogueGame game, List<Percept> percepts)
        {
            if (percepts == null || percepts.Count == 0)
                return null;

            Point from = m_Actor.Location.Position;

            List<Percept> sortedList = new List<Percept>(percepts);

            sortedList.Sort((pA, pB) =>
            {
                float dA = game.Rules.StdDistance(pA.Location.Position, from);
                float dB = game.Rules.StdDistance(pB.Location.Position, from);

                return dA > dB ? 1 :
                    dA < dB ? -1 :
                    0;
            });

            return sortedList;
        }

        /// <summary>
        /// Most recent first.
        /// </summary>
        /// <param name="game"></param>
        /// <param name="percepts"></param>
        /// <returns></returns>
        protected List<Percept> SortByDate(RogueGame game, List<Percept> percepts)
        {
            if (percepts == null || percepts.Count == 0)
                return null;

            List<Percept> sortedList = new List<Percept>(percepts);

            sortedList.Sort((pA, pB) =>
            {
                return pA.Turn < pB.Turn ? 1 :
                    pA.Turn > pB.Turn ? -1 :
                    0;
            });

            return sortedList;
        }

    }
}