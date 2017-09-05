using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using djack.RogueSurvivor.Data;
using djack.RogueSurvivor.Engine;
using djack.RogueSurvivor.Engine.Actions;
using djack.RogueSurvivor.Engine.AI;
using djack.RogueSurvivor.Engine.Items;
using djack.RogueSurvivor.Engine.MapObjects;
using djack.RogueSurvivor.Gameplay.AI.Sensors;

namespace djack.RogueSurvivor.Gameplay.AI
{
    [Serializable]
    abstract partial class BaseAI : AIController
    {
        #region Types
        protected class ChoiceEval<_T_>
        {
            public _T_ Choice { get; private set; }
            public float Value { get; private set; }

            public ChoiceEval(_T_ choice, float value)
            {
                this.Choice = choice;
                this.Value = value;
            }

            public override string ToString()
            {
                return String.Format("ChoiceEval({0}; {1:F})", (this.Choice == null ? "NULL" : this.Choice.ToString()), this.Value);
            }
        }
        #endregion

        #region Constants
        const int FLEE_THROUGH_EXIT_CHANCE = 50;

        const int EMOTE_GRAB_ITEM_CHANCE = 30;
        const int EMOTE_FLEE_CHANCE = 30;
        const int EMOTE_FLEE_TRAPPED_CHANCE = 50;
        const int EMOTE_CHARGE_CHANCE = 30;

        const float MOVE_DISTANCE_PENALTY = 0.42f;  // slightly > to diagonal distance (sqrt(2))

        const float LEADER_LOF_PENALTY = 1;
        #endregion

        #region Fields
        ActorOrder m_Order;
        ActorDirective m_Directive;
        Location m_prevLocation;
        List<Item> m_TabooItems;    // list is better than dictionary since we expect it to be very small.
        List<Point> m_TabooTiles;
        List<Actor> m_TabooTrades;
        #endregion

        #region Properties
        public override ActorOrder Order
        {
            get { return m_Order; }
        }

        public override ActorDirective Directives
        {
            get 
            {
                if (m_Directive == null)
                    m_Directive = new ActorDirective();
                return m_Directive; 
            }
            set { m_Directive = value; }
        }

        protected Location PrevLocation
        {
            get { return m_prevLocation; }
        }

        protected List<Item> TabooItems
        {
            get { return m_TabooItems; }
        }

        protected List<Point> TabooTiles
        {
            get { return m_TabooTiles; }
        }

        protected List<Actor> TabooTrades
        {
            get { return m_TabooTrades; }
        }
        #endregion

        #region AIController
        public override void TakeControl(Actor actor)
        {
            base.TakeControl(actor);

            CreateSensors();

            m_TabooItems = null;
            m_TabooTiles = null;
            m_TabooTrades = null;
        }

        public override void SetOrder(ActorOrder newOrder)
        {
            m_Order = newOrder;
        }

        public override ActorAction GetAction(RogueGame game)
        {
            /////////////////////////
            // 1. Update sensors.
            // 2. Issue action.
            /////////////////////////

            // 2. Update sensors.
            List<Percept> percepts = UpdateSensors(game);

            // 3. Issue action.
            if (m_prevLocation.Map == null)
                m_prevLocation = m_Actor.Location;
            m_Actor.TargetActor = null;
            ActorAction bestAction = SelectAction(game, percepts);
            m_prevLocation = m_Actor.Location;
            if (bestAction == null)
            {
                m_Actor.Activity = Activity.IDLE;
                return new ActionWait(m_Actor, game);
            }
            return bestAction;
        }
        #endregion

        #region Strategy followed in GetAction
        protected abstract void CreateSensors();
        protected abstract List<Percept> UpdateSensors(RogueGame game);
        protected abstract ActorAction SelectAction(RogueGame game, List<Percept> percepts);
        #endregion

        #region Common behaviors
        #region Resting, Eating & Sleeping
        protected ActorAction BehaviorRestIfTired(RogueGame game)
        {
            // if not tired, don't.
            if (m_Actor.StaminaPoints >= Rules.STAMINA_MIN_FOR_ACTIVITY)
                return null;

            // tired, rest.
            return new ActionWait(m_Actor, game);
        }

        protected ActorAction BehaviorEat(RogueGame game)
        {
            // find best edible eat.
            Item it = GetBestEdibleItem(game);
            if (it == null)
                return null;

            // i can haz it?
            if (!game.Rules.CanActorUseItem(m_Actor, it))
                return null;
            
            // eat it!
            return new ActionUseItem(m_Actor, game, it);
        }

        protected ActorAction BehaviorSleep(RogueGame game, HashSet<Point> FOV)
        {
            // can?
            if (!game.Rules.CanActorSleep(m_Actor))
                return null;

            // if next to a door/window, try moving away from it.
            Map map = m_Actor.Location.Map;
            if (map.HasAnyAdjacentInMap(m_Actor.Location.Position, (pt) => map.GetMapObjectAt(pt) is DoorWindow))
            {
                // wander where there is no door/window and not adjacent to a door window.
                ActorAction wanderAwayFromDoor = BehaviorWander(game, 
                    (loc) => map.GetMapObjectAt(loc.Position) as DoorWindow == null && !map.HasAnyAdjacentInMap(loc.Position, (pt) => loc.Map.GetMapObjectAt(pt) is DoorWindow));
                if (wanderAwayFromDoor != null)
                    return wanderAwayFromDoor;
                // no good spot, just try normal sleep behavior.
            }

            // sleep on a couch.
            if (game.Rules.IsOnCouch(m_Actor))
            {
                return new ActionSleep(m_Actor, game);
            }
            // find nearest couch.
            Point? couchPos = null;
            float nearestDist = float.MaxValue;
            foreach (Point p in FOV)
            {
                MapObject mapObj = map.GetMapObjectAt(p);
                if (mapObj != null && mapObj.IsCouch && map.GetActorAt(p) == null)
                {
                    float dist = game.Rules.StdDistance(m_Actor.Location.Position, p);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        couchPos = p;
                    }
                }
            }
            // if we have a couch, try to get there.
            if (couchPos != null)
            {
                ActorAction moveThere = BehaviorIntelligentBumpToward(game, couchPos.Value);
                if (moveThere != null)
                {
                    return moveThere;
                }
            }

            // no couch or can't move there, sleep there.
            return new ActionSleep(m_Actor, game);
        }
        #endregion

        #region Pushing
        protected ActorAction BehaviorPushNonWalkableObject(RogueGame game)
        {
            // check ability.
            if (!game.Rules.HasActorPushAbility(m_Actor))
                return null;

            // find adjacent pushables that are blocking for us.
            Map map = m_Actor.Location.Map;
            List<Point> adjPushables = map.FilterAdjacentInMap(m_Actor.Location.Position,
                (pt) =>
                {
                    MapObject obj = map.GetMapObjectAt(pt);
                    if (obj == null)
                        return false;
                    // ignore if we can walk through it.
                    if (obj.IsWalkable)
                        return false;
                    // finally only if we can push it.
                    return game.Rules.CanActorPush(m_Actor, obj);
                });

            // if none, fail.
            if (adjPushables == null)
                return null;

            // try to push one at random in a random direction.
            MapObject randomPushable = map.GetMapObjectAt(adjPushables[game.Rules.Roll(0, adjPushables.Count)]);
            ActionPush pushIt = new ActionPush(m_Actor, game, randomPushable, game.Rules.RollDirection());
            if (pushIt.IsLegal())
                return pushIt;

            // nope :(
            return null;
        }
        #endregion

