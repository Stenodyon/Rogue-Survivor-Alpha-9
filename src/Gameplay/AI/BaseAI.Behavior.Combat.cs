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
    /* This part of BaseAI provides combat behaviors */
    abstract partial class BaseAI
    {
        #region Melee attack
        protected ActorAction BehaviorMeleeAttack(RogueGame game, Percept target)
        {
            Actor targetActor = target.Percepted as Actor;
            if (targetActor == null)
                throw new ArgumentException("percepted is not an actor");

            // if illegal cant.
            if (!game.Rules.CanActorMeleeAttack(m_Actor, targetActor))
                return null;

            // melee!
            return new ActionMeleeAttack(m_Actor, game, targetActor);
        }
        #endregion

        #region Ranged attack
        protected ActorAction BehaviorRangedAttack(RogueGame game, Percept target)
        {
            Actor targetActor = target.Percepted as Actor;
            if (targetActor == null)
                throw new ArgumentException("percepted is not an actor");

            // if illegal cant.
            if (!game.Rules.CanActorFireAt(m_Actor,targetActor))
                return null;

            // fire!
            return new ActionRangedAttack(m_Actor, game, targetActor);
        }
        #endregion

        #region Breaking objects
        protected ActorAction BehaviorAttackBarricade(RogueGame game)
        {
            // find barricades.
            Map map = m_Actor.Location.Map;
            List<Point> adjBarricades = map.FilterAdjacentInMap(m_Actor.Location.Position,
                (pt) =>
                {
                    DoorWindow door = map.GetMapObjectAt(pt) as DoorWindow;
                    return (door != null && door.IsBarricaded);
                });

            // if none, fail.
            if (adjBarricades == null)
                return null;

            // try to attack one at random.
            DoorWindow randomBarricade = map.GetMapObjectAt(adjBarricades[game.Rules.Roll(0, adjBarricades.Count)]) as DoorWindow;
            ActionBreak attackBarricade = new ActionBreak(m_Actor, game, randomBarricade);
            if (attackBarricade.IsLegal())
                return attackBarricade;

            // nope :(
            return null;
        }

        protected ActorAction BehaviorAssaultBreakables(RogueGame game, HashSet<Point> fov)
        {
            // find all barricades & breakables in fov.
            Map map = m_Actor.Location.Map;
            List<Percept> breakables = null;
            foreach (Point pt in fov)
            {
                MapObject mapObj = map.GetMapObjectAt(pt);
                if (mapObj == null)
                    continue;
                if (!mapObj.IsBreakable)
                    continue;
                if (breakables == null)
                    breakables = new List<Percept>();
                breakables.Add(new Percept(mapObj, map.LocalTime.TurnCounter, new Location(map, pt)));
            }

            // if nothing to assault, fail.
            if (breakables == null)
                return null;

            // get nearest.
            Percept nearest = FilterNearest(game, breakables);

            // if adjacent, try to break it.
            if (game.Rules.IsAdjacent(m_Actor.Location.Position, nearest.Location.Position))
            {
                ActionBreak breakIt = new ActionBreak(m_Actor, game, nearest.Percepted as MapObject);
                if (breakIt.IsLegal())
                    return breakIt;
                else
                    // illegal, don't bother with it.
                    return null;
            }

            // not adjacent, try to get there.
            return BehaviorIntelligentBumpToward(game, nearest.Location.Position);
        }
        #endregion

        #region Charging enemy
        protected ActorAction BehaviorChargeEnemy(RogueGame game, Percept target)
        {
            // try melee attack first.
            ActorAction attack = BehaviorMeleeAttack(game, target);
            if (attack != null)
                return attack;

            Actor enemy = target.Percepted as Actor;

            // if we are tired and next to enemy, use med or rest to recover our STA for the next attack.
            if (game.Rules.IsActorTired(m_Actor) && game.Rules.IsAdjacent(m_Actor.Location, target.Location))
            {
                // meds?
                ActorAction useMed = BehaviorUseMedecine(game, 0, 1, 0, 0, 0);
                if (useMed != null)
                    return useMed;

                // rest!
                return new ActionWait(m_Actor, game);
            }

            // then try getting closer.
            ActorAction bumpAction = BehaviorIntelligentBumpToward(game, target.Location.Position);
            if (bumpAction != null)
            {
                // do we rush? 
                // we want to rush if enemy has a range advantage, we want to get closer asap.
                if (m_Actor.CurrentRangedAttack.Range < enemy.CurrentRangedAttack.Range)
                    RunIfPossible(game.Rules);

                // chaaarge!
                return bumpAction;
            }

            // failed.
            return null;
        }
        #endregion

        #region Fight or Flee
        /// <summary>
        /// 
        /// </summary>
        /// <param name="game"></param>
        /// <param name="enemies"></param>
        /// <param name="hasVisibleLeader"></param>
        /// <param name="isLeaderFighting"></param>
        /// <param name="courage"></param>
        /// <param name="emotes">0 = flee; 1 = trapped; 2 = charge</param>
        /// <returns></returns>
        protected ActorAction BehaviorFightOrFlee(RogueGame game, List<Percept> enemies, bool hasVisibleLeader, bool isLeaderFighting, ActorCourage courage,
            string[] emotes)
        {
            Percept nearestEnemy = FilterNearest(game, enemies);

            bool decideToFlee;
            bool doRun = false;  // don't run by default.

            Actor enemy = nearestEnemy.Percepted as Actor;

            // Cases that are a no brainer, in this order:
            // 1. Always fight if he has a ranged weapon.
            // 2. Always fight if law enforcer vs murderer.
            // 3. Always flee melee if tired.

            // 1. Always fight if enemy has ranged weapon.
            // if we are here, it means we can't shoot him, cause firing behavior has priority.
            // so we want to get a chance at melee a shooting enemy.
            if (HasEquipedRangedWeapon(enemy))
                decideToFlee = false;
            // 2. Always fight if law enforcer vs murderer.
            // do our duty.
            else if (m_Actor.Model.Abilities.IsLawEnforcer && enemy.MurdersCounter > 0)
                decideToFlee = false;
            // 3. Always flee melee if tired.
            else if (game.Rules.IsActorTired(m_Actor) && game.Rules.IsAdjacent(m_Actor.Location, enemy.Location))
                decideToFlee = true;
            // Case need more analysis.
            else
            {
                if (m_Actor.Leader != null)
                {
                    //////////////////////////
                    // Fighting with a leader.
                    //////////////////////////
                    #region
                    switch (courage)
                    {
                        case ActorCourage.COWARD:
                            // always flee and run.
                            decideToFlee = true;
                            doRun = true;
                            break;

                        case ActorCourage.CAUTIOUS:
                            // kite.
                            decideToFlee = WantToEvadeMelee(game, m_Actor, courage, enemy);
                            doRun = !HasSpeedAdvantage(game, m_Actor, enemy);
                            break;

                        case ActorCourage.COURAGEOUS:
                            // fight if leader is fighting.
                            // otherwise kite.
                            if (isLeaderFighting)
                                decideToFlee = false;
                            else
                            {
                                decideToFlee = WantToEvadeMelee(game, m_Actor, courage, enemy);
                                doRun = !HasSpeedAdvantage(game, m_Actor, enemy);
                            }
                            break;

                        default:
                            throw new ArgumentOutOfRangeException("unhandled courage");
                    }
                    #endregion
                }
                else
                {
                    ////////////////////////
                    // Leaderless fighting.
                    ////////////////////////
                    #region
                    switch (courage)
                    {
                        case ActorCourage.COWARD:
                            // always flee and run.
                            decideToFlee = true;
                            doRun = true;
                            break;

                        case ActorCourage.CAUTIOUS:
                        case ActorCourage.COURAGEOUS:
                            // kite.
                            decideToFlee = WantToEvadeMelee(game, m_Actor, courage, enemy);
                            doRun = !HasSpeedAdvantage(game, m_Actor, enemy);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("unhandled courage");
                    }
                    #endregion
                }
            }

            // flee or fight?
            if (decideToFlee)
            {
                #region Flee
                ////////////////////////////////////////////////////////////////////////////////////////
                // Try to:
                // 1. Close door between me and the enemy if he can't open it right after we closed it.
                // 2. Barricade door between me and the enemy.
                // 3. Use exit?
                // 4. Use medecine?
                // 5. Walk/run away.
                // 6. Blocked, turn to fight.
                ////////////////////////////////////////////////////////////////////////////////////////

                // emote?
                if (m_Actor.Model.Abilities.CanTalk && game.Rules.RollChance(EMOTE_FLEE_CHANCE))
                    game.DoEmote(m_Actor, String.Format("{0} {1}!", emotes[0], enemy.Name));

                // 1. Close door between me and the enemy if he can't open it right after we closed it.
                #region
                if (m_Actor.Model.Abilities.CanUseMapObjects)
                {
                    ChoiceEval<Direction> closeDoorBetweenDirection = Choose<Direction>(game, Direction.COMPASS_LIST,
                        (dir) =>
                        {
                            Point pos = m_Actor.Location.Position + dir;
                            DoorWindow door = m_Actor.Location.Map.GetMapObjectAt(pos) as DoorWindow;
                            if (door == null)
                                return false;
                            if (!IsBetween(game, m_Actor.Location.Position, pos, enemy.Location.Position))
                                return false;
                            if (!game.Rules.IsClosableFor(m_Actor, door))
                                return false;
                            if (game.Rules.GridDistance(pos, enemy.Location.Position) == 1 && game.Rules.IsClosableFor(enemy, door))
                                return false;
                            return true;
                        },
                        (dir) =>
                        {
                            return game.Rules.Roll(0, 666);  // random eval, all things being equal.
                        },
                        (a, b) => a > b);
                    if (closeDoorBetweenDirection != null)
                    {
                        return new ActionCloseDoor(m_Actor, game, m_Actor.Location.Map.GetMapObjectAt(m_Actor.Location.Position + closeDoorBetweenDirection.Choice) as DoorWindow);
                    }
                }
                #endregion

                // 2. Barricade door between me and the enemy.
                #region
                if (m_Actor.Model.Abilities.CanBarricade)
                {
                    ChoiceEval<Direction> barricadeDoorBetweenDirection = Choose<Direction>(game, Direction.COMPASS_LIST,
                        (dir) =>
                        {
                            Point pos = m_Actor.Location.Position + dir;
                            DoorWindow door = m_Actor.Location.Map.GetMapObjectAt(pos) as DoorWindow;
                            if (door == null)
                                return false;
                            if (!IsBetween(game, m_Actor.Location.Position, pos, enemy.Location.Position))
                                return false;
                            if (!game.Rules.CanActorBarricadeDoor(m_Actor, door))
                                return false;
                            return true;
                        },
                        (dir) =>
                        {
                            return game.Rules.Roll(0, 666);  // random eval, all things being equal.
                        },
                        (a, b) => a > b);
                    if (barricadeDoorBetweenDirection != null)
                    {
                        return new ActionBarricadeDoor(m_Actor, game, m_Actor.Location.Map.GetMapObjectAt(m_Actor.Location.Position + barricadeDoorBetweenDirection.Choice) as DoorWindow);
                    }
                }
                #endregion

                // 3. Use exit?
                #region
                if (m_Actor.Model.Abilities.AI_CanUseAIExits &&
                    game.Rules.RollChance(FLEE_THROUGH_EXIT_CHANCE))
                {
                    ActorAction useExit = BehaviorUseExit(game, UseExitFlags.NONE);
                    if (useExit != null)
                    {
                        bool doUseExit = true;

                        // Exception : if follower use exit only if leading to our leader.
                        // we don't want to leave our leader behind (mostly annoying for the player).
                        if (m_Actor.HasLeader)
                        {
                            Exit exitThere = m_Actor.Location.Map.GetExitAt(m_Actor.Location.Position);
                            if (exitThere != null) // well. who knows?
                                doUseExit = (m_Actor.Leader.Location.Map == exitThere.ToMap);
                        }

                        // do it?
                        if (doUseExit)
                        {
                            m_Actor.Activity = Activity.FLEEING;
                            return useExit;
                        }
                    }
                }
                #endregion

                // 4. Use medecine?
                #region
                // when to use medecine? only when fighting vs an unranged enemy and not in contact.
                ItemRangedWeapon rngEnemy = enemy.GetEquippedWeapon() as ItemRangedWeapon;
                if (rngEnemy == null && !game.Rules.IsAdjacent(m_Actor.Location, enemy.Location))
                {
                    ActorAction medAction = BehaviorUseMedecine(game, 2, 2, 1, 0, 0);
                    if (medAction != null)
                    {
                        m_Actor.Activity = Activity.FLEEING;
                        return medAction;
                    }
                }
                #endregion

                // 5. Walk/run away.
                #region
                ActorAction bumpAction = BehaviorWalkAwayFrom(game, enemies);
                if (bumpAction != null)
                {
                    if (doRun)
                        RunIfPossible(game.Rules);
                    m_Actor.Activity = Activity.FLEEING;
                    return bumpAction;
                }
                #endregion

                // 6. Blocked, turn to fight.
                #region
                if (bumpAction == null)
                {
                    // fight!
                    if (IsAdjacentToEnemy(game, enemy))
                    {
                        // emote?
                        if (m_Actor.Model.Abilities.CanTalk && game.Rules.RollChance(EMOTE_FLEE_TRAPPED_CHANCE))
                            game.DoEmote(m_Actor, emotes[1]);

                        return BehaviorMeleeAttack(game, nearestEnemy);
                    }
                }
                #endregion

                #endregion
            }
            else
            {
                #region Fight
                ActorAction attackAction = BehaviorChargeEnemy(game, nearestEnemy);
                if (attackAction != null)
                {
                    // emote?
                    if (m_Actor.Model.Abilities.CanTalk && game.Rules.RollChance(EMOTE_CHARGE_CHANCE))
                        game.DoEmote(m_Actor, String.Format("{0} {1}!", emotes[2], enemy.Name));

                    // chaaarge!
                    m_Actor.Activity = Activity.FIGHTING;
                    m_Actor.TargetActor = nearestEnemy.Percepted as Actor;
                    return attackAction;
                }
                #endregion
            }

            // nope.
            return null;
        }
        #endregion

        #region Explosives
        protected ActorAction BehaviorFleeFromExplosives(RogueGame game, List<Percept> itemStacks)
        {
            // if no items in view, don't bother.
            if (itemStacks == null || itemStacks.Count == 0)
                return null;

            // filter stacks that have primed explosives.
            List<Percept> primedExplosives = Filter(game, itemStacks, 
                (p) =>
                {
                    Inventory stack = p.Percepted as Inventory;
                    if (stack == null || stack.IsEmpty)
                        return false;
                    foreach (Item it in stack.Items)
                    {
                        ItemPrimedExplosive explosive = it as ItemPrimedExplosive;
                        if (explosive == null)
                            continue;
                        // found a primed explosive.
                        return true;
                    }
                    // no primed explosive in this stack.
                    return false;
                });

            // if no primed explosive in sight, no worries.
            if (primedExplosives == null || primedExplosives.Count == 0)
                return null;

            // run away from primed explosives!
            ActorAction runAway = BehaviorWalkAwayFrom(game, primedExplosives);
            if (runAway == null)
                return null;
            RunIfPossible(game.Rules);
            return runAway;
        }

        protected ActorAction BehaviorThrowGrenade(RogueGame game, HashSet<Point> fov, List<Percept> enemies)
        {
            // don't bother if no enemies.
            if (enemies == null || enemies.Count == 0)
                return null;

            // only throw if enough enemies.
            if (enemies.Count < 3)
                return null;

            // don't bother if no grenade in inventory.
            Inventory inv = m_Actor.Inventory;
            if (inv == null || inv.IsEmpty)
                return null;
            ItemGrenade grenade = GetFirstGrenade((it) => !IsItemTaboo(it));
            if (grenade == null)
                return null;
            ItemGrenadeModel model = grenade.Model as ItemGrenadeModel;

            // find the best throw point : a spot with many enemies around and no friend to hurt.
            #region
            int maxThrowRange = game.Rules.ActorMaxThrowRange(m_Actor, model.MaxThrowDistance);
            Point? bestSpot = null;
            int bestSpotScore = 0;
            foreach (Point pt in fov)
            {
                // never throw within blast radius - don't suicide ^^
                if (game.Rules.GridDistance(m_Actor.Location.Position, pt) <= model.BlastAttack.Radius)
                    continue;

                // if we can't throw there, don't bother.
                if (game.Rules.GridDistance(m_Actor.Location.Position, pt) > maxThrowRange)
                    continue;
                if (!LOS.CanTraceThrowLine(m_Actor.Location, pt, maxThrowRange, null))
                    continue;

                // compute interest of throwing there.
                // - pro: number of enemies within blast radius.
                // - cons: friend in radius.
                // don't bother checking for blast wave actuallly reaching the targets.
                int score = 0;
                for (int x = pt.X - model.BlastAttack.Radius; x <= pt.X + model.BlastAttack.Radius; x++)
                    for (int y = pt.Y - model.BlastAttack.Radius; y <= pt.Y + model.BlastAttack.Radius; y++)
                    {
                        if (!m_Actor.Location.Map.IsInBounds(x, y))
                            continue;
                        Actor otherActor = m_Actor.Location.Map.GetActorAt(x, y);
                        if (otherActor == null)
                            continue;
                        if (otherActor == m_Actor)
                            continue;
                        int blastDistToTarget = game.Rules.GridDistance(pt, otherActor.Location.Position);
                        if (blastDistToTarget > model.BlastAttack.Radius)
                            continue;

                        // other actor within blast radius.
                        // - if friend, abort and never throw there.
                        // - if enemy, increase score.
                        if (game.Rules.IsEnemyOf(m_Actor, otherActor))
                        {
                            // score = damage inflicted vs target toughness(max hp).
                            // -> means it is better to hurt badly one big enemy than a few scratch on a group of weaklings.
                            int value = game.Rules.BlastDamage(blastDistToTarget, model.BlastAttack) * game.Rules.ActorMaxHPs(otherActor);
                            score += value;
                        }
                        else
                        {
                            score = -1;
                            break;
                        }
                    }

                // if negative score (eg: friends get hurt), don't throw.
                if (score <= 0)
                    continue;

                // possible spot. best one?
                if (bestSpot == null || score > bestSpotScore)
                {
                    bestSpot = pt;
                    bestSpotScore = score;
                }
            }
            #endregion

            // if no throw point, don't.
            if (bestSpot == null)
                return null;

            // equip then throw.
            if (!grenade.IsEquipped)
            {
                Item otherEquipped = m_Actor.GetEquippedWeapon();
                if (otherEquipped != null)
                    return new ActionUnequipItem(m_Actor, game, otherEquipped);
                else
                    return new ActionEquipItem(m_Actor, game, grenade);
            }
            else
            {
                ActorAction throwAction = new ActionThrowGrenade(m_Actor, game, bestSpot.Value);
                if (!throwAction.IsLegal())
                    return null;
                return throwAction;
            }
        }
        #endregion
    }
}