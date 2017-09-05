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
    /* This part of BaseAI provides inventory management behaviors */
    abstract partial class BaseAI
    {
        #region Equipping
        protected ActorAction BehaviorEquipWeapon(RogueGame game)
        {
            #region Ranged first
            // If already equiped a ranged weapon, we might want to reload it.
            Item eqWpn = GetEquippedWeapon();
            if (eqWpn != null && eqWpn is ItemRangedWeapon)
            {
                // ranged weapon equipped, if directive disabled unequip it!
                if (!this.Directives.CanFireWeapons)
                    return new ActionUnequipItem(m_Actor, game, eqWpn);

                // ranged weapon equipped, reload it?
                ItemRangedWeapon rw = eqWpn as ItemRangedWeapon;
                if (rw.Ammo <= 0)
                {
                    // reload it if we can.
                    ItemAmmo ammoIt = GetCompatibleAmmoItem(game, rw);
                    if (ammoIt != null)
                        return new ActionUseItem(m_Actor, game, ammoIt);
                }
                else
                    // nope, ranged equipped with ammo, nothing more to do with it.
                    return null;
            }

            // No ranged weapon equipped or equipped but out of ammo and no ammos to reload.
            // Equip other best available ranged weapon, if allowed to fire.
            if (this.Directives.CanFireWeapons)
            {
                Item newRanged = GetBestRangedWeaponWithAmmo((it) => !IsItemTaboo(it));
                if (newRanged != null)
                {
                    // equip new.
                    if (game.Rules.CanActorEquipItem(m_Actor, newRanged))
                        return new ActionEquipItem(m_Actor, game, newRanged);
                }
            }
            #endregion

            #region Melee second
            // Get best melee weapon in inventory.
            ItemMeleeWeapon bestMeleeWeapon = GetBestMeleeWeapon(game, (it) => !IsItemTaboo(it));

            // If none, nothing to do.
            if (bestMeleeWeapon == null)
                return null;
            
            // If it is already equipped, done.
            if (eqWpn == bestMeleeWeapon)
                return null;

            // If no weapon equipped, equip best now.
            if (eqWpn == null)
            {
                if (game.Rules.CanActorEquipItem(m_Actor, bestMeleeWeapon))
                    return new ActionEquipItem(m_Actor, game, bestMeleeWeapon);
                else
                    return null;
            }

            // Another weapon equipped, unequip it.
            if (eqWpn != null)
            {
                if (game.Rules.CanActorUnequipItem(m_Actor, eqWpn))
                    return new ActionUnequipItem(m_Actor, game, eqWpn);
                else
                    return null;
            }
            #endregion

            // Fail.
            return null;
        }

        protected ActorAction BehaviorEquipBodyArmor(RogueGame game)
        {
            // Get best armor available.
            ItemBodyArmor bestArmor = GetBestBodyArmor(game, (it) => !IsItemTaboo(it));

            // If none, don't bother.
            if (bestArmor == null)
                return null;

            // If already equipped, fine.
            Item eqArmor = GetEquippedBodyArmor();
            if (eqArmor == bestArmor)
                return null;

            // If another armor already equipped, unequip it first.
            if (eqArmor != null)
            {
                if (game.Rules.CanActorUnequipItem(m_Actor, eqArmor))
                    return new ActionUnequipItem(m_Actor, game, eqArmor);
                else
                    return null;
            }

            // Equip the new armor.
            if (eqArmor == null)
            {
                if (game.Rules.CanActorEquipItem(m_Actor, bestArmor))
                    return new ActionEquipItem(m_Actor, game, bestArmor);
                else
                    return null;
            }

            // Fail.
            return null;
        }

        protected ActorAction BehaviorEquipCellPhone(RogueGame game)
        {
            // Only equip cellphone if :
            // - is a leader.
            // - or if leader does.
            bool wantTracker = false;
            if (m_Actor.CountFollowers > 0)
                wantTracker = true;
            else if (m_Actor.HasLeader)
            {
                bool leaderHasTrackerEq = false;
                ItemTracker leaderTr = m_Actor.Leader.GetEquippedItem(DollPart.LEFT_HAND) as ItemTracker;
                if (leaderTr == null)
                    leaderHasTrackerEq = false;
                else if (leaderTr.CanTrackFollowersOrLeader)
                    leaderHasTrackerEq = true;

                wantTracker = leaderHasTrackerEq;
            }

            // If already equiped a cellphone, nothing to do or unequip it.
            Item eqTrack = GetEquippedCellPhone();
            if (eqTrack != null)
            {
                if (!wantTracker)
                    return new ActionUnequipItem(m_Actor, game, eqTrack);
                else
                    return null;
            }

            if (!wantTracker)
                return null;

            // Equip first available cellphone.
            Item newTracker = GetFirstTracker((it) => it.CanTrackFollowersOrLeader && !IsItemTaboo(it));
            if (newTracker != null)
            {
                // equip new.
                if (game.Rules.CanActorEquipItem(m_Actor, newTracker))
                    return new ActionEquipItem(m_Actor, game, newTracker);
            }

            // Fail.
            return null;
        }

        protected ActorAction BehaviorUnequipCellPhoneIfLeaderHasNot(RogueGame game)
        {
            // get left eq item.
            ItemTracker tr = m_Actor.GetEquippedItem(DollPart.LEFT_HAND) as ItemTracker;
            if (tr == null)
                return null;
            if (!tr.CanTrackFollowersOrLeader)
                return null;

            // we have a cell phone equiped.
            // unequip if leader has not one equiped.
            ItemTracker leaderTr = m_Actor.Leader.GetEquippedItem(DollPart.LEFT_HAND) as ItemTracker;
            if (leaderTr == null || !leaderTr.CanTrackFollowersOrLeader)
            {
                // unequip!
                if (game.Rules.CanActorUnequipItem(m_Actor, tr))
                    return new ActionUnequipItem(m_Actor, game, tr);
            }

            // fail.
            return null;
        }

        protected ActorAction BehaviorEquipLight(RogueGame game)
        {
            // If already equiped a light, nothing to do.
            Item eqLight = GetEquippedLight();
            if (eqLight != null)
                return null;

            // Equip first available light.
            Item newLight = GetFirstLight((it) => !IsItemTaboo(it));
            if (newLight != null)
            {
                // equip new.
                if (game.Rules.CanActorEquipItem(m_Actor, newLight))
                    return new ActionEquipItem(m_Actor, game, newLight);
            }

            // Fail.
            return null;
        }

        protected ActorAction BehaviorEquipStenchKiller(RogueGame game)
        {
            // If already equiped a suitable one, nothing to do.
            Item eqStench = GetEquippedStenchKiller();
            if (eqStench != null)
                return null;

            // Equip first available.
            ItemSprayScent newStench = GetFirstStenchKiller((it) => !IsItemTaboo(it));
            if (newStench != null)
            {
                // equip new.
                if (game.Rules.CanActorEquipItem(m_Actor, newStench))
                    return new ActionEquipItem(m_Actor, game, newStench);
            }

            // Fail.
            return null;
        }

        protected ActorAction BehaviorUnequipLeftItem(RogueGame game)
        {
            // get left eq item.
            Item eqLeft = m_Actor.GetEquippedItem(DollPart.LEFT_HAND);
            if (eqLeft == null)
                return null;

            // try to unequip it.
            if (game.Rules.CanActorUnequipItem(m_Actor, eqLeft))
                return new ActionUnequipItem(m_Actor, game, eqLeft);

            // fail.
            return null;
        }
        #endregion

        #region Getting items
        protected ActorAction BehaviorGrabFromStack(RogueGame game, Point position, Inventory stack)
        {
            // ignore empty stacks.
            if (stack == null || stack.IsEmpty)
                return null;

            // fix: don't try to get items under blocking map objects - bumping will say "yes can move" but we actually cannot take it.
            MapObject objThere = m_Actor.Location.Map.GetMapObjectAt(position);
            if (objThere != null)
            {
                // un-walkable fortification
                Fortification fort = objThere as Fortification;
                if (fort != null && !fort.IsWalkable)
                    return null;
                // barricaded door/window
                DoorWindow door = objThere as DoorWindow;
                if (door != null && door.IsBarricaded)
                    return null;
            }

            // for each item in the stack, consider only the takeable and interesting ones.
            Item goodItem = null;
            foreach (Item it in stack.Items)
            {
                // if can't take, ignore.
                if (!game.Rules.CanActorGetItem(m_Actor, it))
                    continue;
                // if not interesting, ignore.
                if (!IsInterestingItem(game, it))
                    continue;
                // gettable and interesting, get it.
                goodItem = it;
                break;
            }

            // if no good item, ignore.
            if (goodItem == null)
                return null;

            // take it!
            Item takeIt = goodItem;

            // emote?
            if (game.Rules.RollChance(EMOTE_GRAB_ITEM_CHANCE))
                game.DoEmote(m_Actor, String.Format("{0}! Great!", takeIt.AName));

            // try to move/get one.
            if (position == m_Actor.Location.Position)
                return new ActionTakeItem(m_Actor, game, position, takeIt);
            else
                return BehaviorIntelligentBumpToward(game, position);
        }
        #endregion

        #region Droping items
        protected ActorAction BehaviorDropItem(RogueGame game, Item it)
        {
            if (it == null)
                return null;

            // 1. unequip?
            if (game.Rules.CanActorUnequipItem(m_Actor, it))
            {
                // mark item as taboo.
                MarkItemAsTaboo(it);

                // unequip.
                return new ActionUnequipItem(m_Actor, game, it);
            }

            // 2. drop?
            if (game.Rules.CanActorDropItem(m_Actor, it))
            {
                // unmark item as taboo.
                UnmarkItemAsTaboo(it);

                // drop.
                return new ActionDropItem(m_Actor, game, it);
            }

            // failed!
            return null;
        }


        protected ActorAction BehaviorDropUselessItem(RogueGame game)
        {
            if (m_Actor.Inventory.IsEmpty)
                return null;

            // unequip/drop first light/tracker/spray out of batteries/quantity.
            foreach (Item it in m_Actor.Inventory.Items)
            {
                bool dropIt = false;

                if (it is ItemLight)
                    dropIt = (it as ItemLight).Batteries <= 0;
                else if (it is ItemTracker)
                    dropIt = (it as ItemTracker).Batteries <= 0;
                else if (it is ItemSprayPaint)
                    dropIt = (it as ItemSprayPaint).PaintQuantity <= 0;
                else if (it is ItemSprayScent)
                    dropIt = (it as ItemSprayScent).SprayQuantity <= 0;

                if (dropIt)
                    return BehaviorDropItem(game, it);
            }

            // nope.
            return null;
        }
        #endregion

        #region Inventory management
        protected ActorAction BehaviorMakeRoomForFood(RogueGame game, List<Percept> stacks)
        {
            // if no items in view, fail.
            if (stacks == null || stacks.Count == 0)
                return null;

            // if inventory not full, no need.
            int maxInv = game.Rules.ActorMaxInv(m_Actor);
            if (m_Actor.Inventory.CountItems < maxInv)
                return null;

            // if food item in inventory, no need.
            if (HasItemOfType(typeof(ItemFood)))
                return null;

            // if no food item in view, fail.
            bool hasFoodVisible = false;
            foreach (Percept p in stacks)
            {
                Inventory inv = p.Percepted as Inventory;
                if (inv == null)
                    continue;

                if (inv.HasItemOfType(typeof(ItemFood)))
                {
                    hasFoodVisible = true;
                    break;
                }
            }
            if (!hasFoodVisible)
                return null;

            // want to get rid of an item.
            // order of preference:
            // 1. get rid of not interesting item.
            // 2. get rid of barricading material.
            // 3. get rid of light & sprays.
            // 4. get rid of ammo.
            // 5. get rid of medecine.
            // 6. last resort, get rid of random item.
            Inventory myInv = m_Actor.Inventory;

            // 1. get rid of not interesting item.
            Item notInteresting = myInv.GetFirstMatching((it) => !IsInterestingItem(game, it));
            if (notInteresting != null)
                return BehaviorDropItem(game, notInteresting);

            // 2. get rid of barricading material.
            Item material = myInv.GetFirstMatching((it) => it is ItemBarricadeMaterial);
            if (material != null)
                return BehaviorDropItem(game, material);

            // 3. get rid of light & sprays.
            Item light = myInv.GetFirstMatching((it) => it is ItemLight);
            if (light != null)
                return BehaviorDropItem(game, light);
            Item spray = myInv.GetFirstMatching((it) => it is ItemSprayPaint);
            if (spray != null)
                return BehaviorDropItem(game, spray);
            spray = myInv.GetFirstMatching((it) => it is ItemSprayScent);
            if (spray != null)
                return BehaviorDropItem(game, spray);

            // 4. get rid of ammo.
            Item ammo = myInv.GetFirstMatching((it) => it is ItemAmmo);
            if (ammo != null)
                return BehaviorDropItem(game, ammo);

            // 5. get rid of medecine.
            Item med = myInv.GetFirstMatching((it) => it is ItemMedicine);
            if (med != null)
                return BehaviorDropItem(game, med);

            // 6. last resort, get rid of random item.
            Item anyItem = myInv[game.Rules.Roll(0, myInv.CountItems)];
            return BehaviorDropItem(game, anyItem);
        }
        #endregion
    }
}