        #region Healing & Entertainment
        protected ActorAction BehaviorUseMedecine(RogueGame game, int factorHealing, int factorStamina, int factorSleep, int factorCure, int factorSan)
        {
            // if no items, don't bother.
            Inventory inv = m_Actor.Inventory;
            if (inv == null || inv.IsEmpty)
                return null;

            // check needs.
            bool needHP = m_Actor.HitPoints < game.Rules.ActorMaxHPs(m_Actor);
            bool needSTA = game.Rules.IsActorTired(m_Actor);
            bool needSLP = m_Actor.Model.Abilities.HasToSleep && WouldLikeToSleep(game, m_Actor);
            bool needCure = m_Actor.Infection > 0;
            bool needSan = m_Actor.Model.Abilities.HasSanity && m_Actor.Sanity < (int)(0.75f * game.Rules.ActorMaxSanity(m_Actor));
            
            // if no need, don't.
            if (!needHP && !needSTA && !needSLP && !needCure && !needSan)
                return null;

            // list meds items.
            List<ItemMedicine> medItems = inv.GetItemsByType<ItemMedicine>();
            if (medItems == null)
                return null;

            // use best item.
            ChoiceEval<ItemMedicine> bestMedChoice = Choose<ItemMedicine>(game, medItems,
                (it) =>
                {
                    return true;
                },
                (it) =>
                {
                    int score = 0;
                    if (needHP) score += factorHealing * it.Healing;
                    if (needSTA) score += factorStamina * it.StaminaBoost;
                    if (needSLP) score += factorSleep * it.SleepBoost;
                    if (needCure) score += factorCure * it.InfectionCure;
                    if (needSan) score += factorSan * it.SanityCure;
                    return score;
                },
                (a, b) => a > b);

            // if no suitable items or best item scores zero, do not want!
            if (bestMedChoice == null || bestMedChoice.Value <= 0)
                return null;
                
            // use med.
            return new ActionUseItem(m_Actor, game, bestMedChoice.Choice);
        }
        
        protected ActorAction BehaviorUseEntertainment(RogueGame game)
        {
            Inventory inv = m_Actor.Inventory;
            if (inv.IsEmpty) return null;

            // use first entertainment item available.
            ItemEntertainment ent = (ItemEntertainment)inv.GetFirstByType(typeof(ItemEntertainment));
            if (ent == null) return null;

            if (!game.Rules.CanActorUseItem(m_Actor, ent))
                return null;

            return new ActionUseItem(m_Actor, game, ent);
        }

        protected ActorAction BehaviorDropBoringEntertainment(RogueGame game)
        {
            Inventory inv = m_Actor.Inventory;
            if (inv.IsEmpty) return null;

            foreach (Item it in inv.Items)
            {
                if (it is ItemEntertainment && m_Actor.IsBoredOf(it))
                    return new ActionDropItem(m_Actor, game, it);
            }

            return null;
        }

        #endregion

        #region Tracking scents
        protected ActorAction BehaviorTrackScent(RogueGame game, List<Percept> scents)
        {
            // if no scents, nothing to do.
            if (scents == null || scents.Count == 0)
                return null;

            // get highest scent.
            Percept best = FilterStrongestScent(game, scents);

            // 2 cases:
            // 1. Standing on best scent.
            // or
            // 2. Best scent is adjacent.
            #region
            Map map = m_Actor.Location.Map;
            // 1. Standing on best scent.
            if (m_Actor.Location.Position == best.Location.Position)
            {
                // if exit there and can and want to use it, do it.
                Exit exitThere = map.GetExitAt(m_Actor.Location.Position);
                if (exitThere != null && m_Actor.Model.Abilities.AI_CanUseAIExits)
                    return BehaviorUseExit(game, UseExitFlags.ATTACK_BLOCKING_ENEMIES | UseExitFlags.BREAK_BLOCKING_OBJECTS);
                else
                    return null;
            }

            // 2. Best scent is adjacent.
            // try to bump there.
            ActorAction bump = BehaviorIntelligentBumpToward(game, best.Location.Position);
            if (bump != null)
                return bump;
            #endregion

            // nope.
            return null;
        }
        #endregion

        #region Exploring
        protected ActorAction BehaviorExplore(RogueGame game, ExplorationData exploration)
        {
            // prepare data.
            Direction prevDirection = Direction.FromVector(m_Actor.Location.Position.X - m_prevLocation.Position.X, m_Actor.Location.Position.Y - m_prevLocation.Position.Y);
            bool imStarvingOrCourageous = game.Rules.IsActorStarving(m_Actor) || Directives.Courage == ActorCourage.COURAGEOUS;

            // eval all adjacent tiles for exploration utility and get the best one.
            ChoiceEval<Direction> chooseExploreDir = Choose<Direction>(game,
                Direction.COMPASS_LIST,
                (dir) =>
                {
                    Location next = m_Actor.Location + dir;
                    if (exploration.HasExplored(next))
                        return false;
                    return IsValidMoveTowardGoalAction(game.Rules.IsBumpableFor(m_Actor, game, next));
                },
                (dir) =>
                {
                    Location next = m_Actor.Location + dir;
                    Map map = next.Map;
                    Point pos = next.Position;

                    // intelligent NPC: forbid stepping on deadly traps, unless starving or courageous (desperate).
                    if (m_Actor.Model.Abilities.IsIntelligent && !imStarvingOrCourageous)
                    {
                        int trapsDamage = ComputeTrapsMaxDamage(map, pos);
                        if (trapsDamage >= m_Actor.HitPoints)
                            return float.NaN;
                    }

                    // Heuristic scoring:
                    // 1st Prefer unexplored zones.
                    // 2nd Prefer unexplored locs.
                    // 3rd Prefer doors and barricades (doors/windows, pushables)
                    // 4th Punish stepping on activated traps.
                    // 5th Prefer inside during the night vs outside during the day.
                    // 6th Prefer continue in same direction.
                    // 7th Small randomness.
                    const int EXPLORE_ZONES = 1000;
                    const int EXPLORE_LOCS = 500;
                    const int EXPLORE_BARRICADES = 100;
                    const int AVOID_TRAPS = -50;
                    const int EXPLORE_INOUT = 50;
                    const int EXPLORE_DIRECTION = 25;
                    const int EXPLORE_RANDOM = 10;

                    int score = 0;
                    // 1st Prefer unexplored zones.
                    if (!exploration.HasExplored(map.GetZonesAt(pos.X, pos.Y)))
                        score += EXPLORE_ZONES;
                    // 2nd Prefer unexplored locs.
                    if (!exploration.HasExplored(next))
                        score += EXPLORE_LOCS;
                    // 3rd Prefer doors and barricades (doors/windows, pushables)
                    MapObject mapObj = map.GetMapObjectAt(pos);
                    if (mapObj != null && (mapObj.IsMovable || mapObj is DoorWindow))
                        score += EXPLORE_BARRICADES;
                    // 4th Punish stepping on activated traps.
                    if (IsAnyActivatedTrapThere(map, pos))
                        score += AVOID_TRAPS;
                    // 5th Prefer inside during the night vs outside during the day.
                    bool isInside = map.GetTileAt(pos.X, pos.Y).IsInside;
                    if (isInside)
                    {
                        if (map.LocalTime.IsNight)
                            score += EXPLORE_INOUT;
                    }
                    else
                    {
                        if (!map.LocalTime.IsNight)
                            score += EXPLORE_INOUT;
                    }
                    // 6th Prefer continue in same direction.
                    if (dir == prevDirection)
                        score += EXPLORE_DIRECTION;
                    // 7th Small randomness.
                    score += game.Rules.Roll(0, EXPLORE_RANDOM);

                    // done.
                    return score;
                },
                (a, b) => !float.IsNaN(a) && a > b);

            if (chooseExploreDir != null)
                return new ActionBump(m_Actor, game, chooseExploreDir.Choice);
            else
                return null;
        }
        #endregion

        #region Sprays
        protected ActorAction BehaviorUseStenchKiller(RogueGame game)
        {
            ItemSprayScent spray = m_Actor.GetEquippedItem(DollPart.LEFT_HAND) as ItemSprayScent;

            // if no spray or empty, nope.
            if (spray == null)
                return null;
            if (spray.SprayQuantity <= 0)
                return null;
            // if not proper odor, nope.
            ItemSprayScentModel model = spray.Model as ItemSprayScentModel;
            if (model.Odor != Odor.PERFUME_LIVING_SUPRESSOR)
                return null;

            // spot must be interesting to spray.
            if (!IsGoodStenchKillerSpot(game, m_Actor.Location.Map, m_Actor.Location.Position))
                return null;

            // good spot, try to do it.
            ActionUseItem sprayIt = new ActionUseItem(m_Actor, game, spray);
            if (sprayIt.IsLegal())
                return sprayIt;

            // nope.
            return null;
        }

