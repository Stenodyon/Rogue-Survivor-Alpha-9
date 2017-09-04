using System;

using djack.RogueSurvivor.Engine;
using djack.RogueSurvivor.Engine.Items;
using djack.RogueSurvivor.Data;

namespace djack.RogueSurvivor.Gameplay.Generators
{
    /* This part of BaseMapGenerator provides item makers */
    abstract partial class BaseMapGenerator : MapGenerator
    {
        public Item MakeItemBandages()
        {
            return new ItemMedicine(m_Game.GameItems.BANDAGE)
            {
                Quantity = m_Rules.Roll(1, m_Game.GameItems.BANDAGE.StackingLimit)
            };
        }

        public Item MakeItemMedikit()
        {
            return new ItemMedicine(m_Game.GameItems.MEDIKIT);
        }

        public Item MakeItemPillsSTA()
        {
            return new ItemMedicine(m_Game.GameItems.PILLS_STA)
            {
                Quantity = m_Rules.Roll(1, m_Game.GameItems.PILLS_STA.StackingLimit)
            };
        }

        public Item MakeItemPillsSLP()
        {
            return new ItemMedicine(m_Game.GameItems.PILLS_SLP)
            {
                Quantity = m_Rules.Roll(1, m_Game.GameItems.PILLS_SLP.StackingLimit)
            };
        }

        public Item MakeItemPillsSAN()
        {
            return new ItemMedicine(m_Game.GameItems.PILLS_SAN)
            {
                Quantity = m_Rules.Roll(1, m_Game.GameItems.PILLS_SAN.StackingLimit)
            };
        }

        public Item MakeItemPillsAntiviral()
        {
            return new ItemMedicine(m_Game.GameItems.PILLS_ANTIVIRAL)
            {
                Quantity = m_Rules.Roll(1, m_Game.GameItems.PILLS_ANTIVIRAL.StackingLimit)
            };
        }


        public Item MakeItemGroceries()
        {
            // FIXME: should be map local time.
            int timeNow = m_Game.Session.WorldTime.TurnCounter;

            int max = WorldTime.TURNS_PER_DAY * m_Game.GameItems.GROCERIES.BestBeforeDays;
            int min = max / 2;
            int freshUntil = timeNow + m_Rules.Roll(min, max);

            return new ItemFood(m_Game.GameItems.GROCERIES, freshUntil);
        }

        public Item MakeItemCannedFood()
        {
            // canned food not perishable.
            return new ItemFood(m_Game.GameItems.CANNED_FOOD)
            {
                Quantity = m_Rules.Roll(1, m_Game.GameItems.CANNED_FOOD.StackingLimit)
            };
        }

        public Item MakeItemCrowbar()
        {
            return new ItemMeleeWeapon(m_Game.GameItems.CROWBAR)
            {
                Quantity = m_Rules.Roll(1, m_Game.GameItems.CROWBAR.StackingLimit)
            };
        }

        public Item MakeItemBaseballBat()
        {
            return new ItemMeleeWeapon(m_Game.GameItems.BASEBALLBAT);
        }

        public Item MakeItemCombatKnife()
        {
            return new ItemMeleeWeapon(m_Game.GameItems.COMBAT_KNIFE);
        }

        public Item MakeItemTruncheon()
        {
            return new ItemMeleeWeapon(m_Game.GameItems.TRUNCHEON);
        }

        public Item MakeItemGolfClub()
        {
            return new ItemMeleeWeapon(m_Game.GameItems.GOLFCLUB);
        }

        public Item MakeItemIronGolfClub()
        {
            return new ItemMeleeWeapon(m_Game.GameItems.IRON_GOLFCLUB);
        }

        public Item MakeItemHugeHammer()
        {
            return new ItemMeleeWeapon(m_Game.GameItems.HUGE_HAMMER);
        }

        public Item MakeItemSmallHammer()
        {
            return new ItemMeleeWeapon(m_Game.GameItems.SMALL_HAMMER);
        }

