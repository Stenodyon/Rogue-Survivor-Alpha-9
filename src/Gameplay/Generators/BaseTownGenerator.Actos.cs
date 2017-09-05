using System;
using System.Collections.Generic;

using djack.RogueSurvivor.Data;
using djack.RogueSurvivor.Engine;
using djack.RogueSurvivor.Engine.Items;
using djack.RogueSurvivor.Gameplay.AI;

namespace djack.RogueSurvivor.Gameplay.Generators
{
    /* This part of BaseTownGenerator provides Actor generation functions */
    partial class BaseTownGenerator
    {
        public void GiveRandomItemToActor(DiceRoller roller, Actor actor, int spawnTime)
        {
            Item it = null;

            // rare item chance after Day X
            int day = new WorldTime(spawnTime).Day;
            if (day > Rules.GIVE_RARE_ITEM_DAY && roller.RollChance(Rules.GIVE_RARE_ITEM_CHANCE))
            {
                int roll = roller.Roll(0, 6);
                switch (roll)
                {
                    case 0: it = MakeItemGrenade(); break;
                    case 1: it = MakeItemArmyBodyArmor(); break;
                    case 2: it = MakeItemHeavyPistolAmmo(); break;
                    case 3: it = MakeItemHeavyRifleAmmo(); break;
                    case 4: it = MakeItemPillsAntiviral(); break;
                    case 5: it = MakeItemCombatKnife(); break;
                    default: it = null; break;
                }
            }
            else
            {
                // standard item.
                int roll = roller.Roll(0, 10);
                switch (roll)
                {
                    case 0: it = MakeRandomShopItem(ShopType.CONSTRUCTION); break;
                    case 1: it = MakeRandomShopItem(ShopType.GENERAL_STORE); break;
                    case 2: it = MakeRandomShopItem(ShopType.GROCERY); break;
                    case 3: it = MakeRandomShopItem(ShopType.GUNSHOP); break;
                    case 4: it = MakeRandomShopItem(ShopType.PHARMACY); break;
                    case 5: it = MakeRandomShopItem(ShopType.SPORTSWEAR); break;
                    case 6: it = MakeRandomShopItem(ShopType.HUNTING); break;
                    case 7: it = MakeRandomParkItem(); break;
                    case 8: it = MakeRandomBedroomItem(); break;
                    case 9: it = MakeRandomKitchenItem(); break;
                    default: it = null; break;
                }
            }

            if (it != null)
                actor.Inventory.AddAll(it);
        }

        public Actor CreateNewRefugee(int spawnTime, int itemsToCarry)
        {
            Actor newRefugee;

            // civilian, policeman?
            if (m_DiceRoller.RollChance(Params.PolicemanChance))
            {
                newRefugee = CreateNewPoliceman(spawnTime);
                // add random items.
                for(int i = 0; i < itemsToCarry && newRefugee.Inventory.CountItems < newRefugee.Inventory.MaxCapacity; i++)
                    GiveRandomItemToActor(m_DiceRoller, newRefugee, spawnTime);
            }
            else
            {
                newRefugee = CreateNewCivilian(spawnTime, itemsToCarry, 1);
            }

            // give skills : 1 per day + 1 for starting.
            int nbSkills = 1 + new WorldTime(spawnTime).Day;
            base.GiveRandomSkillsToActor(m_DiceRoller, newRefugee, nbSkills);

            // done.
            return newRefugee;
        }

