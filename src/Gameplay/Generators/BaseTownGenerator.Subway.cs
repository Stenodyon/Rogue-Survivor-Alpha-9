using System;
using System.Drawing;
using System.Collections.Generic;

using djack.RogueSurvivor.Data;
using djack.RogueSurvivor.Engine;

namespace djack.RogueSurvivor.Gameplay.Generators
{
    partial class BaseTownGenerator
    {
        private class SubwayGenerator
        {
            BaseTownGenerator parent;
            Map subway;
            Map surface;

            const int toolsRoomWidth = 5;
            const int toolsRoomHeight = 5;
            const int railStartX = 0;
            const int railSize = 4;
            int railEndX;
            int railY;

            int sideRoll;
            int entryFenceX, entryFenceY;
            Direction digDirection;
            Point digPos;

            public SubwayGenerator(BaseTownGenerator parent, int seed, District district)
            {
                this.parent = parent;

                parent.m_DiceRoller = new DiceRoller(seed);
                subway = new Map(seed, "subway", district.EntryMap.Width, district.EntryMap.Height)
                {
                    Lighting = Lighting.DARKNESS
                };
                parent.TileFill(subway, parent.m_Game.GameTiles.WALL_BRICK);

                surface = district.EntryMap;
                railEndX = subway.Width - 1;
                railY = subway.Width / 2 - 1;
            }

            public Map Generate()
            {
                // 1. Trace rail line.
                MakeRails();
                // 2. Make station linked to surface.
                MakeStation();
                // 3.  Small tools room.
                MakeToolsRoom();
                // 4. Tags & Posters almost everywhere.
                MakeWallDeco();

                // 5. Additional jobs.
                // Mark all the map as inside.
                for (int x = 0; x < subway.Width; x++)
                    for (int y = 0; y < subway.Height; y++)
                        subway.GetTileAt(x, y).IsInside = true;

                // Done.
                return subway;
            }

            private void MakeRails()
            {
                for (int x = railStartX; x <= railEndX; x++)
                {
                    for (int y = railY; y < railY + railSize; y++)
                        subway.SetTileModelAt(x, y, parent.m_Game.GameTiles.RAIL_EW);
                }
                subway.AddZone(parent.MakeUniqueZone(RogueGame.NAME_SUBWAY_RAILS, new Rectangle(railStartX, railY, railEndX - railStartX + 1, railSize)));
            }

            /// Generates the station part
            private void MakeStation()
            {
                List<Block> goodBlocks = FindSuitableStationBlocks();
                // if found, make station room and building.
                if (goodBlocks != null)
                {
                    // pick one at random.
                    Block surfaceBlock = goodBlocks[parent.m_DiceRoller.Roll(0, goodBlocks.Count)];
                    GenerateStation(surfaceBlock);
                }
            }

            /// Returns a list of surface blocks suitable for a subway station
            private List<Block> FindSuitableStationBlocks()
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

                    // we must carve a room in the subway and must not be to close to rails.
                    bool hasRoom = true;
                    int minDistToRails = 8;
                    for (int x = b.Rectangle.Left - minDistToRails; x < b.Rectangle.Right + minDistToRails && hasRoom; x++)
                    {
                        for (int y = b.Rectangle.Top - minDistToRails; y < b.Rectangle.Bottom + minDistToRails && hasRoom; y++)
                        {
                            if (!subway.IsInBounds(x, y))
                                continue;
                            if (subway.GetTileAt(x, y).Model.IsWalkable)
                                hasRoom = false;
                        }
                    }
                    if (!hasRoom)
                        continue;

                    // found one.
                    if (goodBlocks == null)
                        goodBlocks = new List<Block>(parent.m_SurfaceBlocks.Count);
                    goodBlocks.Add(b);
                    break; // only 1 block ?
                }
                return goodBlocks;
            }

            /// Generate the station using the given surface block
            private void GenerateStation(Block surfaceBlock)
            {
                // clear surface building.
                parent.ClearRectangle(surface, surfaceBlock.BuildingRect);
                parent.TileFill(surface, parent.m_Game.GameTiles.FLOOR_CONCRETE, surfaceBlock.BuildingRect);
                parent.m_SurfaceBlocks.Remove(surfaceBlock);

                // make station building on the surface & room in the subway.
                Block newSurfaceBlock = new Block(surfaceBlock.Rectangle);
                Point stairsPos = new Point(newSurfaceBlock.BuildingRect.Left + newSurfaceBlock.BuildingRect.Width / 2, newSurfaceBlock.InsideRect.Top);
                MakeSubwayStationBuilding(surface, true, newSurfaceBlock, subway, stairsPos);
                Block subwayRoom = new Block(surfaceBlock.Rectangle);
                MakeSubwayStationBuilding(subway, false, subwayRoom, surface, stairsPos);
            }