        protected bool IsGoodStenchKillerSpot(RogueGame game, Map map, Point pos)
        {
            // 1. Don't spray at an already sprayed spot.
            // 2. Spray in a good position:
            //    2.1 entering or leaving a building.
            //    2.2 a door/window.
            //    2.3 an exit.

            // 1. Don't spray at an already sprayed spot.
            if (map.GetScentByOdorAt(Odor.PERFUME_LIVING_SUPRESSOR, pos) > 0)
                return false;

            // 2. Spray in a good position:
            
            //    2.1 entering or leaving a building.
            bool wasInside = m_prevLocation.Map.GetTileAt(m_prevLocation.Position).IsInside;
            bool isInside  = map.GetTileAt(pos).IsInside;
            if (wasInside != isInside)
                return true;
            //    2.2 a door/window.
            MapObject objThere = map.GetMapObjectAt(pos);
            if (objThere != null && objThere is DoorWindow)
                return true;
            //    2.3 an exit.
            if (map.GetExitAt(pos) != null)
                return true;

            // nope.
            return false;
        }
        #endregion

        #region Law enforcement
        protected ActorAction BehaviorEnforceLaw(RogueGame game, List<Percept> percepts, out Actor target)
        {
            target = null;

            // sanity checks.
            if (!m_Actor.Model.Abilities.IsLawEnforcer)
                return null;
            if (percepts == null)
                return null;
            
            // filter murderers that are not our enemies yet.
            List<Percept> murderers = FilterActors(game, percepts,
                (a) => a.MurdersCounter > 0 && !game.Rules.IsEnemyOf(m_Actor, a));

            // if none, nothing to do.
            if (murderers == null || murderers.Count == 0)
                return null;

            // get nearest murderer.
            Percept nearestMurderer = FilterNearest(game, murderers);
            target = nearestMurderer.Percepted as Actor;

            // roll against target unsuspicious skill.
            if (game.Rules.RollChance(game.Rules.ActorUnsuspicousChance(m_Actor, target)))
            {
                // emote.
                game.DoEmote(target, String.Format("moves unnoticed by {0}.", m_Actor.Name));

                // done.
                return null;
            }

            // mmmmhhh. who's that?
            game.DoEmote(m_Actor, String.Format("takes a closer look at {0}.", target.Name));

            // then roll chance to spot and recognize him as murderer.
            int spotChance = game.Rules.ActorSpotMurdererChance(m_Actor, target);

            // if did not spot, nothing to do.
            if (!game.Rules.RollChance(spotChance))
                return null;

            // make him our enemy and tell him!
            game.DoMakeAggression(m_Actor, target);
            return new ActionSay(m_Actor, game, target, 
                String.Format("HEY! YOU ARE WANTED FOR {0} MURDER{1}!", target.MurdersCounter, target.MurdersCounter > 1 ? "s" : ""), RogueGame.Sayflags.IS_IMPORTANT);
        }
        #endregion

        #region Animals
        protected ActorAction BehaviorGoEatFoodOnGround(RogueGame game, List<Percept> stacksPercepts)
        {
            // nope if no percepts.
            if (stacksPercepts == null)
                return null;

            // filter stacks with food.
            List<Percept> foodStacks = Filter(game, stacksPercepts, (p) =>
            {
                Inventory inv = p.Percepted as Inventory;
                return inv.HasItemOfType(typeof(ItemFood));
            });

            // nope if no food stacks.
            if (foodStacks == null)
                return null;

            // either 1) eat there or 2) go get it

            // 1) check food here.
            Inventory invThere = m_Actor.Location.Map.GetItemsAt(m_Actor.Location.Position);
            if (invThere != null && invThere.HasItemOfType(typeof(ItemFood)))
            {
                // eat the first food we get.
                Item eatIt = invThere.GetFirstByType(typeof(ItemFood));
                return new ActionEatFoodOnGround(m_Actor, game, eatIt);
            }
            // 2) go to nearest food.
            Percept nearest = FilterNearest(game, foodStacks);
            return BehaviorStupidBumpToward(game, nearest.Location.Position);
        }
        #endregion

        #region Corpses & Revival
        protected ActorAction BehaviorGoEatCorpse(RogueGame game, List<Percept> corpsesPercepts)
        {
            // nope if no percepts.
            if (corpsesPercepts == null)
                return null;

            // if undead, must need health.
            if (m_Actor.Model.Abilities.IsUndead && m_Actor.HitPoints >= game.Rules.ActorMaxHPs(m_Actor))
                return null;

            // either 1) eat corpses or 2) go get them.

            // 1) check corpses here.
            List<Corpse> corpses = m_Actor.Location.Map.GetCorpsesAt(m_Actor.Location.Position);
            if (corpses != null)
            {
                // eat the first corpse.
                Corpse eatIt = corpses[0];
                if (game.Rules.CanActorEatCorpse(m_Actor, eatIt))
                    return new ActionEatCorpse(m_Actor, game, eatIt);
            }
            // 2) go to nearest corpses.
            Percept nearest = FilterNearest(game, corpsesPercepts);
            return m_Actor.Model.Abilities.IsIntelligent ? 
                    BehaviorIntelligentBumpToward(game, nearest.Location.Position) : 
                    BehaviorStupidBumpToward(game, nearest.Location.Position);
        }

        /// <summary>
        /// TrRy to revive non-enemy corpses.
        /// </summary>
        /// <param name="game"></param>
        /// <param name="corpsesPercepts"></param>
        /// <returns></returns>
        protected ActorAction BehaviorGoReviveCorpse(RogueGame game, List<Percept> corpsesPercepts)
        {
            // nope if no percepts.
            if (corpsesPercepts == null)
                return null;

            // make sure we have the basics : medic skill & medikit item.
            if (m_Actor.Sheet.SkillTable.GetSkillLevel((int)Skills.IDs.MEDIC) == 0)
                return null;
            if (!HasItemOfModel(game.GameItems.MEDIKIT))
                return null;

            // keep only corpses stacks where we can revive at least one corpse.
            List<Percept> revivables = Filter(game, corpsesPercepts, (p) =>
                {
                    List<Corpse> corpsesThere = p.Percepted as List<Corpse>;
                    foreach (Corpse c in corpsesThere)
                    {
                        // dont revive enemies!
                        if (game.Rules.CanActorReviveCorpse(m_Actor, c) && !game.Rules.IsEnemyOf(m_Actor,c.DeadGuy))
                            return true;
                    }
                    return false;
                });
            if (revivables == null)
                return null;

            // either 1) revive corpse or 2) go get them.

            // 1) check corpses here.
            List<Corpse> corpses = m_Actor.Location.Map.GetCorpsesAt(m_Actor.Location.Position);
            if (corpses != null)
            {
                // get the first corpse we can revive.
                foreach (Corpse c in corpses)
                {
                    if (game.Rules.CanActorReviveCorpse(m_Actor, c) && !game.Rules.IsEnemyOf(m_Actor,c.DeadGuy))
                        return new ActionReviveCorpse(m_Actor, game, c);
                }
            }
            // 2) go to nearest revivable.
            Percept nearest = FilterNearest(game, revivables);
            return m_Actor.Model.Abilities.IsIntelligent ?
                    BehaviorIntelligentBumpToward(game, nearest.Location.Position) :
                    BehaviorStupidBumpToward(game, nearest.Location.Position);
        }

        #endregion

        #endregion

        #region Behaviors helpers

        #region Messages
        string MakeCentricLocationDirection(RogueGame game, Location from, Location to)
        {
            // if not same location, just says the map.
            if (from.Map != to.Map)
            {
                return String.Format("in {0}", to.Map.Name);
            }

            // same location, says direction.
            Point fromPos = from.Position;
            Point toPos = to.Position;
            Point vDir = new Point(toPos.X - fromPos.X, toPos.Y - fromPos.Y);
            return String.Format("{0} tiles to the {1}", (int)game.Rules.StdDistance(vDir), Direction.ApproximateFromVector(vDir));
        }
        #endregion

        #region Items

