using System;

using djack.RogueSurvivor.Data;
using djack.RogueSurvivor.Engine;

namespace djack.RogueSurvivor.Gameplay.Generators
{
    partial class BaseTownGenerator
    {
        public struct Parameters
        {
            #region Fields
            int m_MapWidth;
            int m_MapHeight;
            int m_MinBlockSize;
            int m_WreckedCarChance;
            int m_CHARBuildingChance;
            int m_ShopBuildingChance;
            int m_ParkBuildingChance;
            int m_PostersChance;
            int m_TagsChance;
            int m_ItemInShopShelfChance;
            int m_PolicemanChance;
            #endregion

            #region Properties
            /// <summary>
            /// District the map is currently generated in.
            /// </summary>
            public District District
            {
                get;
                set;
            }

            /// <summary>
            /// Do we need to generate the Police Station in this district?
            /// </summary>
            public bool GeneratePoliceStation
            {
                get;
                set;
            }

            /// <summary>
            /// Do we need to generate the Hospital in this district?
            /// </summary>
            public bool GenerateHospital
            {
                get;
                set;
            }

            public int MapWidth
            {
                get { return m_MapWidth; }
                set
                {
                    if (value <= 0 || value > RogueGame.MAP_MAX_WIDTH) throw new ArgumentOutOfRangeException("MapWidth");
                    m_MapWidth = value;
                }
            }

            public int MapHeight
            {
                get { return m_MapHeight; }
                set
                {
                    if (value <= 0 || value > RogueGame.MAP_MAX_WIDTH) throw new ArgumentOutOfRangeException("MapHeight");
                    m_MapHeight = value;
                }
            }

            public int MinBlockSize
            {
                get { return m_MinBlockSize; }
                set
                {
                    if (value < 4 || value > 32) throw new ArgumentOutOfRangeException("MinBlockSize must be [4..32]");
                    m_MinBlockSize = value;
                }
            }

            public int WreckedCarChance
            {
                get { return m_WreckedCarChance; }
                set
                {
                    if (value < 0 || value > 100) throw new ArgumentOutOfRangeException("WreckedCarChance must be [0..100]");
                    m_WreckedCarChance = value;
                }
            }

            public int ShopBuildingChance
            {
                get { return m_ShopBuildingChance; }
                set
                {
                    if (value < 0 || value > 100) throw new ArgumentOutOfRangeException("ShopBuildingChance must be [0..100]");
                    m_ShopBuildingChance = value;
                }
            }

            public int ParkBuildingChance
            {
                get { return m_ParkBuildingChance; }
                set
                {
                    if (value < 0 || value > 100) throw new ArgumentOutOfRangeException("ParkBuildingChance must be [0..100]");
                    m_ParkBuildingChance = value;
 
                }
            }

            public int CHARBuildingChance
            {
                get { return m_CHARBuildingChance; }
                set
                {
                    if (value < 0 || value > 100) throw new ArgumentOutOfRangeException("CHARBuildingChance must be [0..100]");
                    m_CHARBuildingChance = value;

                }
            }

            public int PostersChance
            {
                get { return m_PostersChance; }
                set
                {
                    if (value < 0 || value > 100) throw new ArgumentOutOfRangeException("PostersChance must be [0..100]");
                    m_PostersChance = value;

                }
            }

            public int TagsChance
            {
                get { return m_TagsChance; }
                set
                {
                    if (value < 0 || value > 100) throw new ArgumentOutOfRangeException("TagsChance must be [0..100]");
                    m_TagsChance = value;

                }
            }

            public int ItemInShopShelfChance
            {
                get { return m_ItemInShopShelfChance; }
                set
                {
                    if (value < 0 || value > 100) throw new ArgumentOutOfRangeException("ItemInShopShelfChance must be [0..100]");
                    m_ItemInShopShelfChance = value;

                }
            }


            public int PolicemanChance
            {
                get { return m_PolicemanChance; }
                set
                {
                    if (value < 0 || value > 100) throw new ArgumentOutOfRangeException("PolicemanChance must be [0..100]");
                    m_PolicemanChance = value;

                }
            }
            #endregion
        }
    }
}