            private void MakeToolsRoom()
            {
                Direction toolsRoomDir = parent.m_DiceRoller.RollChance(50) ? Direction.N : Direction.S;
                Rectangle toolsRoom = Rectangle.Empty;
                bool foundToolsRoom = false;
                int toolsRoomAttempt = 0;
                do
                {
                    int x = parent.m_DiceRoller.Roll(10, subway.Width - 10);
                    int y = (toolsRoomDir == Direction.N ? railY - 1  : railY + railSize);

                    if (!subway.GetTileAt(x, y).Model.IsWalkable)
                    {
                        // make room rectangle.
                        if (toolsRoomDir == Direction.N)
                            toolsRoom = new Rectangle(x, y - toolsRoomHeight + 1, toolsRoomWidth, toolsRoomHeight);
                        else
                            toolsRoom = new Rectangle(x, y, toolsRoomWidth, toolsRoomHeight);
                        // check room rect is all walls (do not overlap with platform or other rooms)
                        foundToolsRoom = parent.CheckForEachTile(subway, toolsRoom, (pt) => !subway.GetTileAt(pt).Model.IsWalkable);
                    }
                    ++toolsRoomAttempt;
                }
                while (toolsRoomAttempt < subway.Width * subway.Height && !foundToolsRoom);

                if (foundToolsRoom)
                {
                    // room.
                    parent.TileFill(subway, parent.m_Game.GameTiles.FLOOR_CONCRETE, toolsRoom);
                    parent.TileRectangle(subway, parent.m_Game.GameTiles.WALL_BRICK, toolsRoom);
                    parent.PlaceDoor(
                        subway,
                        toolsRoom.Left + toolsRoomWidth / 2,
                        (toolsRoomDir == Direction.N ? toolsRoom.Bottom - 1 : toolsRoom.Top),
                        parent.m_Game.GameTiles.FLOOR_CONCRETE,
                        parent.MakeObjIronDoor());
                    subway.AddZone(parent.MakeUniqueZone("tools room", toolsRoom));

                    // shelves on walls with construction items.
                    parent.DoForEachTile(subway, toolsRoom,
                        (pt) =>
                        {
                            if (!subway.IsWalkable(pt.X, pt.Y))
                                return;
                            if (parent.CountAdjWalls(subway, pt.X, pt.Y) == 0 || parent.CountAdjDoors(subway, pt.X, pt.Y) > 0)
                                return;

                            subway.PlaceMapObjectAt(parent.MakeObjShelf(GameImages.OBJ_SHOP_SHELF), pt);
                            subway.DropItemAt(parent.MakeShopConstructionItem(), pt);
                        });
                }
            }

            private void MakeWallDeco()
            {
                for (int x = 0; x < subway.Width; x++)
                {
                    for (int y = 0; y < subway.Height; y++)
                    {
                        if (parent.m_DiceRoller.RollChance(SUBWAY_TAGS_POSTERS_CHANCE))
                        {
                            // must be a wall with walkables around.
                            Tile t = subway.GetTileAt(x, y);
                            if (t.Model.IsWalkable)
                                continue;
                            if (parent.CountAdjWalkables(subway, x, y) < 2)
                                continue;

                            // poster?
                            if (parent.m_DiceRoller.RollChance(50))
                                t.AddDecoration(POSTERS[parent.m_DiceRoller.Roll(0, POSTERS.Length)]);

                            // tag?
                            if (parent.m_DiceRoller.RollChance(50))
                                t.AddDecoration(TAGS[parent.m_DiceRoller.Roll(0, TAGS.Length)]);
                        }
                    }
                }
            }

