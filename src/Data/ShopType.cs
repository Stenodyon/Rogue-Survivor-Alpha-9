using System;

using djack.RogueSurvivor.Engine;
using djack.RogueSurvivor.Gameplay;
using djack.RogueSurvivor.Gameplay.Generators;

namespace djack.RogueSurvivor.Data
{
    /* Holds data for each type of shop */
    class ShopType
    {
        public string Name { get; private set; }
        public string Image { get; private set; }

        public ShopType(string name, string image)
        {
            this.Name = name;
            this.Image = image;
        }

        public virtual Item GetRandomItem(BaseTownGenerator generator,
                                          DiceRoller dice)
        {
            return null;
        }

        public static ShopType PickRandomShop(DiceRoller dice)
        {
            int shop_id = dice.Roll(0, shopTypes.Length);
            return shopTypes[shop_id];
        }

        public static ShopType GENERAL_STORE = new GeneralStore();
        public static ShopType GROCERY = new Grocery();
        public static ShopType SPORTSWEAR = new Sportswear();
        public static ShopType PHARMACY = new Pharmacy();
        public static ShopType CONSTRUCTION = new Construction();
        public static ShopType GUNSHOP = new Gunshop();
        public static ShopType HUNTING = new Hunting();

        public static ShopType[] shopTypes = {
            GENERAL_STORE,
            GROCERY,
            SPORTSWEAR,
            PHARMACY,
            CONSTRUCTION,
            GUNSHOP,
            HUNTING
        };

        private class GeneralStore : ShopType
        {
            public GeneralStore()
                : base("General Store", GameImages.DECO_SHOP_GENERAL_STORE)
            {}

            public override Item GetRandomItem(BaseTownGenerator generator,
                                               DiceRoller dice)
            {
                int roll = dice.Roll(0, 6);
                switch(roll)
                {
                    case 0:
                        return PHARMACY.GetRandomItem(generator, dice);
                    case 1:
                        return SPORTSWEAR.GetRandomItem(generator, dice);
                    case 2:
                        return CONSTRUCTION.GetRandomItem(generator, dice);
                    case 3:
                        return GROCERY.GetRandomItem(generator, dice);
                    case 4:
                        return HUNTING.GetRandomItem(generator, dice);
                    case 5:
                        return generator.MakeRandomBedroomItem();
                    default: 
                        throw new ArgumentOutOfRangeException("unhandled roll");
                }
            }
        }

        private class Grocery : ShopType
        {
            public Grocery()
                : base("Grocery", GameImages.DECO_SHOP_GROCERY)
            {}

            public override Item GetRandomItem(BaseTownGenerator generator,
                                               DiceRoller dice)
            {
                if(dice.RollChance(50))
                    return generator.MakeItemCannedFood();
                else
                    return generator.MakeItemGroceries();
            }
        }

        private class Sportswear : ShopType
        {
            public Sportswear()
                : base("Sportswear", GameImages.DECO_SHOP_SPORTSWEAR)
            {}

            public override Item GetRandomItem(BaseTownGenerator generator,
                                               DiceRoller dice)
            {
                int roll = dice.Roll(0, 10);

                switch (roll)
                {
                    case 0:
                        if (dice.RollChance(30))
                            return generator.MakeItemHuntingRifle();
                        else
                            return generator.MakeItemLightRifleAmmo();
                    case 1:
                        if (dice.RollChance(30))
                            return generator.MakeItemHuntingCrossbow();
                        else
                            return generator.MakeItemBoltsAmmo();
                    case 2:
                    case 3:
                    case 4:
                    case 5: return generator.MakeItemBaseballBat();       // 40%

                    case 6:
                    case 7: return generator.MakeItemIronGolfClub();      // 20%

                    case 8:
                    case 9: return generator.MakeItemGolfClub();          // 20%
                    default:
                        throw new ArgumentOutOfRangeException("unhandled roll");
                }
            }
        }

        private class Pharmacy : ShopType
        {
            public Pharmacy()
                : base("Pharmacy", GameImages.DECO_SHOP_PHARMACY)
            {}