        public Item MakeItemJasonMyersAxe()
        {
            return new ItemMeleeWeapon(m_Game.GameItems.UNIQUE_JASON_MYERS_AXE)
            {
                IsUnique = true
            };
        }

        public Item MakeItemShovel()
        {
            return new ItemMeleeWeapon(m_Game.GameItems.SHOVEL);
        }

        public Item MakeItemShortShovel()
        {
            return new ItemMeleeWeapon(m_Game.GameItems.SHORT_SHOVEL);
        }

        public ItemBarricadeMaterial MakeItemWoodenPlank()
        {
            return new ItemBarricadeMaterial(m_Game.GameItems.WOODENPLANK);
        }

        public Item MakeItemHuntingCrossbow()
        {
            return new ItemRangedWeapon(m_Game.GameItems.HUNTING_CROSSBOW);
        }

        public Item MakeItemBoltsAmmo()
        {
            return new ItemAmmo(m_Game.GameItems.AMMO_BOLTS);
        }

        public Item MakeItemHuntingRifle()
        {
            return new ItemRangedWeapon(m_Game.GameItems.HUNTING_RIFLE);
        }

        public Item MakeItemLightRifleAmmo()
        {
            return new ItemAmmo(m_Game.GameItems.AMMO_LIGHT_RIFLE);
        }

        public Item MakeItemPistol()
        {
            return new ItemRangedWeapon(m_Game.GameItems.PISTOL);
        }

        public Item MakeItemKoltRevolver()
        {
            return new ItemRangedWeapon(m_Game.GameItems.KOLT_REVOLVER);
        }

        public Item MakeItemRandomPistol()
        {
            return m_Game.Rules.RollChance(50) ? MakeItemPistol() : MakeItemKoltRevolver();
        }

        public Item MakeItemLightPistolAmmo()
        {
            return new ItemAmmo(m_Game.GameItems.AMMO_LIGHT_PISTOL);
        }

        public Item MakeItemShotgun()
        {
            return new ItemRangedWeapon(m_Game.GameItems.SHOTGUN);
        }

        public Item MakeItemShotgunAmmo()
        {
            return new ItemAmmo(m_Game.GameItems.AMMO_SHOTGUN);
        }

        public Item MakeItemCHARLightBodyArmor()
        {
            return new ItemBodyArmor(m_Game.GameItems.CHAR_LT_BODYARMOR);
        }

        public Item MakeItemBikerGangJacket(GameGangs.IDs gangId)
        {
            switch (gangId)
            {
                case GameGangs.IDs.BIKER_FREE_ANGELS:
                    return new ItemBodyArmor(m_Game.GameItems.FREE_ANGELS_JACKET);
                case GameGangs.IDs.BIKER_HELLS_SOULS:
                    return new ItemBodyArmor(m_Game.GameItems.HELLS_SOULS_JACKET);
                default:
                    throw new ArgumentException("unhandled biker gang");
            }
        }

        public Item MakeItemPoliceJacket()
        {
            return new ItemBodyArmor(m_Game.GameItems.POLICE_JACKET);
        }

        public Item MakeItemPoliceRiotArmor()
        {
            return new ItemBodyArmor(m_Game.GameItems.POLICE_RIOT);
        }

        public Item MakeItemHunterVest()
        {
            return new ItemBodyArmor(m_Game.GameItems.HUNTER_VEST);
        }

        public Item MakeItemCellPhone()
        {
            return new ItemTracker(m_Game.GameItems.CELL_PHONE);
        }

