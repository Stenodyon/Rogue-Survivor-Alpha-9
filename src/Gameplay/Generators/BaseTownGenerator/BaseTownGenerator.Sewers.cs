using System;
using System.Drawing;
using System.Collections.Generic;

using djack.RogueSurvivor.Data;
using djack.RogueSurvivor.Engine;

namespace djack.RogueSurvivor.Gameplay.Generators
{
    partial class BaseTownGenerator
    {
        private class SewerGenerator
        {
            BaseTownGenerator parent;

            Map sewers;
            Map surface;
            List<Block> blocks;

            int doorX, doorY;
            Direction digDirection;
            int sideRoll;

            public SewerGenerator(BaseTownGenerator parent,
                                  int seed,
                                  District district)
            {
                this.parent = parent;

                parent.m_DiceRoller = new DiceRoller(seed);
                sewers = new Map(seed, "sewers", district.EntryMap.Width, district.EntryMap.Height)
                {
                    Lighting = Lighting.DARKNESS
                };
                sewers.AddZone(parent.MakeUniqueZone("sewers", sewers.Rect));
                parent.TileFill(sewers, parent.m_Game.GameTiles.WALL_SEWER);

                surface = district.EntryMap;

                blocks = new List<Block>(parent.m_SurfaceBlocks.Count);
            }

            /* Generates the sewers' map */
            public Map Generate()
            {
                // Create.

                ///////////////////////////////////////////////////
                // 1. Make blocks.
                // 2. Make tunnels.
                // 3. Link with surface.
                // 4. Additional jobs.
                // 5. Sewers Maintenance Room & Building(surface).
                // 6. Some rooms.
                // 7. Objects.
                // 8. Items.
                // 9. Tags.
                ///////////////////////////////////////////////////

                // 1. Make blocks.
                parent.MakeBlocks(sewers, false, ref blocks, new Rectangle(0, 0, sewers.Width, sewers.Height));

                // 2. Make tunnels.
                MakeTunnels();

                // 3. Link with surface.
                LinkWithSurface();

                // 4. Additional jobs.
                #region
                // Mark all the map as inside.
                for (int x = 0; x < sewers.Width; x++)
                    for (int y = 0; y < sewers.Height; y++)
                        sewers.GetTileAt(x, y).IsInside = true; 
                #endregion

                // 5. Sewers Maintenance Room & Building(surface).
                MakeSewersBuilding();

                // 6. Some rooms.
                #region
                foreach (Block b in blocks)
                {
                    // chance?
                    if (!parent.m_DiceRoller.RollChance(SEWERS_ROOM_CHANCE))
                        continue;

                    // must be all walls = not already assigned as a room.
                    if (!parent.CheckForEachTile(sewers, b.BuildingRect, (pt) => !sewers.GetTileAt(pt).Model.IsWalkable))
                        continue;

                    // carve a room.
                    parent.TileFill(sewers, parent.m_Game.GameTiles.FLOOR_CONCRETE, b.InsideRect);

                    // 4 entries.
                    sewers.SetTileModelAt(b.BuildingRect.Left + b.BuildingRect.Width / 2, b.BuildingRect.Top, parent.m_Game.GameTiles.FLOOR_CONCRETE);
                    sewers.SetTileModelAt(b.BuildingRect.Left + b.BuildingRect.Width / 2, b.BuildingRect.Bottom - 1, parent.m_Game.GameTiles.FLOOR_CONCRETE);
                    sewers.SetTileModelAt(b.BuildingRect.Left, b.BuildingRect.Top + b.BuildingRect.Height / 2, parent.m_Game.GameTiles.FLOOR_CONCRETE);
                    sewers.SetTileModelAt(b.BuildingRect.Right - 1, b.BuildingRect.Top + b.BuildingRect.Height / 2, parent.m_Game.GameTiles.FLOOR_CONCRETE);

                    // zone.
                    sewers.AddZone(parent.MakeUniqueZone("room", b.InsideRect));
                }
                #endregion

                // 7. Objects.
                #region
                // junk.
                parent.MapObjectFill(sewers, new Rectangle(0, 0, sewers.Width, sewers.Height),
                    (pt) =>
                    {
                        if (!parent.m_DiceRoller.RollChance(SEWERS_JUNK_CHANCE))
                            return null;
                        if (!sewers.IsWalkable(pt.X, pt.Y))
                            return null;

                        return parent.MakeObjJunk(GameImages.OBJ_JUNK);
                    });
                #endregion

                // 8. Items.
                #region
                for (int x = 0; x < sewers.Width;x++)
                    for (int y = 0; y < sewers.Height; y++)
                    {
                        if (!parent.m_DiceRoller.RollChance(SEWERS_ITEM_CHANCE))
                            continue;
                        if (!sewers.IsWalkable(x, y))
                            continue;

                        // drop item.
                        Item it;
                        int roll = parent.m_DiceRoller.Roll(0, 3);
                        switch (roll)
                        {
                            case 0: it = parent.MakeItemBigFlashlight(); break;
                            case 1: it = parent.MakeItemCrowbar(); break;
                            case 2: it = parent.MakeItemSprayPaint(); break;
                            default:
                                throw new ArgumentOutOfRangeException("unhandled roll");
                        }
                        sewers.DropItemAt(it, x, y);
                    }
                #endregion

                // 9. Tags.
                #region
                for (int x = 0; x < sewers.Width; x++)
                {
                    for (int y = 0; y < sewers.Height; y++)
                    {
                        if (parent.m_DiceRoller.RollChance(SEWERS_TAG_CHANCE))
                        {
                            // must be a wall with walkables around.
                            Tile t = sewers.GetTileAt(x, y);
                            if (t.Model.IsWalkable)
                                continue;
                            if (parent.CountAdjWalkables(sewers, x, y) < 2)
                                continue;

                            // tag.
                            t.AddDecoration(TAGS[parent.m_DiceRoller.Roll(0, TAGS.Length)]);
                        }
                    }
                }
                #endregion

                // Done.
                return sewers;
            }