        protected bool IsItemWorthTellingAbout(Item it)
        {
            if (it == null)
                return false;

            // items type to ignore:
            // - barricading material (planks drop a lot).
            if (it is ItemBarricadeMaterial)
                return false;

            // ignore items we are carrying (we have seen it then taken it)
            if (m_Actor.Inventory != null && !m_Actor.Inventory.IsEmpty && m_Actor.Inventory.Contains(it))
                return false;

            // ok.
            return true;
        }

        protected Item GetEquippedWeapon()
        {
            if (m_Actor.Inventory == null || m_Actor.Inventory.IsEmpty)
                return null;

            foreach (Item it in m_Actor.Inventory.Items)
                if (it.IsEquipped && it is ItemWeapon)
                    return it;

            return null;
        }

        protected Item GetBestRangedWeaponWithAmmo(Predicate<Item> fn)
        {
            if (m_Actor.Inventory == null || m_Actor.Inventory.IsEmpty)
                return null;

            Item best = null;
            int bestSc = 0;
            foreach (Item it in m_Actor.Inventory.Items)
            {
                ItemRangedWeapon w = it as ItemRangedWeapon;
                if (w != null && (fn == null || fn(it)))
                {
                    bool checkIt = false;
                    if (w.Ammo > 0)
                    {
                        checkIt = true;
                    }
                    else
                    {
                        // out of ammo, but do we have a matching ammo item in inventory we could reload it with?
                        foreach (Item itReload in m_Actor.Inventory.Items)
                        {
                            if (itReload is ItemAmmo && (fn == null || fn(itReload)))
                            {
                                ItemAmmo itAmmo = itReload as ItemAmmo;
                                if (itAmmo.AmmoType == w.AmmoType)
                                {
                                    checkIt = true;
                                    break;
                                }
                            }
                        }
                    }
                    if (checkIt)
                    {
                        int sc = ScoreRangedWeapon(w);
                        if (best == null || sc > bestSc)
                        {
                            best = w;
                            bestSc = sc;
                        }

                    }
                }
            }

            return best;
        }

        protected int ScoreRangedWeapon(ItemRangedWeapon w)
        {
            ItemRangedWeaponModel m = w.Model as ItemRangedWeaponModel;
            return 1000 * m.Attack.Range + m.Attack.DamageValue;
        }

        protected Item GetFirstMeleeWeapon(Predicate<Item> fn)
        {
            if (m_Actor.Inventory == null || m_Actor.Inventory.IsEmpty)
                return null;

            foreach (Item it in m_Actor.Inventory.Items)
            {
                if (it is ItemMeleeWeapon && (fn == null || fn(it)))
                    return it;
            }

            return null;
        }

        protected Item GetFirstBodyArmor(Predicate<Item> fn)
        {
            if (m_Actor.Inventory == null || m_Actor.Inventory.IsEmpty)
                return null;

            foreach (Item it in m_Actor.Inventory.Items)
            {
                if (it is ItemBodyArmor && (fn == null || fn(it)))
                    return it;
            }

            return null;
        }

        protected ItemGrenade GetFirstGrenade(Predicate<Item> fn)
        {
            if (m_Actor.Inventory == null || m_Actor.Inventory.IsEmpty)
                return null;

            foreach (Item it in m_Actor.Inventory.Items)
            {
                if (it is ItemGrenade && (fn == null || fn(it)))
                    return it as ItemGrenade;
            }

            return null;
        }

        protected Item GetEquippedBodyArmor()
        {
            if (m_Actor.Inventory == null || m_Actor.Inventory.IsEmpty)
                return null;

            foreach (Item it in m_Actor.Inventory.Items)
                if (it.IsEquipped && it is ItemBodyArmor)
                    return it;

            return null;
        }

        protected Item GetEquippedCellPhone()
        {
            if (m_Actor.Inventory == null || m_Actor.Inventory.IsEmpty)
                return null;

            foreach (Item it in m_Actor.Inventory.Items)
                if (it.IsEquipped && it is ItemTracker)
                {
                    ItemTracker t = it as ItemTracker;
                    if (t.CanTrackFollowersOrLeader)
                        return it;
                }

            return null;
        }

        protected Item GetFirstTracker(Predicate<ItemTracker> fn)
        {
            if (m_Actor.Inventory == null || m_Actor.Inventory.IsEmpty)
                return null;

            foreach (Item it in m_Actor.Inventory.Items)
            {
                ItemTracker t = it as ItemTracker;
                if (t != null && (fn == null || fn(t)))
                    return it;
            }

            return null;
        }

        protected Item GetEquippedLight()
        {
            if (m_Actor.Inventory == null || m_Actor.Inventory.IsEmpty)
                return null;

            foreach (Item it in m_Actor.Inventory.Items)
                if (it.IsEquipped && it is ItemLight)
                    return it;

            return null;
        }

        protected Item GetFirstLight(Predicate<Item> fn)
        {
            if (m_Actor.Inventory == null || m_Actor.Inventory.IsEmpty)
                return null;

            foreach (Item it in m_Actor.Inventory.Items)
            {
                if (it is ItemLight && (fn == null || fn(it)))
                    return it;
            }

            return null;
        }

        protected ItemSprayScent GetEquippedStenchKiller()
        {
            if (m_Actor.Inventory == null || m_Actor.Inventory.IsEmpty)
                return null;

            foreach (Item it in m_Actor.Inventory.Items)
                if (it.IsEquipped && it is ItemSprayScent)
                {
                    ItemSprayScentModel m = (it as ItemSprayScent).Model as ItemSprayScentModel;
                    if (m.Odor == Odor.PERFUME_LIVING_SUPRESSOR)
                        return it as ItemSprayScent;
                }

            return null;
        }

        protected ItemSprayScent GetFirstStenchKiller(Predicate<ItemSprayScent> fn)
        {
            if (m_Actor.Inventory == null || m_Actor.Inventory.IsEmpty)
                return null;

            foreach (Item it in m_Actor.Inventory.Items)
            {
                if (it is ItemSprayScent && (fn == null || fn(it as ItemSprayScent)))
                    return it as ItemSprayScent;
            }

            return null;
        }

        protected bool IsRangedWeaponOutOfAmmo(Item it)
        {
            ItemRangedWeapon w = it as ItemRangedWeapon;
            if (w == null)
                return false;
            return w.Ammo <= 0;
        }

        protected bool IsLightOutOfBatteries(Item it)
        {
            ItemLight l = it as ItemLight;
            if (l == null)
                return false;
            return l.Batteries <= 0;
        }

        protected Item GetBestEdibleItem(RogueGame game)
        {
            if (m_Actor.Inventory == null || m_Actor.Inventory.IsEmpty)
                return null;

            int turn = m_Actor.Location.Map.LocalTime.TurnCounter;
            int need = game.Rules.ActorMaxFood(m_Actor) - m_Actor.FoodPoints;
            Item bestFood = null;
            int bestScore = int.MinValue;
            foreach (Item it in m_Actor.Inventory.Items)
            {
                ItemFood foodIt = it as ItemFood;
                if (foodIt == null)
                    continue;

                // compute heuristic score.
                // - economize food : punish food wasting, the more waste the worse.
                // - keep non-perishable food : punish eating non-perishable food, the more nutrition the worse.
                int score = 0;

                int nutrition = game.Rules.FoodItemNutrition(foodIt, turn);
                int waste = nutrition - need;

                // - punish food wasting, the more waste the worse.
                if (waste > 0)
                    score -= waste;

                // - punish eating non-perishable food, the more nutrition the worse.
                if (!foodIt.IsPerishable)
                    score -= nutrition;

                // best?
                if (bestFood == null || score > bestScore)
                {
                    bestFood = foodIt;
                    bestScore = score;
                }
            }

            // return best.
            return bestFood;
        }

