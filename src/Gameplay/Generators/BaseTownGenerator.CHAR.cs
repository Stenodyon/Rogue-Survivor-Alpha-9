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

        protected class CHARUndergroundGenerator
        {
            BaseTownGenerator parent;

            Map surfaceMap, underground;
            Zone officeZone;

            public CHARUndergroundGenerator(BaseTownGenerator parent, Map surfaceMap, Zone officeZone)
            {
                this.parent = parent;
                this.surfaceMap = surfaceMap;
                this.officeZone = officeZone;
                // huge map.
                underground = new Map((surfaceMap.Seed << 3) ^ surfaceMap.Seed, "CHAR Underground Facility", RogueGame.MAP_MAX_WIDTH, RogueGame.MAP_MAX_HEIGHT)
                {
                    Lighting = Lighting.DARKNESS,
                    IsSecret = true
                };
            }

            public Map Generate()
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
                // fill & enclose.
                parent.TileFill(underground, parent.m_Game.GameTiles.FLOOR_OFFICE, (tile, model, x, y) => tile.IsInside = true);
                parent.TileRectangle(underground, parent.m_Game.GameTiles.WALL_CHAR_OFFICE, new Rectangle(0, 0, underground.Width, underground.Height));

                // 2. Link to office.
                LinkToOffice();

                // 3. Create floorplan & rooms.
                List<Rectangle> roomsList = MakeFloorPlans();

                // 4. Rooms, furniture & items.
                FurnishRooms(roomsList);

                // 5. Posters & Blood.
                #region
                // char propaganda posters & blood almost everywhere.
                for(int x = 0; x < underground.Width;x++)
                    for (int y = 0; y < underground.Height; y++)
                    {
                        // poster on wall?
                        if (parent.m_DiceRoller.RollChance(25))
                        {
                            Tile tile = underground.GetTileAt(x,y);
                            if (tile.Model.IsWalkable)
                                continue;
                            tile.AddDecoration(CHAR_POSTERS[parent.m_DiceRoller.Roll(0, CHAR_POSTERS.Length)]);
                        }

                        // blood?
                        if (parent.m_DiceRoller.RollChance(20))
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
                    Actor undead = parent.CreateNewUndead(0);
                    for (; ; )
                    {
                        GameActors.IDs upID = parent.m_Game.NextUndeadEvolution((GameActors.IDs)undead.Model.ID);
                        if (upID == (GameActors.IDs) undead.Model.ID)
                            break;
                        undead.Model = parent.m_Game.GameActors[upID];
                    }
                    parent.ActorPlace(parent.m_DiceRoller, underground.Width * underground.Height, underground, undead, (pt) => underground.GetExitAt(pt) == null);
                }         
    
                // CHAR Guards.
                int nbGuards = underground.Width / 10; // 10 for 100.
                for (int i = 0; i < nbGuards; i++)
                {
                    Actor guard = parent.CreateNewCHARGuard(0);
                    parent.ActorPlace(parent.m_DiceRoller, underground.Width * underground.Height, underground, guard, (pt) => underground.GetExitAt(pt) == null);
                }
                #endregion

                // 7. Add uniques.
                // TODO...
                #region
                #endregion

                // done.
                return underground;
            }

            private void LinkToOffice()
            {
                // find surface point in office:
                // - in a random office room.
                // - set exit somewhere walkable inside.
                // - iron door, barricade the door.
                Zone roomZone = null;
                Point surfaceExit = FindRandomRoom(out roomZone);

                // barricade the rooms door.
                BarricadeDoor(roomZone.Bounds);

                // stairs.
                // underground : in the middle of the map.
                Point undergroundStairs = new Point(underground.Width / 2, underground.Height / 2);
                underground.SetExitAt(undergroundStairs, new Exit(surfaceMap, surfaceExit));
                underground.GetTileAt(undergroundStairs.X, undergroundStairs.Y).AddDecoration(GameImages.DECO_STAIRS_UP);
                surfaceMap.SetExitAt(surfaceExit, new Exit(underground, undergroundStairs));
                surfaceMap.GetTileAt(surfaceExit.X, surfaceExit.Y).AddDecoration(GameImages.DECO_STAIRS_DOWN);
                // floor logo.
                parent.ForEachAdjacent(underground, undergroundStairs.X, undergroundStairs.Y, (pt) => underground.GetTileAt(pt).AddDecoration(GameImages.DECO_CHAR_FLOOR_LOGO));
            }

            private Point FindRandomRoom(out Zone roomZone)
            {
                Point surfaceExit = new Point();
                while (true)    // loop until found.
                {
                    // find a random room.
                    roomZone = PickRandomRoom();

                    // find somewhere walkable inside.
                    bool foundSurfaceExit = false;
                    int attempts = 0;
                    do
                    {
                        surfaceExit.X = parent.m_DiceRoller.Roll(roomZone.Bounds.Left, roomZone.Bounds.Right);
                        surfaceExit.Y = parent.m_DiceRoller.Roll(roomZone.Bounds.Top, roomZone.Bounds.Bottom);
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
                return surfaceExit;
            }

            private Zone PickRandomRoom()
            {
                Zone roomZone = null;
                do
                {
                    int x = parent.m_DiceRoller.Roll(officeZone.Bounds.Left, officeZone.Bounds.Right);
                    int y = parent.m_DiceRoller.Roll(officeZone.Bounds.Top, officeZone.Bounds.Bottom);
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
                return roomZone;
            }

            private void BarricadeDoor(Rectangle bounds)
            {
                parent.DoForEachTile(surfaceMap, bounds,
                    (pt) =>
                    {
                        DoorWindow door = surfaceMap.GetMapObjectAt(pt) as DoorWindow;
                        if (door == null)
                            return;
                        surfaceMap.RemoveMapObjectAt(pt.X, pt.Y);
                        door = parent.MakeObjIronDoor();
                        door.BarricadePoints = Rules.BARRICADING_MAX;
                        surfaceMap.PlaceMapObjectAt(door, pt);
                    });
            }

            /// Makes the floor plans and return a list of rooms
            private List<Rectangle> MakeFloorPlans()
            {
                // make 4 quarters, splitted by a crossed corridor.
                const int corridorHalfWidth = 1;
                Rectangle qTopLeft = Rectangle.FromLTRB(0, 0, underground.Width / 2 - corridorHalfWidth, underground.Height / 2 - corridorHalfWidth);
                Rectangle qTopRight = Rectangle.FromLTRB(underground.Width / 2 + 1 + corridorHalfWidth, 0, underground.Width, qTopLeft.Bottom);
                Rectangle qBotLeft = Rectangle.FromLTRB(0, underground.Height/2 + 1 + corridorHalfWidth, qTopLeft.Right, underground.Height);
                Rectangle qBotRight = Rectangle.FromLTRB(qTopRight.Left, qBotLeft.Top, underground.Width, underground.Height);

                // split all the map in rooms.
                const int minRoomSize = 6;
                List<Rectangle> roomsList = new List<Rectangle>();
                parent.MakeRoomsPlan(underground, ref roomsList, qBotLeft, minRoomSize);
                parent.MakeRoomsPlan(underground, ref roomsList, qBotRight, minRoomSize);
                parent.MakeRoomsPlan(underground, ref roomsList, qTopLeft, minRoomSize);
                parent.MakeRoomsPlan(underground, ref roomsList, qTopRight, minRoomSize);

                // make the rooms walls.
                foreach (Rectangle roomRect in roomsList)
                {
                    parent.TileRectangle(underground, parent.m_Game.GameTiles.WALL_CHAR_OFFICE, roomRect);
                }

                // add room doors.
                // quarters have door side preferences to lead toward the central corridors.
                PlaceRoomDoors(roomsList);

                // add iron doors closing each corridor.
                for (int x = qTopLeft.Right; x < qBotRight.Left; x++)
                {
                    parent.PlaceDoor(underground, x, qTopLeft.Bottom - 1, parent.m_Game.GameTiles.FLOOR_OFFICE, parent.MakeObjIronDoor());
                    parent.PlaceDoor(underground, x, qBotLeft.Top, parent.m_Game.GameTiles.FLOOR_OFFICE, parent.MakeObjIronDoor());
                }
                for (int y = qTopLeft.Bottom; y < qBotLeft.Top; y++)
                {
                    parent.PlaceDoor(underground, qTopLeft.Right - 1, y, parent.m_Game.GameTiles.FLOOR_OFFICE, parent.MakeObjIronDoor());
                    parent.PlaceDoor(underground, qTopRight.Left, y, parent.m_Game.GameTiles.FLOOR_OFFICE, parent.MakeObjIronDoor());
                }
                return roomsList;
            }

            private void PlaceRoomDoors(List<Rectangle> roomsList)
            {
                foreach (Rectangle roomRect in roomsList)
                {
                    Point westEastDoorPos = roomRect.Left < underground.Width / 2 ? 
                        new Point(roomRect.Right - 1, roomRect.Top + roomRect.Height / 2) : 
                        new Point(roomRect.Left, roomRect.Top + roomRect.Height / 2);
                    if (underground.GetMapObjectAt(westEastDoorPos) == null)
                    {
                        DoorWindow door = parent.MakeObjCharDoor();
                        parent.PlaceDoorIfAccessibleAndNotAdjacent(underground, westEastDoorPos.X, westEastDoorPos.Y, parent.m_Game.GameTiles.FLOOR_OFFICE, 6, door);
                    }

                    Point northSouthDoorPos = roomRect.Top < underground.Height / 2 ? 
                        new Point(roomRect.Left + roomRect.Width / 2, roomRect.Bottom - 1) : 
                        new Point(roomRect.Left + roomRect.Width / 2, roomRect.Top);
                    if (underground.GetMapObjectAt(northSouthDoorPos) == null)
                    {
                        DoorWindow door = parent.MakeObjCharDoor();
                        parent.PlaceDoorIfAccessibleAndNotAdjacent(underground, northSouthDoorPos.X, northSouthDoorPos.Y, parent.m_Game.GameTiles.FLOOR_OFFICE, 6, door);
                    }
                }

            }

            private void FurnishRooms(List<Rectangle> roomsList)
            {
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
                        parent.MakeCHARPowerRoom(underground, roomRect, insideRoomRect);
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
                                    parent.MakeCHARArmoryRoom(underground, insideRoomRect);
                                    break;
                                }
                            case 1: // storage room.
                                {
                                    roomName = "Storage";
                                    parent.MakeCHARStorageRoom(underground, insideRoomRect);
                                    break;
                                }
                            case 2: // living room.
                                {
                                    roomName = "Living";
                                    parent.MakeCHARLivingRoom(underground, insideRoomRect);
                                    break;
                                }
                            case 3: // pharmacy.
                                {
                                    roomName = "Pharmacy";
                                    parent.MakeCHARPharmacyRoom(underground, insideRoomRect);
                                    break;
                                }
                            default:
                                throw new ArgumentOutOfRangeException("unhandled role");
                        }
                    }

                    underground.AddZone(parent.MakeUniqueZone(roomName, insideRoomRect));
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

        protected virtual void PopulateCHAROfficeBuilding(Map map, Block b)
        {
            // Guards
            for (int i = 0; i < MAX_CHAR_GUARDS_PER_OFFICE; i++)
            {
                Actor newGuard = CreateNewCHARGuard(0);
                ActorPlace(m_DiceRoller, 100, map, newGuard, b.InsideRect.Left, b.InsideRect.Top, b.InsideRect.Width, b.InsideRect.Height);
            }

        }

        public Map GenerateUniqueMap_CHARUnderground(Map surfaceMap, Zone officeZone)
        {
            CHARUndergroundGenerator generator = new CHARUndergroundGenerator(
                this, surfaceMap, officeZone
            );
            return generator.Generate();
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

    }
}