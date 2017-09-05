using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

using djack.RogueSurvivor.Data;
using djack.RogueSurvivor.Engine;
using djack.RogueSurvivor.Engine.Items;
using djack.RogueSurvivor.Engine.MapObjects;
using djack.RogueSurvivor.Gameplay;
using djack.RogueSurvivor.Gameplay.AI;
using djack.RogueSurvivor.UI;

namespace djack.RogueSurvivor.Gameplay.Generators
{
    partial class BaseTownGenerator : BaseMapGenerator
    {
        #region Types
        protected enum CHARBuildingType : byte
        {
            NONE,
            AGENCY,
            OFFICE
        }
        #endregion

        #region Constants
        public static readonly Parameters DEFAULT_PARAMS = new Parameters()
        {
            MapWidth = RogueGame.MAP_MAX_WIDTH,
            MapHeight = RogueGame.MAP_MAX_HEIGHT,
            MinBlockSize = 11, // 12 for 75x75 map size; 10 gives to many small buildings.
            WreckedCarChance = 10,
            ShopBuildingChance = 10,
            ParkBuildingChance = 10,
            CHARBuildingChance = 10,
            PostersChance = 2,
            TagsChance = 2,
            ItemInShopShelfChance = 100,
            PolicemanChance = 15
        };

        const int PARK_TREE_CHANCE = 25;
        const int PARK_BENCH_CHANCE = 5;
        const int PARK_ITEM_CHANCE = 5;

        const int MAX_CHAR_GUARDS_PER_OFFICE = 3;

        const int SEWERS_ITEM_CHANCE = 1;
        const int SEWERS_JUNK_CHANCE = 10;
        const int SEWERS_TAG_CHANCE = 10;
        const int SEWERS_IRON_FENCE_PER_BLOCK_CHANCE = 50; // 8 fences average on std maps size 75x75.
        const int SEWERS_ROOM_CHANCE = 20;

        const int SUBWAY_TAGS_POSTERS_CHANCE = 20;

        const int HOUSE_LIVINGROOM_ITEMS_ON_TABLE = 2;
        const int HOUSE_KITCHEN_ITEMS_ON_TABLE = 2;
        const int HOUSE_KITCHEN_ITEMS_IN_FRIDGE = 3;
        const int HOUSE_BASEMENT_CHANCE = 30;
        const int HOUSE_BASEMENT_OBJECT_CHANCE_PER_TILE = 10;
        const int HOUSE_BASEMENT_PILAR_CHANCE = 20;
        const int HOUSE_BASEMENT_WEAPONS_CACHE_CHANCE = 20;
        const int HOUSE_BASEMENT_ZOMBIE_RAT_CHANCE = 5; // per tile.

        const int SHOP_BASEMENT_CHANCE = 30;
        const int SHOP_BASEMENT_SHELF_CHANCE_PER_TILE = 5;
        const int SHOP_BASEMENT_ITEM_CHANCE_PER_SHELF = 33;
        const int SHOP_WINDOW_CHANCE = 30;
        const int SHOP_BASEMENT_ZOMBIE_RAT_CHANCE = 5; // per tile.
        #endregion

        #region Fields
        Parameters m_Params = DEFAULT_PARAMS;
        protected DiceRoller m_DiceRoller;

        /// <summary>
        /// Blocks on surface map since during current generation.
        /// </summary>
        List<Block> m_SurfaceBlocks;
        #endregion

        #region Properties
        public Parameters Params
        {
            get { return m_Params; }
            set { m_Params = value; }
        }
        #endregion

        public BaseTownGenerator(RogueGame game, Parameters parameters)
            : base(game)
        {
            m_Params = parameters;
            m_DiceRoller = new DiceRoller();
        }

        #region Entry Map (Surface)
        public override Map Generate(int seed)
        {
            m_DiceRoller = new DiceRoller(seed);
            Map map = new Map(seed, "Base City", m_Params.MapWidth, m_Params.MapHeight);

            ///////////////////
            // Init with grass
            ///////////////////
            base.TileFill(map, m_Game.GameTiles.FLOOR_GRASS);

            ///////////////
            // Cut blocks
            ///////////////
            List<Block> blocks = new List<Block>();
            Rectangle cityRectangle = new Rectangle(0, 0, map.Width, map.Height);
            MakeBlocks(map, true, ref blocks, cityRectangle);

            ///////////////////////////////////////
            // Make concrete buildings from blocks
            ///////////////////////////////////////
            List<Block> emptyBlocks = new List<Block>(blocks);
            List<Block> completedBlocks = new List<Block>(emptyBlocks.Count);

            // remember blocks.
            m_SurfaceBlocks = new List<Block>(blocks.Count);
            foreach (Block b in blocks)
                m_SurfaceBlocks.Add(new Block(b));

            // Special buildings.
            #region
            // Police Station?
            if (m_Params.GeneratePoliceStation)
            {
                Block policeBlock =
                    blocks[m_DiceRoller.Roll(0, blocks.Count)];
                MakePoliceStation(map, policeBlock);
                emptyBlocks.Remove(policeBlock);
            }
            // Hospital?
            if (m_Params.GenerateHospital)
            {
                Block hospitalBlock =
                    blocks[m_DiceRoller.Roll(0, blocks.Count)];
                MakeHospital(map, hospitalBlock);
                emptyBlocks.Remove(hospitalBlock);
            }
            #endregion

            // shops.
            completedBlocks.Clear();
            foreach (Block b in emptyBlocks)
            {
                if (m_DiceRoller.RollChance(m_Params.ShopBuildingChance) &&
                    MakeShopBuilding(map, b))
                    completedBlocks.Add(b);
            }
            foreach (Block b in completedBlocks)
                emptyBlocks.Remove(b);

            // CHAR buildings..
            completedBlocks.Clear();
            int charOfficesCount = 0;
            foreach (Block b in emptyBlocks)
            {
                if ((m_Params.District.Kind == DistrictKind.BUSINESS && charOfficesCount == 0) || m_DiceRoller.RollChance(m_Params.CHARBuildingChance))
                {
                    CHARBuildingType btype = MakeCHARBuilding(map, b);
                    if (btype == CHARBuildingType.OFFICE)
                    {
                        ++charOfficesCount;
                        PopulateCHAROfficeBuilding(map, b);
                    }
                    if (btype != CHARBuildingType.NONE)
                        completedBlocks.Add(b);
                }
            }
            foreach (Block b in completedBlocks)
                emptyBlocks.Remove(b);

            // parks.
            completedBlocks.Clear();
            foreach (Block b in emptyBlocks)
            {
                if (m_DiceRoller.RollChance(m_Params.ParkBuildingChance) &&
                    MakeParkBuilding(map, b))
                    completedBlocks.Add(b);
            }
            foreach (Block b in completedBlocks)
                emptyBlocks.Remove(b);

            // all the rest is housings.
            completedBlocks.Clear();
            foreach (Block b in emptyBlocks)
            {
                MakeHousingBuilding(map, b);
                completedBlocks.Add(b);
            }
            foreach (Block b in completedBlocks)
                emptyBlocks.Remove(b);

            ////////////
            // Decorate
            ////////////
            AddWreckedCarsOutside(map, cityRectangle);
            DecorateOutsideWallsWithPosters(map, cityRectangle, m_Params.PostersChance);
            DecorateOutsideWallsWithTags(map, cityRectangle, m_Params.TagsChance);

            ////////
            // Done
            ////////
            return map;
        }
        #endregion