        public Actor CreateNewSurvivor(int spawnTime)
        {
            // decide model.
            bool isMale = m_Rules.Roll(0, 2) == 0;
            ActorModel model = isMale ? m_Game.GameActors.MaleCivilian : m_Game.GameActors.FemaleCivilian;

            // create.
            Actor survivor = model.CreateNumberedName(m_Game.GameFactions.TheSurvivors, spawnTime);

            // setup.
            base.GiveNameToActor(m_DiceRoller, survivor);
            base.DressCivilian(m_DiceRoller, survivor);
            survivor.Doll.AddDecoration(DollPart.HEAD, isMale ? GameImages.SURVIVOR_MALE_BANDANA : GameImages.SURVIVOR_FEMALE_BANDANA);

            // give items, good survival gear (7 items).
            #region
            // 1,2   1 can of food, 1 amr.
            survivor.Inventory.AddAll(MakeItemCannedFood());
            survivor.Inventory.AddAll(MakeItemArmyRation());
            // 3,4. 1 fire weapon with 1 ammo box or grenade.
            if (m_DiceRoller.RollChance(50))
            {
                survivor.Inventory.AddAll(MakeItemArmyRifle());
                if (m_DiceRoller.RollChance(50))
                    survivor.Inventory.AddAll(MakeItemHeavyRifleAmmo());
                else
                    survivor.Inventory.AddAll(MakeItemGrenade());
            }
            else
            {
                survivor.Inventory.AddAll(MakeItemShotgun());
                if (m_DiceRoller.RollChance(50))
                    survivor.Inventory.AddAll(MakeItemShotgunAmmo());
                else
                    survivor.Inventory.AddAll(MakeItemGrenade());
            }
            // 5    1 healing item.
            survivor.Inventory.AddAll(MakeItemMedikit());

            // 6    1 pill item.
            switch (m_DiceRoller.Roll(0, 3))
            {
                case 0: survivor.Inventory.AddAll(MakeItemPillsSLP()); break;
                case 1: survivor.Inventory.AddAll(MakeItemPillsSTA()); break;
                case 2: survivor.Inventory.AddAll(MakeItemPillsSAN()); break;
            }
            // 7    1 armor.
            survivor.Inventory.AddAll(MakeItemArmyBodyArmor());
            #endregion

            // give skills : 1 per day + 5 as bonus.
            int nbSkills = 3 + new WorldTime(spawnTime).Day;
            base.GiveRandomSkillsToActor(m_DiceRoller, survivor, nbSkills);

            // AI.
            survivor.Controller = new CivilianAI();

            // slightly randomize Food and Sleep - 0..25%.
            int foodDeviation = (int)(0.25f * survivor.FoodPoints);
            survivor.FoodPoints = survivor.FoodPoints - m_Rules.Roll(0, foodDeviation);
            int sleepDeviation = (int)(0.25f * survivor.SleepPoints);
            survivor.SleepPoints = survivor.SleepPoints - m_Rules.Roll(0, sleepDeviation);

            // done.
            return survivor;
        }

        public Actor CreateNewNakedHuman(int spawnTime, int itemsToCarry, int skills)
        {
            // decide model.
            ActorModel model = m_Rules.Roll(0, 2) == 0 ? m_Game.GameActors.MaleCivilian : m_Game.GameActors.FemaleCivilian;

            // create.
            Actor civilian = model.CreateNumberedName(m_Game.GameFactions.TheCivilians, spawnTime);

            // done.
            return civilian;
        }

        public Actor CreateNewCivilian(int spawnTime, int itemsToCarry, int skills)
        {
            // decide model.
            ActorModel model = m_Rules.Roll(0, 2) == 0 ? m_Game.GameActors.MaleCivilian : m_Game.GameActors.FemaleCivilian;

            // create.
            Actor civilian = model.CreateNumberedName(m_Game.GameFactions.TheCivilians, spawnTime);

            // setup.
            base.DressCivilian(m_DiceRoller, civilian);
            base.GiveNameToActor(m_DiceRoller, civilian);
            for (int i = 0; i < itemsToCarry; i++)
                GiveRandomItemToActor(m_DiceRoller, civilian, spawnTime);
            base.GiveRandomSkillsToActor(m_DiceRoller, civilian, skills);
            civilian.Controller = new CivilianAI();

            // slightly randomize Food and Sleep - 0..25%.
            int foodDeviation = (int)(0.25f * civilian.FoodPoints);
            civilian.FoodPoints = civilian.FoodPoints - m_Rules.Roll(0, foodDeviation);
            int sleepDeviation = (int)(0.25f * civilian.SleepPoints);
            civilian.SleepPoints = civilian.SleepPoints - m_Rules.Roll(0, sleepDeviation);

            // done.
            return civilian;
        }

