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
        protected enum ShopType : byte
        {
            _FIRST,

            GENERAL_STORE = _FIRST,
            GROCERY,
            SPORTSWEAR,
            PHARMACY,
            CONSTRUCTION,
            GUNSHOP,
            HUNTING,

            _COUNT
        }
        
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

        /// <summary>
        /// Either an Office (for large enough buildings) or an Agency (for small buildings).
        /// </summary>
        /// <param name="map"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        protected virtual CHARBuildingType MakeCHARBuilding(Map map, Block b)
        {
            ///////////////////////////////
            // Offices are large buildings.
            // Agency are small ones.
            ///////////////////////////////
            if (b.InsideRect.Width < 8 || b.InsideRect.Height < 8)
            {
                // small, make it an Agency.
                if (MakeCHARAgency(map, b))
                    return CHARBuildingType.AGENCY;
                else
                    return CHARBuildingType.NONE;
            }
            else
            {
                if (MakeCHAROffice(map, b))
                    return CHARBuildingType.OFFICE;
                else
                    return CHARBuildingType.NONE;
            }
        }

        static string[] CHAR_POSTERS = { GameImages.DECO_CHAR_POSTER1, GameImages.DECO_CHAR_POSTER2, GameImages.DECO_CHAR_POSTER3 };

        protected virtual bool MakeCHARAgency(Map map, Block b)
        {
            /////////////////////////////
            // 1. Walkway, floor & walls
            /////////////////////////////
            base.TileRectangle(map, m_Game.GameTiles.FLOOR_WALKWAY, b.Rectangle);
            base.TileRectangle(map, m_Game.GameTiles.WALL_CHAR_OFFICE, b.BuildingRect);
            base.TileFill(map, m_Game.GameTiles.FLOOR_OFFICE, b.InsideRect,
                (tile, prevmodel, x, y) =>
                {
                    tile.IsInside = true;
                    tile.AddDecoration(GameImages.DECO_CHAR_FLOOR_LOGO);
                });

            //////////////////////////
            // 2. Decide orientation.
            //////////////////////////          
            bool horizontalCorridor = (b.InsideRect.Width >= b.InsideRect.Height);

            /////////////////
            // 3. Entry door 
            /////////////////
            #region
            int midX = b.Rectangle.Left + b.Rectangle.Width / 2;
            int midY = b.Rectangle.Top + b.Rectangle.Height / 2;

            // make doors on one side.
            #region
            if (horizontalCorridor)
            {
                bool west = m_DiceRoller.RollChance(50);

                if (west)
                {
                    // west
                    PlaceDoor(map, b.BuildingRect.Left, midY, m_Game.GameTiles.FLOOR_WALKWAY, base.MakeObjGlassDoor());
                    if (b.InsideRect.Height >= 8)
                    {
                        PlaceDoor(map, b.BuildingRect.Left, midY - 1, m_Game.GameTiles.FLOOR_WALKWAY, base.MakeObjGlassDoor());
                        if (b.InsideRect.Height >= 12)
                            PlaceDoor(map, b.BuildingRect.Left, midY + 1, m_Game.GameTiles.FLOOR_WALKWAY, base.MakeObjGlassDoor());
                    }
                }
                else
                {
                    // east
                    PlaceDoor(map, b.BuildingRect.Right - 1, midY, m_Game.GameTiles.FLOOR_WALKWAY, base.MakeObjGlassDoor());
                    if (b.InsideRect.Height >= 8)
                    {
                        PlaceDoor(map, b.BuildingRect.Right - 1, midY - 1, m_Game.GameTiles.FLOOR_WALKWAY, base.MakeObjGlassDoor());
                        if (b.InsideRect.Height >= 12)
                            PlaceDoor(map, b.BuildingRect.Right - 1, midY + 1, m_Game.GameTiles.FLOOR_WALKWAY, base.MakeObjGlassDoor());
                    }
                }
            }
            else
            {
                bool north = m_DiceRoller.RollChance(50);

                if (north)
                {
                    // north
                    PlaceDoor(map, midX, b.BuildingRect.Top, m_Game.GameTiles.FLOOR_WALKWAY, base.MakeObjGlassDoor());
                    if (b.InsideRect.Width >= 8)
                    {
                        PlaceDoor(map, midX - 1, b.BuildingRect.Top, m_Game.GameTiles.FLOOR_WALKWAY, base.MakeObjGlassDoor());
                        if (b.InsideRect.Width >= 12)
                            PlaceDoor(map, midX + 1, b.BuildingRect.Top, m_Game.GameTiles.FLOOR_WALKWAY, base.MakeObjGlassDoor());
                    }
                }
                else
                {
                    // south
                    PlaceDoor(map, midX, b.BuildingRect.Bottom - 1, m_Game.GameTiles.FLOOR_WALKWAY, base.MakeObjGlassDoor());
                    if (b.InsideRect.Width >= 8)
                    {
                        PlaceDoor(map, midX - 1, b.BuildingRect.Bottom - 1, m_Game.GameTiles.FLOOR_WALKWAY, base.MakeObjGlassDoor());
                        if (b.InsideRect.Width >= 12)
                            PlaceDoor(map, midX + 1, b.BuildingRect.Bottom - 1, m_Game.GameTiles.FLOOR_WALKWAY, base.MakeObjGlassDoor());
                    }
                }
            }
            #endregion

            // add office image next to doors.
            string officeImage = GameImages.DECO_CHAR_OFFICE;
            DecorateOutsideWalls(map, b.BuildingRect, (x, y) => map.GetMapObjectAt(x, y) == null && CountAdjDoors(map, x, y) >= 1 ? officeImage : null);
            #endregion

            ////////////////
            // 4. Furniture
            ////////////////
            #region
            // chairs on the sides.
            MapObjectFill(map, b.InsideRect,
                (pt) =>
                {
                    if (CountAdjWalls(map, pt.X, pt.Y) < 3)
                        return null;
                    return MakeObjChair(GameImages.OBJ_CHAR_CHAIR);
                });
            // walls/pilars in the middle.
            TileFill(map, m_Game.GameTiles.WALL_CHAR_OFFICE, new Rectangle(b.InsideRect.Left + b.InsideRect.Width / 2 - 1, b.InsideRect.Top + b.InsideRect.Height / 2 - 1, 3, 2),
                (tile, model, x, y) =>
                {
                    tile.AddDecoration(CHAR_POSTERS[m_DiceRoller.Roll(0, CHAR_POSTERS.Length)]);
                });
            #endregion

            //////////////
            // 5. Posters
            //////////////
            #region
            // outside.
            DecorateOutsideWalls(map, b.BuildingRect,
                (x, y) =>
                {
                    if (CountAdjDoors(map, x, y) > 0)
                        return null;
                    else
                    {
                        if (m_DiceRoller.RollChance(25))
                            return CHAR_POSTERS[m_DiceRoller.Roll(0, CHAR_POSTERS.Length)];
                        else
                            return null;
                    }
                });
            #endregion

            ////////////
            // 6. Zones.
            ////////////
            map.AddZone(MakeUniqueZone("CHAR Agency", b.BuildingRect));
            MakeWalkwayZones(map, b);

            // Done
            return true;
        }

        protected virtual bool MakeCHAROffice(Map map, Block b)
        {

            /////////////////////////////
            // 1. Walkway, floor & walls
            /////////////////////////////
            base.TileRectangle(map, m_Game.GameTiles.FLOOR_WALKWAY, b.Rectangle);
            base.TileRectangle(map, m_Game.GameTiles.WALL_CHAR_OFFICE, b.BuildingRect);
            base.TileFill(map, m_Game.GameTiles.FLOOR_OFFICE, b.InsideRect, (tile, prevmodel, x, y) => tile.IsInside = true);

            //////////////////////////
            // 2. Decide orientation.
            //////////////////////////          
            bool horizontalCorridor = (b.InsideRect.Width >= b.InsideRect.Height);

            /////////////////
            // 3. Entry door 
            /////////////////
            #region
            int midX = b.Rectangle.Left + b.Rectangle.Width / 2;
            int midY = b.Rectangle.Top + b.Rectangle.Height / 2;
            Direction doorSide;

            // make doors on one side.
            #region
            if (horizontalCorridor)
            {
                bool west = m_DiceRoller.RollChance(50);

                if (west)
                {
                    doorSide = Direction.W;
                    // west
                    PlaceDoor(map, b.BuildingRect.Left, midY, m_Game.GameTiles.FLOOR_WALKWAY, base.MakeObjGlassDoor());
                    if (b.InsideRect.Height >= 8)
                    {
                        PlaceDoor(map, b.BuildingRect.Left, midY - 1, m_Game.GameTiles.FLOOR_WALKWAY, base.MakeObjGlassDoor());
                        if (b.InsideRect.Height >= 12)
                            PlaceDoor(map, b.BuildingRect.Left, midY + 1, m_Game.GameTiles.FLOOR_WALKWAY, base.MakeObjGlassDoor());
                    }
                }
                else
                {
                    doorSide = Direction.E;
                    // east
                    PlaceDoor(map, b.BuildingRect.Right - 1, midY, m_Game.GameTiles.FLOOR_WALKWAY, base.MakeObjGlassDoor());
                    if (b.InsideRect.Height >= 8)
                    {
                        PlaceDoor(map, b.BuildingRect.Right - 1, midY - 1, m_Game.GameTiles.FLOOR_WALKWAY, base.MakeObjGlassDoor());
                        if (b.InsideRect.Height >= 12)
                            PlaceDoor(map, b.BuildingRect.Right - 1, midY + 1, m_Game.GameTiles.FLOOR_WALKWAY, base.MakeObjGlassDoor());
                    }
                }
            }
            else
            {
                bool north = m_DiceRoller.RollChance(50);

                if (north)
                {
                    doorSide = Direction.N;
                    // north
                    PlaceDoor(map, midX, b.BuildingRect.Top, m_Game.GameTiles.FLOOR_WALKWAY, base.MakeObjGlassDoor());
                    if (b.InsideRect.Width >= 8)
                    {
                        PlaceDoor(map, midX - 1, b.BuildingRect.Top, m_Game.GameTiles.FLOOR_WALKWAY, base.MakeObjGlassDoor());
                        if (b.InsideRect.Width >= 12)
                            PlaceDoor(map, midX + 1, b.BuildingRect.Top, m_Game.GameTiles.FLOOR_WALKWAY, base.MakeObjGlassDoor());
                    }
                }
                else
                {
                    doorSide = Direction.S;
                    // south
                    PlaceDoor(map, midX, b.BuildingRect.Bottom - 1, m_Game.GameTiles.FLOOR_WALKWAY, base.MakeObjGlassDoor());
                    if (b.InsideRect.Width >= 8)
                    {
                        PlaceDoor(map, midX - 1, b.BuildingRect.Bottom - 1, m_Game.GameTiles.FLOOR_WALKWAY, base.MakeObjGlassDoor());
                        if (b.InsideRect.Width >= 12)
                            PlaceDoor(map, midX + 1, b.BuildingRect.Bottom - 1, m_Game.GameTiles.FLOOR_WALKWAY, base.MakeObjGlassDoor());
                    }
                }
            }
            #endregion

            // add office image next to doors.
            string officeImage = GameImages.DECO_CHAR_OFFICE;
            DecorateOutsideWalls(map, b.BuildingRect, (x, y) => map.GetMapObjectAt(x, y) == null && CountAdjDoors(map, x, y) >= 1 ? officeImage : null);

            // barricade entry doors.
            BarricadeDoors(map, b.BuildingRect, Rules.BARRICADING_MAX);
            #endregion

            ///////////////////////
            // 4. Make entry hall.
            ///////////////////////
            #region
            const int hallDepth = 3;
            if (doorSide == Direction.N)
            {
                base.TileHLine(map, m_Game.GameTiles.WALL_CHAR_OFFICE, b.InsideRect.Left, b.InsideRect.Top + hallDepth, b.InsideRect.Width);
            }
            else if (doorSide == Direction.S)
            {
                base.TileHLine(map, m_Game.GameTiles.WALL_CHAR_OFFICE, b.InsideRect.Left, b.InsideRect.Bottom - 1 - hallDepth, b.InsideRect.Width);
            }
            else if (doorSide == Direction.E)
            {
                base.TileVLine(map, m_Game.GameTiles.WALL_CHAR_OFFICE, b.InsideRect.Right - 1 - hallDepth, b.InsideRect.Top, b.InsideRect.Height);
            }
            else if (doorSide == Direction.W)
            {
                base.TileVLine(map, m_Game.GameTiles.WALL_CHAR_OFFICE, b.InsideRect.Left + hallDepth, b.InsideRect.Top, b.InsideRect.Height);
            }
            else
                throw new InvalidOperationException("unhandled door side");
            #endregion

            /////////////////////////////////////
            // 5. Make central corridor & wings
            /////////////////////////////////////
            #region
            Rectangle corridorRect;
            Point corridorDoor;
            if (doorSide == Direction.N)
            {
                corridorRect = new Rectangle(midX - 1, b.InsideRect.Top + hallDepth, 3, b.BuildingRect.Height - 1 - hallDepth);
                corridorDoor = new Point(corridorRect.Left + 1, corridorRect.Top);
            }
            else if (doorSide == Direction.S)
            {
                corridorRect = new Rectangle(midX - 1, b.BuildingRect.Top, 3, b.BuildingRect.Height - 1 - hallDepth);
                corridorDoor = new Point(corridorRect.Left + 1, corridorRect.Bottom - 1);
            }
            else if (doorSide == Direction.E)
            {
                corridorRect = new Rectangle(b.BuildingRect.Left, midY - 1, b.BuildingRect.Width - 1 - hallDepth, 3);
                corridorDoor = new Point(corridorRect.Right - 1, corridorRect.Top + 1);
            }
            else if (doorSide == Direction.W)
            {
                corridorRect = new Rectangle(b.InsideRect.Left + hallDepth, midY - 1, b.BuildingRect.Width - 1 - hallDepth, 3);
                corridorDoor = new Point(corridorRect.Left, corridorRect.Top + 1);
            }
            else
                throw new InvalidOperationException("unhandled door side");

            base.TileRectangle(map, m_Game.GameTiles.WALL_CHAR_OFFICE, corridorRect);
            PlaceDoor(map, corridorDoor.X, corridorDoor.Y, m_Game.GameTiles.FLOOR_OFFICE, base.MakeObjCharDoor());
            #endregion

            /////////////////////////
            // 6. Make office rooms.
            /////////////////////////
            #region
            // make wings.
            Rectangle wingOne;
            Rectangle wingTwo;
            if (horizontalCorridor)
            {
                // top side.
                wingOne = new Rectangle(corridorRect.Left, b.BuildingRect.Top, corridorRect.Width, 1 + corridorRect.Top - b.BuildingRect.Top);
                // bottom side.
                wingTwo = new Rectangle(corridorRect.Left, corridorRect.Bottom - 1, corridorRect.Width, 1 + b.BuildingRect.Bottom - corridorRect.Bottom);
            }
            else
            {
                // left side
                wingOne = new Rectangle(b.BuildingRect.Left, corridorRect.Top, 1 + corridorRect.Left - b.BuildingRect.Left, corridorRect.Height);
                // right side
                wingTwo = new Rectangle(corridorRect.Right - 1, corridorRect.Top, 1 + b.BuildingRect.Right - corridorRect.Right, corridorRect.Height);
            }

            // make rooms in each wing with doors leaving toward corridor.
            const int officeRoomsSize = 4;

            List<Rectangle> officesOne = new List<Rectangle>();
            MakeRoomsPlan(map, ref officesOne, wingOne, officeRoomsSize);

            List<Rectangle> officesTwo = new List<Rectangle>();
            MakeRoomsPlan(map, ref officesTwo, wingTwo, officeRoomsSize);

            List<Rectangle> allOffices = new List<Rectangle>(officesOne.Count + officesTwo.Count);
            allOffices.AddRange(officesOne);
            allOffices.AddRange(officesTwo);

            foreach (Rectangle roomRect in officesOne)
            {
                base.TileRectangle(map, m_Game.GameTiles.WALL_CHAR_OFFICE, roomRect);
                map.AddZone(MakeUniqueZone("Office room", roomRect));
            }
            foreach (Rectangle roomRect in officesTwo)
            {
                base.TileRectangle(map, m_Game.GameTiles.WALL_CHAR_OFFICE, roomRect);
                map.AddZone(MakeUniqueZone("Office room", roomRect));
            }

            foreach (Rectangle roomRect in officesOne)
            {
                if (horizontalCorridor)
                {
                    PlaceDoor(map, roomRect.Left + roomRect.Width / 2, roomRect.Bottom - 1, m_Game.GameTiles.FLOOR_OFFICE, base.MakeObjCharDoor());
                }
                else
                {
                    PlaceDoor(map, roomRect.Right - 1, roomRect.Top + roomRect.Height / 2, m_Game.GameTiles.FLOOR_OFFICE, base.MakeObjCharDoor());
                }
            }
            foreach (Rectangle roomRect in officesTwo)
            {
                if (horizontalCorridor)
                {
                    PlaceDoor(map, roomRect.Left + roomRect.Width / 2, roomRect.Top, m_Game.GameTiles.FLOOR_OFFICE, base.MakeObjCharDoor());
                }
                else
                {
                    PlaceDoor(map, roomRect.Left, roomRect.Top + roomRect.Height / 2, m_Game.GameTiles.FLOOR_OFFICE, base.MakeObjCharDoor());
                }
            }

            // tables with chairs.
            foreach (Rectangle roomRect in allOffices)
            {
                // table.
                Point tablePos = new Point(roomRect.Left + roomRect.Width / 2, roomRect.Top + roomRect.Height / 2);
                map.PlaceMapObjectAt(base.MakeObjTable(GameImages.OBJ_CHAR_TABLE), tablePos);

                // try to put chairs around.
                int nbChairs = 2;
                Rectangle insideRoom = new Rectangle(roomRect.Left + 1, roomRect.Top + 1, roomRect.Width - 2, roomRect.Height - 2);
                if (!insideRoom.IsEmpty)
                {
                    for (int i = 0; i < nbChairs; i++)
                    {
                        Rectangle adjTableRect = new Rectangle(tablePos.X - 1, tablePos.Y - 1, 3, 3);
                        adjTableRect.Intersect(insideRoom);
                        MapObjectPlaceInGoodPosition(map, adjTableRect,
                            (pt) => pt != tablePos,
                            m_DiceRoller,
                            (pt) => MakeObjChair(GameImages.OBJ_CHAR_CHAIR));
                    }
                }
            }
            #endregion

            ////////////////
            // 7. Add items.
            ////////////////
            #region
            // drop goodies in rooms.
            foreach (Rectangle roomRect in allOffices)
            {
                base.ItemsDrop(map, roomRect,
                    (pt) =>
                    {
                        Tile tile = map.GetTileAt(pt.X, pt.Y);
                        if (tile.Model != m_Game.GameTiles.FLOOR_OFFICE)
                            return false;
                        MapObject mapObj = map.GetMapObjectAt(pt);
                        if (mapObj != null)
                            return false;
                        return true;
                    },
                    (pt) => MakeRandomCHAROfficeItem());
            }
            #endregion

            ///////////
            // 8. Zone
            ///////////
            Zone zone = base.MakeUniqueZone("CHAR Office", b.BuildingRect);
            zone.SetGameAttribute<bool>(ZoneAttributes.IS_CHAR_OFFICE, true);
            map.AddZone(zone);
            MakeWalkwayZones(map, b);

            // Done
            return true;
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
        protected Item MakeRandomShopItem(ShopType shop)
        {
            switch (shop)
            {
                case ShopType.CONSTRUCTION:
                    return MakeShopConstructionItem();
                case ShopType.GENERAL_STORE:
                    return MakeShopGeneralItem();
                case ShopType.GROCERY:
                    return MakeShopGroceryItem();
                case ShopType.GUNSHOP:
                    return MakeShopGunshopItem();
                case ShopType.PHARMACY:
                    return MakeShopPharmacyItem();
                case ShopType.SPORTSWEAR:
                    return MakeShopSportsWearItem();
                case ShopType.HUNTING:
                    return MakeHuntingShopItem();
                default:
                    throw new ArgumentOutOfRangeException("unhandled shoptype");

            }
        }

        public Item MakeShopGroceryItem()
        {
            if (m_DiceRoller.RollChance(50))
                return MakeItemCannedFood();
            else
                return MakeItemGroceries();
        }

        public Item MakeShopPharmacyItem()
        {
            int randomItem = m_DiceRoller.Roll(0, 6);
            switch (randomItem)
            {
                case 0: return MakeItemBandages();
                case 1: return MakeItemMedikit();
                case 2: return MakeItemPillsSLP();
                case 3: return MakeItemPillsSTA();
                case 4: return MakeItemPillsSAN();
                case 5: return MakeItemStenchKiller();

                default:
                    throw new ArgumentOutOfRangeException("unhandled roll");
            }
        }

        public Item MakeShopSportsWearItem()
        {
            int roll = m_DiceRoller.Roll(0, 10);

            switch (roll)
            {
                case 0:
                    if (m_DiceRoller.RollChance(30))
                        return MakeItemHuntingRifle();
                    else
                        return MakeItemLightRifleAmmo();
                case 1:
                    if (m_DiceRoller.RollChance(30))
                        return MakeItemHuntingCrossbow();
                    else
                        return MakeItemBoltsAmmo();
                case 2:
                case 3:
                case 4:
                case 5: return MakeItemBaseballBat();       // 40%

                case 6:
                case 7: return MakeItemIronGolfClub();      // 20%

                case 8:
                case 9: return MakeItemGolfClub();          // 20%
                default:
                    throw new ArgumentOutOfRangeException("unhandled roll");
            }
        }

        public Item MakeShopConstructionItem()
        {
            int roll = m_DiceRoller.Roll(0, 24);
            switch (roll)
            {
                case 0:
                case 1:
                case 2: return m_DiceRoller.RollChance(50) ? MakeItemShovel() : MakeItemShortShovel();

                case 3:
                case 4:
                case 5: return MakeItemCrowbar();

                case 6: 
                case 7:
                case 8: return m_DiceRoller.RollChance(50) ? MakeItemHugeHammer() : MakeItemSmallHammer();

                case 9:
                case 10:
                case 11: return MakeItemWoodenPlank();
                
                case 12:
                case 13:
                case 14: return MakeItemFlashlight();

                case 15:
                case 16:
                case 17: return MakeItemBigFlashlight();

                case 18:
                case 19: 
                case 20: return MakeItemSpikes();

                case 21:
                case 22:
                case 23: return MakeItemBarbedWire();

                default:
                    throw new ArgumentOutOfRangeException("unhandled roll");
            }
        }

        public Item MakeShopGunshopItem()
        {
            // Weapons (40%) vs Ammo (60%)
            if (m_DiceRoller.RollChance(40))
            {
                int roll = m_DiceRoller.Roll(0, 4);

                switch (roll)
                {
                    case 0: return MakeItemRandomPistol();
                    case 1: return MakeItemShotgun();
                    case 2: return MakeItemHuntingRifle();
                    case 3: return MakeItemHuntingCrossbow();

                    default:
                        return null;
                }
            }
            else
            {
                int roll = m_DiceRoller.Roll(0, 4);

                switch (roll)
                {
                    case 0: return MakeItemLightPistolAmmo();
                    case 1: return MakeItemShotgunAmmo();
                    case 2: return MakeItemLightRifleAmmo();
                    case 3: return MakeItemBoltsAmmo();

                    default:
                        return null;
                }
            }
        }

        public Item MakeHuntingShopItem()
        {
            // Weapons/Ammo (50%) Outfits&Traps (50%)
            if (m_DiceRoller.RollChance(50))
            {
                // Weapons(40) Ammo(60)
                if (m_DiceRoller.RollChance(40))
                {
                    int roll = m_DiceRoller.Roll(0, 2);

                    switch (roll)
                    {
                        case 0: return MakeItemHuntingRifle();
                        case 1: return MakeItemHuntingCrossbow();
                        default:
                            return null;
                    }
                }
                else
                {
                    int roll = m_DiceRoller.Roll(0, 2);

                    switch (roll)
                    {
                        case 0: return MakeItemLightRifleAmmo();
                        case 1: return MakeItemBoltsAmmo();
                        default:
                            return null;
                    }
                }
            }
            else
            {
                // Outfits&Traps
                int roll = m_DiceRoller.Roll(0, 2);
                switch (roll)
                {
                    case 0: return MakeItemHunterVest();
                    case 1: return MakeItemBearTrap();
                    default: 
                        return null;
                }
            }
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

        public Item MakeRandomCHAROfficeItem()
        {
            int randomItem = m_DiceRoller.Roll(0, 10);
            switch (randomItem)
            {
                case 0:
                    // weapons:
                    // - grenade (rare).
                    // - shotgun/ammo
                    if (m_DiceRoller.RollChance(10))
                    {
                        // grenade!
                        return MakeItemGrenade();
                    }
                    else
                    {
                        // shotgun/ammo
                        if (m_DiceRoller.RollChance(30))
                            return MakeItemShotgun();
                        else
                            return MakeItemShotgunAmmo();
                    }

                case 1: 
                case 2:
                    if (m_DiceRoller.RollChance(50))
                        return MakeItemBandages();
                    else
                        return MakeItemMedikit();

                case 3:
                    return MakeItemCannedFood();

                case 4: // rare tracker items
                    if (m_DiceRoller.RollChance(50))
                    {
                        if (m_DiceRoller.RollChance(50))
                            return MakeItemZTracker();
                        else
                            return MakeItemBlackOpsGPS();
                    }
                    else
                        return null;

                default: return null; // 50% chance to find nothing.
            }
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

        #region Populating buildings
        protected virtual void PopulateCHAROfficeBuilding(Map map, Block b)
        {
            //////////
            // Guards
            //////////
            for (int i = 0; i < MAX_CHAR_GUARDS_PER_OFFICE; i++)
            {
                Actor newGuard = CreateNewCHARGuard(0);
                ActorPlace(m_DiceRoller, 100, map, newGuard, b.InsideRect.Left, b.InsideRect.Top, b.InsideRect.Width, b.InsideRect.Height);
            }

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

        #region CHAR Underground Facility
        public Map GenerateUniqueMap_CHARUnderground(Map surfaceMap, Zone officeZone)
        {
            /////////////////////////
            // 1. Create basic secret map.
            // 2. Link to office.
            // 3. Create rooms.
            // 4. Furniture & Items.
            // 5. Posters & Blood.
            // 6. Populate.
            // 7. Add uniques.
            /////////////////////////

            // 1. Create basic secret map.
            #region
            // huge map.
            Map underground = new Map((surfaceMap.Seed << 3) ^ surfaceMap.Seed, "CHAR Underground Facility", RogueGame.MAP_MAX_WIDTH, RogueGame.MAP_MAX_HEIGHT)
            {
                Lighting = Lighting.DARKNESS,
                IsSecret = true
            };
            // fill & enclose.
            TileFill(underground, m_Game.GameTiles.FLOOR_OFFICE, (tile, model, x, y) => tile.IsInside = true);
            TileRectangle(underground, m_Game.GameTiles.WALL_CHAR_OFFICE, new Rectangle(0, 0, underground.Width, underground.Height));
            #endregion

            // 2. Link to office.
            #region
            // find surface point in office:
            // - in a random office room.
            // - set exit somewhere walkable inside.
            // - iron door, barricade the door.
            Zone roomZone = null;
            Point surfaceExit = new Point();
            while (true)    // loop until found.
            {
                // find a random room.
                do
                {
                    int x = m_DiceRoller.Roll(officeZone.Bounds.Left, officeZone.Bounds.Right);
                    int y = m_DiceRoller.Roll(officeZone.Bounds.Top, officeZone.Bounds.Bottom);
                    List<Zone> zonesHere = surfaceMap.GetZonesAt(x, y);
                    if (zonesHere == null || zonesHere.Count == 0)
                        continue;
                    foreach (Zone z in zonesHere)
                        if (z.Name.Contains("room"))
                        {
                            roomZone = z;
                            break;
                        }
                }
                while (roomZone == null);

                // find somewhere walkable inside.
                bool foundSurfaceExit = false;
                int attempts = 0;
                do
                {
                    surfaceExit.X = m_DiceRoller.Roll(roomZone.Bounds.Left, roomZone.Bounds.Right);
                    surfaceExit.Y = m_DiceRoller.Roll(roomZone.Bounds.Top, roomZone.Bounds.Bottom);
                    foundSurfaceExit = surfaceMap.IsWalkable(surfaceExit.X, surfaceExit.Y);
                    ++attempts;
                }
                while (attempts < 100 && !foundSurfaceExit);

                // failed?
                if (foundSurfaceExit == false)
                    continue;

                // found everything, good!
                break;
            }

            // barricade the rooms door.
            DoForEachTile(surfaceMap, roomZone.Bounds,
                (pt) =>
                {
                    DoorWindow door = surfaceMap.GetMapObjectAt(pt) as DoorWindow;
                    if (door == null)
                        return;
                    surfaceMap.RemoveMapObjectAt(pt.X, pt.Y);
                    door = MakeObjIronDoor();
                    door.BarricadePoints = Rules.BARRICADING_MAX;
                    surfaceMap.PlaceMapObjectAt(door, pt);
                });

            // stairs.
            // underground : in the middle of the map.
            Point undergroundStairs = new Point(underground.Width / 2, underground.Height / 2);
            underground.SetExitAt(undergroundStairs, new Exit(surfaceMap, surfaceExit));
            underground.GetTileAt(undergroundStairs.X, undergroundStairs.Y).AddDecoration(GameImages.DECO_STAIRS_UP);
            surfaceMap.SetExitAt(surfaceExit, new Exit(underground, undergroundStairs));
            surfaceMap.GetTileAt(surfaceExit.X, surfaceExit.Y).AddDecoration(GameImages.DECO_STAIRS_DOWN);
            // floor logo.
            ForEachAdjacent(underground, undergroundStairs.X, undergroundStairs.Y, (pt) => underground.GetTileAt(pt).AddDecoration(GameImages.DECO_CHAR_FLOOR_LOGO));
            #endregion

            // 3. Create floorplan & rooms.
            #region
            // make 4 quarters, splitted by a crossed corridor.
            const int corridorHalfWidth = 1;
            Rectangle qTopLeft = Rectangle.FromLTRB(0, 0, underground.Width / 2 - corridorHalfWidth, underground.Height / 2 - corridorHalfWidth);
            Rectangle qTopRight = Rectangle.FromLTRB(underground.Width / 2 + 1 + corridorHalfWidth, 0, underground.Width, qTopLeft.Bottom);
            Rectangle qBotLeft = Rectangle.FromLTRB(0, underground.Height/2 + 1 + corridorHalfWidth, qTopLeft.Right, underground.Height);
            Rectangle qBotRight = Rectangle.FromLTRB(qTopRight.Left, qBotLeft.Top, underground.Width, underground.Height);

            // split all the map in rooms.
            const int minRoomSize = 6;
            List<Rectangle> roomsList = new List<Rectangle>();
            MakeRoomsPlan(underground, ref roomsList, qBotLeft, minRoomSize);
            MakeRoomsPlan(underground, ref roomsList, qBotRight, minRoomSize);
            MakeRoomsPlan(underground, ref roomsList, qTopLeft, minRoomSize);
            MakeRoomsPlan(underground, ref roomsList, qTopRight, minRoomSize);

            // make the rooms walls.
            foreach (Rectangle roomRect in roomsList)
            {
                TileRectangle(underground, m_Game.GameTiles.WALL_CHAR_OFFICE, roomRect);
            }

            // add room doors.
            // quarters have door side preferences to lead toward the central corridors.
            foreach (Rectangle roomRect in roomsList)
            {
                Point westEastDoorPos = roomRect.Left < underground.Width / 2 ? 
                    new Point(roomRect.Right - 1, roomRect.Top + roomRect.Height / 2) : 
                    new Point(roomRect.Left, roomRect.Top + roomRect.Height / 2);
                if (underground.GetMapObjectAt(westEastDoorPos) == null)
                {
                    DoorWindow door = MakeObjCharDoor();
                    PlaceDoorIfAccessibleAndNotAdjacent(underground, westEastDoorPos.X, westEastDoorPos.Y, m_Game.GameTiles.FLOOR_OFFICE, 6, door);
                }

                Point northSouthDoorPos = roomRect.Top < underground.Height / 2 ? 
                    new Point(roomRect.Left + roomRect.Width / 2, roomRect.Bottom - 1) : 
                    new Point(roomRect.Left + roomRect.Width / 2, roomRect.Top);
                if (underground.GetMapObjectAt(northSouthDoorPos) == null)
                {
                    DoorWindow door = MakeObjCharDoor();
                    PlaceDoorIfAccessibleAndNotAdjacent(underground, northSouthDoorPos.X, northSouthDoorPos.Y, m_Game.GameTiles.FLOOR_OFFICE, 6, door);
                }
            }

            // add iron doors closing each corridor.
            for (int x = qTopLeft.Right; x < qBotRight.Left; x++)
            {
                PlaceDoor(underground, x, qTopLeft.Bottom - 1, m_Game.GameTiles.FLOOR_OFFICE, MakeObjIronDoor());
                PlaceDoor(underground, x, qBotLeft.Top, m_Game.GameTiles.FLOOR_OFFICE, MakeObjIronDoor());
            }
            for (int y = qTopLeft.Bottom; y < qBotLeft.Top; y++)
            {
                PlaceDoor(underground, qTopLeft.Right - 1, y, m_Game.GameTiles.FLOOR_OFFICE, MakeObjIronDoor());
                PlaceDoor(underground, qTopRight.Left, y, m_Game.GameTiles.FLOOR_OFFICE, MakeObjIronDoor());
            }
            #endregion

            // 4. Rooms, furniture & items.
            #region
            // furniture + items in rooms.
            // room roles with zones:
            // - corners room : Power Room.
            // - top left quarter : armory.
            // - top right quarter : storage.
            // - bottom left quarter : living.
            // - bottom right quarter : pharmacy.
            foreach (Rectangle roomRect in roomsList)
            {
                Rectangle insideRoomRect = new Rectangle(roomRect.Left + 1, roomRect.Top + 1, roomRect.Width - 2, roomRect.Height - 2);
                string roomName = "<noname>";

                // special room?
                // one power room in each corner.
                bool isPowerRoom = (roomRect.Left == 0 && roomRect.Top == 0) ||
                    (roomRect.Left == 0 && roomRect.Bottom == underground.Height) ||
                    (roomRect.Right == underground.Width && roomRect.Top == 0) ||
                    (roomRect.Right == underground.Width && roomRect.Bottom == underground.Height);
                if (isPowerRoom)
                {
                    roomName = "Power Room";
                    MakeCHARPowerRoom(underground, roomRect, insideRoomRect);
                }
                else
                {
                    // common room.
                    int roomRole = (roomRect.Left < underground.Width / 2 && roomRect.Top < underground.Height / 2) ? 0 :
                        (roomRect.Left >= underground.Width / 2 && roomRect.Top < underground.Height / 2) ? 1 :
                        (roomRect.Left < underground.Width / 2 && roomRect.Top >= underground.Height / 2) ? 2 :
                        3;
                    switch (roomRole)
                    {
                        case 0: // armory room.
                            {
                                roomName = "Armory";
                                MakeCHARArmoryRoom(underground, insideRoomRect);
                                break;
                            }
                        case 1: // storage room.
                            {
                                roomName = "Storage";
                                MakeCHARStorageRoom(underground, insideRoomRect);
                                break;
                            }
                        case 2: // living room.
                            {
                                roomName = "Living";
                                MakeCHARLivingRoom(underground, insideRoomRect);
                                break;
                            }
                        case 3: // pharmacy.
                            {
                                roomName = "Pharmacy";
                                MakeCHARPharmacyRoom(underground, insideRoomRect);
                                break;
                            }
                        default:
                            throw new ArgumentOutOfRangeException("unhandled role");
                    }
                }

                underground.AddZone(MakeUniqueZone(roomName, insideRoomRect));
            }
            #endregion

            // 5. Posters & Blood.
            #region
            // char propaganda posters & blood almost everywhere.
            for(int x = 0; x < underground.Width;x++)
                for (int y = 0; y < underground.Height; y++)
                {
                    // poster on wall?
                    if (m_DiceRoller.RollChance(25))
                    {
                        Tile tile = underground.GetTileAt(x,y);
                        if (tile.Model.IsWalkable)
                            continue;
                        tile.AddDecoration(CHAR_POSTERS[m_DiceRoller.Roll(0, CHAR_POSTERS.Length)]);
                    }

                    // blood?
                    if (m_DiceRoller.RollChance(20))
                    {
                        Tile tile = underground.GetTileAt(x, y);
                        if (tile.Model.IsWalkable)
                            tile.AddDecoration(GameImages.DECO_BLOODIED_FLOOR);
                        else
                            tile.AddDecoration(GameImages.DECO_BLOODIED_WALL);
                    }
                }
            #endregion

            // 6. Populate.
            // don't block exits!
            #region
            // leveled up undeads!
            int nbZombies = underground.Width;  // 100 for 100.
            for (int i = 0; i < nbZombies; i++)
            {
                Actor undead = CreateNewUndead(0);
                for (; ; )
                {
                    GameActors.IDs upID = m_Game.NextUndeadEvolution((GameActors.IDs)undead.Model.ID);
                    if (upID == (GameActors.IDs) undead.Model.ID)
                        break;
                    undead.Model = m_Game.GameActors[upID];
                }
                ActorPlace(m_DiceRoller, underground.Width * underground.Height, underground, undead, (pt) => underground.GetExitAt(pt) == null);
            }         
   
            // CHAR Guards.
            int nbGuards = underground.Width / 10; // 10 for 100.
            for (int i = 0; i < nbGuards; i++)
            {
                Actor guard = CreateNewCHARGuard(0);
                ActorPlace(m_DiceRoller, underground.Width * underground.Height, underground, guard, (pt) => underground.GetExitAt(pt) == null);
            }
            #endregion

            // 7. Add uniques.
            // TODO...
            #region
            #endregion

             // done.
            return underground;
        }

        void MakeCHARArmoryRoom(Map map, Rectangle roomRect)
        {
            // Shelves with weapons/ammo along walls.
            MapObjectFill(map, roomRect,
                (pt) =>
                {
                    if (CountAdjWalls(map, pt.X, pt.Y) < 3)
                        return null;
                    // dont block exits!
                    if (map.GetExitAt(pt) != null)
                        return null;

                    // table + tracker/armor/weapon.
                    if (m_DiceRoller.RollChance(20))
                    {                        
                        Item it;
                        if (m_DiceRoller.RollChance(20))
                            it = MakeItemCHARLightBodyArmor();
                        else if (m_DiceRoller.RollChance(20))
                        {
                            it = m_DiceRoller.RollChance(50) ? MakeItemZTracker() : MakeItemBlackOpsGPS();
                        }
                        else
                        {
                            // rare grenades.
                            if (m_DiceRoller.RollChance(20))
                            {
                                it = MakeItemGrenade();
                            }
                            else
                            {
                                // weapon vs ammo.
                                if (m_DiceRoller.RollChance(30))
                                {
                                    it = m_DiceRoller.RollChance(50) ? MakeItemShotgun() : MakeItemHuntingRifle();
                                }
                                else
                                {
                                    it = m_DiceRoller.RollChance(50) ? MakeItemShotgunAmmo() : MakeItemLightRifleAmmo();
                                }
                            }
                        }
                        map.DropItemAt(it, pt);

                        MapObject shelf = MakeObjShelf(GameImages.OBJ_SHOP_SHELF);
                        return shelf;
                    }
                    else
                        return null;
                });
        }

        void MakeCHARStorageRoom(Map map, Rectangle roomRect)
        {
            // Replace floor with concrete.
            TileFill(map, m_Game.GameTiles.FLOOR_CONCRETE, roomRect);

            // Objects.
            // Barrels & Junk in the middle of the room.
            MapObjectFill(map, roomRect,
                (pt) =>
                {
                    if (CountAdjWalls(map, pt.X, pt.Y) > 0)
                        return null;
                    // dont block exits!
                    if (map.GetExitAt(pt) != null)
                        return null;

                    // barrels/junk?
                    if (m_DiceRoller.RollChance(50))
                        return m_DiceRoller.RollChance(50) ? MakeObjJunk(GameImages.OBJ_JUNK) : MakeObjBarrels(GameImages.OBJ_BARRELS);
                    else
                        return null;
                });

            // Items.
            // Construction items in this mess.
            for(int x = roomRect.Left; x < roomRect.Right;x++)
                for (int y = roomRect.Top; y < roomRect.Bottom; y++)
                {
                    if (CountAdjWalls(map, x, y) > 0)
                        continue;
                    if(map.GetMapObjectAt(x,y) != null)
                        continue;

                    map.DropItemAt(MakeShopConstructionItem(), x, y);
                }
        }

        void MakeCHARLivingRoom(Map map, Rectangle roomRect)
        {
            // Replace floor with wood with painted logo.
            TileFill(map, m_Game.GameTiles.FLOOR_PLANKS, roomRect, (tile, model, x, y) => tile.AddDecoration(GameImages.DECO_CHAR_FLOOR_LOGO));
            
            // Objects.
            // Beds/Fridges along walls.
            MapObjectFill(map, roomRect,
                (pt) =>
                {
                    if (CountAdjWalls(map, pt.X, pt.Y) < 3)
                        return null;
                    // dont block exits!
                    if (map.GetExitAt(pt) != null)
                        return null;

                    // bed/fridge?
                    if (m_DiceRoller.RollChance(30))
                    {
                        if (m_DiceRoller.RollChance(50))
                            return MakeObjBed(GameImages.OBJ_BED);
                        else
                            return MakeObjFridge(GameImages.OBJ_FRIDGE);
                    }
                    else
                        return null;
                });
            // Tables(with canned food) & Chairs in the middle.
            MapObjectFill(map, roomRect,
                (pt) =>
                {
                    if (CountAdjWalls(map, pt.X, pt.Y) > 0)
                        return null;
                    // dont block exits!
                    if (map.GetExitAt(pt) != null)
                        return null;

                    // tables/chairs.
                    if (m_DiceRoller.RollChance(30))
                    {
                        if (m_DiceRoller.RollChance(30))
                        {
                            MapObject table = MakeObjTable(GameImages.OBJ_CHAR_TABLE);
                            map.DropItemAt(MakeItemCannedFood(), pt);
                            return table;
                        }
                        else
                            return MakeObjChair(GameImages.OBJ_CHAR_CHAIR);
                    }
                    else
                        return null;
                });
        }

        void MakeCHARPharmacyRoom(Map map, Rectangle roomRect)
        {
            // Shelves with medicine along walls.
            MapObjectFill(map, roomRect,
                (pt) =>
                {
                    if (CountAdjWalls(map, pt.X, pt.Y) < 3)
                        return null;
                    // dont block exits!
                    if (map.GetExitAt(pt) != null)
                        return null;

                    // table + meds.
                    if (m_DiceRoller.RollChance(20))
                    {
                        Item it = MakeHospitalItem();                        
                        map.DropItemAt(it, pt);

                        MapObject shelf = MakeObjShelf(GameImages.OBJ_SHOP_SHELF);
                        return shelf;
                    }
                    else
                        return null;
                });
        }

        void MakeCHARPowerRoom(Map map, Rectangle wallsRect, Rectangle roomRect)
        {
            // Replace floor with concrete.
            TileFill(map, m_Game.GameTiles.FLOOR_CONCRETE, roomRect);

            // add deco power sign next to doors.
            DoForEachTile(map, wallsRect,
                (pt) =>
                {
                    if (!(map.GetMapObjectAt(pt) is DoorWindow))
                        return;
                    DoForEachAdjacentInMap(map, pt, (
                        ptAdj) =>
                        {
                            Tile tile = map.GetTileAt(ptAdj);
                            if (tile.Model.IsWalkable)
                                return;
                            tile.RemoveAllDecorations();
                            tile.AddDecoration(GameImages.DECO_POWER_SIGN_BIG);
                        });
                });

            // add power generators along walls.
            DoForEachTile(map, roomRect,
                (pt) =>
                {
                    if (!map.GetTileAt(pt).Model.IsWalkable)
                        return;
                    if (map.GetExitAt(pt) != null)
                        return;
                    if (CountAdjWalls(map, pt.X, pt.Y) < 3)
                        return;

                    PowerGenerator powGen = MakeObjPowerGenerator(GameImages.OBJ_POWERGEN_OFF, GameImages.OBJ_POWERGEN_ON);
                    map.PlaceMapObjectAt(powGen, pt);
                });
        }
        #endregion

        #region Police Station
        void MakePoliceStation(Map map, Block policeBlock)
        {
            ////////////////////////////////
            // 2. Generate surface station.
            // 3. Generate level -1.
            // 4. Generate level -2.
            // 5. Link maps.
            // 6. Add maps to district.
            // 7. Set unique maps.
            ////////////////////////////////

            // 2. Generate surface station.
            Point surfaceStairsPos;
            GeneratePoliceStation(map, policeBlock, out surfaceStairsPos);

            // 3. Generate Offices level (-1).
            Map officesLevel = GeneratePoliceStation_OfficesLevel(map, policeBlock, surfaceStairsPos);

            // 4. Generate Jails level (-2).
            Map jailsLevel = GeneratePoliceStation_JailsLevel(officesLevel);

            // 5. Link maps.
            // surface <-> offices level
            AddExit(map, surfaceStairsPos, officesLevel, new Point(1,1), GameImages.DECO_STAIRS_DOWN, true);
            AddExit(officesLevel, new Point(1,1), map, surfaceStairsPos, GameImages.DECO_STAIRS_UP, true);

            // offices <-> jails
            AddExit(officesLevel, new Point(1, officesLevel.Height - 2), jailsLevel, new Point(1, 1), GameImages.DECO_STAIRS_DOWN, true);
            AddExit(jailsLevel, new Point(1, 1), officesLevel, new Point(1, officesLevel.Height - 2), GameImages.DECO_STAIRS_UP, true);

            // 6. Add maps to district.
            m_Params.District.AddUniqueMap(officesLevel);
            m_Params.District.AddUniqueMap(jailsLevel);

            // 7. Set unique maps.
            m_Game.Session.UniqueMaps.PoliceStation_OfficesLevel = new UniqueMap() { TheMap = officesLevel };
            m_Game.Session.UniqueMaps.PoliceStation_JailsLevel = new UniqueMap() { TheMap = jailsLevel };

            // done!
        }

        void GeneratePoliceStation(Map surfaceMap, Block policeBlock, out Point stairsToLevel1)
        {
            // Fill & Enclose Building.
            TileFill(surfaceMap, m_Game.GameTiles.FLOOR_TILES, policeBlock.InsideRect);
            TileRectangle(surfaceMap, m_Game.GameTiles.WALL_POLICE_STATION, policeBlock.BuildingRect);
            TileRectangle(surfaceMap, m_Game.GameTiles.FLOOR_WALKWAY, policeBlock.Rectangle);
            DoForEachTile(surfaceMap, policeBlock.InsideRect, (pt) => surfaceMap.GetTileAt(pt).IsInside = true);

            // Entrance to the south with police signs.
            Point entryDoorPos = new Point(policeBlock.BuildingRect.Left + policeBlock.BuildingRect.Width / 2, policeBlock.BuildingRect.Bottom - 1);
            surfaceMap.GetTileAt(entryDoorPos.X - 1, entryDoorPos.Y).AddDecoration(GameImages.DECO_POLICE_STATION);
            surfaceMap.GetTileAt(entryDoorPos.X + 1, entryDoorPos.Y).AddDecoration(GameImages.DECO_POLICE_STATION);

            // Entry hall.
            Rectangle entryHall = Rectangle.FromLTRB(policeBlock.BuildingRect.Left, policeBlock.BuildingRect.Top + 2, policeBlock.BuildingRect.Right, policeBlock.BuildingRect.Bottom);
            TileRectangle(surfaceMap, m_Game.GameTiles.WALL_POLICE_STATION, entryHall);
            PlaceDoor(surfaceMap, entryHall.Left + entryHall.Width / 2, entryHall.Top, m_Game.GameTiles.FLOOR_TILES, MakeObjIronDoor());
            PlaceDoor(surfaceMap, entryDoorPos.X, entryDoorPos.Y, m_Game.GameTiles.FLOOR_TILES, MakeObjGlassDoor());
            DoForEachTile(surfaceMap, entryHall,
                (pt) =>
                {
                    if (!surfaceMap.IsWalkable(pt.X, pt.Y))
                        return;
                    if (CountAdjWalls(surfaceMap, pt.X, pt.Y) == 0 || CountAdjDoors(surfaceMap, pt.X, pt.Y) > 0)
                        return;
                    surfaceMap.PlaceMapObjectAt(MakeObjBench(GameImages.OBJ_BENCH), pt);
                });

            // Place stairs, north side.
            stairsToLevel1 = new Point(entryDoorPos.X, policeBlock.InsideRect.Top);

            // Zone.
            surfaceMap.AddZone(MakeUniqueZone("Police Station", policeBlock.BuildingRect));
            MakeWalkwayZones(surfaceMap, policeBlock);
        }

        Map GeneratePoliceStation_OfficesLevel(Map surfaceMap, Block policeBlock, Point exitPos)
        {
            //////////////////
            // 1. Create map.
            // 2. Floor plan.
            // 3. Populate.
            //////////////////

            // 1. Create map.
            int seed = (surfaceMap.Seed << 1) ^ surfaceMap.Seed;
            Map map = new Map(seed, "Police Station - Offices", 20, 20)
            {
                Lighting = Lighting.DARKNESS
            };
            DoForEachTile(map, map.Rect, (pt) => map.GetTileAt(pt).IsInside = true);

            // 2. Floor plan.
            TileFill(map, m_Game.GameTiles.FLOOR_TILES);
            TileRectangle(map, m_Game.GameTiles.WALL_POLICE_STATION, map.Rect);
            // - offices rooms on the east side, doors leading west.
            Rectangle officesRect = Rectangle.FromLTRB(3, 0, map.Width, map.Height);
            List<Rectangle> roomsList = new List<Rectangle>();
            MakeRoomsPlan(map, ref roomsList, officesRect, 5);
            foreach (Rectangle roomRect in roomsList)
            {
                Rectangle inRoomRect = Rectangle.FromLTRB(roomRect.Left + 1, roomRect.Top + 1, roomRect.Right - 1, roomRect.Bottom - 1);
                // 2 kind of rooms.
                // - farthest east from corridor : security.
                // - others : offices.
                if (roomRect.Right == map.Width)
                {
                    // Police Security Room.
                    #region
                    // make room with door.
                    TileRectangle(map, m_Game.GameTiles.WALL_POLICE_STATION, roomRect);
                    PlaceDoor(map, roomRect.Left, roomRect.Top + roomRect.Height / 2, m_Game.GameTiles.FLOOR_CONCRETE, MakeObjIronDoor());

                    // shelves with weaponry & armor next to the walls.
                    DoForEachTile(map, inRoomRect,
                        (pt) =>
                        {
                            if (!map.IsWalkable(pt.X, pt.Y) || CountAdjWalls(map, pt.X, pt.Y) == 0 || CountAdjDoors(map, pt.X, pt.Y) > 0)
                                return;

                            // shelf.
                            map.PlaceMapObjectAt(MakeObjShelf(GameImages.OBJ_SHOP_SHELF), pt);

                            // weaponry/armor/radios.
                            Item it = null;
                            int roll = m_DiceRoller.Roll(0, 10);
                            switch(roll)
                            {
                                    // 20% armors
                                case 0:
                                case 1:
                                    it = m_DiceRoller.RollChance(50) ? MakeItemPoliceJacket() : MakeItemPoliceRiotArmor();
                                    break;

                                    // 20% light/radio
                                case 2:
                                case 3:
                                    it = m_DiceRoller.RollChance(50) ? (m_DiceRoller.RollChance(50) ? MakeItemFlashlight() : MakeItemBigFlashlight()) : MakeItemPoliceRadio();
                                    break;

                                    // 20% truncheon
                                case 4:
                                case 5:
                                    it = MakeItemTruncheon();
                                    break;

                                    // 20% pistol/ammo - 30% pistol 70% amo
                                case 6:
                                case 7:
                                    it = m_DiceRoller.RollChance(30) ? MakeItemPistol() : MakeItemLightPistolAmmo();
                                    break;

                                    // 20% shotgun/ammo - 30% shotgun 70% amo
                                case 8:
                                case 9:
                                    it = m_DiceRoller.RollChance(30) ? MakeItemShotgun() : MakeItemShotgunAmmo();
                                    break;

                                default:
                                    throw new ArgumentOutOfRangeException("unhandled roll");

                            }

                            map.DropItemAt(it, pt);
                                
                        });

                    // zone.
                    map.AddZone(MakeUniqueZone("security", inRoomRect));
                    #endregion
                }
                else
                {
                    // Police Office Room.
                    #region
                    // make room with door.
                    TileFill(map, m_Game.GameTiles.FLOOR_PLANKS, roomRect);
                    TileRectangle(map, m_Game.GameTiles.WALL_POLICE_STATION, roomRect);
                    PlaceDoor(map, roomRect.Left, roomRect.Top + roomRect.Height / 2, m_Game.GameTiles.FLOOR_PLANKS, MakeObjWoodenDoor());

                    // add furniture : 1 table, 2 chairs.
                    MapObjectPlaceInGoodPosition(map, inRoomRect,
                        (pt) => map.IsWalkable(pt.X, pt.Y) && CountAdjDoors(map, pt.X, pt.Y) == 0,
                        m_DiceRoller,
                        (pt) => MakeObjTable(GameImages.OBJ_TABLE));
                    MapObjectPlaceInGoodPosition(map, inRoomRect,
                        (pt) => map.IsWalkable(pt.X, pt.Y) && CountAdjDoors(map, pt.X, pt.Y) == 0,
                        m_DiceRoller,
                        (pt) => MakeObjChair(GameImages.OBJ_CHAIR));
                    MapObjectPlaceInGoodPosition(map, inRoomRect,
                        (pt) => map.IsWalkable(pt.X, pt.Y) && CountAdjDoors(map, pt.X, pt.Y) == 0,
                        m_DiceRoller,
                        (pt) => MakeObjChair(GameImages.OBJ_CHAIR));

                    // zone.
                    map.AddZone(MakeUniqueZone("office", inRoomRect));
                    #endregion
                }
            }
            // - benches in corridor.
            DoForEachTile(map, new Rectangle(1, 1, 1, map.Height - 2),
                (pt) =>
                {
                    if (pt.Y % 2 == 1)
                        return;
                    if (!map.IsWalkable(pt))
                        return;
                    if (CountAdjWalls(map, pt) != 3)
                        return;

                    map.PlaceMapObjectAt(MakeObjIronBench(GameImages.OBJ_IRON_BENCH), pt);
                });

            // 3. Populate.
            // - cops.
            const int nbCops = 5;
            for (int i = 0; i < nbCops; i++)
            {
                Actor cop = CreateNewPoliceman(0);
                ActorPlace(m_DiceRoller, map.Width * map.Height, map, cop);
            }

            // done.
            return map;
        }

        Map GeneratePoliceStation_JailsLevel(Map surfaceMap)
        {
            //////////////////
            // 1. Create map.
            // 2. Floor plan.
            // 3. Populate.
            //////////////////

            // 1. Create map.
            int seed = (surfaceMap.Seed << 1) ^ surfaceMap.Seed;
            Map map = new Map(seed, "Police Station - Jails", 22, 6)
            {
                Lighting = Lighting.DARKNESS
            };
            DoForEachTile(map, map.Rect, (pt) => map.GetTileAt(pt).IsInside = true);

            // 2. Floor plan.
            TileFill(map, m_Game.GameTiles.FLOOR_TILES);
            TileRectangle(map, m_Game.GameTiles.WALL_POLICE_STATION, map.Rect);
            // - small cells.
            const int cellWidth = 3;
            const int cellHeight = 3;
            const int yCells = 3;
            List<Rectangle> cells = new List<Rectangle>();
            for (int x = 0; x + cellWidth <= map.Width; x += cellWidth - 1)
            {
                // room.
                Rectangle cellRoom = new Rectangle(x, yCells, cellWidth, cellHeight);
                cells.Add(cellRoom);
                TileFill(map, m_Game.GameTiles.FLOOR_CONCRETE, cellRoom);
                TileRectangle(map, m_Game.GameTiles.WALL_POLICE_STATION, cellRoom);

                // couch.
                Point couchPos = new Point(x + 1, yCells + 1);
                map.PlaceMapObjectAt(MakeObjIronBench(GameImages.OBJ_IRON_BENCH), couchPos);

                // gate.
                Point gatePos = new Point(x + 1, yCells);
                map.SetTileModelAt(gatePos.X, gatePos.Y, m_Game.GameTiles.FLOOR_CONCRETE);
                map.PlaceMapObjectAt(MakeObjIronGate(GameImages.OBJ_GATE_CLOSED), gatePos);

                // zone.
                map.AddZone(MakeUniqueZone(RogueGame.NAME_POLICE_STATION_JAILS_CELL, cellRoom));
            }
            // - corridor.
            Rectangle corridor = Rectangle.FromLTRB(1, 1, map.Width, yCells);
            map.AddZone(MakeUniqueZone("cells corridor", corridor));
            // - the switch to open/close the cells.
            map.PlaceMapObjectAt(MakeObjPowerGenerator(GameImages.OBJ_POWERGEN_OFF, GameImages.OBJ_POWERGEN_ON), new Point(map.Width - 2, 1));

            // 3. Populate.
            // - prisonners in each cell.
            //   keep the last cell for the special prisonner.
            for (int i = 0; i < cells.Count - 1; i++)
            {
                Rectangle cell = cells[i];

                // jailed. Civilian.
                Actor prisonner = CreateNewCivilian(0, 0, 1);

                // make sure he is stripped of all default items!
                while (!prisonner.Inventory.IsEmpty)
                    prisonner.Inventory.RemoveAllQuantity(prisonner.Inventory[0]);

                // give him some food.
                prisonner.Inventory.AddAll(MakeItemGroceries());

                // drop him.
                map.PlaceActorAt(prisonner, new Point(cell.Left + 1, cell.Top + 1));
            }
            // - Special prisonner in the last cell.
            Rectangle lastCell = cells[cells.Count - 1];
            Actor specialPrisonner = CreateNewCivilian(0, 0, 1);
            specialPrisonner.Name = "The Prisoner Who Should Not Be";
            for (int i = 0; i < specialPrisonner.Inventory.MaxCapacity; i++)
                specialPrisonner.Inventory.AddAll(MakeItemArmyRation());
            map.PlaceActorAt(specialPrisonner, new Point(lastCell.Left + 1, lastCell.Top + 1));
            m_Game.Session.UniqueActors.PoliceStationPrisonner = new UniqueActor()
            {
                TheActor = specialPrisonner,
                IsSpawned = true
            };

            // done.
            return map;
        }
        #endregion

        #region Hospital
        /// Layout :
        ///  0 floor: Entry Hall.
        /// -1 floor: Admissions (short term patients).
        /// -2 floor: Offices. (doctors)
        /// -3 floor: Patients. (nurses, injured patients)
        /// -4 floor: Storage. (bunch of meds & pills; blocked by closed gates, need power on)
        /// -5 floor: Power. (restore power to the whole building = lights, open storage gates)
        void MakeHospital(Map map, Block hospitalBlock)
        {
            ////////////////////////////////
            // 2. Generate surface building.
            // 3. Generate other levels maps.
            // 5. Link maps.
            // 6. Add maps to district.
            // 7. Set unique maps.
            ////////////////////////////////

            // 2. Generate surface.
            GenerateHospitalEntryHall(map, hospitalBlock);

            // 3. Generate other levels maps.
            Map admissions = GenerateHospital_Admissions((map.Seed << 1) ^ map.Seed);
            Map offices = GenerateHospital_Offices((map.Seed << 2) ^ map.Seed);
            Map patients = GenerateHospital_Patients((map.Seed << 3) ^ map.Seed);
            Map storage = GenerateHospital_Storage((map.Seed << 4) ^ map.Seed);
            Map power = GenerateHospital_Power((map.Seed << 5) ^ map.Seed);

            // 5. Link maps.
            // entry <-> admissions
            Point entryStairs = new Point(hospitalBlock.InsideRect.Left + hospitalBlock.InsideRect.Width / 2, hospitalBlock.InsideRect.Top);
            Point admissionsUpStairs = new Point(admissions.Width / 2, 1);
            AddExit(map, entryStairs, admissions, admissionsUpStairs, GameImages.DECO_STAIRS_DOWN, true);
            AddExit(admissions, admissionsUpStairs, map, entryStairs, GameImages.DECO_STAIRS_UP, true);

            // admissions <-> offices
            Point admissionsDownStairs = new Point(admissions.Width / 2, admissions.Height - 2);
            Point officesUpStairs = new Point(offices.Width / 2, 1);
            AddExit(admissions, admissionsDownStairs, offices, officesUpStairs, GameImages.DECO_STAIRS_DOWN, true);
            AddExit(offices, officesUpStairs, admissions, admissionsDownStairs, GameImages.DECO_STAIRS_UP, true);

            // offices <-> patients
            Point officesDownStairs = new Point(offices.Width / 2, offices.Height - 2);
            Point patientsUpStairs = new Point(patients.Width / 2, 1);
            AddExit(offices, officesDownStairs, patients, patientsUpStairs, GameImages.DECO_STAIRS_DOWN, true);
            AddExit(patients, patientsUpStairs, offices, officesDownStairs, GameImages.DECO_STAIRS_UP, true);

            // patients <-> storage
            Point patientsDownStairs = new Point(patients.Width / 2, patients.Height - 2);
            Point storageUpStairs = new Point(1, 1);
            AddExit(patients, patientsDownStairs, storage, storageUpStairs, GameImages.DECO_STAIRS_DOWN, true);
            AddExit(storage, storageUpStairs, patients, patientsDownStairs, GameImages.DECO_STAIRS_UP, true);

            // storage <-> power
            Point storageDownStairs = new Point(storage.Width - 2, 1);
            Point powerUpStairs = new Point(1, 1);
            AddExit(storage, storageDownStairs, power, powerUpStairs, GameImages.DECO_STAIRS_DOWN, true);
            AddExit(power, powerUpStairs, storage, storageDownStairs, GameImages.DECO_STAIRS_UP, true);

            // 6. Add maps to district.
            m_Params.District.AddUniqueMap(admissions);
            m_Params.District.AddUniqueMap(offices);
            m_Params.District.AddUniqueMap(patients);
            m_Params.District.AddUniqueMap(storage);
            m_Params.District.AddUniqueMap(power);

            // 7. Set unique maps.
            m_Game.Session.UniqueMaps.Hospital_Admissions = new UniqueMap() { TheMap = admissions };
            m_Game.Session.UniqueMaps.Hospital_Offices = new UniqueMap() { TheMap = offices };
            m_Game.Session.UniqueMaps.Hospital_Patients = new UniqueMap() { TheMap = patients };
            m_Game.Session.UniqueMaps.Hospital_Storage = new UniqueMap() { TheMap = storage };
            m_Game.Session.UniqueMaps.Hospital_Power = new UniqueMap() { TheMap = power };

            // done!
        }

        void GenerateHospitalEntryHall(Map surfaceMap, Block block)
        {
            // Fill & Enclose Building.
            TileFill(surfaceMap, m_Game.GameTiles.FLOOR_TILES, block.InsideRect);
            TileRectangle(surfaceMap, m_Game.GameTiles.WALL_HOSPITAL, block.BuildingRect);
            TileRectangle(surfaceMap, m_Game.GameTiles.FLOOR_WALKWAY, block.Rectangle);
            DoForEachTile(surfaceMap, block.InsideRect, (pt) => surfaceMap.GetTileAt(pt).IsInside = true);

            // 2 entrances to the south with signs.
            Point entryRightDoorPos = new Point(block.BuildingRect.Left + block.BuildingRect.Width / 2, block.BuildingRect.Bottom - 1);
            Point entryLeftDoorPos = new Point(entryRightDoorPos.X - 1, entryRightDoorPos.Y);
            surfaceMap.GetTileAt(entryLeftDoorPos.X - 1, entryLeftDoorPos.Y).AddDecoration(GameImages.DECO_HOSPITAL);
            surfaceMap.GetTileAt(entryRightDoorPos.X + 1, entryRightDoorPos.Y).AddDecoration(GameImages.DECO_HOSPITAL);

            // Entry hall = whole building.
            Rectangle entryHall = Rectangle.FromLTRB(block.BuildingRect.Left, block.BuildingRect.Top, block.BuildingRect.Right, block.BuildingRect.Bottom);
            PlaceDoor(surfaceMap, entryRightDoorPos.X, entryRightDoorPos.Y, m_Game.GameTiles.FLOOR_TILES, MakeObjGlassDoor());
            PlaceDoor(surfaceMap, entryLeftDoorPos.X, entryLeftDoorPos.Y, m_Game.GameTiles.FLOOR_TILES, MakeObjGlassDoor());
            DoForEachTile(surfaceMap, entryHall,
                (pt) =>
                {
                    // benches only on west & east sides.
                    if (pt.Y == block.InsideRect.Top || pt.Y == block.InsideRect.Bottom - 1)
                        return;
                    if (!surfaceMap.IsWalkable(pt.X, pt.Y))
                        return;
                    if (CountAdjWalls(surfaceMap, pt.X, pt.Y) == 0 || CountAdjDoors(surfaceMap, pt.X, pt.Y) > 0)
                        return;
                    surfaceMap.PlaceMapObjectAt(MakeObjIronBench(GameImages.OBJ_IRON_BENCH), pt);
                });

            // Zone.
            surfaceMap.AddZone(MakeUniqueZone("Hospital", block.BuildingRect));
            MakeWalkwayZones(surfaceMap, block);
        }

        Map GenerateHospital_Admissions(int seed)
        {
            //////////////////
            // 1. Create map.
            // 2. Floor plan.
            // 3. Populate.
            //////////////////

            // 1. Create map.
            Map map = new Map(seed, "Hospital - Admissions", 13, 33)
            {
                Lighting = Lighting.DARKNESS
            };
            DoForEachTile(map, map.Rect, (pt) => map.GetTileAt(pt).IsInside = true);
            TileFill(map, m_Game.GameTiles.FLOOR_TILES);
            TileRectangle(map, m_Game.GameTiles.WALL_HOSPITAL, map.Rect);

            // 2. Floor plan.
            // One central south->north corridor with admission rooms on each sides.
            const int roomSize = 5;

            // 1. Central corridor.
            Rectangle corridor = new Rectangle(roomSize - 1, 0, 5, map.Height);
            TileRectangle(map, m_Game.GameTiles.WALL_HOSPITAL, corridor);
            map.AddZone(MakeUniqueZone("corridor", corridor));

            // 2. Admission rooms, all similar 5x5 rooms (3x3 inside)            
            Rectangle leftWing = new Rectangle(0, 0, roomSize, map.Height);
            for (int roomY = 0; roomY <= map.Height - roomSize; roomY += roomSize - 1)
            {
                Rectangle room = new Rectangle(leftWing.Left, roomY, roomSize, roomSize);
                MakeHospitalPatientRoom(map, "patient room", room, true);
            }

            Rectangle rightWing = new Rectangle(map.Rect.Right - roomSize, 0, roomSize, map.Height);
            for (int roomY = 0; roomY <= map.Height - roomSize; roomY += roomSize - 1)
            {
                Rectangle room = new Rectangle(rightWing.Left, roomY, roomSize, roomSize);
                MakeHospitalPatientRoom(map, "patient room", room, false);
            }

            // 3. Populate.
            // patients in rooms.
            const int nbPatients = 10;
            for (int i = 0; i < nbPatients; i++)
            {
                // create.
                Actor patient = CreateNewHospitalPatient(0);
                // place.
                ActorPlace(m_DiceRoller, map.Width * map.Height, map, patient, (pt) => map.HasZonePartiallyNamedAt(pt, "patient room"));
            }

            // nurses & doctor in corridor.
            const int nbNurses = 4;
            for (int i = 0; i < nbNurses; i++)
            {
                // create.
                Actor nurse = CreateNewHospitalNurse(0);
                // place.
                ActorPlace(m_DiceRoller, map.Width * map.Height, map, nurse, (pt) => map.HasZonePartiallyNamedAt(pt, "corridor"));
            }
            const int nbDoctor = 1;
            for (int i = 0; i < nbDoctor; i++)
            {
                // create.
                Actor nurse = CreateNewHospitalDoctor(0);
                // place.
                ActorPlace(m_DiceRoller, map.Width * map.Height, map, nurse, (pt) => map.HasZonePartiallyNamedAt(pt, "corridor"));
            }

            // done.
            return map;
        }

        Map GenerateHospital_Offices(int seed)
        {
            //////////////////
            // 1. Create map.
            // 2. Floor plan.
            // 3. Populate.
            //////////////////

            // 1. Create map.
            Map map = new Map(seed, "Hospital - Offices", 13, 33)
            {
                Lighting = Lighting.DARKNESS
            };
            DoForEachTile(map, map.Rect, (pt) => map.GetTileAt(pt).IsInside = true);
            TileFill(map, m_Game.GameTiles.FLOOR_TILES);
            TileRectangle(map, m_Game.GameTiles.WALL_HOSPITAL, map.Rect);

            // 2. Floor plan.
            // One central south->north corridor with offices rooms on each sides.
            const int roomSize = 5;

            // 1. Central corridor.
            Rectangle corridor = new Rectangle(roomSize - 1, 0, 5, map.Height);
            TileRectangle(map, m_Game.GameTiles.WALL_HOSPITAL, corridor);
            map.AddZone(MakeUniqueZone("corridor", corridor));

            // 2. Offices rooms, all similar 5x5 rooms (3x3 inside)
            Rectangle leftWing = new Rectangle(0, 0, roomSize, map.Height);
            for (int roomY = 0; roomY <= map.Height - roomSize; roomY += roomSize - 1)
            {
                Rectangle room = new Rectangle(leftWing.Left, roomY, roomSize, roomSize);
                MakeHospitalOfficeRoom(map, "office", room, true);
            }

            Rectangle rightWing = new Rectangle(map.Rect.Right - roomSize, 0, roomSize, map.Height);
            for (int roomY = 0; roomY <= map.Height - roomSize; roomY += roomSize - 1)
            {
                Rectangle room = new Rectangle(rightWing.Left, roomY, roomSize, roomSize);
                MakeHospitalOfficeRoom(map, "office", room, false);
            }

            // 3. Populate.
            // nurses & doctor in offices.
            const int nbNurses = 5;
            for (int i = 0; i < nbNurses; i++)
            {
                // create.
                Actor nurse = CreateNewHospitalNurse(0);
                // place.
                ActorPlace(m_DiceRoller, map.Width * map.Height, map, nurse, (pt) => map.HasZonePartiallyNamedAt(pt, "office"));
            }
            const int nbDoctor = 2;
            for (int i = 0; i < nbDoctor; i++)
            {
                // create.
                Actor nurse = CreateNewHospitalDoctor(0);
                // place.
                ActorPlace(m_DiceRoller, map.Width * map.Height, map, nurse, (pt) => map.HasZonePartiallyNamedAt(pt, "office"));
            }

            // done.
            return map;
        }

        Map GenerateHospital_Patients(int seed)
        {
            //////////////////
            // 1. Create map.
            // 2. Floor plan.
            // 3. Populate.
            //////////////////

            // 1. Create map.
            Map map = new Map(seed, "Hospital - Patients", 13, 49)
            {
                Lighting = Lighting.DARKNESS
            };
            DoForEachTile(map, map.Rect, (pt) => map.GetTileAt(pt).IsInside = true);
            TileFill(map, m_Game.GameTiles.FLOOR_TILES);
            TileRectangle(map, m_Game.GameTiles.WALL_HOSPITAL, map.Rect);

            // 2. Floor plan.
            // One central south->north corridor with admission rooms on each sides.
            const int roomSize = 5;

            // 1. Central corridor.
            Rectangle corridor = new Rectangle(roomSize - 1, 0, 5, map.Height);
            TileRectangle(map, m_Game.GameTiles.WALL_HOSPITAL, corridor);
            map.AddZone(MakeUniqueZone("corridor", corridor));

            // 2. Patients rooms, all similar 5x5 rooms (3x3 inside)            
            Rectangle leftWing = new Rectangle(0, 0, roomSize, map.Height);
            for (int roomY = 0; roomY <= map.Height - roomSize; roomY += roomSize - 1)
            {
                Rectangle room = new Rectangle(leftWing.Left, roomY, roomSize, roomSize);
                MakeHospitalPatientRoom(map, "patient room", room, true);
            }

            Rectangle rightWing = new Rectangle(map.Rect.Right - roomSize, 0, roomSize, map.Height);
            for (int roomY = 0; roomY <= map.Height - roomSize; roomY += roomSize - 1)
            {
                Rectangle room = new Rectangle(rightWing.Left, roomY, roomSize, roomSize);
                MakeHospitalPatientRoom(map, "patient room", room, false);
            }

            // 3. Populate.
            // patients in rooms.
            const int nbPatients = 20;
            for (int i = 0; i < nbPatients; i++)
            {
                // create.
                Actor patient = CreateNewHospitalPatient(0);
                // place.
                ActorPlace(m_DiceRoller, map.Width * map.Height, map, patient, (pt) => map.HasZonePartiallyNamedAt(pt, "patient room"));
            }

            // nurses & doctor in corridor.
            const int nbNurses = 8;
            for (int i = 0; i < nbNurses; i++)
            {
                // create.
                Actor nurse = CreateNewHospitalNurse(0);
                // place.
                ActorPlace(m_DiceRoller, map.Width * map.Height, map, nurse, (pt) => map.HasZonePartiallyNamedAt(pt, "corridor"));
            }
            const int nbDoctor = 2;
            for (int i = 0; i < nbDoctor; i++)
            {
                // create.
                Actor nurse = CreateNewHospitalDoctor(0);
                // place.
                ActorPlace(m_DiceRoller, map.Width * map.Height, map, nurse, (pt) => map.HasZonePartiallyNamedAt(pt, "corridor"));
            }

            // done.
            return map;
        }

        Map GenerateHospital_Storage(int seed)
        {
            //////////////////
            // 1. Create map.
            // 2. Floor plan.
            //////////////////

            // 1. Create map.
            Map map = new Map(seed, "Hospital - Storage", 51, 16)
            {
                Lighting = Lighting.DARKNESS
            };
            DoForEachTile(map, map.Rect, (pt) => map.GetTileAt(pt).IsInside = true);
            TileFill(map, m_Game.GameTiles.FLOOR_TILES);
            TileRectangle(map, m_Game.GameTiles.WALL_HOSPITAL, map.Rect);

            // 2. Floor plan.
            // 1 north corridor linking stairs.
            // 1 central corridor to storage rooms, locked by an iron gate.
            // 1 south corridor to other storage rooms.

            // 1 north corridor linking stairs.
            const int northCorridorHeight = 4;
            Rectangle northCorridorRect = Rectangle.FromLTRB(0, 0, map.Width, northCorridorHeight);
            TileRectangle(map, m_Game.GameTiles.WALL_HOSPITAL, northCorridorRect);
            map.AddZone(MakeUniqueZone("north corridor", northCorridorRect));

            // 1 corridor to storage rooms, locked by an iron gate.
            const int corridorHeight = 4;
            Rectangle centralCorridorRect = Rectangle.FromLTRB(0, northCorridorRect.Bottom - 1, map.Width, northCorridorRect.Bottom - 1 + corridorHeight);
            TileRectangle(map, m_Game.GameTiles.WALL_HOSPITAL, centralCorridorRect);
            map.SetTileModelAt(1, centralCorridorRect.Top, m_Game.GameTiles.FLOOR_TILES);
             map.PlaceMapObjectAt(MakeObjIronGate(GameImages.OBJ_GATE_CLOSED), new Point(1, centralCorridorRect.Top));
            map.AddZone(MakeUniqueZone("central corridor", centralCorridorRect));
            // storage rooms.
            const int storageWidth = 5;
            const int storageHeight = 4;
            Rectangle storageCentral = new Rectangle(2, centralCorridorRect.Bottom - 1, map.Width - 2, storageHeight);
            for (int roomX = storageCentral.Left; roomX <= map.Width - storageWidth; roomX += storageWidth - 1)
            {
                Rectangle room = new Rectangle(roomX, storageCentral.Top, storageWidth, storageHeight);
                MakeHospitalStorageRoom(map, "storage", room);
            }
            map.SetTileModelAt(1, storageCentral.Top, m_Game.GameTiles.FLOOR_TILES);

            // 1 south corridor to other storage rooms.
            Rectangle southCorridorRect = Rectangle.FromLTRB(0, storageCentral.Bottom - 1, map.Width, storageCentral.Bottom - 1 + corridorHeight);
            TileRectangle(map, m_Game.GameTiles.WALL_HOSPITAL, southCorridorRect);
            map.SetTileModelAt(1, southCorridorRect.Top, m_Game.GameTiles.FLOOR_TILES);
            map.AddZone(MakeUniqueZone("south corridor", southCorridorRect));
            // storage rooms.
            Rectangle storageSouth = new Rectangle(2, southCorridorRect.Bottom - 1, map.Width - 2, storageHeight);
            for (int roomX = storageSouth.Left; roomX <= map.Width - storageWidth; roomX += storageWidth - 1)
            {
                Rectangle room = new Rectangle(roomX, storageSouth.Top, storageWidth, storageHeight);
                MakeHospitalStorageRoom(map, "storage", room);
            }
            map.SetTileModelAt(1, storageSouth.Top, m_Game.GameTiles.FLOOR_TILES);

            // done.
            return map;
        }

        Map GenerateHospital_Power(int seed)
        {
            //////////////////
            // 1. Create map.
            // 2. Floor plan.
            // 3. Populate.
            //////////////////

            // 1. Create map.
            Map map = new Map(seed, "Hospital - Power", 10, 10)
            {
                Lighting = Lighting.DARKNESS
            };
            DoForEachTile(map, map.Rect, (pt) => map.GetTileAt(pt).IsInside = true);
            TileFill(map, m_Game.GameTiles.FLOOR_CONCRETE);
            TileRectangle(map, m_Game.GameTiles.WALL_BRICK, map.Rect);

            // 2. Floor plan.
            // one narrow corridor separated from the power gen room by iron fences.
            // barricade room for the Enraged Patient.

            // corridor with fences.
            Rectangle corridor = Rectangle.FromLTRB(1, 1, 3, map.Height);
            map.AddZone(MakeUniqueZone("corridor", corridor));
            for (int yFence = 1; yFence < map.Height - 2; yFence++)
                map.PlaceMapObjectAt(MakeObjIronFence(GameImages.OBJ_IRON_FENCE), new Point(2, yFence));

            // power room.
            Rectangle room = Rectangle.FromLTRB(3, 0, map.Width, map.Height);
            map.AddZone(MakeUniqueZone("power room", room));

            // power generators.
            DoForEachTile(map, room,
                (pt) =>
                {
                    if (pt.X == room.Left)
                        return;
                    if (!map.IsWalkable(pt))
                        return;
                    if (CountAdjWalls(map, pt) < 3)
                        return;

                    map.PlaceMapObjectAt(MakeObjPowerGenerator(GameImages.OBJ_POWERGEN_OFF, GameImages.OBJ_POWERGEN_ON), pt);
                });

            // 3. Populate.
            // enraged patient!
            ActorModel model = m_Game.GameActors.JasonMyers;
            Actor jason = model.CreateNamed(m_Game.GameFactions.ThePsychopaths, "Jason Myers", false, 0);
            jason.IsUnique = true;
            jason.Doll.AddDecoration(DollPart.SKIN, GameImages.ACTOR_JASON_MYERS);
            GiveStartingSkillToActor(jason, Skills.IDs.TOUGH);
            GiveStartingSkillToActor(jason, Skills.IDs.TOUGH);
            GiveStartingSkillToActor(jason, Skills.IDs.TOUGH);
            GiveStartingSkillToActor(jason, Skills.IDs.STRONG);
            GiveStartingSkillToActor(jason, Skills.IDs.STRONG);
            GiveStartingSkillToActor(jason, Skills.IDs.STRONG);
            GiveStartingSkillToActor(jason, Skills.IDs.AGILE);
            GiveStartingSkillToActor(jason, Skills.IDs.AGILE);
            GiveStartingSkillToActor(jason, Skills.IDs.AGILE);
            GiveStartingSkillToActor(jason, Skills.IDs.HIGH_STAMINA);
            GiveStartingSkillToActor(jason, Skills.IDs.HIGH_STAMINA);
            GiveStartingSkillToActor(jason, Skills.IDs.HIGH_STAMINA);
            jason.Inventory.AddAll(MakeItemJasonMyersAxe());
            map.PlaceActorAt(jason, new Point(map.Width / 2, map.Height / 2));
            m_Game.Session.UniqueActors.JasonMyers = new UniqueActor()
            {
                TheActor = jason,
                IsSpawned = true
            };

            // done.
            return map;
        }

        Actor CreateNewHospitalPatient(int spawnTime)
        {
            // decide model.
            ActorModel model = m_Rules.Roll(0, 2) == 0 ? m_Game.GameActors.MaleCivilian : m_Game.GameActors.FemaleCivilian;

            // create.
            Actor patient = model.CreateNumberedName(m_Game.GameFactions.TheCivilians, 0);
            SkinNakedHuman(m_DiceRoller, patient);
            GiveNameToActor(m_DiceRoller, patient);
            patient.Name = "Patient " + patient.Name;
            patient.Controller = new CivilianAI();            

            // skills.
            GiveRandomSkillsToActor(m_DiceRoller, patient, 1);

            // add patient uniform.
            patient.Doll.AddDecoration(DollPart.TORSO, GameImages.HOSPITAL_PATIENT_UNIFORM);

            // done.
            return patient;
        }

        Actor CreateNewHospitalNurse(int spawnTime)
        {
            // create.
            Actor nurse = m_Game.GameActors.FemaleCivilian.CreateNumberedName(m_Game.GameFactions.TheCivilians, 0);
            SkinNakedHuman(m_DiceRoller, nurse);
            GiveNameToActor(m_DiceRoller, nurse);
            nurse.Name = "Nurse " + nurse.Name;
            nurse.Controller = new CivilianAI();

            // add uniform.
            nurse.Doll.AddDecoration(DollPart.TORSO, GameImages.HOSPITAL_NURSE_UNIFORM);

            // skills : 1 + 1-Medic.
            GiveRandomSkillsToActor(m_DiceRoller, nurse, 1);
            GiveStartingSkillToActor(nurse, Skills.IDs.MEDIC);

            // items : bandages.
            nurse.Inventory.AddAll(MakeItemBandages());

            // done.
            return nurse;
        }

        Actor CreateNewHospitalDoctor(int spawnTime)
        {
            // create.
            Actor doctor = m_Game.GameActors.MaleCivilian.CreateNumberedName(m_Game.GameFactions.TheCivilians, 0);
            SkinNakedHuman(m_DiceRoller, doctor);
            GiveNameToActor(m_DiceRoller, doctor);
            doctor.Name = "Doctor " + doctor.Name;
            doctor.Controller = new CivilianAI();

            // add uniform.
            doctor.Doll.AddDecoration(DollPart.TORSO, GameImages.HOSPITAL_DOCTOR_UNIFORM);

            // skills : 1 + 3-Medic + 1-Leadership.
            GiveRandomSkillsToActor(m_DiceRoller, doctor, 1);
            GiveStartingSkillToActor(doctor, Skills.IDs.MEDIC);
            GiveStartingSkillToActor(doctor, Skills.IDs.MEDIC);
            GiveStartingSkillToActor(doctor, Skills.IDs.MEDIC);
            GiveStartingSkillToActor(doctor, Skills.IDs.LEADERSHIP);

            // items : medikit + bandages.
            doctor.Inventory.AddAll(MakeItemMedikit());
            doctor.Inventory.AddAll(MakeItemBandages());

            // done.
            return doctor;
        }

        void MakeHospitalPatientRoom(Map map, string baseZoneName, Rectangle room, bool isFacingEast)
        {
            TileRectangle(map, m_Game.GameTiles.WALL_HOSPITAL, room);
            map.AddZone(MakeUniqueZone(baseZoneName, room));

            int xDoor = (isFacingEast ? room.Right - 1 : room.Left);

            // door in the corner.
            PlaceDoor(map, xDoor, room.Top + 1, m_Game.GameTiles.FLOOR_TILES, MakeObjHospitalDoor());

            // bed in the middle in the south.
            Point bedPos = new Point(room.Left + room.Width / 2, room.Bottom - 2);
            map.PlaceMapObjectAt(MakeObjBed(GameImages.OBJ_HOSPITAL_BED), bedPos);

            // chair and nighttable on either side of the bed.
            map.PlaceMapObjectAt(MakeObjChair(GameImages.OBJ_HOSPITAL_CHAIR), new Point(isFacingEast ? bedPos.X + 1 : bedPos.X - 1, bedPos.Y));
            Point tablePos = new Point(isFacingEast ? bedPos.X - 1 : bedPos.X + 1, bedPos.Y);
            map.PlaceMapObjectAt(MakeObjNightTable(GameImages.OBJ_HOSPITAL_NIGHT_TABLE), tablePos);

            // chance of some meds/food/book on nightable.
            if (m_DiceRoller.RollChance(50))
            {
                int roll = m_DiceRoller.Roll(0, 3);
                Item it = null;
                switch (roll)
                {
                    case 0: it = MakeShopPharmacyItem(); break;
                    case 1: it = MakeItemGroceries(); break;
                    case 2: it = MakeItemBook(); break;
                }
                if (it != null)
                    map.DropItemAt(it, tablePos);
            }

            // wardrobe in the corner.
            map.PlaceMapObjectAt(MakeObjWardrobe(GameImages.OBJ_HOSPITAL_WARDROBE), new Point(isFacingEast ? room.Left + 1: room.Right - 2, room.Top + 1));
        }

        void MakeHospitalOfficeRoom(Map map, string baseZoneName, Rectangle room, bool isFacingEast)
        {
            TileFill(map, m_Game.GameTiles.FLOOR_PLANKS, room);
            TileRectangle(map, m_Game.GameTiles.WALL_HOSPITAL, room);
            map.AddZone(MakeUniqueZone(baseZoneName, room));

            int xDoor = (isFacingEast ? room.Right - 1 : room.Left);
            int yDoor = room.Top+2;

            // door in the middle.
            PlaceDoor(map, xDoor, yDoor, m_Game.GameTiles.FLOOR_TILES, MakeObjWoodenDoor());

            // chairs and table facing the door.
            int xTable = (isFacingEast ? room.Left + 2 : room.Right - 3);
            map.PlaceMapObjectAt(MakeObjTable(GameImages.OBJ_TABLE), new Point(xTable, yDoor));
            map.PlaceMapObjectAt(MakeObjChair(GameImages.OBJ_CHAIR), new Point(xTable - 1, yDoor));
            map.PlaceMapObjectAt(MakeObjChair(GameImages.OBJ_CHAIR), new Point(xTable + 1, yDoor));
        }

        void MakeHospitalStorageRoom(Map map, string baseZoneName, Rectangle room)
        {
            TileRectangle(map, m_Game.GameTiles.WALL_HOSPITAL, room);
            map.AddZone(MakeUniqueZone(baseZoneName, room));

            // door.
            PlaceDoor(map, room.Left + 2, room.Top, m_Game.GameTiles.FLOOR_TILES, MakeObjHospitalDoor());

            // shelves with meds.
            DoForEachTile(map, room,
                (pt) =>
                {
                    if (!map.IsWalkable(pt))
                        return;

                    if (CountAdjDoors(map, pt.X, pt.Y) > 0)
                        return;

                    // shelf.
                    map.PlaceMapObjectAt(MakeObjShelf(GameImages.OBJ_SHOP_SHELF), pt);

                    // full stacks of meds or canned food.
                    Item it;
                    it = m_DiceRoller.RollChance(80) ? MakeHospitalItem() : MakeItemCannedFood();
                    if (it.Model.IsStackable)
                        it.Quantity = it.Model.StackingLimit;
                    map.DropItemAt(it, pt);
                });
        }

        #endregion
        #endregion

        #region Actors

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