        #region Blocks generation

        void QuadSplit(Rectangle rect, int minWidth, int minHeight, out int splitX, out int splitY, out Rectangle topLeft, out Rectangle topRight, out Rectangle bottomLeft, out Rectangle bottomRight)
        {
            // Choose a random split point.
            int leftWidthSplit = m_DiceRoller.Roll(rect.Width / 3, (2 * rect.Width) / 3);
            int topHeightSplit = m_DiceRoller.Roll(rect.Height / 3, (2 * rect.Height) / 3);

            // Ensure splitting does not produce rects below minima.
            if(leftWidthSplit < minWidth)
                leftWidthSplit = minWidth;
            if(topHeightSplit < minHeight)
                topHeightSplit = minHeight;

            int rightWidthSplit = rect.Width - leftWidthSplit;
            int bottomHeightSplit = rect.Height - topHeightSplit;

            bool doSplitX , doSplitY;
            doSplitX = doSplitY = true;

            if (rightWidthSplit < minWidth)
            {
                leftWidthSplit = rect.Width;
                rightWidthSplit = 0;
                doSplitX = false;
            }
            if (bottomHeightSplit < minHeight)
            {
                topHeightSplit = rect.Height;
                bottomHeightSplit = 0;
                doSplitY = false;
            }
            
            // Split point.
            splitX = rect.Left + leftWidthSplit;
            splitY = rect.Top + topHeightSplit;            

            // Make the quads.
            topLeft = new Rectangle(rect.Left, rect.Top, leftWidthSplit, topHeightSplit);

            if (doSplitX)
                topRight = new Rectangle(splitX, rect.Top, rightWidthSplit, topHeightSplit);
            else
                topRight = Rectangle.Empty;

            if (doSplitY)
                bottomLeft = new Rectangle(rect.Left, splitY, leftWidthSplit, bottomHeightSplit);
            else
                bottomLeft = Rectangle.Empty;

            if (doSplitX && doSplitY)
                bottomRight = new Rectangle(splitX, splitY, rightWidthSplit, bottomHeightSplit);
            else
                bottomRight = Rectangle.Empty;
        }

        void MakeBlocks(Map map, bool makeRoads, ref List<Block> list, Rectangle rect)
        {
            const int ring = 1; // dont change, keep to 1 (0=no roads, >1 = out of map)

            ////////////
            // 1. Split
            ////////////
            int splitX, splitY;
            Rectangle topLeft, topRight, bottomLeft, bottomRight;
            // +N to account for the road ring.
            QuadSplit(rect, m_Params.MinBlockSize + ring, m_Params.MinBlockSize + ring, out splitX, out splitY, out topLeft, out topRight, out bottomLeft, out bottomRight);

            ///////////////////
            // 2. Termination?
            ///////////////////
            if (topRight.IsEmpty && bottomLeft.IsEmpty && bottomRight.IsEmpty)
            {
                // Make road ring?
                if (makeRoads)
                {
                    MakeRoad(map, m_Game.GameTiles[GameTiles.IDs.ROAD_ASPHALT_EW], new Rectangle(rect.Left, rect.Top, rect.Width, ring));        // north side
                    MakeRoad(map, m_Game.GameTiles[GameTiles.IDs.ROAD_ASPHALT_EW], new Rectangle(rect.Left, rect.Bottom - 1, rect.Width, ring)); // south side
                    MakeRoad(map, m_Game.GameTiles[GameTiles.IDs.ROAD_ASPHALT_NS], new Rectangle(rect.Left, rect.Top, ring, rect.Height));       // west side
                    MakeRoad(map, m_Game.GameTiles[GameTiles.IDs.ROAD_ASPHALT_NS], new Rectangle(rect.Right - 1, rect.Top, ring, rect.Height));       // east side

                    // Adjust rect.
                    topLeft.Width -= 2 * ring;
                    topLeft.Height -= 2 * ring;
                    topLeft.Offset(ring, ring);
                }

                // Add block.
                list.Add(new Block(topLeft));
                return;
            }

            //////////////
            // 3. Recurse
            //////////////
            // always top left.
            MakeBlocks(map, makeRoads, ref list, topLeft);
            // then recurse in non empty quads.
            if (!topRight.IsEmpty)
            {
                MakeBlocks(map, makeRoads, ref list, topRight);
            }
            if (!bottomLeft.IsEmpty)
            {
                MakeBlocks(map, makeRoads, ref list, bottomLeft);
            }
            if (!bottomRight.IsEmpty)
            {
                MakeBlocks(map, makeRoads, ref list, bottomRight);
            }
        }

