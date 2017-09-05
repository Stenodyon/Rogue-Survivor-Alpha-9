using System;
using System.Drawing;
using System.Collections.Generic;

using djack.RogueSurvivor.Data;
using djack.RogueSurvivor.Engine;
using djack.RogueSurvivor.Engine.AI;
using djack.RogueSurvivor.Engine.Actions;
using djack.RogueSurvivor.Engine.Items;
using djack.RogueSurvivor.Engine.MapObjects;
using djack.RogueSurvivor.Gameplay.AI.Sensors;

namespace djack.RogueSurvivor.Gameplay.AI
{
    /* This part of BaseAI provides movement behaviors */
    abstract partial class BaseAI
    {
        #region Basic Movement
        protected ActorAction BehaviorWander(RogueGame game, Predicate<Location> goodWanderLocFn)
        {
            ChoiceEval<Direction> chooseRandomDir = Choose<Direction>(game,
                Direction.COMPASS_LIST,
                (dir) =>
                {
                    Location next = m_Actor.Location + dir;
                    if (goodWanderLocFn != null && !goodWanderLocFn(next))
                        return false;
                    ActorAction bumpAction = game.Rules.IsBumpableFor(m_Actor, game, next);
                    return isValidWanderAction(game, bumpAction);
                },
                (dir) =>
                {
                    int score = game.Rules.Roll(0, 666); 
                    if (m_Actor.Model.Abilities.IsIntelligent)
                    {
                        if (IsAnyActivatedTrapThere(m_Actor.Location.Map, (m_Actor.Location + dir).Position))
                            score -= 1000;
                    }
                    return score;
                },
                (a, b) => a > b);

            if (chooseRandomDir != null)
                return new ActionBump(m_Actor, game, chooseRandomDir.Choice);
            else
                return null;
        }