            public override Item GetRandomItem(BaseTownGenerator generator,
                                               DiceRoller dice)
            {
                int randomItem = dice.Roll(0, 6);
                switch (randomItem)
                {
                    case 0: return generator.MakeItemBandages();
                    case 1: return generator.MakeItemMedikit();
                    case 2: return generator.MakeItemPillsSLP();
                    case 3: return generator.MakeItemPillsSTA();
                    case 4: return generator.MakeItemPillsSAN();
                    case 5: return generator.MakeItemStenchKiller();

                    default:
                        throw new ArgumentOutOfRangeException("unhandled roll");
                }
            }
        }

        private class Construction : ShopType
        {
            public Construction()
                : base("Construction", GameImages.DECO_SHOP_CONSTRUCTION)
            {}

            public override Item GetRandomItem(BaseTownGenerator generator,
                                               DiceRoller dice)
            {
                int roll = dice.Roll(0, 8);
                switch (roll)
                {
                    case 0:
                        return dice.RollChance(50) ? generator.MakeItemShovel()
                                                   : generator.MakeItemShortShovel();
                    case 1:
                        return generator.MakeItemCrowbar();
                    case 2:
                        return dice.RollChance(50) ? generator.MakeItemHugeHammer()
                                                   : generator.MakeItemSmallHammer();
                    case 3:
                        return generator.MakeItemWoodenPlank();
                    case 4:
                        return generator.MakeItemFlashlight();
                    case 5:
                        return generator.MakeItemBigFlashlight();
                    case 6:
                        return generator.MakeItemSpikes();
                    case 7:
                        return generator.MakeItemBarbedWire();

                    default:
                        throw new ArgumentOutOfRangeException("unhandled roll");
                }
            }
        }

        private class Gunshop : ShopType
        {
            public Gunshop()
                : base("Gunshop", GameImages.DECO_SHOP_GUNSHOP)
            {}

            public override Item GetRandomItem(BaseTownGenerator generator,
                                               DiceRoller dice)
            {
                // Weapons (40%) vs Ammo (60%)
                if (dice.RollChance(40))
                {
                    int roll = dice.Roll(0, 4);

                    switch (roll)
                    {
                        case 0: return generator.MakeItemRandomPistol();
                        case 1: return generator.MakeItemShotgun();
                        case 2: return generator.MakeItemHuntingRifle();
                        case 3: return generator.MakeItemHuntingCrossbow();

                        default:
                            return null;
                    }
                }
                else
                {
                    int roll = dice.Roll(0, 4);

                    switch (roll)
                    {
                        case 0: return generator.MakeItemLightPistolAmmo();
                        case 1: return generator.MakeItemShotgunAmmo();
                        case 2: return generator.MakeItemLightRifleAmmo();
                        case 3: return generator.MakeItemBoltsAmmo();

                        default:
                            return null;
                    }
                }
            }
        }

        private class Hunting : ShopType
        {
            public Hunting()
                : base("Hunting", GameImages.DECO_SHOP_HUNTING)
            {}

            public override Item GetRandomItem(BaseTownGenerator generator,
                                               DiceRoller dice)
            {
                // Weapons/Ammo (50%) Outfits&Traps (50%)
                if (dice.RollChance(50))
                {
                    // Weapons(40) Ammo(60)
                    if (dice.RollChance(40))
                    {
                        int roll = dice.Roll(0, 2);

                        switch (roll)
                        {
                            case 0: return generator.MakeItemHuntingRifle();
                            case 1: return generator.MakeItemHuntingCrossbow();
                            default:
                                return null;
                        }
                    }
                    else
                    {
                        int roll = dice.Roll(0, 2);

                        switch (roll)
                        {
                            case 0: return generator.MakeItemLightRifleAmmo();
                            case 1: return generator.MakeItemBoltsAmmo();
                            default:
                                return null;
                        }
                    }
                }
                else
                {
                    // Outfits&Traps
                    int roll = dice.Roll(0, 2);
                    switch (roll)
                    {
                        case 0: return generator.MakeItemHunterVest();
                        case 1: return generator.MakeItemBearTrap();
                        default: 
                            return null;
                    }
                }
            }
        }
    }
}