        protected virtual void MakeRoad(Map map, TileModel roadModel, Rectangle rect)
        {
            base.TileFill(map, roadModel, rect,
                (tile, prevmodel, x, y) =>
                {
                    // don't overwrite roads!
                    if (m_Game.GameTiles.IsRoadModel(prevmodel))
                        map.SetTileModelAt(x, y, prevmodel);
                });
            map.AddZone(base.MakeUniqueZone("road", rect));
        }
        #endregion

        #region Door/Window placement
        protected virtual void PlaceDoor(Map map, int x, int y, TileModel floor, DoorWindow door)
        {
            map.SetTileModelAt(x, y, floor);
            base.MapObjectPlace(map, x, y, door);
        }

        protected virtual void PlaceDoorIfNoObject(Map map, int x, int y, TileModel floor, DoorWindow door)
        {
            if (map.GetMapObjectAt(x, y) != null)
                return;
            PlaceDoor(map, x, y, floor, door);
        }

        protected virtual bool PlaceDoorIfAccessible(Map map, int x, int y, TileModel floor, int minAccessibility, DoorWindow door)
        {
            int countWalkable = 0;

            Point p = new Point(x, y);
            foreach (Direction d in Direction.COMPASS)
            {
                Point next = p + d;
                if (map.IsWalkable(next.X,next.Y))
                    ++countWalkable;
            }

            if (countWalkable >= minAccessibility)
            {
                PlaceDoorIfNoObject(map, x, y, floor, door);
                return true;
            }
            else
                return false;
        }

        protected virtual bool PlaceDoorIfAccessibleAndNotAdjacent(Map map, int x, int y, TileModel floor, int minAccessibility, DoorWindow door)
        {
            int countWalkable = 0;

            Point p = new Point(x, y);
            foreach (Direction d in Direction.COMPASS)
            {
                Point next = p + d;
                if (map.IsWalkable(next.X, next.Y))
                    ++countWalkable;
                if (map.GetMapObjectAt(next.X, next.Y) is DoorWindow)
                    return false;
            }

            if (countWalkable >= minAccessibility)
            {
                PlaceDoorIfNoObject(map, x, y, floor, door);
                return true;
            }
            else
                return false;
        }
        #endregion

        #region Cars
        protected virtual void AddWreckedCarsOutside(Map map, Rectangle rect)
        {
            //////////////////////////////////////
            // Add random cars (+ on fire effect)
            //////////////////////////////////////
            base.MapObjectFill(map, rect,
                (pt) =>
                {
                    if (m_DiceRoller.RollChance(m_Params.WreckedCarChance))
                    {
                        Tile tile = map.GetTileAt(pt.X, pt.Y);
                        if (!tile.IsInside && tile.Model.IsWalkable && tile.Model != m_Game.GameTiles.FLOOR_GRASS)
                        {
                            MapObject car = base.MakeObjWreckedCar(m_DiceRoller);
                            if (m_DiceRoller.RollChance(50))
                            {
                                m_Game.ApplyOnFire(car);
                            }
                            return car;
                        }
                    }
                    return null;
                });
        }
        #endregion

        #region Concrete buildings
        protected bool IsThereASpecialBuilding(Map map, Rectangle rect)
        {
            // must not be a special building.
            List<Zone> zonesUpThere = map.GetZonesAt(rect.Left, rect.Top);
            if (zonesUpThere != null)
            {
                bool special = false;
                foreach (Zone z in zonesUpThere)
                    if (z.Name.Contains(RogueGame.NAME_SEWERS_MAINTENANCE) || z.Name.Contains(RogueGame.NAME_SUBWAY_STATION) || z.Name.Contains("office") || z.Name.Contains("shop"))
                    {
                        special = true;
                        break;
                    }
                if (special)
                    return true;
            }

            // must not have an exit.
            if (map.HasAnExitIn(rect))
                return true;

            // all clear.
            return false;
        }

