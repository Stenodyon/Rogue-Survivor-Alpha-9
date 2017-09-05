using System;
using System.Drawing;
using System.Collections.Generic;

using djack.RogueSurvivor.Data;
using djack.RogueSurvivor.Engine;
using djack.RogueSurvivor.Engine.MapObjects;

namespace djack.RogueSurvivor.Gameplay.Generators
{
    /* This part of BaseTownGenerator provides CHAR building generators */
    partial class BaseTownGenerator
    {
        protected abstract class CHARBuildingGenerator
        {
            protected BaseTownGenerator parent;

            protected Map map;
            protected Block b;

            protected int midX, midY;
            protected Direction doorSide;
            protected bool horizontalCorridor;

            public CHARBuildingGenerator(BaseTownGenerator parent, Map map, Block b)
            {
                this.parent = parent;
                this.map = map;
                this.b = b;
                this.horizontalCorridor = (b.InsideRect.Width >= b.InsideRect.Height);
            }

            protected virtual void MakeEntrance()
            {
                PlaceDoors();

                // add office image next to doors.
                string officeImage = GameImages.DECO_CHAR_OFFICE;
                parent.DecorateOutsideWalls(map, b.BuildingRect,
                    (x, y) =>
                        map.GetMapObjectAt(x, y) == null
                        && parent.CountAdjDoors(map, x, y) >= 1 ? officeImage : null);
            }

            protected virtual void PlaceDoors()
            {
                // Decide orientation
                midX = b.Rectangle.Left + b.Rectangle.Width / 2;
                midY = b.Rectangle.Top + b.Rectangle.Height / 2;

                // make doors on one side.
                if (horizontalCorridor)
                {
                    bool west = parent.m_DiceRoller.RollChance(50);
                    int doorX = west ? b.BuildingRect.Left
                                     : b.BuildingRect.Right - 1;
                    doorSide = west ? Direction.W : Direction.E;

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
                    doorSide = north ? Direction.N : Direction.S;

                    PlaceDoor(midX, doorY);
                    if (b.InsideRect.Width >= 8)
                        PlaceDoor(midX - 1, doorY);
                    if (b.InsideRect.Width >= 12)
                        PlaceDoor(midX + 1, doorY);
                }
            }

            protected virtual void PlaceDoor(int x, int y)
            {
                parent.PlaceDoor(map, x, y,
                    parent.m_Game.GameTiles.FLOOR_WALKWAY,
                    parent.MakeObjGlassDoor());
            }

            protected void PlaceCHARDoor(int x, int y)
            {
                parent.PlaceDoor(map, x, y,
                    parent.m_Game.GameTiles.FLOOR_OFFICE,
                    parent.MakeObjCharDoor());
            }
        }

        protected class CHARAgencyGenerator : CHARBuildingGenerator
        {
            public CHARAgencyGenerator(BaseTownGenerator parent, Map map, Block b)
                : base(parent, map, b)
            {}

            public bool Generate()
            {
                // 1. Walkway, floor & walls
                MakeFoundations();

                // 3. Entry door 
                MakeEntrance();

                // 4. Furniture
                PlaceFurniture();

                // 5. Posters
                Decorate();

                // 6. Zones.
                map.AddZone(parent.MakeUniqueZone("CHAR Agency", b.BuildingRect));
                parent.MakeWalkwayZones(map, b);

                // Done
                return true;
            }

            /// Walway, floor and walls
            private void MakeFoundations()
            {
                parent.TileRectangle(map, parent.m_Game.GameTiles.FLOOR_WALKWAY, b.Rectangle);
                parent.TileRectangle(map, parent.m_Game.GameTiles.WALL_CHAR_OFFICE, b.BuildingRect);
                parent.TileFill(map, parent.m_Game.GameTiles.FLOOR_OFFICE, b.InsideRect,
                    (tile, prevmodel, x, y) =>
                    {
                        tile.IsInside = true;
                        tile.AddDecoration(GameImages.DECO_CHAR_FLOOR_LOGO);
                    });
            }