        protected ActorAction BehaviorWander(RogueGame game)
        {
            return BehaviorWander(game, null);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="game"></param>
        /// <param name="goal"></param>
        /// <param name="distanceFn">float.Nan to forbid a move</param>
        /// <returns></returns>
        protected ActorAction BehaviorBumpToward(RogueGame game, Point goal, Func<Point, Point, float> distanceFn)
        {
            ChoiceEval<ActorAction> bestCloserDir = ChooseExtended<Direction, ActorAction>(game,
                Direction.COMPASS_LIST,
                (dir) =>
                {
                    Location next = m_Actor.Location + dir;
                    ActorAction bumpAction = game.Rules.IsBumpableFor(m_Actor, game, next);
                    if (bumpAction == null)
                    {
                        // for undeads, try to push the blocking object randomly.
                        if (m_Actor.Model.Abilities.IsUndead && game.Rules.HasActorPushAbility(m_Actor))
                        {
                            MapObject obj = m_Actor.Location.Map.GetMapObjectAt(next.Position);
                            if (obj != null)
                            {
                                if (game.Rules.CanActorPush(m_Actor, obj))
                                {
                                    Direction pushDir = game.Rules.RollDirection();
                                    if (game.Rules.CanPushObjectTo(obj, obj.Location.Position + pushDir))
                                        return new ActionPush(m_Actor, game, obj, pushDir);
                                }
                            }
                        }

                        // failed.
                        return null;
                    }
                    if (next.Position == goal)
                        return bumpAction;
                    if (IsValidMoveTowardGoalAction(bumpAction))
                        return bumpAction;
                    else
                        return null;
                },
                (dir) =>
                {
                    Location next = m_Actor.Location + dir;
                    if (distanceFn != null)
                        return distanceFn(next.Position, goal);
                    else
                        return game.Rules.StdDistance(next.Position, goal);
                },
                (a, b) => !float.IsNaN(a) && a < b);

            if (bestCloserDir != null)
                return bestCloserDir.Choice;
            else
                return null;
        }

        protected ActorAction BehaviorStupidBumpToward(RogueGame game, Point goal)
        {
            return BehaviorBumpToward(game, goal,
                (ptA, ptB) =>
                {
                    if (ptA == ptB) return 0;
                    float distance = game.Rules.StdDistance(ptA, ptB);
                    //if (distance < 2f) return distance;

                    // penalize having to push/bash/jump.
                    if (!game.Rules.IsWalkableFor(m_Actor, m_Actor.Location.Map, ptA.X, ptA.Y))
                        distance += MOVE_DISTANCE_PENALTY;

                    return distance;
                });
        }

        protected ActorAction BehaviorIntelligentBumpToward(RogueGame game, Point goal)
        {
            float currentDistance = game.Rules.StdDistance(m_Actor.Location.Position, goal);
            bool imStarvingOrCourageous = game.Rules.IsActorStarving(m_Actor) || Directives.Courage == ActorCourage.COURAGEOUS;

            ActorAction bump = BehaviorBumpToward(game, goal,
                (ptA, ptB) =>
                {
                    if (ptA == ptB) return 0;
                    float distance = game.Rules.StdDistance(ptA, ptB);
                    //if (distance < 2f) return distance;

                    // consider only moves that make takes us closer.
                    if (distance >= currentDistance)
                        return float.NaN;

                    // avoid stepping on damaging traps, unless starving or courageous.
                    if (!imStarvingOrCourageous)
                    {
                        int trapsDamage = ComputeTrapsMaxDamage(m_Actor.Location.Map, ptA);
                        if (trapsDamage > 0)
                        {
                            // if death, don't do it.
                            if (trapsDamage >= m_Actor.HitPoints)
                                return float.NaN;
                            // avoid.
                            distance += MOVE_DISTANCE_PENALTY;
                        }
                    }

                    return distance;
                });
            return bump;
        }

        protected ActorAction BehaviorWalkAwayFrom(RogueGame game, Percept goal)
        {
            return BehaviorWalkAwayFrom(game, new List<Percept>(1) { goal });
        }

        protected ActorAction BehaviorWalkAwayFrom(RogueGame game, List<Percept> goals)
        {
            // stuff to avoid stepping into leader LoF.
            Actor myLeader = m_Actor.Leader;
            bool leaderIsFiring = m_Actor.HasLeader && m_Actor.GetEquippedWeapon() is ItemRangedWeapon;
            Actor leaderNearestTarget = null;
            if (leaderIsFiring) leaderNearestTarget = GetNearestTargetFor(game, m_Actor.Leader);
            bool checkLeaderLoF = leaderNearestTarget != null && leaderNearestTarget.Location.Map == m_Actor.Location.Map;
            List<Point> leaderLoF = null;
            if (checkLeaderLoF)
            {
                leaderLoF = new List<Point>(1);
                ItemRangedWeapon wpn = m_Actor.GetEquippedWeapon() as ItemRangedWeapon;
                LOS.CanTraceFireLine(myLeader.Location, leaderNearestTarget.Location.Position, (wpn.Model as ItemRangedWeaponModel).Attack.Range, leaderLoF);
            }

            ChoiceEval<Direction> bestAwayDir = Choose<Direction>(game,
                Direction.COMPASS_LIST,
                (dir) =>
                {
                    Location next = m_Actor.Location + dir;
                    ActorAction bumpAction = game.Rules.IsBumpableFor(m_Actor, game, next);
                    return IsValidFleeingAction(bumpAction);
                },
                (dir) =>
                {
                    Location next = m_Actor.Location + dir;
                    // Heuristic value:
                    // - Safety from dangers.
                    // - If follower, stay close to leader but avoid stepping into leader LoF.
                    float safetyValue = SafetyFrom(game.Rules, next.Position, goals);
                    if (m_Actor.HasLeader)
                    {
                        // stay close to leader.
                        safetyValue -= game.Rules.StdDistance(next.Position, m_Actor.Leader.Location.Position);
                        // don't step into leader LoF.
                        if (checkLeaderLoF)
                        {
                            if (leaderLoF.Contains(next.Position))
                                safetyValue -= LEADER_LOF_PENALTY;
                        }
                    }
                    return safetyValue;
                },
                (a, b) => a > b);

            if (bestAwayDir != null)// && bestAwayDir.Value > notMovingValue) nope, moving is always better than not moving
                return new ActionBump(m_Actor, game, bestAwayDir.Choice);
            else
                return null;
        }
        #endregion

        #region Advanced movement
        protected ActorAction BehaviorCloseDoorBehindMe(RogueGame game, Location previousLocation)
        {
            // if we've gone through a door, try to close it.
            DoorWindow prevDoor = previousLocation.Map.GetMapObjectAt(previousLocation.Position) as DoorWindow;
            if (prevDoor == null)
                return null;
            if (game.Rules.IsClosableFor(m_Actor, prevDoor))
                return new ActionCloseDoor(m_Actor, game, prevDoor);

            // nope.
            return null;
        }

        protected ActorAction BehaviorSecurePerimeter(RogueGame game, HashSet<Point> fov)
        {
            /////////////////////////////////////
            // Secure room procedure:
            // 1. Close doors/windows.
            // 2. Barricade unbarricaded windows.
            /////////////////////////////////////
            Map map = m_Actor.Location.Map;

            foreach (Point pt in fov)
            {
                MapObject mapObj = map.GetMapObjectAt(pt);
                if (mapObj == null) 
                    continue;
                DoorWindow door = mapObj as DoorWindow;
                if (door == null)
                    continue;

                // 1. Close doors/windows.
                if (door.IsOpen && game.Rules.IsClosableFor(m_Actor, door))
                {
                    if (game.Rules.IsAdjacent(door.Location.Position, m_Actor.Location.Position))
                        return new ActionCloseDoor(m_Actor, game, door);
                    else
                        return BehaviorIntelligentBumpToward(game, door.Location.Position);
                }

                // 2. Barricade unbarricaded windows.
                if (door.IsWindow && !door.IsBarricaded && game.Rules.CanActorBarricadeDoor(m_Actor,door))
                {
                    if (game.Rules.IsAdjacent(door.Location.Position, m_Actor.Location.Position))
                        return new ActionBarricadeDoor(m_Actor, game, door);
                    else
                        return BehaviorIntelligentBumpToward(game, door.Location.Position);                    
                }
            }

            // nothing to secure.
            return null;
        }
        #endregion

        #region Exits
        [Flags]
        protected enum UseExitFlags
        {
            /// <summary>
            /// Use only free exits.
            /// </summary>
            NONE = 0,

            /// <summary>
            /// Can try to break a blocking object.
            /// </summary>
            BREAK_BLOCKING_OBJECTS = (1 << 0),

            /// <summary>
            /// Can try to attack a blocking actor.
            /// </summary>
            ATTACK_BLOCKING_ENEMIES = (1 << 1),

            /// <summary>
            /// Do not use exit if we go back to our last location.
            /// </summary>
            DONT_BACKTRACK = (1 << 2)
        }

        /// <summary>
        /// Intelligent use of exit through flags : can prevent from backtracking, can attack object, can attack actor.
        /// </summary>
        /// <param name="game"></param>
        /// <param name="useFlags">combination of flags.</param>
        /// <returns></returns>
        protected ActorAction BehaviorUseExit(RogueGame game, UseExitFlags useFlags)
        {
            // get exit at location, if none or ai flag not set, fail.
            Exit exit = m_Actor.Location.Map.GetExitAt(m_Actor.Location.Position);
            if (exit == null)
                return null;
            if (!exit.IsAnAIExit)
                return null;

            // don't backtrack?
            if ((useFlags & UseExitFlags.DONT_BACKTRACK) != 0)
            {
                if (exit.ToMap == m_prevLocation.Map && exit.ToPosition == m_prevLocation.Position)
                    return null;
            }

            // if exit blocked by an enemy and want to attack it, do it.
            if ((useFlags & UseExitFlags.ATTACK_BLOCKING_ENEMIES) != 0)
            {
                Actor blockingActor = exit.ToMap.GetActorAt(exit.ToPosition);
                if (blockingActor != null && game.Rules.IsEnemyOf(m_Actor, blockingActor) && game.Rules.CanActorMeleeAttack(m_Actor, blockingActor))
                    return new ActionMeleeAttack(m_Actor, game, blockingActor);
            }

            // if exit blocked by a breakable object and want to bash, do it.
            if ((useFlags & UseExitFlags.BREAK_BLOCKING_OBJECTS) != 0)
            {
                MapObject blockingObj = exit.ToMap.GetMapObjectAt(exit.ToPosition);
                if (blockingObj != null && game.Rules.IsBreakableFor(m_Actor, blockingObj))
                    return new ActionBreak(m_Actor, game, blockingObj);
            }

            // if using exit is illegal, fail.
            if (!game.Rules.CanActorUseExit(m_Actor, m_Actor.Location.Position))
                return null;

            // use the exit.
            return new ActionUseExit(m_Actor, m_Actor.Location.Position, game);
        }
        #endregion
    }
}