using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using djack.RogueSurvivor.Gameplay;

namespace djack.RogueSurvivor.Data
{
    [Serializable]
    partial class Actor
    {
        #region Flags
        [Flags]
        enum Flags
        {
            NONE = 0,
            IS_UNIQUE = (1 << 0),
            IS_PROPER_NAME = (1 << 1),
            IS_PLURAL_NAME = (1 << 2),
            IS_DEAD = (1 << 3),
            IS_RUNNING = (1 << 4),
            IS_SLEEPING = (1 << 5)
        }
        #endregion

        #region Fields
        Flags m_Flags;

        #region Definition
        int m_ModelID;
        /*bool m_IsUnique;*/
        int m_FactionID;
        int m_GangID;
        string m_Name;
        /*bool m_IsProperName;
        bool m_IsPluralName;*/
        ActorController m_Controller;
        ActorSheet m_Sheet;
        int m_SpawnTime;
        #endregion

        #region State
        Inventory m_Inventory = null;
        Doll m_Doll;
        Location m_Location;
        int m_ActionPoints;
        int m_LastActionTurn;
        Activity m_Activity = Activity.IDLE;
        Actor m_TargetActor;
        int m_AudioRangeMod;
        int m_Infection;
        Corpse m_DraggedCorpse;
        #endregion
        #endregion

        #region Properties
        #region Definition

        /// <summary>
        /// Gets or sets model. Setting model reset inventory and all stats to the model default values.
        /// </summary>
        public ActorModel Model
        {
            get { return Models.Actors[m_ModelID]; }
            set
            {
                m_ModelID = value.ID;
                OnModelSet();
            }
        }

        public bool IsUnique
        {
            get { return GetFlag(Flags.IS_UNIQUE); }
            set { SetFlag(Flags.IS_UNIQUE, value); }
        }

        public Faction Faction
        {
            get { return Models.Factions[m_FactionID]; }
            set { m_FactionID = value.ID; }
        }

        /// <summary>
        /// Appends "(YOU) " if the actor is the player.
        /// </summary>
        public string Name
        {
            get { return IsPlayer ? "(YOU) " + m_Name : m_Name; }
            set
            {
                m_Name = value;
                if (value != null)
                    m_Name.Replace("(YOU) ", "");
            }
        }

        /// <summary>
        /// Raw name without "(YOU) " for the player.
        /// </summary>
        public string UnmodifiedName
        {
            get { return m_Name; }
        }

        public bool IsProperName
        {
            get { return GetFlag(Flags.IS_PROPER_NAME); }
            set { SetFlag(Flags.IS_PROPER_NAME, value); }
        }

        public bool IsPluralName
        {
            get { return GetFlag(Flags.IS_PLURAL_NAME); }
            set { SetFlag(Flags.IS_PLURAL_NAME, value); }
        }

        public string TheName
        {
            get { return IsProperName || IsPluralName ? Name : "the " + m_Name; }
        }

        public ActorController Controller
        {
            get { return m_Controller; }
            set
            {
                if (m_Controller != null)
                    m_Controller.LeaveControl();
                m_Controller = value;
                if (m_Controller != null)
                    m_Controller.TakeControl(this);
            }
        }

        /// <summary>
        /// Gets if this actor is controlled by the player.
        /// </summary>
        public bool IsPlayer
        {
            get { return m_Controller != null && m_Controller is PlayerController; }
        }

        public int SpawnTime
        {
            get { return m_SpawnTime; }
        }

        public int GangID
        {
            get { return m_GangID; }
            set { m_GangID = value; }
        }

        public bool IsInAGang
        {
            get { return m_GangID != (int)GameGangs.IDs.NONE; }
        }
        #endregion

        #region State
        public Doll Doll
        {
            get { return m_Doll; }
        }

        public bool IsDead
        {
            get { return GetFlag(Flags.IS_DEAD); }
            set { SetFlag(Flags.IS_DEAD, value); }
        }

        public bool IsSleeping
        {
            get { return GetFlag(Flags.IS_SLEEPING); }
            set { SetFlag(Flags.IS_SLEEPING, value); }
        }