            private void PlaceFurniture()
            {
                // chairs on the sides.
                parent.MapObjectFill(map, b.InsideRect,
                    (pt) =>
                    {
                        if (parent.CountAdjWalls(map, pt.X, pt.Y) < 3)
                            return null;
                        return parent.MakeObjChair(GameImages.OBJ_CHAR_CHAIR);
                    });
                // walls/pilars in the middle.
                parent.TileFill(map, parent.m_Game.GameTiles.WALL_CHAR_OFFICE, new Rectangle(b.InsideRect.Left + b.InsideRect.Width / 2 - 1, b.InsideRect.Top + b.InsideRect.Height / 2 - 1, 3, 2),
                    (tile, model, x, y) =>
                    {
                        tile.AddDecoration(CHAR_POSTERS[parent.m_DiceRoller.Roll(0, CHAR_POSTERS.Length)]);
                    });
            }

            /// Tags and posters
            private void Decorate()
            {
                // outside.
                parent.DecorateOutsideWalls(map, b.BuildingRect,
                    (x, y) =>
                    {
                        if (parent.CountAdjDoors(map, x, y) > 0)
                            return null;
                        else
                        {
                            if (parent.m_DiceRoller.RollChance(25))
                                return CHAR_POSTERS[parent.m_DiceRoller.Roll(0, CHAR_POSTERS.Length)];
                            else
                                return null;
                        }
                    });
            }
        }

        protected class CHAROfficeGenerator : CHARBuildingGenerator
        {
            const int hallDepth = 3;
            const int officeRoomsSize = 4;

            List<Rectangle> allOffices;

            public CHAROfficeGenerator(BaseTownGenerator parent, Map map, Block b)
                : base(parent, map, b)
            {}

            public bool Generate()
            {
                // 1. Walkway, floor & walls
                parent.TileRectangle(map, parent.m_Game.GameTiles.FLOOR_WALKWAY, b.Rectangle);
                parent.TileRectangle(map, parent.m_Game.GameTiles.WALL_CHAR_OFFICE, b.BuildingRect);
                parent.TileFill(map, parent.m_Game.GameTiles.FLOOR_OFFICE, b.InsideRect, (tile, prevmodel, x, y) => tile.IsInside = true);

                // 3. Entry door 
                MakeEntrance();

                // 4. Make entry hall.
                MakeHall();

                // 5. Make central corridor & wings
                Rectangle corridorRect = MakeCorridor();

                // 6. Make office rooms.
                MakeOfficeRooms(corridorRect);

                // 7. Add items.
                PopulateRooms();

                // 8. Zone
                Zone zone = parent.MakeUniqueZone("CHAR Office", b.BuildingRect);
                zone.SetGameAttribute<bool>(ZoneAttributes.IS_CHAR_OFFICE, true);
                map.AddZone(zone);
                parent.MakeWalkwayZones(map, b);

                // Done
                return true;
            }

            protected override void MakeEntrance()
            {
                base.MakeEntrance();

                // barricade entry doors.
                parent.BarricadeDoors(map, b.BuildingRect, Rules.BARRICADING_MAX);
            }

            private void MakeHall()
            {
                if (doorSide == Direction.N)
                {
                    parent.TileHLine(map, parent.m_Game.GameTiles.WALL_CHAR_OFFICE, b.InsideRect.Left, b.InsideRect.Top + hallDepth, b.InsideRect.Width);
                }
                else if (doorSide == Direction.S)
                {
                    parent.TileHLine(map, parent.m_Game.GameTiles.WALL_CHAR_OFFICE, b.InsideRect.Left, b.InsideRect.Bottom - 1 - hallDepth, b.InsideRect.Width);
                }
                else if (doorSide == Direction.E)
                {
                    parent.TileVLine(map, parent.m_Game.GameTiles.WALL_CHAR_OFFICE, b.InsideRect.Right - 1 - hallDepth, b.InsideRect.Top, b.InsideRect.Height);
                }
                else if (doorSide == Direction.W)
                {
                    parent.TileVLine(map, parent.m_Game.GameTiles.WALL_CHAR_OFFICE, b.InsideRect.Left + hallDepth, b.InsideRect.Top, b.InsideRect.Height);
                }
                else
                    throw new InvalidOperationException("unhandled door side");
            }

            private Rectangle MakeCorridor()
            {
                Point corridorDoor;
                Rectangle corridorRect;
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

                parent.TileRectangle(map, parent.m_Game.GameTiles.WALL_CHAR_OFFICE, corridorRect);
                parent.PlaceDoor(map, corridorDoor.X, corridorDoor.Y, parent.m_Game.GameTiles.FLOOR_OFFICE, parent.MakeObjCharDoor());

                return corridorRect;
            }

