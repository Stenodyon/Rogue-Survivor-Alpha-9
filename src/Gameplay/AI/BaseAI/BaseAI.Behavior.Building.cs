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
    /* This part of BaseAI provides building (fortifications and traps)
    behaviors */
    abstract partial class BaseAI
    {
        protected int ComputeTrapsMaxDamage(Map map, Point pos)
        {
            Inventory inv = map.GetItemsAt(pos);
            if (inv == null) return 0;

            int sum = 0;
            ItemTrap trp=null;
            foreach (Item it in inv.Items)
            {
                trp = it as ItemTrap;
                if (trp == null) continue;
                sum += trp.TrapModel.Damage;
            }
            return sum;
        }

        protected ActorAction BehaviorBuildTrap(RogueGame game)
        {
            // don't bother if we don't have a trap.
            ItemTrap trap = m_Actor.Inventory.GetFirstByType(typeof(ItemTrap)) as ItemTrap;
            if (trap == null)
                return null;

            // is this a good spot for a trap?            
            string reason;
            if (!IsGoodTrapSpot(game, m_Actor.Location.Map, m_Actor.Location.Position, out reason))
                return null;

            // if trap needs to be activated, do it.
            if (!trap.IsActivated && !trap.TrapModel.ActivatesWhenDropped)
                return new ActionUseItem(m_Actor, game, trap);

            // trap ready to setup, do it!
            game.DoEmote(m_Actor, String.Format("{0} {1}!", reason, trap.AName));
            return new ActionDropItem(m_Actor, game, trap);
        }

        protected bool IsGoodTrapSpot(RogueGame game, Map map, Point pos, out string reason)
        {
            reason = "";
            bool potentialSpot = false;

            // 1. Potential spot?
            // 2. Don't overdo it.

            // 1. Potential spot?
            // outside and has a corpse.
            bool isInside = map.GetTileAt(pos).IsInside;
            if (!isInside && map.GetCorpsesAt(pos) != null)
            {
                reason = "that corpse will serve as a bait for";
                potentialSpot = true;
            }
            else
            {
                //  entering or leaving a building?
                bool wasInside = m_prevLocation.Map.GetTileAt(m_prevLocation.Position).IsInside;
                if (wasInside != isInside)
                {
                    reason = "protecting the building with";
                    potentialSpot = true;
                }
                else
                {
                    // ...or a door/window?
                    MapObject objThere = map.GetMapObjectAt(pos);
                    if (objThere != null && objThere is DoorWindow)
                    {
                        reason = "protecting the doorway with";
                        potentialSpot = true;
                    }
                    // ...or an exit?
                    else if (map.GetExitAt(pos) != null)
                    {
                        reason = "protecting the exit with";
                        potentialSpot = true;
                    }
                }
            }
            if (!potentialSpot)
                return false;

            // 2. Don't overdo it.
            // Never drop more than 3 traps.
            Inventory itemsThere = map.GetItemsAt(pos);
            if (itemsThere != null)
            {
                int countActivated = itemsThere.CountItemsMatching((it) =>
                {
                    ItemTrap trap = it as ItemTrap;
                    if (trap == null) return false;
                    return trap.IsActivated;
                });
                if (countActivated > 3) 
                    return false;
            }
            // TODO Need at least 2 neighbouring non adjacent tiles free of activated traps.

            // ok!
            return true;
        }
        
        protected ActorAction BehaviorBuildSmallFortification(RogueGame game)
        {
            // don't bother if no carpentry skill or not enough material.
            if (m_Actor.Sheet.SkillTable.GetSkillLevel((int)Skills.IDs.CARPENTRY) == 0)
                return null;
            if (game.Rules.CountBarricadingMaterial(m_Actor) < game.Rules.ActorBarricadingMaterialNeedForFortification(m_Actor, false))
                return null;

            // pick a good adjacent tile.
            // good tiles are :
            // - in bounds, walkable, empty, not border.
            // - not exits.
            // - doorways.
            // eval is random.
            Map map = m_Actor.Location.Map;
            ChoiceEval<Direction> choice = Choose<Direction>(game, Direction.COMPASS_LIST,
                (dir) =>
                {
                    Point pt = m_Actor.Location.Position + dir;
                    if (!map.IsInBounds(pt))
                        return false;
                    if (!map.IsWalkable(pt))
                        return false;
                    if (map.IsOnMapBorder(pt.X, pt.Y))
                        return false;
                    if (map.GetActorAt(pt) != null)
                        return false;
                    if (map.GetExitAt(pt) != null)
                        return false;
                    return IsDoorwayOrCorridor(game, map, pt);
                },
                (dir) => game.Rules.Roll(0, 666),
                (a, b) => a > b);

            // if no choice, fail.
            if (choice == null)
                return null;

            // get pos.
            Point adj = m_Actor.Location.Position + choice.Choice;

            // if can't build there, fail.
            if (!game.Rules.CanActorBuildFortification(m_Actor, adj, false))
                return null;

            // ok!
            return new ActionBuildFortification(m_Actor, game, adj, false);
        }

        /// <summary>
        /// Try to make a line of large fortifications.
        /// </summary>
        /// <param name="game"></param>
        /// <returns></returns>
        protected ActorAction BehaviorBuildLargeFortification(RogueGame game, int startLineChance)
        {
            // don't bother if no carpentry skill or not enough material.
            if (m_Actor.Sheet.SkillTable.GetSkillLevel((int)Skills.IDs.CARPENTRY) == 0)
                return null;
            if (game.Rules.CountBarricadingMaterial(m_Actor) < game.Rules.ActorBarricadingMaterialNeedForFortification(m_Actor, true))
                return null;

            // pick a good adjacent tile.
            // good tiles are :
            // - not exit.
            // - not map border.
            // - outside and anchor/continue wall.
            // all things being equal, eval is random.
            Map map = m_Actor.Location.Map;
            ChoiceEval<Direction> choice = Choose<Direction>(game, Direction.COMPASS_LIST,
                (dir) =>
                {
                    Point pt = m_Actor.Location.Position + dir;
                    if (!map.IsInBounds(pt))
                        return false;
                    if (!map.IsWalkable(pt))
                        return false;
                    if (map.IsOnMapBorder(pt.X, pt.Y))
                        return false;
                    if (map.GetActorAt(pt) != null)
                        return false;
                    if (map.GetExitAt(pt) != null)
                        return false;

                    // outside.
                    if (map.GetTileAt(pt.X, pt.Y).IsInside)
                        return false;

                    // count stuff there.
                    int wallsAround = map.CountAdjacentInMap(pt, (ptAdj) => !map.GetTileAt(ptAdj).Model.IsWalkable);
                    int lfortsAround = map.CountAdjacentInMap(pt,
                        (ptAdj) =>
                        {
                            Fortification f = map.GetMapObjectAt(ptAdj) as Fortification;
                            return f != null && !f.IsTransparent;
                        });

                    // good spot?
                    if (wallsAround == 3 && lfortsAround == 0 && game.Rules.RollChance(startLineChance))
                        // fort line anchor.
                        return true;
                    if (wallsAround == 0 && lfortsAround == 1)
                        // fort line continuation.
                        return true;

                    // nope.
                    return false;
                },
                (dir) => game.Rules.Roll(0, 666),
                (a, b) => a > b);

            // if no choice, fail.
            if (choice == null)
                return null;

            // get pos.
            Point adj = m_Actor.Location.Position + choice.Choice;

            // if can't build there, fail.
            if (!game.Rules.CanActorBuildFortification(m_Actor, adj, true))
                return null;

            // ok!
            return new ActionBuildFortification(m_Actor, game, adj, true);
        }

    }
}