            /// Create the tunnels and add random fences blocking the way
            private void MakeTunnels()
            {
                // Carve tunnels.
                foreach (Block b in blocks)
                {
                    parent.TileRectangle(sewers, parent.m_Game.GameTiles.FLOOR_SEWER_WATER, b.Rectangle);
                }
                // Iron Fences blocking some tunnels.
                foreach (Block b in blocks)
                {
                    // chance?
                    if (!parent.m_DiceRoller.RollChance(SEWERS_IRON_FENCE_PER_BLOCK_CHANCE))
                        continue;

                    // fences on a side.
                    int fx1, fy1, fx2, fy2;
                    bool goodFencePos = false;
                    do
                    {
                        // roll side.
                        int sideRoll = parent.m_DiceRoller.Roll(0, 4);
                        switch (sideRoll)
                        {
                            case 0: // north.
                            case 1: // south.
                                fx1 = parent.m_DiceRoller.Roll(b.Rectangle.Left, b.Rectangle.Right - 1);
                                fy1 = (sideRoll == 0 ? b.Rectangle.Top : b.Rectangle.Bottom - 1);

                                fx2 = fx1;
                                fy2 = (sideRoll == 0 ? fy1 - 1 : fy1 + 1);
                                break;
                            case 2: // east.
                            case 3: // west.
                                fx1 = (sideRoll == 2 ? b.Rectangle.Left : b.Rectangle.Right - 1);
                                fy1 = parent.m_DiceRoller.Roll(b.Rectangle.Top, b.Rectangle.Bottom - 1);

                                fx2 = (sideRoll == 2 ? fx1 - 1 : fx1 + 1);
                                fy2 = fy1;
                                break;
                            default:
                                throw new ArgumentOutOfRangeException("unhandled roll");
                        }

                        // never on border.
                        if (sewers.IsOnMapBorder(fx1, fy1) || sewers.IsOnMapBorder(fx2, fy2))
                            continue;

                        // must have walls.
                        if (parent.CountAdjWalls(sewers, fx1, fy1) != 3)
                            continue;
                        if (parent.CountAdjWalls(sewers, fx2, fy2) != 3)
                            continue;

                        // found!
                        goodFencePos = true;
                    }
                    while (!goodFencePos);

                    // add (both of them)
                    parent.MapObjectPlace(sewers, fx1, fy1, parent.MakeObjIronFence(GameImages.OBJ_IRON_FENCE));
                    parent.MapObjectPlace(sewers, fx2, fy2, parent.MakeObjIronFence(GameImages.OBJ_IRON_FENCE));
                }
            }