        public bool IsInterestingItem(RogueGame game, Item it)
        {
            /////////////////////////////////////////////////////////////////////////////
            // Interesting items:
            // 0 Reject anything not food if only one slot left.
            // 1 Reject forbidden items.
            // 2 Reject spray paint.
            // 3 Reject activated traps.
            // 4 Food.
            // 5 Ranged weapons.
            // 6 Ammo.
            // 7 Other Weapons, Medicine.
            // 8 Lights.
            // 9 Reject primed explosives!
            // 10 Reject boring items.
            // 11 Rest.
            /////////////////////////////////////////////////////////////////////////////

            bool onlyOneSlotLeft = (m_Actor.Inventory.CountItems == game.Rules.ActorMaxInv(m_Actor) - 1);

            // 0 Reject anything not food if only one slot left.
            if (onlyOneSlotLeft)
            {
                if (it is ItemFood)
                    return true;
                else
                    return false;
            }

            // 1 Reject forbidden items.
            if (it.IsForbiddenToAI)
                return false;

            // 2 Reject spray paint.
            if (it is ItemSprayPaint)
                return false;

            // 3 Reject activated traps.
            if (it is ItemTrap)
            {
                if ((it as ItemTrap).IsActivated)
                    return false;
            }

            // 4 Food
            if (it is ItemFood)
            {
                // accept any food if hungry.
                if (game.Rules.IsActorHungry(m_Actor))
                    return true;

                bool hasEnoughFood = HasEnoughFoodFor(game, m_Actor.Sheet.BaseFoodPoints / 2);

                // food not urgent, only interested in not spoiled food and if need more.
                return !hasEnoughFood && !game.Rules.IsFoodSpoiled(it as ItemFood, m_Actor.Location.Map.LocalTime.TurnCounter);
            }

            // 5 Ranged weapons.
            // Reject is AI_NotInterestedInRangedWeapons flag set.
            // Reject empty if no matching ammo, not already 2 ranged weapons in inventory, and different than any weapon we already have.
            if (it is ItemRangedWeapon)
            {
                // ai flag.
                if (m_Actor.Model.Abilities.AI_NotInterestedInRangedWeapons)
                    return false;

                ItemRangedWeapon rw = it as ItemRangedWeapon;
                // empty and no matching ammo : no.
                if (rw.Ammo <= 0 && GetCompatibleAmmoItem(game, rw) == null)
                    return false;

                // already 1 ranged weapon = no
                if (CountItemsOfSameType(typeof(ItemRangedWeapon)) >= 1)
                    return false;

                // new item but same as a weapon we already have = no
                if (!m_Actor.Inventory.Contains(it) && HasItemOfModel(it.Model))
                    return false;

                // all clear, me want!
                return true;
            }

            // 6 Ammo : only if has matching weapon and if has less than two full stacks.
            if (it is ItemAmmo)
            {
                ItemAmmo am = it as ItemAmmo;
                if (GetCompatibleRangedWeapon(game, am) == null)
                    return false;
                return !HasAtLeastFullStackOfItemTypeOrModel(it, 2);
            }

            // 7 Melee weapons, Medecine
            // Reject melee weapons if we are skilled in martial arts or we alreay have 2.
            // Reject medecine if we alredy have full stacks.
            if (it is ItemMeleeWeapon)
            {
                // martial artists ignore melee weapons.
                if (m_Actor.Sheet.SkillTable.GetSkillLevel((int)Skills.IDs.MARTIAL_ARTS) > 0)
                    return false;
                // only two melee weapons max.
                int nbMeleeWeaponsInInventory = CountItemQuantityOfType(typeof(ItemMeleeWeapon));
                return nbMeleeWeaponsInInventory < 2;
            }            
            if(it is ItemMedicine)
            {
                return !HasAtLeastFullStackOfItemTypeOrModel(it, 2);
            }

            // 8 Lights : ignore out of batteries.
            if (IsLightOutOfBatteries(it))
                return false;

            // 9 Reject primed explosives!
            if (it is ItemPrimedExplosive)
                return false;

            // 10 Reject boring items.
            if (m_Actor.IsBoredOf(it))
                return false;

            // 11 Rest : if has less than one full stack.
            return !HasAtLeastFullStackOfItemTypeOrModel(it, 1);
        }

        public bool HasAnyInterestingItem(RogueGame game, Inventory inv)
        {
            if (inv == null)
                return false;
            foreach (Item it in inv.Items)
                if (IsInterestingItem(game, it))
                    return true;
            return false;
        }

        protected Item FirstInterestingItem(RogueGame game, Inventory inv)
        {
            if (inv == null)
                return null;
            foreach (Item it in inv.Items)
                if (IsInterestingItem(game, it))
                    return it;
            return null;
        }

        protected bool HasEnoughFoodFor(RogueGame game, int nutritionNeed)
        {
            if (m_Actor.Inventory == null || m_Actor.Inventory.IsEmpty)
                return false;

            int turnCounter = m_Actor.Location.Map.LocalTime.TurnCounter;
            int nutritionTotal = 0;
            foreach (Item it in m_Actor.Inventory.Items)
            {
                if (it is ItemFood)
                {
                    nutritionTotal += game.Rules.FoodItemNutrition(it as ItemFood, turnCounter);
                    if (nutritionTotal >= nutritionNeed) // exit asap
                        return true;
                }
            }

            return false;
        }

        protected bool HasAtLeastFullStackOfItemTypeOrModel(Item it, int n)
        {
            if (m_Actor.Inventory == null || m_Actor.Inventory.IsEmpty)
                return false;

            if (it.Model.IsStackable)
            {
                // we want N stacks of it.
                return CountItemsQuantityOfModel(it.Model) >= n * it.Model.StackingLimit;
            }
            else
            {
                // not stackable, we are happy with N items of its type.
                return CountItemsOfSameType(it.GetType()) >= n;
            }
        }

        protected bool HasItemOfModel(ItemModel model)
        {
            if (m_Actor.Inventory == null || m_Actor.Inventory.IsEmpty)
                return false;

            foreach (Item it in m_Actor.Inventory.Items)
                if (it.Model == model)
                    return true;

            return false;
        }

        protected int CountItemsQuantityOfModel(ItemModel model)
        {
            if (m_Actor.Inventory == null || m_Actor.Inventory.IsEmpty)
                return 0;

            int count = 0;
            foreach (Item it in m_Actor.Inventory.Items)
            {
                if (it.Model == model)
                    count += it.Quantity;
            }

            return count;
        }

        protected bool HasItemOfType(Type tt)
        {
            if (m_Actor.Inventory == null || m_Actor.Inventory.IsEmpty)
                return false;

            return m_Actor.Inventory.HasItemOfType(tt);
        }

        protected int CountItemQuantityOfType(Type tt)
        {
            if (m_Actor.Inventory == null || m_Actor.Inventory.IsEmpty)
                return 0;

            int quantity = 0;
            foreach (Item otherIt in m_Actor.Inventory.Items)
            {
                if (otherIt.GetType() == tt)
                    quantity += otherIt.Quantity;
            }

            return quantity;
        }

        protected int CountItemsOfSameType(Type tt)
        {
            if (m_Actor.Inventory == null || m_Actor.Inventory.IsEmpty)
                return 0;

            int count = 0;
            foreach (Item otherIt in m_Actor.Inventory.Items)
            {
                if (otherIt.GetType() == tt)
                    ++count;
            }

            return count;
        }

        #endregion

        #region Running
        protected void RunIfPossible(Rules rules)
        {
            if (rules.CanActorRun(m_Actor))
                m_Actor.IsRunning = true;
        }
        #endregion

        #region Distances & Safety
        protected int GridDistancesSum(Rules rules, Point from, List<Percept> goals)
        {
            int sum = 0;
            foreach (Percept to in goals)
                sum += rules.GridDistance(from, to.Location.Position);
            return sum;
        }