            private void MakeOfficeRooms(Rectangle corridorRect)
            {
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
                List<Rectangle> officesOne = new List<Rectangle>();
                parent.MakeRoomsPlan(map, ref officesOne, wingOne, officeRoomsSize);

                List<Rectangle> officesTwo = new List<Rectangle>();
                parent.MakeRoomsPlan(map, ref officesTwo, wingTwo, officeRoomsSize);

                allOffices = new List<Rectangle>(officesOne.Count + officesTwo.Count);
                allOffices.AddRange(officesOne);
                allOffices.AddRange(officesTwo);

                foreach (Rectangle roomRect in officesOne)
                {
                    parent.TileRectangle(map, parent.m_Game.GameTiles.WALL_CHAR_OFFICE, roomRect);
                    map.AddZone(parent.MakeUniqueZone("Office room", roomRect));
                }
                foreach (Rectangle roomRect in officesTwo)
                {
                    parent.TileRectangle(map, parent.m_Game.GameTiles.WALL_CHAR_OFFICE, roomRect);
                    map.AddZone(parent.MakeUniqueZone("Office room", roomRect));
                }

                foreach (Rectangle roomRect in officesOne)
                {
                    if (horizontalCorridor)
                        PlaceCHARDoor(roomRect.Left + roomRect.Width / 2, roomRect.Bottom - 1);
                    else
                        PlaceCHARDoor(roomRect.Right - 1, roomRect.Top + roomRect.Height / 2);
                }
                foreach (Rectangle roomRect in officesTwo)
                {
                    if (horizontalCorridor)
                        PlaceCHARDoor(roomRect.Left + roomRect.Width / 2, roomRect.Top);
                    else
                        PlaceCHARDoor(roomRect.Left, roomRect.Top + roomRect.Height / 2);
                }

                FurnishRooms();
            }

            /// Place furniture inside the office rooms
            private void FurnishRooms()
            {
                // tables with chairs.
                foreach (Rectangle roomRect in allOffices)
                {
                    // table.
                    Point tablePos = new Point(roomRect.Left + roomRect.Width / 2, roomRect.Top + roomRect.Height / 2);
                    map.PlaceMapObjectAt(parent.MakeObjTable(GameImages.OBJ_CHAR_TABLE), tablePos);

                    // try to put chairs around.
                    int nbChairs = 2;
                    Rectangle insideRoom = new Rectangle(roomRect.Left + 1, roomRect.Top + 1, roomRect.Width - 2, roomRect.Height - 2);
                    if (!insideRoom.IsEmpty)
                    {
                        for (int i = 0; i < nbChairs; i++)
                        {
                            Rectangle adjTableRect = new Rectangle(tablePos.X - 1, tablePos.Y - 1, 3, 3);
                            adjTableRect.Intersect(insideRoom);
                            parent.MapObjectPlaceInGoodPosition(map, adjTableRect,
                                (pt) => pt != tablePos,
                                parent.m_DiceRoller,
                                (pt) => parent.MakeObjChair(GameImages.OBJ_CHAR_CHAIR));
                        }
                    }
                }
            }

            /// Place items inside the office rooms
            private void PopulateRooms()
            {
                // drop goodies in rooms.
                foreach (Rectangle roomRect in allOffices)
                {
                    parent.ItemsDrop(map, roomRect,
                        (pt) =>
                        {
                            Tile tile = map.GetTileAt(pt.X, pt.Y);
                            if (tile.Model != parent.m_Game.GameTiles.FLOOR_OFFICE)
                                return false;
                            MapObject mapObj = map.GetMapObjectAt(pt);
                            if (mapObj != null)
                                return false;
                            return true;
                        },
                        (pt) => parent.MakeRandomCHAROfficeItem());
                }
            }
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
                CHARAgencyGenerator generator = new CHARAgencyGenerator(
                    this, map, b
                );
                // small, make it an Agency.
                if (generator.Generate())
                    return CHARBuildingType.AGENCY;
                else
                    return CHARBuildingType.NONE;
            }
            else
            {
                CHAROfficeGenerator generator = new CHAROfficeGenerator(
                    this, map, b
                );
                if(generator.Generate())
                    return CHARBuildingType.OFFICE;
                else
                    return CHARBuildingType.NONE;
            }
        }

        static string[] CHAR_POSTERS = { GameImages.DECO_CHAR_POSTER1, GameImages.DECO_CHAR_POSTER2, GameImages.DECO_CHAR_POSTER3 };

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

    }
}