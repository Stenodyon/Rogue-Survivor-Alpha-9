using System;
using System.Drawing;
using System.Collections.Generic;

using djack.RogueSurvivor.Data;
using djack.RogueSurvivor.Engine;
using djack.RogueSurvivor.Engine.MapObjects;
using djack.RogueSurvivor.Gameplay.AI;

namespace djack.RogueSurvivor.Gameplay.Generators
{
    /* This part of BaseTownGenerator provides the Hospital generation
    functions */
    partial class BaseTownGenerator
    {
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

    }
}