        /// <summary>
        /// Compute safety from a list of dangers at a given position.
        /// </summary>
        /// <param name="rules"></param>
        /// <param name="from">position to compute the safety</param>
        /// <param name="dangers">dangers to avoid</param>
        /// <returns>a heuristic value, the higher the better the safety from the dangers</returns>
        protected float SafetyFrom(Rules rules, Point from, List<Percept> dangers)
        {
            Map map = m_Actor.Location.Map;

            // Heuristics:
            // Primary: Get away from dangers.
            // Weighting factors:
            // 1 Avoid getting in corners.
            // 2 Prefer going outside/inside if majority of dangers are inside/outside.
            // 3 If can tire, prefer not jumping.

            // Primary: Get away from dangers.
            #region
            float avgDistance = GridDistancesSum(rules, from, dangers) / (1 + dangers.Count);
            #endregion

            // 1 Avoid getting in corners.
            #region
            int countFreeSquares = 0;
            foreach (Direction d in Direction.COMPASS)
            {
                Point to = from + d;
                if (to == m_Actor.Location.Position || rules.IsWalkableFor(m_Actor, map, to.X, to.Y))
                    ++countFreeSquares;
            }
            float avoidCornerBonus = countFreeSquares * 0.1f;   // [0,+0.8]
            #endregion

            // 2 Prefer going outside/inside if majority of dangers are inside/outside.
            #region
            bool isFromInside = map.GetTileAt(from).IsInside;
            int majorityDangersInside = 0;
            foreach (Percept p in dangers)
            {
                if (map.GetTileAt(p.Location.Position).IsInside)
                    ++majorityDangersInside;
                else
                    --majorityDangersInside;
            }
            const float inOutFactor = 1.25f;
            float inOutBonus = 0;
            if (isFromInside)
            {
                // from is inside, want that if majority dangers are outside.
                if (majorityDangersInside < 0) inOutBonus = inOutFactor;
            }
            else
            {
                // from is outside, want that if majority dangers are inside.                              
                if (majorityDangersInside > 0) inOutBonus = inOutFactor;
            }
            #endregion

            // 3 If can tire, prefer not jumping.
            #region
            float jumpPenalty = 0;
            if (m_Actor.Model.Abilities.CanTire && m_Actor.Model.Abilities.CanJump)
            {
                MapObject obj = map.GetMapObjectAt(from);
                if (obj != null && obj.IsJumpable)
                    jumpPenalty = 0.1f;
            }
            #endregion

            // Final Safety = getting away * heuristics weights.
            float heursticFactorBonus = 1f + avoidCornerBonus + inOutBonus - jumpPenalty;
            return avgDistance * heursticFactorBonus;
        }
        #endregion

        #region Choice making
        protected ChoiceEval<_T_> Choose<_T_>(RogueGame game, List<_T_> listOfChoices, 
            Func<_T_, bool> isChoiceValidFn,
            Func<_T_, float> evalChoiceFn, 
            Func<float, float, bool> isBetterEvalThanFn)
        {
            //Console.Out.WriteLine("Evaluating choices");

            // Degenerate cases.
            if (listOfChoices.Count == 0)
            {
                //Console.Out.WriteLine("no choice.");
                return null;
            }

            // Find valid choices and best value.
            bool hasValue = false;
            float bestValue = 0;    // irrevelant for 1st value, use flag hasValue instead.
            List<ChoiceEval<_T_>> validChoices = new List<ChoiceEval<_T_>>(listOfChoices.Count);
            for(int i = 0; i < listOfChoices.Count; i++)
            {
                if(!isChoiceValidFn(listOfChoices[i]))
                    continue;

                float value_i = evalChoiceFn(listOfChoices[i]);
                if (float.IsNaN(value_i))
                    continue;

                validChoices.Add(new ChoiceEval<_T_>(listOfChoices[i], value_i));

                if (!hasValue || isBetterEvalThanFn(value_i, bestValue))
                {
                    hasValue = true;
                    bestValue = value_i;
                }
            }

            /*Console.Out.WriteLine("Evals {");
            for (int j = 0; j < validChoices.Count; j++)
            {
                Console.Out.WriteLine("  {0}", validChoices[j].ToString());
            }
            Console.Out.WriteLine("}");*/

            // Degenerate cases.
            if (validChoices.Count == 0)
            {
                //Console.Out.WriteLine("no valid choice!");
                return null;
            }
            if (validChoices.Count == 1)
            {
                return validChoices[0];
            }

            // Keep all the candidates that have the best value.
            List<ChoiceEval<_T_>> candidates = new List<ChoiceEval<_T_>>(validChoices.Count);
            for (int i = 0; i < validChoices.Count; i++)
                if (validChoices[i].Value == bestValue)
                    candidates.Add(validChoices[i]);

            /*Console.Out.WriteLine("Candidates {");
            for (int j = 0; j < candidates.Count; j++)
            {
                Console.Out.WriteLine("  {0}", candidates[j].ToString());
            }
            Console.Out.WriteLine("}");*/

            // Of all the candidates randomly choose one.
            int iChoice = game.Rules.Roll(0, candidates.Count);
            return candidates[iChoice];
        }

        protected ChoiceEval<_DATA_> ChooseExtended<_T_, _DATA_>(RogueGame game, List<_T_> listOfChoices,
            Func<_T_, _DATA_> isChoiceValidFn,
            Func<_T_, float> evalChoiceFn,
            Func<float, float, bool> isBetterEvalThanFn)
        {
            //Console.Out.WriteLine("Evaluating choices");

            // Degenerate cases.
            if (listOfChoices.Count == 0)
            {
                //Console.Out.WriteLine("no choice.");
                return null;
            }

            // Find valid choices and best value.
            bool hasValue = false;
            float bestValue = 0;    // irrevelant for 1st value, use flag hasValue instead.
            List<ChoiceEval<_DATA_>> validChoices = new List<ChoiceEval<_DATA_>>(listOfChoices.Count);
            for (int i = 0; i < listOfChoices.Count; i++)
            {
                _DATA_ choiceData = isChoiceValidFn(listOfChoices[i]);
                if (choiceData == null)
                    continue;

                float value_i = evalChoiceFn(listOfChoices[i]);

                if (float.IsNaN(value_i))
                    continue;

                validChoices.Add(new ChoiceEval<_DATA_>(choiceData, value_i));

                if (!hasValue || isBetterEvalThanFn(value_i, bestValue))
                {
                    hasValue = true;
                    bestValue = value_i;
                }
            }

            /*Console.Out.WriteLine("Evals {");
            for (int j = 0; j < validChoices.Count; j++)
            {
                Console.Out.WriteLine("  {0}", validChoices[j].ToString());
            }
            Console.Out.WriteLine("}");*/

            // Degenerate cases.
            if (validChoices.Count == 0)
            {
                //Console.Out.WriteLine("no valid choice!");
                return null;
            }
            if (validChoices.Count == 1)
            {
                return validChoices[0];
            }

            // Keep all the candidates that have the best value.
            List<ChoiceEval<_DATA_>> candidates = new List<ChoiceEval<_DATA_>>(validChoices.Count);
            for (int i = 0; i < validChoices.Count; i++)
                if (validChoices[i].Value == bestValue)
                    candidates.Add(validChoices[i]);

            // TEST: if no best value, nope.
            if (candidates.Count == 0)
                return null;

            /*Console.Out.WriteLine("Candidates {");
            for (int j = 0; j < candidates.Count; j++)
            {
                Console.Out.WriteLine("  {0}", candidates[j].ToString());
            }
            Console.Out.WriteLine("}");*/

            // Of all the candidates randomly choose one.
            int iChoice = game.Rules.Roll(0, candidates.Count);
            return candidates[iChoice];
        }
        #endregion

        #region Action filtering
        /// <summary>
        /// Checks if an action can be considered a valid fleeing action : Move, OpenDoor, SwitchPlace.
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        protected bool IsValidFleeingAction(ActorAction a)
        {
            return a != null && (a is ActionMoveStep || a is ActionOpenDoor || a is ActionSwitchPlace);
        }

        /// <summary>
        /// Checks if an action can be considered a valid wandering action : Move, SwitchPlace, Push, OpenDoor, Chat/Trade, Bash, GetFromContainer, Barricade.
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        protected bool isValidWanderAction(RogueGame game, ActorAction a)
        {
            return a != null && 
                (a is ActionMoveStep || 
                a is ActionSwitchPlace ||
                a is ActionPush ||
                a is ActionOpenDoor || 
                (a is ActionChat && (this.Directives.CanTrade || (a as ActionChat).Target == m_Actor.Leader)) || 
                a is ActionBashDoor || 
                (a is ActionGetFromContainer && IsInterestingItem(game, (a as ActionGetFromContainer).Item)) ||
                a is ActionBarricadeDoor);
        }