        protected virtual bool MakeParkBuilding(Map map, Block b)
        {
            ////////////////////////
            // 0. Check suitability
            ////////////////////////
            if (b.InsideRect.Width < 3 || b.InsideRect.Height < 3)
                return false;

            /////////////////////////////
            // 1. Grass, walkway & fence
            /////////////////////////////
            base.TileRectangle(map, m_Game.GameTiles.FLOOR_WALKWAY, b.Rectangle);
            base.TileFill(map, m_Game.GameTiles.FLOOR_GRASS, b.InsideRect);
            base.MapObjectFill(map, b.BuildingRect,
                (pt) =>
                {
                    bool placeFence = (pt.X == b.BuildingRect.Left || pt.X == b.BuildingRect.Right - 1 || pt.Y == b.BuildingRect.Top || pt.Y == b.BuildingRect.Bottom - 1);
                    if (placeFence)
                        return base.MakeObjFence(GameImages.OBJ_FENCE);
                    else
                        return null;
                });

            ///////////////////////////////
            // 2. Random trees and benches
            ///////////////////////////////
            base.MapObjectFill(map, b.InsideRect,
                (pt) =>
                {
                    bool placeTree = m_DiceRoller.RollChance(PARK_TREE_CHANCE);
                    if (placeTree)
                        return base.MakeObjTree(GameImages.OBJ_TREE);
                    else
                        return null;
                });

            base.MapObjectFill(map, b.InsideRect,
                (pt) =>
                {
                    bool placeBench = m_DiceRoller.RollChance(PARK_BENCH_CHANCE);
                    if (placeBench)
                        return base.MakeObjBench(GameImages.OBJ_BENCH);
                    else
                        return null;
                });

            ///////////////
            // 3. Entrance
            ///////////////
            int entranceFace = m_DiceRoller.Roll(0, 4);
            int ex, ey;
            switch (entranceFace)
            {
                case 0: // west
                    ex = b.BuildingRect.Left;
                    ey = b.BuildingRect.Top + b.BuildingRect.Height / 2;
                    break;
                case 1: // east
                    ex = b.BuildingRect.Right - 1;
                    ey = b.BuildingRect.Top + b.BuildingRect.Height / 2;
                    break;
                case 3: // north
                    ex = b.BuildingRect.Left + b.BuildingRect.Width / 2;
                    ey = b.BuildingRect.Top;
                    break;
                default: // south
                    ex = b.BuildingRect.Left + b.BuildingRect.Width / 2;
                    ey = b.BuildingRect.Bottom - 1;
                    break;
            }
            map.RemoveMapObjectAt(ex, ey);
            map.SetTileModelAt(ex, ey, m_Game.GameTiles.FLOOR_WALKWAY);

            ////////////
            // 4. Items
            ////////////
            base.ItemsDrop(map, b.InsideRect,
                (pt) => map.GetMapObjectAt(pt) == null && m_DiceRoller.RollChance(PARK_ITEM_CHANCE),
                (pt) => MakeRandomParkItem());

            ///////////
            // 5. Zone
            ///////////
            map.AddZone(MakeUniqueZone("Park", b.BuildingRect));
            MakeWalkwayZones(map, b);

            // Done.
            return true;
        }

        protected virtual bool MakeHousingBuilding(Map map, Block b)
        {
            ////////////////////////
            // 0. Check suitability
            ////////////////////////
            if (b.InsideRect.Width < 4 || b.InsideRect.Height < 4)
                return false;

            /////////////////////////////
            // 1. Walkway, floor & walls
            /////////////////////////////
            base.TileRectangle(map, m_Game.GameTiles.FLOOR_WALKWAY, b.Rectangle);
            base.TileRectangle(map, m_Game.GameTiles.WALL_BRICK, b.BuildingRect);
            base.TileFill(map, m_Game.GameTiles.FLOOR_PLANKS, b.InsideRect, (tile, prevmodel, x, y) => tile.IsInside = true);

            ///////////////////////
            // 2. Rooms floor plan
            ///////////////////////
            List<Rectangle> roomsList = new List<Rectangle>();
            MakeRoomsPlan(map, ref roomsList, b.BuildingRect, 5);

            /////////////////
            // 3. Make rooms
            /////////////////
            foreach (Rectangle roomRect in roomsList)
            {
                MakeHousingRoom(map, roomRect, m_Game.GameTiles.FLOOR_PLANKS, m_Game.GameTiles.WALL_BRICK);
                FillHousingRoomContents(map, roomRect);
            }

            ////////////////////////////
            // 5. Fix doorless building
            ////////////////////////////
            #region
            bool hasOutsideDoor = false;
            for (int x = b.BuildingRect.Left; x < b.BuildingRect.Right && !hasOutsideDoor; x++)
                for (int y = b.BuildingRect.Top; y < b.BuildingRect.Bottom && !hasOutsideDoor; y++)
                {
                    if (!map.GetTileAt(x, y).IsInside)
                    {
                        DoorWindow door = map.GetMapObjectAt(x, y) as DoorWindow;
                        if (door != null && !door.IsWindow)
                            hasOutsideDoor = true;
                    }
                }
            if (!hasOutsideDoor)
            {
                // replace a random window with a door.
                do
                {
                    // dumb brute force...
                    int x = m_DiceRoller.Roll(b.BuildingRect.Left, b.BuildingRect.Right);
                    int y = m_DiceRoller.Roll(b.BuildingRect.Top, b.BuildingRect.Bottom);
                    if (!map.GetTileAt(x, y).IsInside)
                    {
                        DoorWindow door = map.GetMapObjectAt(x, y) as DoorWindow;
                        if (door != null && door.IsWindow)
                        {
                            map.RemoveMapObjectAt(x, y);
                            map.PlaceMapObjectAt(MakeObjWoodenDoor(), new Point(x, y));
                            hasOutsideDoor = true;
                        }
                    }
                }
                while (!hasOutsideDoor);
            }
            #endregion

            ////////////////
            // 6. Basement?
            ////////////////
            #region
            if (m_DiceRoller.RollChance(HOUSE_BASEMENT_CHANCE))
            {
                Map basementMap = GenerateHouseBasementMap(map, b);
                m_Params.District.AddUniqueMap(basementMap);
            }
            #endregion

            ///////////
            // 7. Zone
            ///////////
            map.AddZone(MakeUniqueZone("Housing", b.BuildingRect));
            MakeWalkwayZones(map, b);

            // Done
            return true;
        }