            protected virtual void MakeSubwayStationBuilding(Map map, bool isSurface, Block b, Map linkedMap, Point exitPosition)
            {
                ///////////////
                // Outer walls.
                ///////////////
                #region
                // if sewers dig room.
                if (!isSurface)
                    parent.TileFill(map, parent.m_Game.GameTiles.FLOOR_CONCRETE, b.InsideRect);
                // outer walls.
                parent.TileRectangle(map, parent.m_Game.GameTiles.WALL_SUBWAY, b.BuildingRect);
                // make sure its marked as inside (in case we replace a park for instance)
                for (int x = b.InsideRect.Left; x < b.InsideRect.Right; x++)
                    for (int y = b.InsideRect.Top; y < b.InsideRect.Bottom; y++)
                        map.GetTileAt(x, y).IsInside = true;
                #endregion

                // Entrance
                RollEntranceDirection(map, b, isSurface);
                if(isSurface)
                    MakeSationSurfaceEntrance(map, b);

                ///////////////////////////
                // Stairs to the other map.
                ///////////////////////////
                #region
                // add exits.
                for (int ex = exitPosition.X - 1; ex <= exitPosition.X + 1; ex++)
                {
                    Point thisExitPos = new Point(ex, exitPosition.Y);
                    map.GetTileAt(thisExitPos.X, thisExitPos.Y).AddDecoration(isSurface ? GameImages.DECO_STAIRS_DOWN : GameImages.DECO_STAIRS_UP);
                    map.SetExitAt(thisExitPos, new Exit(linkedMap, thisExitPos) { IsAnAIExit = true });
                }
                #endregion

                // Undeground part of the station
                if (!isSurface)
                    MakeStationUnderground(map, b);

                /////////////////////
                // Furniture & Items.
                /////////////////////
                // iron benches in station.
                #region
                for (int bx = b.InsideRect.Left; bx < b.InsideRect.Right; bx++)
                    for (int by = b.InsideRect.Top + 1; by < b.InsideRect.Bottom - 1; by++)
                    {
                        // next to walls and no doors.
                        if (parent.CountAdjWalls(map, bx, by) < 2 || parent.CountAdjDoors(map, bx, by) > 0)
                            continue;

                        // not next to stairs.
                        if (parent.m_Game.Rules.GridDistance(new Point(bx, by), new Point(entryFenceX, entryFenceY)) < 2)
                            continue;

                        // bench.
                        map.PlaceMapObjectAt(parent.MakeObjIronBench(GameImages.OBJ_IRON_BENCH), new Point(bx, by));
                    }
                #endregion

                /////////////////////////////////////
                // Add subway police guy on surface.
                /////////////////////////////////////
                if (isSurface)
                {
                    Actor policeMan = parent.CreateNewPoliceman(0);
                    parent.ActorPlace(parent.m_DiceRoller, b.Rectangle.Width * b.Rectangle.Height, map, policeMan, b.InsideRect.Left, b.InsideRect.Top, b.InsideRect.Width, b.InsideRect.Height);
                }

                //////////////
                // Make zone.
                //////////////
                map.AddZone(parent.MakeUniqueZone(RogueGame.NAME_SUBWAY_STATION, b.BuildingRect));
            }

            /// Rolls the direction of the station entrance
            private void RollEntranceDirection(Map map, Block b, bool isSurface)
            {
                // if not surface, we must dig toward the rails.
                if (isSurface)
                    sideRoll = parent.m_DiceRoller.Roll(0, 4);
                else
                    sideRoll = b.Rectangle.Bottom < map.Width / 2 ? 1 : 0;
                switch (sideRoll)
                {
                    case 0: // north.
                        digDirection = Direction.N;
                        entryFenceX = b.BuildingRect.Left + b.BuildingRect.Width / 2;
                        entryFenceY = b.BuildingRect.Top;
                        break;

                    case 1: // south.
                        digDirection = Direction.S;
                        entryFenceX = b.BuildingRect.Left + b.BuildingRect.Width / 2;
                        entryFenceY = b.BuildingRect.Bottom - 1;
                        break;

                    case 2: // west.
                        digDirection = Direction.W;
                        entryFenceX = b.BuildingRect.Left;
                        entryFenceY = b.BuildingRect.Top + b.BuildingRect.Height / 2;
                        break;

                    case 3: // east.
                        digDirection = Direction.E;
                        entryFenceX = b.BuildingRect.Right - 1;
                        entryFenceY = b.BuildingRect.Top + b.BuildingRect.Height / 2;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException("unhandled roll");
                }
                digPos = new Point(entryFenceX, entryFenceY) + digDirection;
            }

