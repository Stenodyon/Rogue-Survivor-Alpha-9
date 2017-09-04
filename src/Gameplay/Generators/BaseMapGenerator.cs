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
    abstract partial class BaseMapGenerator : MapGenerator
    {
        #region Fields
        protected readonly RogueGame m_Game;
        #endregion

        #region Init
        protected BaseMapGenerator(RogueGame game)
            : base(game.Rules)
        {
            m_Game = game;
        }
        #endregion

        #region Common map objects
        protected DoorWindow MakeObjWoodenDoor()
        {
            return new DoorWindow("wooden door", GameImages.OBJ_WOODEN_DOOR_CLOSED, GameImages.OBJ_WOODEN_DOOR_OPEN, GameImages.OBJ_WOODEN_DOOR_BROKEN, DoorWindow.BASE_HITPOINTS)
            {
                GivesWood = true
            };
        }

        protected DoorWindow MakeObjHospitalDoor()
        {
            return new DoorWindow("door", GameImages.OBJ_HOSPITAL_DOOR_CLOSED, GameImages.OBJ_HOSPITAL_DOOR_OPEN, GameImages.OBJ_HOSPITAL_DOOR_BROKEN, DoorWindow.BASE_HITPOINTS)
            {
                GivesWood = true
            };
        }

        protected DoorWindow MakeObjCharDoor()
        {
            return new DoorWindow("CHAR door", GameImages.OBJ_CHAR_DOOR_CLOSED, GameImages.OBJ_CHAR_DOOR_OPEN, GameImages.OBJ_CHAR_DOOR_BROKEN, 4 * DoorWindow.BASE_HITPOINTS);
        }

        protected DoorWindow MakeObjGlassDoor()
        {
            return new DoorWindow("glass door", GameImages.OBJ_GLASS_DOOR_CLOSED, GameImages.OBJ_GLASS_DOOR_OPEN, GameImages.OBJ_GLASS_DOOR_BROKEN, DoorWindow.BASE_HITPOINTS / 4)
            {
                IsMaterialTransparent = true,
                BreaksWhenFiredThrough = true
            };
        }

        protected DoorWindow MakeObjIronDoor()
        {
            return new DoorWindow("iron door", GameImages.OBJ_IRON_DOOR_CLOSED, GameImages.OBJ_IRON_DOOR_OPEN, GameImages.OBJ_IRON_DOOR_BROKEN, 8 * DoorWindow.BASE_HITPOINTS)
            {
                IsAn = true
            };
        }

        protected DoorWindow MakeObjWindow()
        {
            // windows as transparent doors.
            return new DoorWindow("window", GameImages.OBJ_WINDOW_CLOSED, GameImages.OBJ_WINDOW_OPEN, GameImages.OBJ_WINDOW_BROKEN, DoorWindow.BASE_HITPOINTS / 4)
            {
                IsWindow = true,
                IsMaterialTransparent = true,
                GivesWood = true,
                BreaksWhenFiredThrough = true
            };
        }

        protected MapObject MakeObjFence(string fenceImageID)
        {
            return new MapObject("fence", fenceImageID, MapObject.Break.BREAKABLE, MapObject.Fire.UNINFLAMMABLE, DoorWindow.BASE_HITPOINTS * 10)
            {
                IsMaterialTransparent = true,
                JumpLevel = 1,
                GivesWood = true,
                StandOnFovBonus = true
            };
        }

        protected MapObject MakeObjIronFence(string fenceImageID)
        {
            return new MapObject("iron fence", fenceImageID)
            {
                IsMaterialTransparent = true,
                IsAn = true,
            };
        }

        protected MapObject MakeObjIronGate(string gateImageID)
        {
            return new MapObject("iron gate", gateImageID, MapObject.Break.BREAKABLE, MapObject.Fire.UNINFLAMMABLE, DoorWindow.BASE_HITPOINTS * 20)
            {
                IsMaterialTransparent = true,
                IsAn = true
            };
        }

        public Fortification MakeObjSmallFortification(string imageID)
        {
            return new Fortification("small fortification", imageID, Fortification.SMALL_BASE_HITPOINTS)
            {
                IsMaterialTransparent = true,
                GivesWood = true,
                IsMovable = true,
                Weight = 4,
                JumpLevel = 1
            };
        }

        public Fortification MakeObjLargeFortification(string imageID)
        {
            return new Fortification("large fortification", imageID, Fortification.LARGE_BASE_HITPOINTS)
            {
                GivesWood = true
            };
        }

        protected MapObject MakeObjTree(string treeImageID)
        {
            return new MapObject("tree", treeImageID, MapObject.Break.BREAKABLE, MapObject.Fire.BURNABLE, DoorWindow.BASE_HITPOINTS * 10)
            {
                GivesWood = true
            };
        }

        static string[] CARS = { GameImages.OBJ_CAR1, GameImages.OBJ_CAR2, GameImages.OBJ_CAR3, GameImages.OBJ_CAR4 };

        /// <summary>
        /// Makes a new wrecked car of a random model.
        /// <see>MakeWreckedCar(string)</see>
        /// </summary>
        /// <param name="roller"></param>
        /// <returns></returns>
        protected MapObject MakeObjWreckedCar(DiceRoller roller)
        {
            return MakeObjWreckedCar(CARS[roller.Roll(0, CARS.Length)]);
        }

        /// <summary>
        /// Makes a new wrecked car : transparent, not walkable but jumpable, movable.
        /// </summary>
        /// <param name="carImageID"></param>
        /// <returns></returns>
        protected MapObject MakeObjWreckedCar(string carImageID)
        {
            return new MapObject("wrecked car", carImageID)
            {
                BreakState = MapObject.Break.BROKEN,
                IsMaterialTransparent = true,
                JumpLevel = 1,
                IsMovable = true,
                Weight = 100,
                StandOnFovBonus = true
            };
        }

        protected MapObject MakeObjShelf(string shelfImageID)
        {
            return new MapObject("shelf", shelfImageID, MapObject.Break.BREAKABLE, MapObject.Fire.UNINFLAMMABLE, DoorWindow.BASE_HITPOINTS)
            {
                IsContainer = true,
                GivesWood = true,
                IsMovable = true,
                Weight = 6
            };
        }

        protected MapObject MakeObjBench(string benchImageID)
        {
            return new MapObject("bench", benchImageID, MapObject.Break.BREAKABLE, MapObject.Fire.UNINFLAMMABLE, DoorWindow.BASE_HITPOINTS * 2)
            {
                IsMaterialTransparent = true,
                JumpLevel = 1,
                IsCouch = true,
                GivesWood = true
            };
        }

        protected MapObject MakeObjIronBench(string benchImageID)
        {
            return new MapObject("iron bench", benchImageID)
            {
                IsMaterialTransparent = true,
                JumpLevel = 1,
                IsCouch = true,
                IsAn = true
            };
        }

        protected MapObject MakeObjBed(string bedImageID)
        {
            return new MapObject("bed", bedImageID, MapObject.Break.BREAKABLE, MapObject.Fire.UNINFLAMMABLE, DoorWindow.BASE_HITPOINTS * 2)
            {
                IsMaterialTransparent = true,
                IsWalkable = true,
                IsCouch = true,
                GivesWood = true,
                IsMovable = true,
                Weight = 6
            };
        }

        protected MapObject MakeObjWardrobe(string wardrobeImageID)
        {
            return new MapObject("wardrobe", wardrobeImageID, MapObject.Break.BREAKABLE, MapObject.Fire.UNINFLAMMABLE, DoorWindow.BASE_HITPOINTS * 6)
            {
                IsMaterialTransparent = false,
                IsContainer = true,
                GivesWood = true,
                IsMovable = true,
                Weight = 10
            };
        }

        protected MapObject MakeObjDrawer(string drawerImageID)
        {
            return new MapObject("drawer", drawerImageID, MapObject.Break.BREAKABLE, MapObject.Fire.UNINFLAMMABLE, DoorWindow.BASE_HITPOINTS)
            {
                IsMaterialTransparent = true,
                IsContainer = true,
                GivesWood = true,
                IsMovable = true,
                Weight = 6
            };
        }

        protected MapObject MakeObjTable(string tableImageID)
        {
            return new MapObject("table", tableImageID, MapObject.Break.BREAKABLE, MapObject.Fire.UNINFLAMMABLE, DoorWindow.BASE_HITPOINTS)
            {
                IsMaterialTransparent = true,
                JumpLevel = 1,
                GivesWood = true,
                IsMovable = true,
                Weight = 2
            };
        }

        protected MapObject MakeObjChair(string chairImageID)
        {
            return new MapObject("chair", chairImageID, MapObject.Break.BREAKABLE, MapObject.Fire.UNINFLAMMABLE, DoorWindow.BASE_HITPOINTS / 3)
            {
                IsMaterialTransparent = true,
                JumpLevel = 1,
                GivesWood = true,
                IsMovable = true,
                Weight = 1
            };
        }

        protected MapObject MakeObjNightTable(string nightTableImageID)
        {
            return new MapObject("night table", nightTableImageID, MapObject.Break.BREAKABLE, MapObject.Fire.UNINFLAMMABLE, DoorWindow.BASE_HITPOINTS / 3)
            {
                IsMaterialTransparent = true,
                JumpLevel = 1,
                GivesWood = true,
                IsMovable = true,
                Weight = 1
            };
        }

        protected MapObject MakeObjFridge(string fridgeImageID)
        {
            return new MapObject("fridge", fridgeImageID, MapObject.Break.BREAKABLE, MapObject.Fire.UNINFLAMMABLE, DoorWindow.BASE_HITPOINTS * 6)
            {
                IsContainer = true,
                IsMovable = true,
                Weight = 10
            };
        }

        protected MapObject MakeObjJunk(string junkImageID)
        {
            return new MapObject("junk", junkImageID, MapObject.Break.BREAKABLE, MapObject.Fire.UNINFLAMMABLE, DoorWindow.BASE_HITPOINTS)
            {
                IsPlural = true,
                IsMaterialTransparent = true,
                IsMovable = true,
                GivesWood = true,
                Weight = 6
            };
        }

        protected MapObject MakeObjBarrels(string barrelsImageID)
        {
            return new MapObject("barrels", barrelsImageID, MapObject.Break.BREAKABLE, MapObject.Fire.UNINFLAMMABLE, 2 * DoorWindow.BASE_HITPOINTS)
            {
                IsPlural = true,
                IsMaterialTransparent = true,
                IsMovable = true,
                GivesWood = true,
                Weight = 10
            };
        }

        protected PowerGenerator MakeObjPowerGenerator(string offImageID, string onImageID)
        {
            return new PowerGenerator("power generator", offImageID, onImageID);
        }

        public MapObject MakeObjBoard(string imageID, string[] text)
        {
            return new Board("board", imageID, text);
        }
        #endregion

        #region Common tile decorations
        public void DecorateOutsideWalls(Map map, Rectangle rect, Func<int, int, string> decoFn)
        {
            for (int x = rect.Left; x < rect.Right; x++)
                for (int y = rect.Top; y < rect.Bottom; y++)
                {
                    Tile tile = map.GetTileAt(x, y);
                    if (tile.Model.IsWalkable)
                        continue;
                    if (tile.IsInside)
                        continue;

                    string deco = decoFn(x, y);
                    if (deco != null)
                        tile.AddDecoration(deco);
                }
        }
        #endregion

        #region Common tasks
        protected void BarricadeDoors(Map map, Rectangle rect, int barricadeLevel)
        {
            barricadeLevel = Math.Min(Rules.BARRICADING_MAX, barricadeLevel);

            for (int x = rect.Left; x < rect.Right; x++)
                for (int y = rect.Top; y < rect.Bottom; y++)
                {
                    DoorWindow door = map.GetMapObjectAt(x, y) as DoorWindow;
                    if (door == null)
                        continue;
                    door.BarricadePoints = barricadeLevel;
                }
        }
        #endregion

        #region Zones
        protected Zone MakeUniqueZone(string basename, Rectangle rect)
        {
            string name = String.Format("{0}@{1}-{2}", basename, rect.Left + rect.Width / 2, rect.Top + rect.Height / 2);
            return new Zone(name, rect);
        }
        #endregion
    }
}
