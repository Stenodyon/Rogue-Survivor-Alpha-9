using System.Drawing;
using System.Collections.Generic;

using djack.RogueSurvivor.Data;
using djack.RogueSurvivor.Engine;

namespace djack.RogueSurvivor.Gameplay.Generators
{
    partial class BaseTownGenerator
    {
        const int RAIL_SIZE = 4;

        public virtual Map GenerateSubwayMap(int seed, District district)
        {
            // Create.
            m_DiceRoller = new DiceRoller(seed);
            Map subway = new Map(seed, "subway", district.EntryMap.Width, district.EntryMap.Height)
            {
                Lighting = Lighting.DARKNESS
            };
            TileFill(subway, m_Game.GameTiles.WALL_BRICK);

            /////////////////////////////////////
            // 1. Trace rail line.
            // 2. Make station linked to surface?
            // 3. Small tools room.
            // 4. Tags & Posters almost everywhere.
            // 5. Additional jobs.
            /////////////////////////////////////
            Map surface = district.EntryMap;
            int railY = subway.Width / 2 - 1;

            // 1. Trace rail line.
            GenerateSubwayMap_Rails(subway, railY);

            // 2. Make station linked to surface.
            GenerateSubwayMap_Station(subway, surface);

            // 3.  Small tools room.
            GenerateSubwayMap_ToolsRoom(subway, railY);

            // 4. Tags & Posters almost everywhere.
            GenerateSubwayMap_WallDeco(subway);

            // 5. Additional jobs.
            // Mark all the map as inside.
            for (int x = 0; x < subway.Width; x++)
                for (int y = 0; y < subway.Height; y++)
                    subway.GetTileAt(x, y).IsInside = true;

            // Done.
            return subway;
        }

        /// Generates the rails
        public virtual void GenerateSubwayMap_Rails(Map subwayMap, int railY)
        {
            int railStartX = 0;
            int railEndX = subwayMap.Width - 1;

            for (int x = railStartX; x <= railEndX; x++)
            {
                for (int y = railY; y < railY + RAIL_SIZE; y++)
                    subwayMap.SetTileModelAt(x, y, m_Game.GameTiles.RAIL_EW);
            }
            subwayMap.AddZone(MakeUniqueZone(RogueGame.NAME_SUBWAY_RAILS, new Rectangle(railStartX, railY, railEndX - railStartX + 1, RAIL_SIZE)));
        }

        public virtual void GenerateSubwayMap_Station(Map subway, Map surface)
        {
            // search a suitable surface blocks.
            List<Block> goodBlocks = null;
            foreach (Block b in m_SurfaceBlocks)
            {
                // surface building must be of minimal size.
                if (b.BuildingRect.Width > m_Params.MinBlockSize + 2 || b.BuildingRect.Height > m_Params.MinBlockSize + 2)
                    continue;

                // must not be a special building or have an exit (eg: houses with basements)
                if (IsThereASpecialBuilding(surface, b.InsideRect))
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
                    goodBlocks = new List<Block>(m_SurfaceBlocks.Count);
                goodBlocks.Add(b);
                break;
            }

            // if found, make station room and building.
            if (goodBlocks != null)
            {
                // pick one at random.
                Block surfaceBlock = goodBlocks[m_DiceRoller.Roll(0, goodBlocks.Count)];

                // clear surface building.
                ClearRectangle(surface, surfaceBlock.BuildingRect);
                TileFill(surface, m_Game.GameTiles.FLOOR_CONCRETE, surfaceBlock.BuildingRect);
                m_SurfaceBlocks.Remove(surfaceBlock);

                // make station building on the surface & room in the subway.
                Block newSurfaceBlock = new Block(surfaceBlock.Rectangle);
                Point stairsPos = new Point(newSurfaceBlock.BuildingRect.Left + newSurfaceBlock.BuildingRect.Width / 2, newSurfaceBlock.InsideRect.Top);
                MakeSubwayStationBuilding(surface, true, newSurfaceBlock, subway, stairsPos);
                Block subwayRoom = new Block(surfaceBlock.Rectangle);
                MakeSubwayStationBuilding(subway, false, subwayRoom, surface, stairsPos);
            }
        }