            private void LinkWithSurface()
            {
                // loop until we got at least one link.
                int countLinks = 0;
                do
                {
                    for (int x = 0; x < sewers.Width; x++)
                        for (int y = 0; y < sewers.Height; y++)
                        {
                            // link? roll chance. 3%
                            bool doLink = parent.m_DiceRoller.RollChance(3);
                            if (!doLink)
                                continue;

                            // both surface and sewer tile must be walkable.
                            Tile tileSewer = sewers.GetTileAt(x, y);
                            if (!tileSewer.Model.IsWalkable)
                                continue;
                            Tile tileSurface = surface.GetTileAt(x, y);
                            if (!tileSurface.Model.IsWalkable)
                                continue;

                            // no blocking object.
                            if (sewers.GetMapObjectAt(x, y) != null)
                                continue;

                            // surface tile must be outside.
                            if (tileSurface.IsInside)
                                continue;
                            // surface tile must be walkway or grass.
                            if (tileSurface.Model != parent.m_Game.GameTiles.FLOOR_WALKWAY && tileSurface.Model != parent.m_Game.GameTiles.FLOOR_GRASS)
                                continue;
                            // surface tile must not be obstructed by an object.
                            if (surface.GetMapObjectAt(x, y) != null)
                                continue;

                            // must not be adjacent to another exit.
                            Point pt = new Point(x, y);
                            if (sewers.HasAnyAdjacentInMap(pt, (p) => sewers.GetExitAt(p) != null))
                                continue;
                            if (surface.HasAnyAdjacentInMap(pt, (p) => surface.GetExitAt(p) != null))
                                continue;

                            // link with ladder and sewer hole.
                            parent.AddExit(sewers, pt, surface, pt, GameImages.DECO_SEWER_LADDER, true);
                            parent.AddExit(surface, pt, sewers, pt, GameImages.DECO_SEWER_HOLE, true);

                            // - one more link.
                            ++countLinks;
                        }
                }
                while (countLinks < 1);
            }

            private void MakeSewersBuilding()
            {
                List<Block> goodBlocks = FindSuitableBlocks();

                // if found, make maintenance room in sewers and building on surface.
                if (goodBlocks != null)
                {
                    // pick one at random.
                    Block surfaceBlock = goodBlocks[parent.m_DiceRoller.Roll(0, goodBlocks.Count)];

                    // clear surface building.
                    parent.ClearRectangle(surface, surfaceBlock.BuildingRect);
                    parent.TileFill(surface, parent.m_Game.GameTiles.FLOOR_CONCRETE, surfaceBlock.BuildingRect);
                    parent.m_SurfaceBlocks.Remove(surfaceBlock);

                    // make maintenance building on the surface & room in the sewers.
                    Block newSurfaceBlock = new Block(surfaceBlock.Rectangle);
                    Point ladderHolePos = new Point(newSurfaceBlock.BuildingRect.Left + newSurfaceBlock.BuildingRect.Width / 2, newSurfaceBlock.BuildingRect.Top + newSurfaceBlock.BuildingRect.Height / 2);
                    MakeSewersMaintenanceBuilding(surface, true, newSurfaceBlock, sewers, ladderHolePos);
                    Block sewersRoom = new Block(surfaceBlock.Rectangle);
                    MakeSewersMaintenanceBuilding(sewers, false, sewersRoom, surface, ladderHolePos);
                }
            }

