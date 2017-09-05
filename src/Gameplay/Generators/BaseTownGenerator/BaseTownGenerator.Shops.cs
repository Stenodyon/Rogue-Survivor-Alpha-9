using System;
using System.Drawing;
using System.Collections.Generic;

using djack.RogueSurvivor.Data;
using djack.RogueSurvivor.Engine;

namespace djack.RogueSurvivor.Gameplay.Generators
{
    partial class BaseTownGenerator
    {
        private class ShopGenerator
        {
            BaseTownGenerator parent;

            Map map;
            Block b;

            ShopType shopType;

            int alleysStartX;
            int alleysStartY;
            int alleysEndX;
            int alleysEndY;
            bool horizontalAlleys;

            public ShopGenerator(BaseTownGenerator parent, Map map, Block b)
            {
                this.parent = parent;
                this.map = map;
                this.b = b;
                shopType = ShopType.PickRandomShop(parent.m_DiceRoller);
            }

            public bool Generate()
            {
                // 0. Check suitability
                if (b.InsideRect.Width < 5 || b.InsideRect.Height < 5)
                    return false;

                // 1. Walkway, floor & walls
                parent.TileRectangle(map, parent.m_Game.GameTiles.FLOOR_WALKWAY, b.Rectangle);
                parent.TileRectangle(map, parent.m_Game.GameTiles.WALL_STONE, b.BuildingRect);
                parent.TileFill(map, parent.m_Game.GameTiles.FLOOR_TILES, b.InsideRect, (tile, prevmodel, x, y) => tile.IsInside = true);

                // 3. Make sections alleys with displays.
                MakeAlleys();

                // 4. Entry door with shop ids
                //    Might add window(s).
                MakeEntrance();

                ///////////////////////////
                // 5. Add items to shelves.
                ///////////////////////////
                #region
                parent.ItemsDrop(map, b.InsideRect,
                    (pt) =>
                    {
                        MapObject mapObj = map.GetMapObjectAt(pt);
                        if (mapObj == null)
                            return false;
                        return mapObj.ImageID == GameImages.OBJ_SHOP_SHELF &&
                            parent.m_DiceRoller.RollChance(parent.m_Params.ItemInShopShelfChance);
                    },
                    (pt) => shopType.GetRandomItem(parent, parent.m_DiceRoller));
                #endregion

                // 6. Zone
                // shop building.
                map.AddZone(parent.MakeUniqueZone(shopType.Name, b.BuildingRect));
                // walkway zones.
                parent.MakeWalkwayZones(map, b);

                // 7. Basement?
                if (parent.m_DiceRoller.RollChance(SHOP_BASEMENT_CHANCE))
                    MakeBasement();

                // Done
                return true;
            }

            private void MakeAlleys()
            {
                alleysStartX = b.InsideRect.Left;
                alleysStartY = b.InsideRect.Top;
                alleysEndX = b.InsideRect.Right;
                alleysEndY = b.InsideRect.Bottom;
                horizontalAlleys = b.Rectangle.Width >= b.Rectangle.Height;
                int centralAlley;

                if (horizontalAlleys)
                {
                    ++alleysStartX;
                    --alleysEndX;
                    centralAlley = b.InsideRect.Left + b.InsideRect.Width / 2;
                }
                else
                {
                    ++alleysStartY;
                    --alleysEndY;
                    centralAlley = b.InsideRect.Top + b.InsideRect.Height / 2;
                }
                Rectangle alleysRect = Rectangle.FromLTRB(alleysStartX, alleysStartY, alleysEndX, alleysEndY);

                parent.MapObjectFill(map, alleysRect,
                    (pt) =>
                    {
                        bool addShelf;

                        if (horizontalAlleys)
                            addShelf = ((pt.Y - alleysRect.Top) % 2 == 1) && pt.X != centralAlley;
                        else
                            addShelf = ((pt.X - alleysRect.Left) % 2 == 1) && pt.Y != centralAlley;

                        if (addShelf)
                            return parent.MakeObjShelf(GameImages.OBJ_SHOP_SHELF);
                        else
                            return null;
                    });
            }