        public bool IsRunning
        {
            get { return GetFlag(Flags.IS_RUNNING); }
            set { SetFlag(Flags.IS_RUNNING, value); }
        }

        public Inventory Inventory
        {
            get { return m_Inventory; }
            set { m_Inventory = value; }
        }

        public ActorSheet Sheet
        {
            get { return m_Sheet; }
        }

        public int ActionPoints
        {
            get { return m_ActionPoints; }
            set { m_ActionPoints = value; }
        }

        public int LastActionTurn
        {
            get { return m_LastActionTurn; }
            set { m_LastActionTurn = value; }
        }

        public Location Location
        {
            get { return m_Location; }
            set { m_Location = value; }
        }

        public Activity Activity
        {
            get { return m_Activity; }
            set { m_Activity = value; }
        }

        public Actor TargetActor
        {
            get { return m_TargetActor; }
            set { m_TargetActor = value; }
        }

        public int AudioRange
        {
            get { return m_Sheet.BaseAudioRange + m_AudioRangeMod; }
        }

        public int AudioRangeMod
        {
            get { return m_AudioRangeMod; }
            set { m_AudioRangeMod = value; }
        }

        public int Infection
        {
            get { return m_Infection; }
            set { m_Infection = value; }
        }

        public Corpse DraggedCorpse
        {
            get { return m_DraggedCorpse; }
            set { m_DraggedCorpse = value; }
        }
        #endregion
        #endregion

        #region Init
        public Actor(ActorModel model, Faction faction, string name, bool isProperName, bool isPluralName, int spawnTime)
        {
            if (model == null)
                throw new ArgumentNullException("model");
            if (faction == null)
                throw new ArgumentNullException("faction");
            if (name == null)
                throw new ArgumentNullException("name");

            m_ModelID = model.ID;
            m_FactionID = faction.ID;
            m_GangID = (int)GameGangs.IDs.NONE;
            m_Name = name;
            this.IsProperName = isProperName;
            this.IsPluralName = isPluralName;
            m_Location = new Location();
            m_SpawnTime = spawnTime;
            this.IsUnique = false;
            this.IsDead = false;

            OnModelSet();
        }

        public Actor(ActorModel model, Faction faction, int spawnTime)
            : this(model, faction, model.Name, false, false, spawnTime)
        {
        }

        void OnModelSet()
        {
            ActorModel model = this.Model;

            m_Doll = new Doll(model.DollBody);
            m_Sheet = new ActorSheet(model.StartingSheet);

            // starting points maxed.
            m_ActionPoints = m_Doll.Body.Speed;
            stats.Initialize(m_Sheet);

            // create inventory.
            if (model.Abilities.HasInventory)
                m_Inventory = new Inventory(model.StartingSheet.BaseInventoryCapacity);

            // starting attacks.
            m_CurrentMeleeAttack = model.StartingSheet.UnarmedAttack;
            m_CurrentDefence = model.StartingSheet.BaseDefence;
            m_CurrentRangedAttack = Attack.BLANK;
        }
        #endregion

        #region Equipment helpers
        public Item GetEquippedItem(DollPart part)
        {
            if (m_Inventory == null || part == DollPart.NONE)
                return null;

            foreach (Item it in m_Inventory.Items)
                if (it.EquippedPart == part)
                    return it;

            return null;
        }

        /// <summary>
        /// Assumed to be equiped at Right hand.
        /// </summary>
        /// <returns></returns>
        public Item GetEquippedWeapon()
        {
            return GetEquippedItem(DollPart.RIGHT_HAND);
        }
        #endregion

        #region Flags helpers
        private bool GetFlag(Flags f) { return (m_Flags & f) != 0; }
        private void SetFlag(Flags f, bool value) { if (value) m_Flags |= f; else m_Flags &= ~f; }
        private void OneFlag(Flags f) { m_Flags |= f; }
        private void ZeroFlag(Flags f) { m_Flags &= ~f; }
        #endregion

        #region Pre-save
        public void OptimizeBeforeSaving()
        {
            // remove dead target.
            if (m_TargetActor != null && m_TargetActor.IsDead) m_TargetActor = null;

            // trim.
            if (m_BoringItems != null) m_BoringItems.TrimExcess();
        }
        #endregion
    }
}
