using System;
using System.Drawing;
using System.Collections.Generic;

using djack.RogueSurvivor.Data;
using djack.RogueSurvivor.Engine;
using djack.RogueSurvivor.Engine.MapObjects;
using djack.RogueSurvivor.Gameplay.AI;

namespace djack.RogueSurvivor.Gameplay.Generators
{
    /* This part of BaseTownGenerator provides the Police Station generation
    functions */
    partial class BaseTownGenerator
    {
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
    }
}