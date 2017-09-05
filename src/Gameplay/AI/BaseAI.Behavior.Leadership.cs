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
    /* This part of BaseAI provides leadership behaviors */
    abstract partial class BaseAI
    {
        #region Following
        protected ActorAction BehaviorFollowActor(RogueGame game, Actor other, Point otherPosition, bool isVisible, int maxDist)
        {
            // if no other or dead, don't.
            if (other == null || other.IsDead)
                return null;

            // if close enough and visible, wait there.
            int dist = game.Rules.GridDistance(m_Actor.Location.Position, otherPosition);
            if (isVisible && dist <= maxDist)
                return new ActionWait(m_Actor, game);

            // if in different map and standing on an exit that leads there, try to use the exit.
            if (other.Location.Map != m_Actor.Location.Map)
            {
                Exit exitThere = m_Actor.Location.Map.GetExitAt(m_Actor.Location.Position);
                if (exitThere != null && exitThere.ToMap == other.Location.Map)
                {
                    if (game.Rules.CanActorUseExit(m_Actor, m_Actor.Location.Position))
                        return new ActionUseExit(m_Actor, m_Actor.Location.Position, game);
                }
            }

            // try to get close.
            ActorAction bumpAction = BehaviorIntelligentBumpToward(game, otherPosition);
            if (bumpAction != null && bumpAction.IsLegal())
            {
                // run if other is running.
                if (other.IsRunning)
                    RunIfPossible(game.Rules);

                // done.
                return bumpAction;
            }
            
            // fail.
            return null;
        }

        protected ActorAction BehaviorHangAroundActor(RogueGame game, Actor other, Point otherPosition, int minDist, int maxDist)
        {
            // if no other or dead, don't.
            if (other == null || other.IsDead)
                return null;

            // pick a random spot around other within distance.
            Point hangSpot;
            int tries = 0;
            const int maxTries = 100;
            do
            {
                hangSpot = otherPosition;
                hangSpot.X += game.Rules.Roll(minDist, maxDist + 1) - game.Rules.Roll(minDist, maxDist + 1);
                hangSpot.Y += game.Rules.Roll(minDist, maxDist + 1) - game.Rules.Roll(minDist, maxDist + 1);
                m_Actor.Location.Map.TrimToBounds(ref hangSpot);
            }
            while (game.Rules.GridDistance(hangSpot, otherPosition) < minDist && ++tries < maxTries);
            
            // try to get close.
            ActorAction bumpAction = BehaviorIntelligentBumpToward(game, hangSpot);
            if (bumpAction != null && IsValidMoveTowardGoalAction(bumpAction) && bumpAction.IsLegal())
            {
                // run if other is running.
                if (other.IsRunning)
                    RunIfPossible(game.Rules);

                // done.
                return bumpAction;
            }

            // fail.
            return null;
        }
        #endregion

        #region Leading
        protected ActorAction BehaviorLeadActor(RogueGame game, Percept target)
        {
            Actor other = target.Percepted as Actor;

            // if can't lead him, fail.
            if (!game.Rules.CanActorTakeLead(m_Actor, other))
                return null;

            // if next to him, lead him.
            if (game.Rules.IsAdjacent(m_Actor.Location.Position, other.Location.Position))
            {
                return new ActionTakeLead(m_Actor, game, other);
            }

            // then try getting closer.
            ActorAction bumpAction = BehaviorIntelligentBumpToward(game, other.Location.Position);
            if (bumpAction != null)
                return bumpAction;

            // failed.
            return null;

        }

        protected ActorAction BehaviorDontLeaveFollowersBehind(RogueGame game, int distance, out Actor target)
        {
            target = null;

            // Scan the group:
            // - Find farthest member of the group.
            // - If at least half the group is close enough we consider the group cohesion to be good enough and do nothing.
            int worstDist = Int32.MinValue;
            Map map = m_Actor.Location.Map;
            Point myPos = m_Actor.Location.Position;
            int closeCount = 0;
            int halfGroup = m_Actor.CountFollowers / 2;
            foreach (Actor a in m_Actor.Followers)
            {
                // ignore actors on different map.
                if (a.Location.Map != map)
                    continue;

                // this actor close enough?
                if (game.Rules.GridDistance(a.Location.Position, myPos) <= distance)
                {
                    // if half close enough, nothing to do.
                    if (++closeCount >= halfGroup)
                        return null;
                }

                // farthest?
                int dist = game.Rules.GridDistance(a.Location.Position, myPos);
                if (target == null || dist > worstDist)
                {
                    target = a;
                    worstDist = dist;
                }
            }

            // try to move toward farther dude.
            if (target == null)
                return null;
            return BehaviorIntelligentBumpToward(game, target.Location.Position);
        }
        #endregion

        #region Communication
        protected ActorAction BehaviorWarnFriends(RogueGame game, List<Percept> friends, Actor nearestEnemy)
        {
            // Never if actor is itself adjacent to the enemy.
            if (game.Rules.IsAdjacent(m_Actor.Location, nearestEnemy.Location))
                return null;

            // Shout if leader is sleeping.
            // (kinda hax, but make followers more useful for players over phone)
            if (m_Actor.HasLeader && m_Actor.Leader.IsSleeping)
                return new ActionShout(m_Actor, game);

            // Shout if we have a friend sleeping.
            foreach (Percept p in friends)
            {
                Actor other = p.Percepted as Actor;
                if (other == null)
                    throw new ArgumentException("percept not an actor");
                if (other == null || other == m_Actor)
                    continue;
                if (!other.IsSleeping)
                    continue;
                if (game.Rules.IsEnemyOf(m_Actor, other))
                    continue;
                if (!game.Rules.IsEnemyOf(other, nearestEnemy))
                    continue;

                // friend sleeping, wake up!
                string shoutText = nearestEnemy == null ? String.Format("Wake up {0}!", other.Name) : String.Format("Wake up {0}! {1} sighted!", other.Name, nearestEnemy.Name);
                return new ActionShout(m_Actor, game, shoutText);
            }

            // no one to alert.
            return null;
        }

        protected ActorAction BehaviorTellFriendAboutPercept(RogueGame game, Percept percept)
        {
            // get an adjacent awake friend, if none nothing to do.
            Map map = m_Actor.Location.Map;
            List<Point> friends = map.FilterAdjacentInMap(m_Actor.Location.Position,
                (pt) =>
                {
                    Actor otherActor = map.GetActorAt(pt);
                    if (otherActor == null)
                        return false;
                    if (otherActor.IsSleeping)
                        return false;
                    if (game.Rules.IsEnemyOf(m_Actor, otherActor))
                        return false;
                    return true;
                });
            if (friends == null || friends.Count == 0)
                return null;

            // pick a random friend.
            Actor friend = map.GetActorAt(friends[game.Rules.Roll(0, friends.Count)]);

            // make message.
            string tellMsg;
            string whereMsg = MakeCentricLocationDirection(game, m_Actor.Location, percept.Location);
            string timeMsg = String.Format("{0} ago", WorldTime.MakeTimeDurationMessage(m_Actor.Location.Map.LocalTime.TurnCounter - percept.Turn));
            if (percept.Percepted is Actor)
            {
                Actor who = percept.Percepted as Actor;
                tellMsg = String.Format("I saw {0} {1} {2}.", who.Name, whereMsg, timeMsg);
            }
            else if (percept.Percepted is Inventory)
            {
                // tell about a random item from the pile.
                // warning: the items might have changed since then, the AI cheats a bit by knowing which items are there now.
                Inventory inv = percept.Percepted as Inventory;
                if (inv.IsEmpty)
                    return null;    // all items were taken or destroyed.
                Item what = inv[game.Rules.Roll(0, inv.CountItems)];

                // ignore worthless items (eg: don't spam about stupid items like planks)
                if (!IsItemWorthTellingAbout(what))
                    return null;

                // ignore stacks that are probably in plain view of the friend.
                int friendFOVRange = game.Rules.ActorFOV(friend, map.LocalTime, game.Session.World.Weather);
                if (percept.Location.Map == friend.Location.Map &&
                    game.Rules.StdDistance(percept.Location.Position, friend.Location.Position) <= 2 + friendFOVRange)
                {
                    return null;
                }

                // do it.
                tellMsg = String.Format("I saw {0} {1} {2}.", what.AName, whereMsg, timeMsg);
            }
            else if (percept.Percepted is String)
            {
                String raidDesc = percept.Percepted as String;
                tellMsg = String.Format("I heard {0} {1} {2}!", raidDesc, whereMsg, timeMsg);
            }
            else
                throw new InvalidOperationException("unhandled percept.Percepted type");

            // tell friend - if legal.
            ActionSay say = new ActionSay(m_Actor, game, friend, tellMsg, RogueGame.Sayflags.NONE);
            if (say.IsLegal())
                return say;
            else
                return null;
        }
        
        #endregion
    }
}