        public Actor CreateNewPoliceman(int spawnTime)
        {
            // model.
            ActorModel model = m_Game.GameActors.Policeman;

            // create.
            Actor newCop = model.CreateNumberedName(m_Game.GameFactions.ThePolice, spawnTime);

            // setup.
            base.DressPolice(m_DiceRoller, newCop);
            base.GiveNameToActor(m_DiceRoller, newCop);
            newCop.Name = "Cop " + newCop.Name;
            base.GiveRandomSkillsToActor(m_DiceRoller, newCop, 1);
            base.GiveStartingSkillToActor(newCop, Skills.IDs.FIREARMS);
            base.GiveStartingSkillToActor(newCop, Skills.IDs.LEADERSHIP);
            newCop.Controller = new CivilianAI();

            // give items.
            if (m_DiceRoller.RollChance(50))
            {
                // pistol
                newCop.Inventory.AddAll(MakeItemPistol());
                newCop.Inventory.AddAll(MakeItemLightPistolAmmo());
            }
            else
            {
                // shoty
                newCop.Inventory.AddAll(MakeItemShotgun());
                newCop.Inventory.AddAll(MakeItemShotgunAmmo());
            }
            newCop.Inventory.AddAll(MakeItemTruncheon());
            newCop.Inventory.AddAll(MakeItemFlashlight());
            newCop.Inventory.AddAll(MakeItemPoliceRadio());
            if (m_DiceRoller.RollChance(50))
            {
                if (m_DiceRoller.RollChance(80))
                    newCop.Inventory.AddAll(MakeItemPoliceJacket());
                else
                    newCop.Inventory.AddAll(MakeItemPoliceRiotArmor());
            }

            // done.
            return newCop;
        }

        public Actor CreateNewUndead(int spawnTime)
        {
            Actor newUndead;

            if (Rules.HasAllZombies(m_Game.Session.GameMode))
            {
                // decide model.
                ActorModel undeadModel;
                int chance = m_Rules.Roll(0, 100);
                undeadModel = (chance < RogueGame.Options.SpawnSkeletonChance ? m_Game.GameActors.Skeleton :
                    chance < RogueGame.Options.SpawnSkeletonChance + RogueGame.Options.SpawnZombieChance ? m_Game.GameActors.Zombie :
                    chance < RogueGame.Options.SpawnSkeletonChance + RogueGame.Options.SpawnZombieChance + RogueGame.Options.SpawnZombieMasterChance ? m_Game.GameActors.ZombieMaster :
                     m_Game.GameActors.Skeleton);

                // create.
                newUndead = undeadModel.CreateNumberedName(m_Game.GameFactions.TheUndeads, spawnTime);
            }
            else
            {
                // zombified.
                newUndead = MakeZombified(null, CreateNewCivilian(spawnTime, 0, 0), spawnTime);
                // skills?
                WorldTime time = new WorldTime(spawnTime);
                int nbSkills = time.Day / 2;
                if (nbSkills > 0)
                {
                    for (int i = 0; i < nbSkills; i++)
                    {
                        Skills.IDs? zombifiedSkill = m_Game.ZombifySkill((Skills.IDs)m_Rules.Roll(0, (int)Skills.IDs._COUNT));
                        if (zombifiedSkill.HasValue)
                            m_Game.SkillUpgrade(newUndead, zombifiedSkill.Value);
                    }
                    RecomputeActorStartingStats(newUndead);
                }
            }

            // done.
            return newUndead;
        }

