using System;
using System.Drawing;
using System.Collections.Generic;

using djack.RogueSurvivor.Data;
using djack.RogueSurvivor.Engine;

namespace djack.RogueSurvivor.Gameplay.Generators
{
    partial class BaseTownGenerator
    {
        public virtual Map GenerateSewersMap(int seed, District district)
        {
            // Create.
            m_DiceRoller = new DiceRoller(seed);
            Map sewers = new Map(seed, "sewers", district.EntryMap.Width, district.EntryMap.Height)
            {
                Lighting = Lighting.DARKNESS
            };
            sewers.AddZone(MakeUniqueZone("sewers", sewers.Rect));
            TileFill(sewers, m_Game.GameTiles.WALL_SEWER);

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
            Map surface = district.EntryMap;

            // 1. Make blocks.
            List<Block> blocks = new List<Block>(m_SurfaceBlocks.Count);
            MakeBlocks(sewers, false, ref blocks, new Rectangle(0, 0, sewers.Width, sewers.Height));

            // 2. Make tunnels.
            #region
            // Carve tunnels.
            foreach (Block b in blocks)
            {
                TileRectangle(sewers, m_Game.GameTiles.FLOOR_SEWER_WATER, b.Rectangle);
            }
            // Iron Fences blocking some tunnels.
            foreach (Block b in blocks)
            {
                // chance?
                if (!m_DiceRoller.RollChance(SEWERS_IRON_FENCE_PER_BLOCK_CHANCE))
                    continue;

                // fences on a side.
                int fx1, fy1, fx2, fy2;
                bool goodFencePos = false;
                do
                {
                    // roll side.
                    int sideRoll = m_DiceRoller.Roll(0, 4);
                    switch (sideRoll)
                    {
                        case 0: // north.
                        case 1: // south.
                            fx1 = m_DiceRoller.Roll(b.Rectangle.Left, b.Rectangle.Right - 1);
                            fy1 = (sideRoll == 0 ? b.Rectangle.Top : b.Rectangle.Bottom - 1);

                            fx2 = fx1;
                            fy2 = (sideRoll == 0 ? fy1 - 1 : fy1 + 1);
                            break;
                        case 2: // east.
                        case 3: // west.
                            fx1 = (sideRoll == 2 ? b.Rectangle.Left : b.Rectangle.Right - 1);
                            fy1 = m_DiceRoller.Roll(b.Rectangle.Top, b.Rectangle.Bottom - 1);

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
                    if (CountAdjWalls(sewers, fx1, fy1) != 3)
                        continue;
                    if (CountAdjWalls(sewers, fx2, fy2) != 3)
                        continue;

                    // found!
                    goodFencePos = true;
                }
                while (!goodFencePos);

                // add (both of them)
                MapObjectPlace(sewers, fx1, fy1, MakeObjIronFence(GameImages.OBJ_IRON_FENCE));
                MapObjectPlace(sewers, fx2, fy2, MakeObjIronFence(GameImages.OBJ_IRON_FENCE));
            }
            #endregion

            // 3. Link with surface.
            #region
            // loop until we got at least one link.
            int countLinks = 0;
            do
            {
                for (int x = 0; x < sewers.Width; x++)
                    for (int y = 0; y < sewers.Height; y++)
                    {
                        // link? roll chance. 3%
                        bool doLink = m_DiceRoller.RollChance(3);
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
                        if (tileSurface.Model != m_Game.GameTiles.FLOOR_WALKWAY && tileSurface.Model != m_Game.GameTiles.FLOOR_GRASS)
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
                        AddExit(sewers, pt, surface, pt, GameImages.DECO_SEWER_LADDER, true);
                        AddExit(surface, pt, sewers, pt, GameImages.DECO_SEWER_HOLE, true);

                        // - one more link.
                        ++countLinks;
                    }
            }
            while (countLinks < 1);
            #endregion

            // 4. Additional jobs.
            #region
            // Mark all the map as inside.
            for (int x = 0; x < sewers.Width; x++)
                for (int y = 0; y < sewers.Height; y++)
                    sewers.GetTileAt(x, y).IsInside = true; 
            #endregion

            // 5. Sewers Maintenance Room & Building(surface).
            #region
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
                    goodBlocks = new List<Block>(m_SurfaceBlocks.Count);
                goodBlocks.Add(b);
                break;
            }

            // if found, make maintenance room in sewers and building on surface.
            if (goodBlocks != null)
            {
                // pick one at random.
                Block surfaceBlock = goodBlocks[m_DiceRoller.Roll(0, goodBlocks.Count)];

                // clear surface building.
                ClearRectangle(surface, surfaceBlock.BuildingRect);
                TileFill(surface, m_Game.GameTiles.FLOOR_CONCRETE, surfaceBlock.BuildingRect);
                m_SurfaceBlocks.Remove(surfaceBlock);

                // make maintenance building on the surface & room in the sewers.
                Block newSurfaceBlock = new Block(surfaceBlock.Rectangle);
                Point ladderHolePos = new Point(newSurfaceBlock.BuildingRect.Left + newSurfaceBlock.BuildingRect.Width / 2, newSurfaceBlock.BuildingRect.Top + newSurfaceBlock.BuildingRect.Height / 2);
                MakeSewersMaintenanceBuilding(surface, true, newSurfaceBlock, sewers, ladderHolePos);
                Block sewersRoom = new Block(surfaceBlock.Rectangle);
                MakeSewersMaintenanceBuilding(sewers, false, sewersRoom, surface, ladderHolePos);
            }
            #endregion

            // 6. Some rooms.
            #region
            foreach (Block b in blocks)
            {
                // chance?
                if (!m_DiceRoller.RollChance(SEWERS_ROOM_CHANCE))
                    continue;

                // must be all walls = not already assigned as a room.
                if (!CheckForEachTile(sewers, b.BuildingRect, (pt) => !sewers.GetTileAt(pt).Model.IsWalkable))
                    continue;

                // carve a room.
                TileFill(sewers, m_Game.GameTiles.FLOOR_CONCRETE, b.InsideRect);

                // 4 entries.
                sewers.SetTileModelAt(b.BuildingRect.Left + b.BuildingRect.Width / 2, b.BuildingRect.Top, m_Game.GameTiles.FLOOR_CONCRETE);
                sewers.SetTileModelAt(b.BuildingRect.Left + b.BuildingRect.Width / 2, b.BuildingRect.Bottom - 1, m_Game.GameTiles.FLOOR_CONCRETE);
                sewers.SetTileModelAt(b.BuildingRect.Left, b.BuildingRect.Top + b.BuildingRect.Height / 2, m_Game.GameTiles.FLOOR_CONCRETE);
                sewers.SetTileModelAt(b.BuildingRect.Right - 1, b.BuildingRect.Top + b.BuildingRect.Height / 2, m_Game.GameTiles.FLOOR_CONCRETE);

                // zone.
                sewers.AddZone(MakeUniqueZone("room", b.InsideRect));
            }
            #endregion

            // 7. Objects.
            #region
            // junk.
            MapObjectFill(sewers, new Rectangle(0, 0, sewers.Width, sewers.Height),
                (pt) =>
                {
                    if (!m_DiceRoller.RollChance(SEWERS_JUNK_CHANCE))
                        return null;
                    if (!sewers.IsWalkable(pt.X, pt.Y))
                        return null;

                    return MakeObjJunk(GameImages.OBJ_JUNK);
                });
            #endregion

            // 8. Items.
            #region
            for (int x = 0; x < sewers.Width;x++)
                for (int y = 0; y < sewers.Height; y++)
                {
                    if (!m_DiceRoller.RollChance(SEWERS_ITEM_CHANCE))
                        continue;
                    if (!sewers.IsWalkable(x, y))
                        continue;

                    // drop item.
                    Item it;
                    int roll = m_DiceRoller.Roll(0, 3);
                    switch (roll)
                    {
                        case 0: it = MakeItemBigFlashlight(); break;
                        case 1: it = MakeItemCrowbar(); break;
                        case 2: it = MakeItemSprayPaint(); break;
                        default:
                            throw new ArgumentOutOfRangeException("unhandled roll");
                    }
                    sewers.DropItemAt(it, x, y);
                }
            #endregion

            // 9. Tags.
            #region
            for (int x = 0; x < sewers.Width; x++)
                for (int y = 0; y < sewers.Height; y++)
                {
                    if (m_DiceRoller.RollChance(SEWERS_TAG_CHANCE))
                    {
                        // must be a wall with walkables around.
                        Tile t = sewers.GetTileAt(x, y);
                        if (t.Model.IsWalkable)
                            continue;
                        if (CountAdjWalkables(sewers, x, y) < 2)
                            continue;

                        // tag.
                        t.AddDecoration(TAGS[m_DiceRoller.Roll(0, TAGS.Length)]);
                    }
                }
            #endregion

            // Done.
            return sewers;
        }
    }
}