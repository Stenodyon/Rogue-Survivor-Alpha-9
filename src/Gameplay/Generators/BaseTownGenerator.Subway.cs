using System.Drawing;
using System.Collections.Generic;

using djack.RogueSurvivor.Data;
using djack.RogueSurvivor.Engine;

namespace djack.RogueSurvivor.Gameplay.Generators
{
    partial class BaseTownGenerator
    {
        const int RAIL_SIZE = 4;

        private class SubwayGenerator
        {
            BaseTownGenerator parent;
            Map subway;
            Map surface;

            const int railStartX = 0;
            const int railSize = 4;
            int railEndX;
            int railY;

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
                    for (int y = railY; y < railY + RAIL_SIZE; y++)
                        subway.SetTileModelAt(x, y, parent.m_Game.GameTiles.RAIL_EW);
                }
                subway.AddZone(parent.MakeUniqueZone(RogueGame.NAME_SUBWAY_RAILS, new Rectangle(railStartX, railY, railEndX - railStartX + 1, RAIL_SIZE)));
            }

            private void MakeStation()
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
                        for (int y = b.Rectangle.Top - minDistToRails; y < b.Rectangle.Bottom + minDistToRails && hasRoom; y++)
                        {
                            if (!subway.IsInBounds(x, y))
                                continue;
                            if (subway.GetTileAt(x, y).Model.IsWalkable)
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

                // if found, make station room and building.
                if (goodBlocks != null)
                {
                    // pick one at random.
                    Block surfaceBlock = goodBlocks[parent.m_DiceRoller.Roll(0, goodBlocks.Count)];

                    // clear surface building.
                    parent.ClearRectangle(surface, surfaceBlock.BuildingRect);
                    parent.TileFill(surface, parent.m_Game.GameTiles.FLOOR_CONCRETE, surfaceBlock.BuildingRect);
                    parent.m_SurfaceBlocks.Remove(surfaceBlock);

                    // make station building on the surface & room in the subway.
                    Block newSurfaceBlock = new Block(surfaceBlock.Rectangle);
                    Point stairsPos = new Point(newSurfaceBlock.BuildingRect.Left + newSurfaceBlock.BuildingRect.Width / 2, newSurfaceBlock.InsideRect.Top);
                    parent.MakeSubwayStationBuilding(surface, true, newSurfaceBlock, subway, stairsPos);
                    Block subwayRoom = new Block(surfaceBlock.Rectangle);
                    parent.MakeSubwayStationBuilding(subway, false, subwayRoom, surface, stairsPos);
                }
            }

            private void MakeToolsRoom()
            {
                const int toolsRoomWidth = 5;
                const int toolsRoomHeight = 5;
                Direction toolsRoomDir = parent.m_DiceRoller.RollChance(50) ? Direction.N : Direction.S;
                Rectangle toolsRoom = Rectangle.Empty;
                bool foundToolsRoom = false;
                int toolsRoomAttempt = 0;
                do
                {
                    int x = parent.m_DiceRoller.Roll(10, subway.Width - 10);
                    int y = (toolsRoomDir == Direction.N ? railY - 1  : railY + RAIL_SIZE);

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

        public virtual Map GenerateSubwayMap(int seed, District district)
        {
            SubwayGenerator generator = new SubwayGenerator(this, seed, district);
            return generator.Generate();
        }
    }
}