            /// Creates the door and signs of the station entrance
            private void MakeSationSurfaceEntrance(Map map, Block b)
            {
                // Subway signs
                switch(sideRoll)
                {
                    case 0:
                    case 1:
                        map.GetTileAt(entryFenceX - 1, entryFenceY).AddDecoration(GameImages.DECO_SUBWAY_BUILDING);
                        map.GetTileAt(entryFenceX + 1, entryFenceY).AddDecoration(GameImages.DECO_SUBWAY_BUILDING);
                        break;
                    case 2:
                    case 3:
                        map.GetTileAt(entryFenceX, entryFenceY - 1).AddDecoration(GameImages.DECO_SUBWAY_BUILDING);
                        map.GetTileAt(entryFenceX, entryFenceY + 1).AddDecoration(GameImages.DECO_SUBWAY_BUILDING);
                        break;
                }
                // add door
                map.SetTileModelAt(entryFenceX, entryFenceY, parent.m_Game.GameTiles.FLOOR_CONCRETE);
                map.PlaceMapObjectAt(parent.MakeObjGlassDoor(), new Point(entryFenceX, entryFenceY));
            }

            /// Builds the underground part of the station
            /// - dig corridor until we reach the rails.
            /// - dig platform and make corridor zone.
            /// - add closed iron fences between corridor and platform.
            /// - make power room.
            private void MakeStationUnderground(Map map, Block b)
            {
                // - dig corridor until we reach the rails.
                DigCorridor(map);

                // - dig platform and make corridor zone.
                Rectangle platformRect = DigPlatform(map, b);

                // - add closed iron gates between corridor and platform.
                #region
                Point ironFencePos;
                if (digDirection == Direction.S)
                    ironFencePos = new Point(entryFenceX, platformRect.Top - 1);
                else
                    ironFencePos = new Point(entryFenceX, platformRect.Bottom);
                map.PlaceMapObjectAt(parent.MakeObjIronGate(GameImages.OBJ_GATE_CLOSED), new Point(ironFencePos.X, ironFencePos.Y));
                map.PlaceMapObjectAt(parent.MakeObjIronGate(GameImages.OBJ_GATE_CLOSED), new Point(ironFencePos.X + 1, ironFencePos.Y));
                map.PlaceMapObjectAt(parent.MakeObjIronGate(GameImages.OBJ_GATE_CLOSED), new Point(ironFencePos.X - 1, ironFencePos.Y));
                #endregion

                // - make power room.
                #region
                // access in the corridor, going toward the center of the map.
                Point powerRoomEntry;
                Rectangle powerRoomRect;
                const int powerRoomWidth = 4;
                const int powerRoomHalfHeight = 2;
                if (entryFenceX > map.Width / 2)
                {
                    // west.
                    powerRoomEntry = new Point(entryFenceX - 2, entryFenceY + powerRoomHalfHeight * digDirection.Vector.Y);
                    powerRoomRect = Rectangle.FromLTRB(powerRoomEntry.X - powerRoomWidth, powerRoomEntry.Y - powerRoomHalfHeight, powerRoomEntry.X + 1, powerRoomEntry.Y + powerRoomHalfHeight + 1);
                }
                else
                {
                    // east.
                    powerRoomEntry = new Point(entryFenceX + 2, entryFenceY + powerRoomHalfHeight * digDirection.Vector.Y);
                    powerRoomRect = Rectangle.FromLTRB(powerRoomEntry.X, powerRoomEntry.Y - powerRoomHalfHeight, powerRoomEntry.X + powerRoomWidth, powerRoomEntry.Y + powerRoomHalfHeight + 1);
                }

                // carve power room.
                parent.TileFill(map, parent.m_Game.GameTiles.FLOOR_CONCRETE, powerRoomRect);
                parent.TileRectangle(map, parent.m_Game.GameTiles.WALL_STONE, powerRoomRect);

                // add door with signs.
                parent.PlaceDoor(map, powerRoomEntry.X, powerRoomEntry.Y, parent.m_Game.GameTiles.FLOOR_CONCRETE, parent.MakeObjIronDoor());
                map.GetTileAt(powerRoomEntry.X, powerRoomEntry.Y - 1).AddDecoration(GameImages.DECO_POWER_SIGN_BIG);
                map.GetTileAt(powerRoomEntry.X, powerRoomEntry.Y + 1).AddDecoration(GameImages.DECO_POWER_SIGN_BIG);

                // add power generators along wall.
                parent.MapObjectFill(map, powerRoomRect,
                    (pt) =>
                    {
                        if (!map.GetTileAt(pt).Model.IsWalkable)
                            return null;
                        if (parent.CountAdjWalls(map, pt.X, pt.Y) < 3 || parent.CountAdjDoors(map, pt.X, pt.Y) > 0)
                            return null;
                        return parent.MakeObjPowerGenerator(GameImages.OBJ_POWERGEN_OFF, GameImages.OBJ_POWERGEN_ON);
                    });

                #endregion
            }