        #endregion

        #region Rooms
        protected virtual void MakeRoomsPlan(Map map, ref List<Rectangle> list, Rectangle rect, int minRoomsSize)
        {
            ////////////
            // 1. Split
            ////////////
            int splitX, splitY;
            Rectangle topLeft, topRight, bottomLeft, bottomRight;
            QuadSplit(rect, minRoomsSize, minRoomsSize, out splitX, out splitY, out topLeft, out topRight, out bottomLeft, out bottomRight);

            ///////////////////
            // 2. Termination?
            ///////////////////
            if (topRight.IsEmpty && bottomLeft.IsEmpty && bottomRight.IsEmpty)
            {
                list.Add(rect);
                return;
            }

            //////////////
            // 3. Recurse
            //////////////
            // always top left.
            MakeRoomsPlan(map, ref list, topLeft, minRoomsSize);
            // then recurse in non empty quads.
            // we shift and inflante the quads cause we want rooms walls and doors to overlap.
            if (!topRight.IsEmpty)
            {
                topRight.Offset(-1, 0);
                ++topRight.Width;
                MakeRoomsPlan(map, ref list, topRight, minRoomsSize);
            }
            if (!bottomLeft.IsEmpty)
            {
                bottomLeft.Offset(0, -1);
                ++bottomLeft.Height;
                MakeRoomsPlan(map, ref list, bottomLeft, minRoomsSize);
            }
            if (!bottomRight.IsEmpty)
            {
                bottomRight.Offset(-1, -1);
                ++bottomRight.Width;
                ++bottomRight.Height;
                MakeRoomsPlan(map, ref list, bottomRight, minRoomsSize);
            }
        }

        protected virtual void MakeHousingRoom(Map map, Rectangle roomRect, TileModel floor, TileModel wall)
        {
            ////////////////////
            // 1. Floor & Walls
            ////////////////////
            base.TileFill(map, floor, roomRect);
            base.TileRectangle(map, wall, roomRect.Left, roomRect.Top, roomRect.Width, roomRect.Height,
                (tile, prevmodel, x, y) =>
                {
                    // if we have a door there, don't put a wall!
                    if (map.GetMapObjectAt(x, y) != null)
                        map.SetTileModelAt(x, y, floor);
                });

            //////////////////////
            // 2. Doors & Windows
            //////////////////////
            int midX = roomRect.Left + roomRect.Width / 2;
            int midY = roomRect.Top + roomRect.Height / 2;
            const int outsideDoorChance = 25;

            PlaceIf(map, midX, roomRect.Top, floor,
                (x, y) => HasNoObjectAt(map, x, y) && IsAccessible(map, x, y) && CountAdjDoors(map, x, y) == 0,
                (x, y) => IsInside(map, x, y) || m_DiceRoller.RollChance(outsideDoorChance) ? MakeObjWoodenDoor() : MakeObjWindow());
            PlaceIf(map, midX, roomRect.Bottom - 1, floor,
                (x, y) => HasNoObjectAt(map, x, y) && IsAccessible(map, x, y) && CountAdjDoors(map, x, y) == 0,
                (x, y) => IsInside(map, x, y) || m_DiceRoller.RollChance(outsideDoorChance) ? MakeObjWoodenDoor() : MakeObjWindow());
            PlaceIf(map, roomRect.Left, midY, floor,
                (x, y) => HasNoObjectAt(map, x, y) && IsAccessible(map, x, y) && CountAdjDoors(map, x, y) == 0,
                (x, y) => IsInside(map, x, y) || m_DiceRoller.RollChance(outsideDoorChance) ? MakeObjWoodenDoor() : MakeObjWindow());
            PlaceIf(map, roomRect.Right - 1, midY, floor,
                (x, y) => HasNoObjectAt(map, x, y) && IsAccessible(map, x, y) && CountAdjDoors(map, x, y) == 0,
                (x, y) => IsInside(map, x, y) || m_DiceRoller.RollChance(outsideDoorChance) ? MakeObjWoodenDoor() : MakeObjWindow());
        }