        public Item MakeItemSprayPaint()
        {
            // random color.
            ItemSprayPaintModel paintModel;
            int roll = m_Game.Rules.Roll(0, 4);
            switch (roll)
            {
                case 0: paintModel = m_Game.GameItems.SPRAY_PAINT1; break;
                case 1: paintModel = m_Game.GameItems.SPRAY_PAINT2; break;
                case 2: paintModel = m_Game.GameItems.SPRAY_PAINT3; break;
                case 3: paintModel = m_Game.GameItems.SPRAY_PAINT4; break;
                default:
                    throw new ArgumentOutOfRangeException("unhandled roll");
            }

            return new ItemSprayPaint(paintModel);
        }

        public Item MakeItemStenchKiller()
        {
            return new ItemSprayScent(m_Game.GameItems.STENCH_KILLER);
        }

        public Item MakeItemArmyRifle()
        {
            return new ItemRangedWeapon(m_Game.GameItems.ARMY_RIFLE);
        }

        public Item MakeItemPrecisionRifle()
        {
            return new ItemRangedWeapon(m_Game.GameItems.PRECISION_RIFLE);
        }

        public Item MakeItemHeavyRifleAmmo()
        {
            return new ItemAmmo(m_Game.GameItems.AMMO_HEAVY_RIFLE);
        }

        public Item MakeItemArmyPistol()
        {
            return new ItemRangedWeapon(m_Game.GameItems.ARMY_PISTOL);
        }

        public Item MakeItemHeavyPistolAmmo()
        {
            return new ItemAmmo(m_Game.GameItems.AMMO_HEAVY_PISTOL);
        }

        public Item MakeItemArmyBodyArmor()
        {
            return new ItemBodyArmor(m_Game.GameItems.ARMY_BODYARMOR);
        }

        public Item MakeItemArmyRation()
        {
            // army rations fresh for 5 days.
            int timeNow = m_Game.Session.WorldTime.TurnCounter;
            int freshUntil = timeNow + WorldTime.TURNS_PER_DAY * m_Game.GameItems.ARMY_RATION.BestBeforeDays;

            return new ItemFood(m_Game.GameItems.ARMY_RATION, freshUntil);
        }

        public Item MakeItemFlashlight()
        {
            return new ItemLight(m_Game.GameItems.FLASHLIGHT);
        }

        public Item MakeItemBigFlashlight()
        {
            return new ItemLight(m_Game.GameItems.BIG_FLASHLIGHT);
        }

        public Item MakeItemZTracker()
        {
            return new ItemTracker(m_Game.GameItems.ZTRACKER);
        }

        public Item MakeItemBlackOpsGPS()
        {
            return new ItemTracker(m_Game.GameItems.BLACKOPS_GPS);
        }

        public Item MakeItemPoliceRadio()
        {
            return new ItemTracker(m_Game.GameItems.POLICE_RADIO);
        }

        public Item MakeItemGrenade()
        {
            return new ItemGrenade(m_Game.GameItems.GRENADE, m_Game.GameItems.GRENADE_PRIMED)
            {
                Quantity = m_Rules.Roll(1, m_Game.GameItems.GRENADE.StackingLimit)
            };
        }

        public Item MakeItemBearTrap()
        {
            return new ItemTrap(m_Game.GameItems.BEAR_TRAP);
        }

        public Item MakeItemSpikes()
        {
            return new ItemTrap(m_Game.GameItems.SPIKES)
            {
                Quantity = m_Rules.Roll(1, m_Game.GameItems.BARBED_WIRE.StackingLimit)
            };
        }

        public Item MakeItemBarbedWire()
        {
            return new ItemTrap(m_Game.GameItems.BARBED_WIRE)
            {
                Quantity = m_Rules.Roll(1, m_Game.GameItems.BARBED_WIRE.StackingLimit)
            };
        }

        public Item MakeItemBook()
        {
            return new ItemEntertainment(m_Game.GameItems.BOOK);
        }

        public Item MakeItemMagazines()
        {
            return new ItemEntertainment(m_Game.GameItems.MAGAZINE)
            {
                Quantity = m_Rules.Roll(1, m_Game.GameItems.MAGAZINE.StackingLimit)
            };
        }
    }
}