        public virtual void GenerateSubwayMap_ToolsRoom(Map subway, int railY)
        {
            const int toolsRoomWidth = 5;
            const int toolsRoomHeight = 5;
            Direction toolsRoomDir = m_DiceRoller.RollChance(50) ? Direction.N : Direction.S;
            Rectangle toolsRoom = Rectangle.Empty;
            bool foundToolsRoom = false;
            int toolsRoomAttempt = 0;
            do
            {
                int x = m_DiceRoller.Roll(10, subway.Width - 10);
                int y = (toolsRoomDir == Direction.N ? railY - 1  : railY + RAIL_SIZE);

                if (!subway.GetTileAt(x, y).Model.IsWalkable)
                {
                    // make room rectangle.
                    if (toolsRoomDir == Direction.N)
                        toolsRoom = new Rectangle(x, y - toolsRoomHeight + 1, toolsRoomWidth, toolsRoomHeight);
                    else
                        toolsRoom = new Rectangle(x, y, toolsRoomWidth, toolsRoomHeight);
                    // check room rect is all walls (do not overlap with platform or other rooms)
                    foundToolsRoom = CheckForEachTile(subway, toolsRoom, (pt) => !subway.GetTileAt(pt).Model.IsWalkable);
                }
                ++toolsRoomAttempt;
            }
            while (toolsRoomAttempt < subway.Width * subway.Height && !foundToolsRoom);

            if (foundToolsRoom)
            {
                // room.
                TileFill(subway, m_Game.GameTiles.FLOOR_CONCRETE, toolsRoom);
                TileRectangle(subway, m_Game.GameTiles.WALL_BRICK, toolsRoom);
                PlaceDoor(subway, toolsRoom.Left + toolsRoomWidth / 2, (toolsRoomDir == Direction.N ? toolsRoom.Bottom - 1 : toolsRoom.Top), m_Game.GameTiles.FLOOR_CONCRETE, MakeObjIronDoor());
                subway.AddZone(MakeUniqueZone("tools room", toolsRoom));

                // shelves on walls with construction items.
                DoForEachTile(subway, toolsRoom,
                    (pt) =>
                    {
                        if (!subway.IsWalkable(pt.X, pt.Y))
                            return;
                        if (CountAdjWalls(subway, pt.X, pt.Y) == 0 || CountAdjDoors(subway, pt.X, pt.Y) > 0)
                            return;

                        subway.PlaceMapObjectAt(MakeObjShelf(GameImages.OBJ_SHOP_SHELF), pt);
                        subway.DropItemAt(MakeShopConstructionItem(), pt);
                    });
            }
        }

        /// Generate Tags and Posters
        public virtual void GenerateSubwayMap_WallDeco(Map subway)
        {
            for (int x = 0; x < subway.Width; x++)
                for (int y = 0; y < subway.Height; y++)
                {
                    if (m_DiceRoller.RollChance(SUBWAY_TAGS_POSTERS_CHANCE))
                    {
                        // must be a wall with walkables around.
                        Tile t = subway.GetTileAt(x, y);
                        if (t.Model.IsWalkable)
                            continue;
                        if (CountAdjWalkables(subway, x, y) < 2)
                            continue;

                        // poster?
                        if (m_DiceRoller.RollChance(50))
                            t.AddDecoration(POSTERS[m_DiceRoller.Roll(0, POSTERS.Length)]);

                        // tag?
                        if (m_DiceRoller.RollChance(50))
                            t.AddDecoration(TAGS[m_DiceRoller.Roll(0, TAGS.Length)]);
                    }
                }
        }
    }
}