        protected virtual void FillHousingRoomContents(Map map, Rectangle roomRect)
        {
            Rectangle insideRoom = new Rectangle(roomRect.Left + 1, roomRect.Top + 1, roomRect.Width - 2, roomRect.Height - 2);

            // Decide room role.
            int role = m_DiceRoller.Roll(0, 10);

            switch (role)
            {
                // 1. Bedroom? 0-4 = 50%
                #region
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                    {
                        #region
                        // beds with night tables.
                        int nbBeds = m_DiceRoller.Roll(1, 3);
                        for (int i = 0; i < nbBeds; i++)
                        {
                            MapObjectPlaceInGoodPosition(map, insideRoom,
                                (pt) => CountAdjWalls(map, pt.X, pt.Y) >= 3 && CountAdjDoors(map, pt.X, pt.Y) == 0,
                                m_DiceRoller,
                                (pt) =>
                                {
                                    // one night table around with item.
                                    Rectangle adjBedRect = new Rectangle(pt.X - 1, pt.Y - 1, 3, 3);
                                    adjBedRect.Intersect(insideRoom);
                                    MapObjectPlaceInGoodPosition(map, adjBedRect,
                                        (pt2) => pt2 != pt && CountAdjDoors(map, pt2.X, pt2.Y) == 0 && CountAdjWalls(map, pt2.X, pt2.Y) > 0,
                                        m_DiceRoller,
                                        (pt2) =>
                                        {
                                            // item.
                                            Item it = MakeRandomBedroomItem();
                                            if (it != null)
                                                map.DropItemAt(it, pt2);

                                            // night table.
                                            return MakeObjNightTable(GameImages.OBJ_NIGHT_TABLE);
                                        });

                                    // bed.
                                    MapObject bed = MakeObjBed(GameImages.OBJ_BED);
                                    return bed;
                                });
                        }

                        // wardrobe/drawer with items
                        int nbWardrobeOrDrawer = m_DiceRoller.Roll(1, 4);
                        for (int i = 0; i < nbWardrobeOrDrawer; i++)
                        {
                            MapObjectPlaceInGoodPosition(map, insideRoom,
                                                (pt) => CountAdjWalls(map, pt.X, pt.Y) >= 2 && CountAdjDoors(map, pt.X, pt.Y) == 0,
                                                m_DiceRoller,
                                                (pt) =>
                                                {
                                                    // item.
                                                    Item it = MakeRandomBedroomItem();
                                                    if (it != null)
                                                        map.DropItemAt(it, pt);

                                                    // wardrobe or drawer
                                                    if (m_DiceRoller.RollChance(50))
                                                        return MakeObjWardrobe(GameImages.OBJ_WARDROBE);
                                                    else
                                                        return MakeObjDrawer(GameImages.OBJ_DRAWER);
                                                });
                        }
                        break;
                        #endregion
                    }
                #endregion

                // 2. Living room? 5-6-7 = 30%
                #region
                case 5:
                case 6:
                case 7:
                    {
                        #region
                        // tables with chairs.
                        int nbTables = m_DiceRoller.Roll(1, 3);

                        for (int i = 0; i < nbTables; i++)
                        {
                            MapObjectPlaceInGoodPosition(map, insideRoom,
                                (pt) => CountAdjWalls(map, pt.X, pt.Y) == 0 && CountAdjDoors(map, pt.X, pt.Y) == 0,
                                m_DiceRoller,
                                (pt) =>
                                {
                                    // items.
                                    for (int ii = 0; ii < HOUSE_LIVINGROOM_ITEMS_ON_TABLE; ii++)
                                    {
                                        Item it = MakeRandomKitchenItem();
                                        if (it != null)
                                            map.DropItemAt(it, pt);
                                    }

                                    // one chair around.
                                    Rectangle adjTableRect = new Rectangle(pt.X - 1, pt.Y - 1, 3, 3);
                                    adjTableRect.Intersect(insideRoom);
                                    MapObjectPlaceInGoodPosition(map, adjTableRect,
                                        (pt2) => pt2 != pt && CountAdjDoors(map, pt2.X, pt2.Y) == 0,
                                        m_DiceRoller,
                                        (pt2) => MakeObjChair(GameImages.OBJ_CHAIR));

                                    // table.
                                    MapObject table = MakeObjTable(GameImages.OBJ_TABLE);
                                    return table;
                                });
                        }

                        // drawers.
                        int nbDrawers = m_DiceRoller.Roll(1, 3);
                        for (int i = 0; i < nbDrawers; i++)
                        {
                            MapObjectPlaceInGoodPosition(map, insideRoom,
                                                (pt) => CountAdjWalls(map, pt.X, pt.Y) >= 2 && CountAdjDoors(map, pt.X, pt.Y) == 0,
                                                m_DiceRoller,
                                                (pt) => MakeObjDrawer(GameImages.OBJ_DRAWER));
                        }
                        break;
                        #endregion
                    }
                #endregion

                // 3. Kitchen? 8-9 = 20%
                #region
                case 8:
                case 9:
                    {
                        #region
                        // table with item & chair.
                        MapObjectPlaceInGoodPosition(map, insideRoom,
                            (pt) => CountAdjWalls(map, pt.X, pt.Y) == 0 && CountAdjDoors(map, pt.X, pt.Y) == 0,
                            m_DiceRoller,
                            (pt) =>
                            {
                                // items.
                                for (int ii = 0; ii < HOUSE_KITCHEN_ITEMS_ON_TABLE; ii++)
                                {
                                    Item it = MakeRandomKitchenItem();
                                    if (it != null)
                                        map.DropItemAt(it, pt);
                                }

                                // one chair around.
                                Rectangle adjTableRect = new Rectangle(pt.X - 1, pt.Y - 1, 3, 3);
                                MapObjectPlaceInGoodPosition(map, adjTableRect,
                                    (pt2) => pt2 != pt && CountAdjDoors(map, pt2.X, pt2.Y) == 0,
                                    m_DiceRoller,
                                    (pt2) => MakeObjChair(GameImages.OBJ_CHAIR));

                                // table.
                                return MakeObjTable(GameImages.OBJ_TABLE);
                            });

                        // fridge with items
                        MapObjectPlaceInGoodPosition(map, insideRoom,
                                            (pt) => CountAdjWalls(map, pt.X, pt.Y) >= 2 && CountAdjDoors(map, pt.X, pt.Y) == 0,
                                            m_DiceRoller,
                                            (pt) =>
                                            {
                                                // items.
                                                for (int ii = 0; ii < HOUSE_KITCHEN_ITEMS_IN_FRIDGE; ii++)
                                                {
                                                    Item it = MakeRandomKitchenItem();
                                                    if (it != null)
                                                        map.DropItemAt(it, pt);
                                                }

                                                // fridge
                                                return MakeObjFridge(GameImages.OBJ_FRIDGE);
                                            });
                        break;
                        #endregion
                    }

                default:
                    throw new ArgumentOutOfRangeException("unhandled roll");
                #endregion
            }
        }

        #endregion

        #region Items