            /// Find a surface block that is suitable for the sewer maintenance
            /// building
            private List<Block> FindSuitableBlocks()
            {
                // search a suitable surface blocks.
                List<Block> goodBlocks = null;
                foreach (Block b in parent.m_SurfaceBlocks)
                {
                    // surface building must be of minimal size.
                    if (b.BuildingRect.Width > parent.m_Params.MinBlockSize + 2 || b.BuildingRect.Height > parent.m_Params.MinBlockSize + 2)
                        continue;

                    // must not be a special building or have an exit (eg: houses with basements)
                    if (parent.IsThereASpecialBuilding(surface, b.InsideRect))
                        continue;

                    // we must carve a room in the sewers.
                    bool hasRoom = true;
                    for (int x = b.Rectangle.Left; x < b.Rectangle.Right && hasRoom; x++)
                        for (int y = b.Rectangle.Top; y < b.Rectangle.Bottom && hasRoom; y++)
                        {
                            if (sewers.GetTileAt(x, y).Model.IsWalkable)
                                hasRoom = false;
                        }
                    if (!hasRoom)
                        continue;

                    // found one.
                    if (goodBlocks == null)
                        goodBlocks = new List<Block>(parent.m_SurfaceBlocks.Count);
                    goodBlocks.Add(b);
                    break;
                }
                return goodBlocks;
            }

            protected virtual void MakeSewersMaintenanceBuilding(Map map, bool isSurface, Block b, Map linkedMap, Point exitPosition)
            {
                // Outer walls.
                // if sewers dig room.
                if (!isSurface)
                    parent.TileFill(map, parent.m_Game.GameTiles.FLOOR_CONCRETE, b.InsideRect);
                // outer walls.
                parent.TileRectangle(map, parent.m_Game.GameTiles.WALL_SEWER, b.BuildingRect);
                // make sure its marked as inside (in case we replace a park for instance)
                for (int x = b.InsideRect.Left; x < b.InsideRect.Right; x++)
                    for (int y = b.InsideRect.Top; y < b.InsideRect.Bottom; y++)
                        map.GetTileAt(x, y).IsInside = true;

                // Entrance door.
                RollEntranceSide(map, b);
                PlaceEntrance(map, b);

                // Hole/Ladder to sewers/surface.
                map.GetTileAt(exitPosition.X, exitPosition.Y).AddDecoration(isSurface ? GameImages.DECO_SEWER_HOLE : GameImages.DECO_SEWER_LADDER);
                map.SetExitAt(exitPosition, new Exit(linkedMap, exitPosition) { IsAnAIExit = true });

                // If sewers, dig corridor until we reach a tunnel.
                if (!isSurface)
                {
                    Point digPos = new Point(doorX, doorY) + digDirection;
                    while (map.IsInBounds(digPos) && !map.GetTileAt(digPos.X, digPos.Y).Model.IsWalkable)
                    {
                        // corridor.
                        map.SetTileModelAt(digPos.X, digPos.Y, parent.m_Game.GameTiles.FLOOR_CONCRETE);
                        // continue digging.
                        digPos += digDirection;
                    }
                }

                // Furniture & Items.
                PopulateSewers(map, b);

                // Make zone.
                map.AddZone(parent.MakeUniqueZone(RogueGame.NAME_SEWERS_MAINTENANCE, b.BuildingRect));
            }

            /// Randomly choose on which side the entrance is
            private void RollEntranceSide(Map map, Block b)
            {
                sideRoll = parent.m_DiceRoller.Roll(0, 4);
                switch (sideRoll)
                {
                    case 0: // north.
                        digDirection = Direction.N;
                        doorX = b.BuildingRect.Left + b.BuildingRect.Width / 2;
                        doorY = b.BuildingRect.Top;
                        break;

                    case 1: // south.
                        digDirection = Direction.S;
                        doorX = b.BuildingRect.Left + b.BuildingRect.Width / 2;
                        doorY = b.BuildingRect.Bottom - 1;
                        break;

                    case 2: // west.
                        digDirection = Direction.W;
                        doorX = b.BuildingRect.Left;
                        doorY = b.BuildingRect.Top + b.BuildingRect.Height / 2;
                        break;

                    case 3: // east.
                        digDirection = Direction.E;
                        doorX = b.BuildingRect.Right - 1;
                        doorY = b.BuildingRect.Top + b.BuildingRect.Height / 2;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("unhandled roll");
                }
            }