        public Actor MakeZombified(Actor zombifier, Actor deadVictim, int turn)
        {
            // create actor.
            string zombiefiedName = String.Format("{0}'s zombie", deadVictim.UnmodifiedName);
            ActorModel zombiefiedModel = deadVictim.Doll.Body.IsMale ? m_Game.GameActors.MaleZombified : m_Game.GameActors.FemaleZombified;
            Faction zombieFaction = (zombifier == null ? m_Game.GameFactions.TheUndeads : zombifier.Faction);
            Actor newZombie = zombiefiedModel.CreateNamed(zombieFaction, zombiefiedName, deadVictim.IsPluralName, turn);

            // dress as victim.
            for (DollPart p = DollPart._FIRST; p < DollPart._COUNT; p++)
            {
                List<string> partDecos = deadVictim.Doll.GetDecorations(p);
                if (partDecos != null)
                {
                    foreach (string deco in partDecos)
                        newZombie.Doll.AddDecoration(p, deco);
                }
            }

            // add blood.
            newZombie.Doll.AddDecoration(DollPart.TORSO, GameImages.BLOODIED);

            return newZombie;
        }

        public Actor CreateNewSewersUndead(int spawnTime)
        {
            if (!Rules.HasAllZombies(m_Game.Session.GameMode))
                return CreateNewUndead(spawnTime);

            // decide model. 
            ActorModel undeadModel = m_DiceRoller.RollChance(80) ? m_Game.GameActors.RatZombie : m_Game.GameActors.Zombie;

            // create.
            Actor newUndead = undeadModel.CreateNumberedName(m_Game.GameFactions.TheUndeads, spawnTime);

            // done.
            return newUndead;
        }

        public Actor CreateNewBasementRatZombie(int spawnTime)
        {
            if (!Rules.HasAllZombies(m_Game.Session.GameMode))
                return CreateNewUndead(spawnTime);

            return m_Game.GameActors.RatZombie.CreateNumberedName(m_Game.GameFactions.TheUndeads, spawnTime);
        }

        public Actor CreateNewSubwayUndead(int spawnTime)
        {
            if (!Rules.HasAllZombies(m_Game.Session.GameMode))
                return CreateNewUndead(spawnTime);

            // standard zombies.
            ActorModel undeadModel = m_Game.GameActors.Zombie;

            // create.
            Actor newUndead = undeadModel.CreateNumberedName(m_Game.GameFactions.TheUndeads, spawnTime);

            // done.
            return newUndead;
        }

        public Actor CreateNewCHARGuard(int spawnTime)
        {
            // model.
            ActorModel model = m_Game.GameActors.CHARGuard;

            // create.
            Actor newGuard = model.CreateNumberedName(m_Game.GameFactions.TheCHARCorporation, spawnTime);

            // setup.
            base.DressCHARGuard(m_DiceRoller, newGuard);
            base.GiveNameToActor(m_DiceRoller, newGuard);
            newGuard.Name = "Gd. " + newGuard.Name;

            // give items.
            newGuard.Inventory.AddAll(MakeItemShotgun());
            newGuard.Inventory.AddAll(MakeItemShotgunAmmo());
            newGuard.Inventory.AddAll(MakeItemCHARLightBodyArmor());

            // done.
            return newGuard;
        }

        public Actor CreateNewArmyNationalGuard(int spawnTime, string rankName)
        {
            // model.
            ActorModel model = m_Game.GameActors.NationalGuard;

            // create.
            Actor newNat = model.CreateNumberedName(m_Game.GameFactions.TheArmy, spawnTime);

            // setup.
            base.DressArmy(m_DiceRoller, newNat);
            base.GiveNameToActor(m_DiceRoller, newNat);
            newNat.Name = rankName + " " + newNat.Name;

            // give items 6/7.
            newNat.Inventory.AddAll(MakeItemArmyRifle());
            newNat.Inventory.AddAll(MakeItemHeavyRifleAmmo());
            newNat.Inventory.AddAll(MakeItemArmyPistol());
            newNat.Inventory.AddAll(MakeItemHeavyPistolAmmo());
            newNat.Inventory.AddAll(MakeItemArmyBodyArmor());
            ItemBarricadeMaterial planks = MakeItemWoodenPlank();
            planks.Quantity = m_Game.GameItems.WOODENPLANK.StackingLimit;
            newNat.Inventory.AddAll(planks);

            // skills : carpentry for building small barricades.
            GiveStartingSkillToActor(newNat, Skills.IDs.CARPENTRY);

            // give skills : 1 per day after min arrival date.
            int nbSkills = new WorldTime(spawnTime).Day - RogueGame.NATGUARD_DAY;
            if (nbSkills > 0)
                base.GiveRandomSkillsToActor(m_DiceRoller, newNat, nbSkills);
    
            // done.
            return newNat;
        }