        public Item MakeHospitalItem()
        {
            int randomItem = m_DiceRoller.Roll(0, 7);
            switch (randomItem)
            {
                case 0: return MakeItemBandages();
                case 1: return MakeItemMedikit();
                case 2: return MakeItemPillsSLP();
                case 3: return MakeItemPillsSTA();
                case 4: return MakeItemPillsSAN();
                case 5: return MakeItemStenchKiller();
                case 6: return MakeItemPillsAntiviral();

                default:
                    throw new ArgumentOutOfRangeException("unhandled roll");
            }
        }


        public Item MakeRandomBedroomItem()
        {
            int randomItem = m_DiceRoller.Roll(0, 24);

            switch (randomItem)
            {
                case 0:
                case 1: return MakeItemBandages();
                case 2: return MakeItemPillsSTA();
                case 3: return MakeItemPillsSLP();
                case 4: return MakeItemPillsSAN();

                case 5: 
                case 6:
                case 7: 
                case 8: return MakeItemBaseballBat();

                case 9: return MakeItemRandomPistol();

                case 10: // rare fire weapon
                    if (m_DiceRoller.RollChance(30))
                    {
                        if (m_DiceRoller.RollChance(50))
                            return MakeItemShotgun();
                        else
                            return MakeItemHuntingRifle();
                    }
                    else
                    {
                        if (m_DiceRoller.RollChance(50))
                            return MakeItemShotgunAmmo();
                        else
                            return MakeItemLightRifleAmmo();
                    }
                case 11: 
                case 12:
                case 13: return MakeItemCellPhone();

                case 14:
                case 15: return MakeItemFlashlight();

                case 16: 
                case 17: return MakeItemLightPistolAmmo();

                case 18: 
                case 19: return MakeItemStenchKiller();

                case 20: return MakeItemHunterVest();

                case 21:
                case 22:
                case 23:
                    if (m_DiceRoller.RollChance(50))
                        return MakeItemBook();
                    else
                        return MakeItemMagazines();

                default: throw new ArgumentOutOfRangeException("unhandled roll");
            }
        }

        public Item MakeRandomKitchenItem()
        {
            if (m_DiceRoller.RollChance(50))
                return MakeItemCannedFood();
            else
                return MakeItemGroceries();
        }

        public Item MakeRandomParkItem()
        {
            int randomItem = m_DiceRoller.Roll(0, 8);
            switch (randomItem)
            {
                case 0: return MakeItemSprayPaint();
                case 1: return MakeItemBaseballBat();
                case 2: return MakeItemPillsSLP();
                case 3: return MakeItemPillsSTA();
                case 4: return MakeItemPillsSAN();
                case 5: return MakeItemFlashlight();
                case 6: return MakeItemCellPhone();
                case 7: return MakeItemWoodenPlank();
                default: throw new ArgumentOutOfRangeException("unhandled item roll");
            }
        }
        #endregion

        #region Decorations

        static readonly string[] POSTERS = { GameImages.DECO_POSTERS1, GameImages.DECO_POSTERS2 };
        protected virtual void DecorateOutsideWallsWithPosters(Map map, Rectangle rect, int chancePerWall)
        {
            base.DecorateOutsideWalls(map, rect,
                (x, y) =>
                {
                    if (m_DiceRoller.RollChance(chancePerWall))
                    {
                        return POSTERS[m_DiceRoller.Roll(0, POSTERS.Length)];
                    }
                    else
                        return null;
                });
        }

        static readonly string[] TAGS = { GameImages.DECO_TAGS1, GameImages.DECO_TAGS2, GameImages.DECO_TAGS3, GameImages.DECO_TAGS4, GameImages.DECO_TAGS5, GameImages.DECO_TAGS6, GameImages.DECO_TAGS7 };

        protected virtual void DecorateOutsideWallsWithTags(Map map, Rectangle rect, int chancePerWall)
        {
            base.DecorateOutsideWalls(map, rect,
                (x, y) =>
                {
                    if (m_DiceRoller.RollChance(chancePerWall))
                    {
                        return TAGS[m_DiceRoller.Roll(0, TAGS.Length)];
                    }
                    else
                        return null;
                });
        }
        #endregion