            /// Create the doors and sign
            private void PlaceEntrance(Map map, Block b)
            {
                switch(sideRoll)
                {
                    case 0: // North
                    case 1: // South
                        map.GetTileAt(doorX - 1, doorY).AddDecoration(GameImages.DECO_SEWERS_BUILDING);
                        map.GetTileAt(doorX + 1, doorY).AddDecoration(GameImages.DECO_SEWERS_BUILDING);
                        break;
                    case 2: // West
                    case 3: // East
                        map.GetTileAt(doorX, doorY - 1).AddDecoration(GameImages.DECO_SEWERS_BUILDING);
                        map.GetTileAt(doorX, doorY + 1).AddDecoration(GameImages.DECO_SEWERS_BUILDING);
                        break;
                }
                // add the door.
                parent.PlaceDoor(map, doorX, doorY, parent.m_Game.GameTiles.FLOOR_CONCRETE, parent.MakeObjIronDoor());
                parent.BarricadeDoors(map, b.BuildingRect, Rules.BARRICADING_MAX);
            }

            /// Add furniture and personnel
            private void PopulateSewers(Map map, Block b)
            {
                // bunch of tables near walls with construction items on them.
                int nbTables = parent.m_DiceRoller.Roll(Math.Max(b.InsideRect.Width, b.InsideRect.Height), 2 * Math.Max(b.InsideRect.Width, b.InsideRect.Height));
                for (int i = 0; i < nbTables; i++)
                {
                    parent.MapObjectPlaceInGoodPosition(map, b.InsideRect,
                        (pt) => parent.CountAdjWalls(map, pt.X, pt.Y) >= 3 && parent.CountAdjDoors(map, pt.X, pt.Y) == 0,
                        parent.m_DiceRoller,
                        (pt) =>
                        {
                            // add item.
                            map.DropItemAt(parent.MakeShopConstructionItem(), pt);

                            // add table.
                            return parent.MakeObjTable(GameImages.OBJ_TABLE);
                        });
                }
                // a bed and a fridge with food if lucky.
                if (parent.m_DiceRoller.RollChance(33))
                {
                    // bed.
                    parent.MapObjectPlaceInGoodPosition(map, b.InsideRect,
                        (pt) => parent.CountAdjWalls(map, pt.X, pt.Y) >= 3 && parent.CountAdjDoors(map, pt.X, pt.Y) == 0,
                        parent.m_DiceRoller,
                        (pt) => parent.MakeObjBed(GameImages.OBJ_BED));

                    // fridge + food.
                    parent.MapObjectPlaceInGoodPosition(map, b.InsideRect,
                        (pt) => parent.CountAdjWalls(map, pt.X, pt.Y) >= 3 && parent.CountAdjDoors(map, pt.X, pt.Y) == 0,
                        parent.m_DiceRoller,
                        (pt) =>
                        {
                            // add food.
                            map.DropItemAt(parent.MakeItemCannedFood(), pt);

                            // add fridge.
                            return parent.MakeObjFridge(GameImages.OBJ_FRIDGE);
                        });
                }

                // Add the poor maintenance guy/gal.
                Actor poorGuy = parent.CreateNewCivilian(0, 3, 1);
                parent.ActorPlace(parent.m_DiceRoller, b.Rectangle.Width * b.Rectangle.Height, map, poorGuy, b.InsideRect.Left, b.InsideRect.Top, b.InsideRect.Width, b.InsideRect.Height);
            }

        } // SewerGenerator

        public virtual Map GenerateSewersMap(int seed, District district)
        {
            SewerGenerator generator = new SewerGenerator(this, seed, district);
            return generator.Generate();
        }

    }
}