            private void MakeEntrance()
            {
                MakeDoors();

                parent.DecorateOutsideWalls(map, b.BuildingRect, (x, y) => map.GetMapObjectAt(x, y) == null && parent.CountAdjDoors(map, x, y) >= 1 ? shopType.Image : null);

                // window?
                MakeWindow();

                // barricade certain shops types.
                if (shopType == ShopType.GUNSHOP)
                {
                    parent.BarricadeDoors(map, b.BuildingRect, Rules.BARRICADING_MAX);
                }

            }

            private void MakeDoors()
            {
                int midX = b.Rectangle.Left + b.Rectangle.Width / 2;
                int midY = b.Rectangle.Top + b.Rectangle.Height / 2;

                if(horizontalAlleys)
                {
                    bool west = parent.m_DiceRoller.RollChance(50);
                    int doorX = west ? b.BuildingRect.Left
                                     : b.BuildingRect.Right - 1;

                    PlaceDoor(doorX, midY);
                    if (b.InsideRect.Height >= 8)
                        PlaceDoor(doorX, midY - 1);
                    if (b.InsideRect.Height >= 12)
                        PlaceDoor(doorX, midY + 1);
                }
                else
                {
                    bool north = parent.m_DiceRoller.RollChance(50);
                    int doorY = north ? b.BuildingRect.Top
                                      : b.BuildingRect.Bottom - 1;

                    PlaceDoor(midX, doorY);
                    if (b.InsideRect.Width >= 8)
                        PlaceDoor(midX - 1, doorY);
                    if (b.InsideRect.Width >= 12)
                        PlaceDoor(midX + 1, doorY);
                }
            }

            private void PlaceDoor(int x, int y)
            {
                parent.PlaceDoor(map, x, y,
                                 parent.m_Game.GameTiles.FLOOR_WALKWAY,
                                 parent.MakeObjGlassDoor());
            }

            private void MakeWindow()
            {
                if (parent.m_DiceRoller.RollChance(SHOP_WINDOW_CHANCE))
                {
                    // pick a random side.
                    int side = parent.m_DiceRoller.Roll(0, 4);
                    int wx, wy;
                    switch (side)
                    {
                        case 0: wx = b.BuildingRect.Left + b.BuildingRect.Width / 2; wy = b.BuildingRect.Top; break;
                        case 1: wx = b.BuildingRect.Left + b.BuildingRect.Width / 2; wy = b.BuildingRect.Bottom - 1; break;
                        case 2: wx = b.BuildingRect.Left; wy = b.BuildingRect.Top + b.BuildingRect.Height / 2; break;
                        case 3: wx = b.BuildingRect.Right - 1; wy = b.BuildingRect.Top + b.BuildingRect.Height / 2; break;
                        default: throw new ArgumentOutOfRangeException("unhandled side");
                    }
                    // check it is ok to make a window there.
                    bool isGoodWindowPos = true;
                    if (map.GetTileAt(wx, wy).Model.IsWalkable) isGoodWindowPos = false;
                    // do it?
                    if (isGoodWindowPos)
                    {
                        parent.PlaceDoor(map, wx, wy, parent.m_Game.GameTiles.FLOOR_TILES, parent.MakeObjWindow());
                    }
                }
            }