            private void DigCorridor(Map map)
            {
                map.SetTileModelAt(entryFenceX, entryFenceY, parent.m_Game.GameTiles.FLOOR_CONCRETE);
                map.SetTileModelAt(entryFenceX + 1, entryFenceY, parent.m_Game.GameTiles.FLOOR_CONCRETE);
                map.SetTileModelAt(entryFenceX - 1, entryFenceY, parent.m_Game.GameTiles.FLOOR_CONCRETE);
                map.SetTileModelAt(entryFenceX - 2, entryFenceY, parent.m_Game.GameTiles.WALL_STONE);
                map.SetTileModelAt(entryFenceX + 2, entryFenceY, parent.m_Game.GameTiles.WALL_STONE);

                while (map.IsInBounds(digPos) && !map.GetTileAt(digPos.X, digPos.Y).Model.IsWalkable)
                {
                    // corridor.
                    map.SetTileModelAt(digPos.X, digPos.Y, parent.m_Game.GameTiles.FLOOR_CONCRETE);
                    map.SetTileModelAt(digPos.X - 1, digPos.Y, parent.m_Game.GameTiles.FLOOR_CONCRETE);
                    map.SetTileModelAt(digPos.X + 1, digPos.Y, parent.m_Game.GameTiles.FLOOR_CONCRETE);
                    map.SetTileModelAt(digPos.X - 2, digPos.Y, parent.m_Game.GameTiles.WALL_STONE);
                    map.SetTileModelAt(digPos.X + 2, digPos.Y, parent.m_Game.GameTiles.WALL_STONE);

                    // continue digging.
                    digPos += digDirection;
                }
            }

            /// Digs the platform and returns the enclosing rectangle
            private Rectangle DigPlatform(Map map, Block b)
            {
                const int platformExtend = 10;
                const int platformWidth = 3;
                Rectangle platformRect;
                int platformLeft = Math.Max(0, b.BuildingRect.Left - platformExtend);
                int platformRight = Math.Min(map.Width - 1, b.BuildingRect.Right + platformExtend);
                int benchesLine;
                if (digDirection == Direction.S)
                {
                    platformRect = Rectangle.FromLTRB(platformLeft, digPos.Y - platformWidth, platformRight, digPos.Y);
                    benchesLine = platformRect.Top;
                    map.AddZone(parent.MakeUniqueZone("corridor", Rectangle.FromLTRB(entryFenceX - 1, entryFenceY, entryFenceX + 1 + 1, platformRect.Top)));
                }
                else
                {
                    platformRect = Rectangle.FromLTRB(platformLeft, digPos.Y + 1, platformRight, digPos.Y + 1 + platformWidth);
                    benchesLine = platformRect.Bottom - 1;
                    map.AddZone(parent.MakeUniqueZone("corridor", Rectangle.FromLTRB(entryFenceX - 1, platformRect.Bottom, entryFenceX + 1 + 1, entryFenceY + 1)));
                }
                parent.TileFill(map, parent.m_Game.GameTiles.FLOOR_CONCRETE, platformRect);

                // - iron benches in platform.
                for (int bx = platformRect.Left; bx < platformRect.Right; bx++)
                {
                    if (parent.CountAdjWalls(map, bx, benchesLine) < 3)
                        continue;
                    map.PlaceMapObjectAt(parent.MakeObjIronBench(GameImages.OBJ_IRON_BENCH), new Point(bx, benchesLine));
                }

                // - platform zone.
                map.AddZone(parent.MakeUniqueZone("platform", platformRect));

                return platformRect;
            }

        } // SubwayGenerator

        public virtual Map GenerateSubwayMap(int seed, District district)
        {
            SubwayGenerator generator = new SubwayGenerator(this, seed, district);
            return generator.Generate();
        }

    }
}