        public Actor CreateNewBikerMan(int spawnTime, GameGangs.IDs gangId)
        {
             // decide model.
            ActorModel model = m_Game.GameActors.BikerMan;

            // create.
            Actor newBiker = model.CreateNumberedName(m_Game.GameFactions.TheBikers, spawnTime);

            // setup.
            newBiker.GangID = (int)gangId;
            base.DressBiker(m_DiceRoller, newBiker);
            base.GiveNameToActor(m_DiceRoller, newBiker);
            newBiker.Controller = new GangAI();

            // give items.
            newBiker.Inventory.AddAll(m_DiceRoller.RollChance(50) ? MakeItemCrowbar() : MakeItemBaseballBat());
            newBiker.Inventory.AddAll(MakeItemBikerGangJacket(gangId));

            // give skills : 1 per day after min arrival date.
            int nbSkills = new WorldTime(spawnTime).Day - RogueGame.BIKERS_RAID_DAY;
            if (nbSkills > 0)
                base.GiveRandomSkillsToActor(m_DiceRoller, newBiker, nbSkills);

            // done.
            return newBiker;
        }

        public Actor CreateNewGangstaMan(int spawnTime, GameGangs.IDs gangId)
        {
            // decide model.
            ActorModel model = m_Game.GameActors.GangstaMan;

            // create.
            Actor newGangsta = model.CreateNumberedName(m_Game.GameFactions.TheGangstas, spawnTime);

            // setup.
            newGangsta.GangID = (int)gangId;
            base.DressGangsta(m_DiceRoller, newGangsta);
            base.GiveNameToActor(m_DiceRoller, newGangsta);
            newGangsta.Controller = new GangAI();

            // give items.
            newGangsta.Inventory.AddAll(m_DiceRoller.RollChance(50) ? MakeItemRandomPistol() : MakeItemBaseballBat());


            // give skills : 1 per day after min arrival date.
            int nbSkills = new WorldTime(spawnTime).Day - RogueGame.GANGSTAS_RAID_DAY;
            if (nbSkills > 0)
                base.GiveRandomSkillsToActor(m_DiceRoller, newGangsta, nbSkills);

            // done.
            return newGangsta;
        }

        public Actor CreateNewBlackOps(int spawnTime, string rankName)
        {
            // model.
            ActorModel model = m_Game.GameActors.BlackOps;

            // create.
            Actor newBO = model.CreateNumberedName(m_Game.GameFactions.TheBlackOps, spawnTime);

            // setup.
            base.DressBlackOps(m_DiceRoller, newBO);
            base.GiveNameToActor(m_DiceRoller, newBO);
            newBO.Name = rankName + " " + newBO.Name;

            // give items.
            newBO.Inventory.AddAll(MakeItemPrecisionRifle());
            newBO.Inventory.AddAll(MakeItemHeavyRifleAmmo());
            newBO.Inventory.AddAll(MakeItemArmyPistol());
            newBO.Inventory.AddAll(MakeItemHeavyPistolAmmo());
            newBO.Inventory.AddAll(MakeItemBlackOpsGPS());

            // done.
            return newBO;
        }

        public Actor CreateNewFeralDog(int spawnTime)
        {
            Actor newDog;

            // model
            newDog = m_Game.GameActors.FeralDog.CreateNumberedName(m_Game.GameFactions.TheFerals, spawnTime);

            // skin
            SkinDog(m_DiceRoller, newDog);

            // done.
            return newDog;
        }

    }
}