            private void MakeBasement()
            {
                // shop basement map:                
                // - a single dark room.
                // - some shop items.

                // - a single dark room.
                Map shopBasement = new Map((map.Seed << 1) ^ shopType.Name.GetHashCode(), "basement-" + shopType.Name, b.BuildingRect.Width, b.BuildingRect.Height)
                {
                    Lighting = Lighting.DARKNESS
                };
                parent.DoForEachTile(shopBasement, shopBasement.Rect, (pt) => shopBasement.GetTileAt(pt).IsInside = true);
                parent.TileFill(shopBasement, parent.m_Game.GameTiles.FLOOR_CONCRETE);
                parent.TileRectangle(shopBasement, parent.m_Game.GameTiles.WALL_BRICK, shopBasement.Rect);
                shopBasement.AddZone(parent.MakeUniqueZone("basement", shopBasement.Rect));

                // - some shelves with shop items.
                // - some rats.
                parent.DoForEachTile(shopBasement, shopBasement.Rect,
                    (pt) =>
                    {
                        if (!shopBasement.IsWalkable(pt.X, pt.Y))
                            return;
                        if (shopBasement.GetExitAt(pt) != null)
                            return;

                        if (parent.m_DiceRoller.RollChance(SHOP_BASEMENT_SHELF_CHANCE_PER_TILE))
                        {
                            shopBasement.PlaceMapObjectAt(parent.MakeObjShelf(GameImages.OBJ_SHOP_SHELF), pt);
                            if (parent.m_DiceRoller.RollChance(SHOP_BASEMENT_ITEM_CHANCE_PER_SHELF))
                            {
                                Item it = shopType.GetRandomItem(parent, parent.m_DiceRoller);
                                if (it != null)
                                    shopBasement.DropItemAt(it, pt);
                            }
                        }

                        if (Rules.HasZombiesInBasements(parent.m_Game.Session.GameMode))
                        {
                            if (parent.m_DiceRoller.RollChance(SHOP_BASEMENT_ZOMBIE_RAT_CHANCE))
                                shopBasement.PlaceActorAt(parent.CreateNewBasementRatZombie(0), pt);
                        }
                    });

                // link maps, stairs in one corner.
                Point basementCorner = new Point();
                basementCorner.X = parent.m_DiceRoller.RollChance(50) ? 1 : shopBasement.Width - 2;
                basementCorner.Y = parent.m_DiceRoller.RollChance(50) ? 1 : shopBasement.Height - 2;
                Point shopCorner = new Point(basementCorner.X - 1 + b.InsideRect.Left, basementCorner.Y - 1 + b.InsideRect.Top);
                parent.AddExit(shopBasement, basementCorner, map, shopCorner, GameImages.DECO_STAIRS_UP, true);
                parent.AddExit(map, shopCorner, shopBasement, basementCorner, GameImages.DECO_STAIRS_DOWN, true);

                // remove any blocking object in the shop.
                MapObject blocker = map.GetMapObjectAt(shopCorner);
                if (blocker != null)
                    map.RemoveMapObjectAt(shopCorner.X, shopCorner.Y);

                // add map.
                parent.m_Params.District.AddUniqueMap(shopBasement);
            }

        } // ShopGenerator

        protected virtual bool MakeShopBuilding(Map map, Block b)
        {
            ShopGenerator generator = new ShopGenerator(this, map, b);
            return generator.Generate();
        }

        protected Item MakeRandomShopItem(ShopType shop)
        {
            return shop.GetRandomItem(this, m_DiceRoller);
        }

        public Item MakeShopGroceryItem()
        {
            return ShopType.GROCERY.GetRandomItem(this, m_DiceRoller);
        }

        public Item MakeShopPharmacyItem()
        {
            return ShopType.PHARMACY.GetRandomItem(this, m_DiceRoller);
        }

        public Item MakeShopSportsWearItem()
        {
            return ShopType.SPORTSWEAR.GetRandomItem(this, m_DiceRoller);
        }

        public Item MakeShopConstructionItem()
        {
            return ShopType.CONSTRUCTION.GetRandomItem(this, m_DiceRoller);
        }

        public Item MakeShopGunshopItem()
        {
            return ShopType.GUNSHOP.GetRandomItem(this, m_DiceRoller);
        }

        public Item MakeHuntingShopItem()
        {
            return ShopType.HUNTING.GetRandomItem(this, m_DiceRoller);
        }

        public Item MakeShopGeneralItem()
        {
            int roll = m_DiceRoller.Roll(0, 6);
            switch (roll)
            {
                case 0: return MakeShopPharmacyItem();
                case 1: return MakeShopSportsWearItem();
                case 2: return MakeShopConstructionItem();
                case 3: return MakeShopGroceryItem();
                case 4: return MakeHuntingShopItem();
                case 5: return MakeRandomBedroomItem();
                default: 
                    throw new ArgumentOutOfRangeException("unhandled roll");
            }
        }
    }
}