        /// <summary>
        /// Checks if an action can be considered a valid action to move toward a goal.
        /// Not valid actions : Chat, GetFromContainer, SwitchPowerGenerator, RechargeItemBattery
        /// </summary>
        /// <param name="a"></param>
        /// <returns></returns>
        protected bool IsValidMoveTowardGoalAction(ActorAction a)
        {
            return a != null &&
                !(a is ActionChat || a is ActionGetFromContainer || a is ActionSwitchPowerGenerator || a is ActionRechargeItemBattery);
        }
        #endregion

        #region Actors predicates
        protected bool HasNoFoodItems(Actor actor)
        {
            Inventory inv = actor.Inventory;
            if (inv == null || inv.IsEmpty)
                return true;
            return !inv.HasItemOfType(typeof(ItemFood));
        }

        protected bool IsSoldier(Actor actor)
        {
            return actor != null && actor.Controller is SoldierAI;
        }

        protected bool WouldLikeToSleep(RogueGame game, Actor actor)
        {
            return game.Rules.IsAlmostSleepy(actor) || game.Rules.IsActorSleepy(actor);
        }

        protected bool IsOccupiedByOther(Map map, Point position)
        {
            Actor other = map.GetActorAt(position);
            return other != null && other != m_Actor;
        }

        protected bool IsAdjacentToEnemy(RogueGame game, Actor actor)
        {
            if (actor == null)
                return false;

            Map map = actor.Location.Map;

            return map.HasAnyAdjacentInMap(actor.Location.Position,
                (pt) =>
                {
                    Actor other = map.GetActorAt(pt);
                    if (other == null)
                        return false;
                    return game.Rules.IsEnemyOf(actor, other);
                });
        }

        protected bool IsInside(Actor actor)
        {
            if (actor == null)
                return false;

            return actor.Location.Map.GetTileAt(actor.Location.Position.X, actor.Location.Position.Y).IsInside;
        }

        protected bool HasEquipedRangedWeapon(Actor actor)
        {
            return (actor.GetEquippedWeapon() as ItemRangedWeapon) != null;
        }

        protected ItemAmmo GetCompatibleAmmoItem(RogueGame game, ItemRangedWeapon rw)
        {
            if (m_Actor.Inventory == null)
                return null;

            // get first compatible ammo item.
            foreach (Item it in m_Actor.Inventory.Items)
            {
                ItemAmmo ammoIt = it as ItemAmmo;
                if (ammoIt == null)
                    continue;
                if (ammoIt.AmmoType == rw.AmmoType && game.Rules.CanActorUseItem(m_Actor, ammoIt))
                    return ammoIt;
            }

            // failed.
            return null;
        }

        protected ItemRangedWeapon GetCompatibleRangedWeapon(RogueGame game, ItemAmmo am)
        {
            if (m_Actor.Inventory == null)
                return null;

            // get first compatible ammo item.
            foreach (Item it in m_Actor.Inventory.Items)
            {
                ItemRangedWeapon rangedIt = it as ItemRangedWeapon;
                if (rangedIt == null)
                    continue;
                if (rangedIt.AmmoType == am.AmmoType)
                    return rangedIt;
            }

            // failed.
            return null;
        }

        protected ItemMeleeWeapon GetBestMeleeWeapon(RogueGame game, Predicate<Item> fn)
        {
            if (m_Actor.Inventory == null)
                return null;

            // best = score = most damage 1st, most attack 2nd, less stamina penalty 3rd.
            int bestScore = 0;
            ItemMeleeWeapon bestWeapon = null;

            foreach (Item it in m_Actor.Inventory.Items)
            {
                if (fn != null && !fn(it))
                    continue;

                ItemMeleeWeapon weapon = it as ItemMeleeWeapon;
                if (weapon == null)
                    continue;
                ItemMeleeWeaponModel model = weapon.Model as ItemMeleeWeaponModel;

                int score = 10000 * model.Attack.DamageValue + 
                            100 * model.Attack.HitValue + 
                            -model.Attack.StaminaPenalty;
                if (score > bestScore)
                {
                    bestScore = score;
                    bestWeapon = weapon;
                }
            }

            // done.
            return bestWeapon;
        }

        protected ItemBodyArmor GetBestBodyArmor(RogueGame game, Predicate<Item> fn)
        {
            if (m_Actor.Inventory == null)
                return null;

            // best = most PRO.
            int bestPRO = 0;
            ItemBodyArmor bestArmor = null;

            foreach (Item it in m_Actor.Inventory.Items)
            {
                if (fn != null && !fn(it))
                    continue;

                ItemBodyArmor armor = it as ItemBodyArmor;
                if (armor == null)
                    continue;

                int pro = armor.Protection_Hit + armor.Protection_Shot;
                if (pro > bestPRO)
                {
                    bestPRO = pro;
                    bestArmor = armor;
                }
            }

            // done.
            return bestArmor;
        }

        protected bool WantToEvadeMelee(RogueGame game, Actor actor, ActorCourage courage, Actor target)
        {
            ///////////////////////////////////////////////////////
            // Targets to evade or not:
            // 1. Yes : if fighting makes me tired.
            // 2. Yes : slower targets that will act next turn (kiting) and are targetting us.
            // 3. No  : target is weaker.
            // 4. Yes : actor is weaker.
            // 5. Unclear cases, utimately decide on courage.
            ///////////////////////////////////////////////////////

            // 1. Always if fighting makes me tired.
            if (WillTireAfterAttack(game, actor))
                return true;

            // 2. Yes : slower targets that will act next turn (kiting) and are targetting us.
            if (game.Rules.ActorSpeed(actor) > game.Rules.ActorSpeed(target))
            {
                // don't evade if we're gonna act again.
                if (game.Rules.WillActorActAgainBefore(actor, target))
                    return false;
                else
                {
                    // evade if he is targetting us.
                    if (target.TargetActor == actor)
                        return true;
                }
            }

            // get weaker actor in melee.
            Actor weakerOne = FindWeakerInMelee(game, m_Actor, target);

            // 3. No : target is weaker.
            if (weakerOne == target)
                return false;

            // 4. Yes : actor is weaker.
            if (weakerOne == m_Actor)
                return true;

            // 5. Unclear cases, utimately decide on courage.
            return courage == ActorCourage.COURAGEOUS ? false : true;
        }

        /// <summary>
        /// Get which of the two actor can be considered as a weaker one in a melee fight.
        /// </summary>
        /// <returns>weaker actor, null if they are equal.</returns>
        protected Actor FindWeakerInMelee(RogueGame game, Actor a, Actor b)
        {
            int value_A = a.HitPoints + a.CurrentMeleeAttack.DamageValue;
            int value_B = b.HitPoints + b.CurrentMeleeAttack.DamageValue;

            return value_A < value_B ? a : value_A > value_B ? b : null;
        }

        protected bool WillTireAfterAttack(RogueGame game, Actor actor)
        {
            if (!actor.Model.Abilities.CanTire)
                return false;
            int staAfter = actor.StaminaPoints - Rules.STAMINA_COST_MELEE_ATTACK;
            return staAfter < Rules.STAMINA_MIN_FOR_ACTIVITY;
        }

        protected bool WillTireAfterRunning(RogueGame game, Actor actor)
        {
            if (!actor.Model.Abilities.CanTire)
                return false;
            int staAfter = actor.StaminaPoints - Rules.STAMINA_COST_RUNNING;
            return staAfter < Rules.STAMINA_MIN_FOR_ACTIVITY;
        }

        protected bool HasSpeedAdvantage(RogueGame game, Actor actor, Actor target)
        {
            int actorSpeed = game.Rules.ActorSpeed(actor);
            int targetSpeed = game.Rules.ActorSpeed(target);

            // if better speed, yes.
            if (actorSpeed > targetSpeed) 
                return true;

            // if we can run and the target can't and that would make us faster without tiring us, then yes!
            if (game.Rules.CanActorRun(actor) && !game.Rules.CanActorRun(target) &&
                !WillTireAfterRunning(game, actor) && actorSpeed * 2 > targetSpeed)
                return true;

            // TODO: other tricky cases?

            return false;
        }