        #region Special Locations
        #region House Basement
        Map GenerateHouseBasementMap(Map map, Block houseBlock)
        {
            // make map.
            #region
            Rectangle rect = houseBlock.BuildingRect;
            int seed = map.Seed << 1 + rect.Left * map.Height + rect.Top;
            Map basement = new Map(seed, String.Format("basement{0}{1}@{2}-{3}", m_Params.District.WorldPosition.X, m_Params.District.WorldPosition.Y, rect.Left + rect.Width / 2, rect.Top + rect.Height / 2), rect.Width, rect.Height)
            {
                Lighting = Lighting.DARKNESS
            };
            basement.AddZone(MakeUniqueZone("basement", basement.Rect));
            #endregion

            // enclose.
            #region
            TileFill(basement, m_Game.GameTiles.FLOOR_CONCRETE, (tile, model, x, y) => tile.IsInside = true);
            TileRectangle(basement, m_Game.GameTiles.WALL_BRICK, new Rectangle(0, 0, basement.Width, basement.Height));
            #endregion

            // link to house with stairs.
            #region
            Point surfaceStairs = new Point();
            for (; ; )
            {
                // roll.
                surfaceStairs.X = m_DiceRoller.Roll(rect.Left, rect.Right);
                surfaceStairs.Y = m_DiceRoller.Roll(rect.Top, rect.Bottom);

                // valid if walkable & no blocking object.
                if (!map.GetTileAt(surfaceStairs.X,surfaceStairs.Y).Model.IsWalkable)
                    continue;
                if (map.GetMapObjectAt(surfaceStairs.X, surfaceStairs.Y) != null)
                    continue;
                
                // good post.
                break;
            }
            Point basementStairs = new Point(surfaceStairs.X - rect.Left, surfaceStairs.Y - rect.Top);
            AddExit(map, surfaceStairs, basement, basementStairs, GameImages.DECO_STAIRS_DOWN, true);
            AddExit(basement, basementStairs, map, surfaceStairs, GameImages.DECO_STAIRS_UP, true);
            #endregion

            // random pilars/walls.
            #region
            DoForEachTile(basement, basement.Rect,
                (pt) =>
                {
                    if (!m_DiceRoller.RollChance(HOUSE_BASEMENT_PILAR_CHANCE))
                        return;
                    if (pt == basementStairs)
                        return;
                    basement.SetTileModelAt(pt.X, pt.Y, m_Game.GameTiles.WALL_BRICK);
                });
            #endregion

            // fill with ome furniture/crap and items.
            #region
            MapObjectFill(basement, basement.Rect,
                (pt) =>
                {
                    if (!m_DiceRoller.RollChance(HOUSE_BASEMENT_OBJECT_CHANCE_PER_TILE))
                        return null;

                    if (basement.GetExitAt(pt) != null)
                        return null;
                    if (!basement.IsWalkable(pt.X, pt.Y))
                        return null;

                    int roll = m_DiceRoller.Roll(0, 5);
                    switch (roll)
                    {
                        case 0: // junk
                            return MakeObjJunk(GameImages.OBJ_JUNK);
                        case 1: // barrels.
                            return MakeObjBarrels(GameImages.OBJ_BARRELS);
                        case 2: // table with random item.
                            {
                                Item it = MakeShopConstructionItem();
                                basement.DropItemAt(it, pt);
                                return MakeObjTable(GameImages.OBJ_TABLE);
                            };
                        case 3: // drawer with random item.
                            {
                                Item it = MakeShopConstructionItem();
                                basement.DropItemAt(it, pt);
                                return MakeObjDrawer(GameImages.OBJ_DRAWER);
                            };
                        case 4: // bed.
                            return MakeObjBed(GameImages.OBJ_BED);

                        default:
                            throw new ArgumentOutOfRangeException("unhandled roll");
                    }
                });
            #endregion

            // rats!
            #region
            if (Rules.HasZombiesInBasements(m_Game.Session.GameMode))
            {
                DoForEachTile(basement, basement.Rect,
                    (pt) =>
                    {
                        if (!basement.IsWalkable(pt.X, pt.Y))
                            return;
                        if (basement.GetExitAt(pt) != null)
                            return;

                        if (m_DiceRoller.RollChance(SHOP_BASEMENT_ZOMBIE_RAT_CHANCE))
                            basement.PlaceActorAt(CreateNewBasementRatZombie(0), pt);
                    });
            }
            #endregion

            // weapons cache?
            #region
            if (m_DiceRoller.RollChance(HOUSE_BASEMENT_WEAPONS_CACHE_CHANCE))
            {
                MapObjectPlaceInGoodPosition(basement, basement.Rect,
                    (pt) =>
                    {
                        if (basement.GetExitAt(pt) != null)
                            return false;
                        if (!basement.IsWalkable(pt.X, pt.Y))
                            return false;
                        if (basement.GetMapObjectAt(pt) != null)
                            return false;
                        if (basement.GetItemsAt(pt) != null)
                            return false;
                        return true;
                    },
                    m_DiceRoller,
                    (pt) =>
                    {
                        // two grenades...
                        basement.DropItemAt(MakeItemGrenade(), pt);
                        basement.DropItemAt(MakeItemGrenade(), pt);

                        // and a handfull of gunshop items.
                        for (int i = 0; i < 5; i++)
                        {
                            Item it = MakeShopGunshopItem();
                            basement.DropItemAt(it, pt);
                        }

                        // shelf.
                        MapObject shelf = MakeObjShelf(GameImages.OBJ_SHOP_SHELF);
                        return shelf;
                    });
            }
            #endregion

            // done.
            return basement;
        }
        #endregion

        #endregion

        #region Exits
        /// <summary>
        /// Add the Exit with the decoration.
        /// </summary>
        /// <param name="from"></param>
        /// <param name="fromPosition"></param>
        /// <param name="to"></param>
        /// <param name="toPosition"></param>
        /// <param name="exitImageID"></param>
        void AddExit(Map from, Point fromPosition, Map to, Point toPosition, string exitImageID, bool isAnAIExit)
        {
            from.SetExitAt(fromPosition, new Exit(to, toPosition) { IsAnAIExit = isAnAIExit });
            from.GetTileAt(fromPosition).AddDecoration(exitImageID);
        }
        #endregion

        #region Zones
        protected void MakeWalkwayZones(Map map, Block b)
        {
            /*
             *  NNNE
             *  W  E
             *  W  E
             *  WSSS
             *
             */
            Rectangle r = b.Rectangle;

            // N
            map.AddZone(MakeUniqueZone("walkway", new Rectangle(r.Left, r.Top, r.Width - 1, 1)));
            // S
            map.AddZone(MakeUniqueZone("walkway", new Rectangle(r.Left + 1, r.Bottom - 1, r.Width - 1, 1)));
            // E
            map.AddZone(MakeUniqueZone("walkway", new Rectangle(r.Right - 1, r.Top, 1, r.Height - 1)));
            // W
            map.AddZone(MakeUniqueZone("walkway", new Rectangle(r.Left, r.Top + 1, 1, r.Height - 1)));
        }
        #endregion
    }
}