        protected bool NeedsLight(RogueGame game)
        {
            switch (m_Actor.Location.Map.Lighting)
            {
                case Lighting.DARKNESS:
                    return true;
                case Lighting.LIT:
                    return false;
                case Lighting.OUTSIDE:
                    // Needs only if At Night & (Outside or Heavy Rain).
                    return m_Actor.Location.Map.LocalTime.IsNight &&
                        (game.Session.World.Weather == Weather.HEAVY_RAIN || !m_Actor.Location.Map.GetTileAt(m_Actor.Location.Position.X, m_Actor.Location.Position.Y).IsInside);
                default:
                    throw new ArgumentOutOfRangeException("unhandled lighting");
            }
        }

        /// <summary>
        /// Check if a point can be considered between two others.
        /// </summary>
        /// <param name="game"></param>
        /// <param name="A"></param>
        /// <param name="B"></param>
        /// <param name="C"></param>
        /// <returns></returns>
        protected bool IsBetween(RogueGame game, Point A, Point between, Point B)
        {
            float A_between = game.Rules.StdDistance(A, between);
            float B_between = game.Rules.StdDistance(B, between);
            float A_B = game.Rules.StdDistance(A, B);

            return A_between + B_between <= A_B + 0.25f;
        }

        protected bool IsDoorwayOrCorridor(RogueGame game, Map map, Point pos)
        {
            ///////////////////////////////////////
            // Check for simple shapes:
            // FREE-WALL-FREE       FREE-FREE-FREE
            // FREE-FREE-FREE       WALL-FREE-WALL
            // FREE-WALL-FREE       FREE-FREE-FREE
            ///////////////////////////////////////

            bool wall = !map.GetTileAt(pos).Model.IsWalkable;
            if(wall)
                return false;

            Point N = pos + Direction.N;
            bool nWall = map.IsInBounds(N) && !map.GetTileAt(N).Model.IsWalkable;
            Point S = pos + Direction.S;
            bool sWall = map.IsInBounds(S) && !map.GetTileAt(S).Model.IsWalkable;
            Point E = pos + Direction.E;
            bool eWall = map.IsInBounds(E) && !map.GetTileAt(E).Model.IsWalkable;
            Point W = pos + Direction.W;
            bool wWall = map.IsInBounds(W) && !map.GetTileAt(W).Model.IsWalkable;

            Point NE = pos + Direction.NE;
            bool neWall = map.IsInBounds(NE) && !map.GetTileAt(NE).Model.IsWalkable;
            Point NW = pos + Direction.NW;
            bool nwWall = map.IsInBounds(NW) && !map.GetTileAt(NW).Model.IsWalkable;
            Point SE = pos + Direction.SE;
            bool seWall = map.IsInBounds(SE) && !map.GetTileAt(SE).Model.IsWalkable;
            Point SW = pos + Direction.SW;
            bool swWall = map.IsInBounds(SW) && !map.GetTileAt(SW).Model.IsWalkable;

            bool freeCorners = !neWall && !seWall && !nwWall && !swWall;

            if (freeCorners && nWall && sWall && !eWall && !wWall)
                return true;
            if (freeCorners && eWall && wWall && !nWall && !sWall)
                return true;

            return false;
        }

        /// <summary>
        /// Not an enemy AND same faction.
        /// </summary>
        /// <param name="game"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        protected bool IsFriendOf(RogueGame game, Actor other)
        {
            return !game.Rules.IsEnemyOf(m_Actor, other) && m_Actor.Faction == other.Faction;
        }

        protected Actor GetNearestTargetFor(RogueGame game, Actor actor)
        {
            Map map = actor.Location.Map;
            Actor nearest = null;
            int best = int.MaxValue;

            // quite uggly but better than computing the whole FoV...
            foreach (Actor a in map.Actors)
            {
                if (a.IsDead) continue;
                if (a == actor) continue;
                if (!game.Rules.IsEnemyOf(actor, a)) continue;

                int d = game.Rules.GridDistance(a.Location.Position, actor.Location.Position);
                if (d < best)
                {
                    if (d == 1 || LOS.CanTraceViewLine(actor.Location, a.Location.Position))
                    {
                        best = d;
                        nearest = a;
                    }
                }
            }

            return nearest;
        }
        #endregion

        #region Exits
        protected List<Exit> ListAdjacentExits(RogueGame game, Location fromLocation)
        {
            List<Exit> list = null;
            foreach (Direction d in Direction.COMPASS)
            {
                Point nextPos = fromLocation.Position + d;
                Exit exit = fromLocation.Map.GetExitAt(nextPos);
                if (exit == null)
                    continue;
                if (list == null)
                    list = new List<Exit>(8);
                list.Add(exit);
            }

            return list;
        }

        protected Exit PickAnyAdjacentExit(RogueGame game, Location fromLocation)
        {
            // get all adjacent exits.
            List<Exit> list = ListAdjacentExits(game, fromLocation);

            // if none, failed.
            if (list == null)
                return null;

            // pick one at random.
            return list[game.Rules.Roll(0, list.Count)];
        }
        #endregion

        #region Map
        public static bool IsAnyActivatedTrapThere(Map map, Point pos)
        {
            Inventory inv = map.GetItemsAt(pos);
            if (inv == null || inv.IsEmpty) return false;
            return inv.GetFirstMatching((it) => { ItemTrap trap = it as ItemTrap; return trap != null && trap.IsActivated; }) != null;
        }

        public static bool IsZoneChange(Map map, Point pos)
        {
            List<Zone> zonesHere = map.GetZonesAt(pos.X, pos.Y);
            if (zonesHere == null) return false;

            // adjacent to another zone.
            return map.HasAnyAdjacentInMap(pos, (adj) =>
            {
                List<Zone> zonesAdj = map.GetZonesAt(adj.X, adj.Y);
                if (zonesAdj == null) return false;
                if (zonesHere == null) return true;
                foreach (Zone z in zonesAdj)
                    if (!zonesHere.Contains(z))
                        return true;
                return false;
            });
        }
        #endregion

        protected Point RandomPositionNear(Rules rules, Map map, Point goal, int range)
        {
            int x = goal.X + rules.Roll(-range, +range);
            int y = goal.Y + rules.Roll(-range, +range);

            map.TrimToBounds(ref x, ref y);

            return new Point(x, y);
        }
        #endregion

        #region Taboo items
        protected void MarkItemAsTaboo(Item it)
        {
            if (m_TabooItems == null)
                m_TabooItems = new List<Item>(1);
            else if (m_TabooItems.Contains(it))
                return;
            m_TabooItems.Add(it);
        }

        protected void UnmarkItemAsTaboo(Item it)
        {
            if (m_TabooItems == null)
                return;
            m_TabooItems.Remove(it);
            if (m_TabooItems.Count == 0)
                m_TabooItems = null;
        }

        protected bool IsItemTaboo(Item it)
        {
            if (m_TabooItems == null)
                return false;
            return m_TabooItems.Contains(it);
        }
        #endregion

        #region Taboo tiles
        protected void MarkTileAsTaboo(Point p)
        {
            if (m_TabooTiles == null)
                m_TabooTiles = new List<Point>(1);
            else if (m_TabooTiles.Contains(p))
                return;
            m_TabooTiles.Add(p);
        }

        protected bool IsTileTaboo(Point p)
        {
            if (m_TabooTiles == null)
                return false;
            return m_TabooTiles.Contains(p);
        }

        protected void ClearTabooTiles()
        {
            m_TabooTiles = null;
        }
        #endregion

        #region Taboo trades
        protected void MarkActorAsRecentTrade(Actor other)
        {
            if (m_TabooTrades == null)
                m_TabooTrades = new List<Actor>(1);
            else if (m_TabooTrades.Contains(other))
                return;
            m_TabooTrades.Add(other);
        }

        protected bool IsActorTabooTrade(Actor other)
        {
            if (m_TabooTrades == null) return false;
            return m_TabooTrades.Contains(other);
        }

        protected void ClearTabooTrades()
        {
            m_TabooTrades = null;
        }
        #endregion
    }
}
