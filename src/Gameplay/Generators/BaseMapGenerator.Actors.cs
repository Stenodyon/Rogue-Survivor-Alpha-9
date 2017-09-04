using djack.RogueSurvivor.Engine;
using djack.RogueSurvivor.Data;

namespace djack.RogueSurvivor.Gameplay.Generators
{
    /* This part of BaseMapGenerator contains actor generation helpers */
    partial class BaseMapGenerator
    {
        static readonly string[] MALE_SKINS = new string[] { GameImages.MALE_SKIN1, GameImages.MALE_SKIN2, GameImages.MALE_SKIN3, GameImages.MALE_SKIN4, GameImages.MALE_SKIN5 };
        static readonly string[] MALE_HEADS = new string[] { GameImages.MALE_HAIR1, GameImages.MALE_HAIR2, GameImages.MALE_HAIR3, GameImages.MALE_HAIR4, GameImages.MALE_HAIR5, GameImages.MALE_HAIR6, GameImages.MALE_HAIR7, GameImages.MALE_HAIR8 };
        static readonly string[] MALE_TORSOS = new string[] { GameImages.MALE_SHIRT1, GameImages.MALE_SHIRT2, GameImages.MALE_SHIRT3, GameImages.MALE_SHIRT4, GameImages.MALE_SHIRT5 };
        static readonly string[] MALE_LEGS = new string[] { GameImages.MALE_PANTS1, GameImages.MALE_PANTS2, GameImages.MALE_PANTS3, GameImages.MALE_PANTS4, GameImages.MALE_PANTS5 };
        static readonly string[] MALE_SHOES = new string[] { GameImages.MALE_SHOES1, GameImages.MALE_SHOES2, GameImages.MALE_SHOES3 };
        static readonly string[] MALE_EYES = new string[] { GameImages.MALE_EYES1, GameImages.MALE_EYES2, GameImages.MALE_EYES3, GameImages.MALE_EYES4, GameImages.MALE_EYES5, GameImages.MALE_EYES6 };

        static readonly string[] FEMALE_SKINS = new string[] { GameImages.FEMALE_SKIN1, GameImages.FEMALE_SKIN2, GameImages.FEMALE_SKIN3, GameImages.FEMALE_SKIN4, GameImages.FEMALE_SKIN5 };
        static readonly string[] FEMALE_HEADS = new string[] { GameImages.FEMALE_HAIR1, GameImages.FEMALE_HAIR2, GameImages.FEMALE_HAIR3, GameImages.FEMALE_HAIR4, GameImages.FEMALE_HAIR5, GameImages.FEMALE_HAIR6, GameImages.FEMALE_HAIR7 };
        static readonly string[] FEMALE_TORSOS = new string[] { GameImages.FEMALE_SHIRT1, GameImages.FEMALE_SHIRT2, GameImages.FEMALE_SHIRT3, GameImages.FEMALE_SHIRT4 };
        static readonly string[] FEMALE_LEGS = new string[] { GameImages.FEMALE_PANTS1, GameImages.FEMALE_PANTS2, GameImages.FEMALE_PANTS3, GameImages.FEMALE_PANTS4, GameImages.FEMALE_PANTS5 };
        static readonly string[] FEMALE_SHOES = new string[] { GameImages.FEMALE_SHOES1, GameImages.FEMALE_SHOES2, GameImages.FEMALE_SHOES3 };
        static readonly string[] FEMALE_EYES = new string[] { GameImages.FEMALE_EYES1, GameImages.FEMALE_EYES2, GameImages.FEMALE_EYES3, GameImages.FEMALE_EYES4, GameImages.FEMALE_EYES5, GameImages.FEMALE_EYES6 };

        static readonly string[] BIKER_HEADS = new string[] { GameImages.BIKER_HAIR1, GameImages.BIKER_HAIR2, GameImages.BIKER_HAIR3 };
        static readonly string[] BIKER_LEGS = new string[] { GameImages.BIKER_PANTS };
        static readonly string[] BIKER_SHOES = new string[] { GameImages.BIKER_SHOES };

        static readonly string[] CHARGUARD_HEADS = new string[] { GameImages.CHARGUARD_HAIR };
        static readonly string[] CHARGUARD_LEGS = new string[] { GameImages.CHARGUARD_PANTS };

        static readonly string[] DOG_SKINS = new string[] { GameImages.DOG_SKIN1, GameImages.DOG_SKIN2, GameImages.DOG_SKIN3 };

        public void DressCivilian(DiceRoller roller, Actor actor)
        {
            if (actor.Model.DollBody.IsMale)
                DressCivilian(roller, actor, MALE_EYES, MALE_SKINS, MALE_HEADS, MALE_TORSOS, MALE_LEGS, MALE_SHOES);
            else
                DressCivilian(roller, actor, FEMALE_EYES, FEMALE_SKINS, FEMALE_HEADS, FEMALE_TORSOS, FEMALE_LEGS, FEMALE_SHOES);
        }

        public void SkinNakedHuman(DiceRoller roller, Actor actor)
        {
            if (actor.Model.DollBody.IsMale)
                SkinNakedHuman(roller, actor, MALE_EYES, MALE_SKINS, MALE_HEADS);
            else
                SkinNakedHuman(roller, actor, FEMALE_EYES, FEMALE_SKINS, FEMALE_HEADS);
        }

        public void DressCivilian(DiceRoller roller, Actor actor, string[] eyes, string[] skins, string[] heads, string[] torsos, string[] legs, string[] shoes)
        {
            actor.Doll.RemoveAllDecorations();
            actor.Doll.AddDecoration(DollPart.EYES, eyes[roller.Roll(0, eyes.Length)]);
            actor.Doll.AddDecoration(DollPart.SKIN, skins[roller.Roll(0, skins.Length)]);
            actor.Doll.AddDecoration(DollPart.HEAD, heads[roller.Roll(0, heads.Length)]);
            actor.Doll.AddDecoration(DollPart.TORSO, torsos[roller.Roll(0, torsos.Length)]);
            actor.Doll.AddDecoration(DollPart.LEGS, legs[roller.Roll(0, legs.Length)]);
            actor.Doll.AddDecoration(DollPart.FEET, shoes[roller.Roll(0, shoes.Length)]);
        }

        public void SkinNakedHuman(DiceRoller roller, Actor actor, string[] eyes, string[] skins, string[] heads)
        {
            actor.Doll.RemoveAllDecorations();
            actor.Doll.AddDecoration(DollPart.EYES, eyes[roller.Roll(0, eyes.Length)]);
            actor.Doll.AddDecoration(DollPart.SKIN, skins[roller.Roll(0, skins.Length)]);
            actor.Doll.AddDecoration(DollPart.HEAD, heads[roller.Roll(0, heads.Length)]);
        }

        public void SkinDog(DiceRoller roller, Actor actor)
        {
            actor.Doll.RemoveAllDecorations();
            actor.Doll.AddDecoration(DollPart.SKIN, DOG_SKINS[roller.Roll(0, DOG_SKINS.Length)]);
        }

        public void DressArmy(DiceRoller roller, Actor actor)
        {
            actor.Doll.RemoveAllDecorations();
            actor.Doll.AddDecoration(DollPart.SKIN, MALE_SKINS[roller.Roll(0, MALE_SKINS.Length)]);
            actor.Doll.AddDecoration(DollPart.HEAD, GameImages.ARMY_HELMET);
            actor.Doll.AddDecoration(DollPart.TORSO, GameImages.ARMY_SHIRT);
            actor.Doll.AddDecoration(DollPart.LEGS, GameImages.ARMY_PANTS);
            actor.Doll.AddDecoration(DollPart.FEET, GameImages.ARMY_SHOES);
        }

        public void DressPolice(DiceRoller roller, Actor actor)
        {
            actor.Doll.RemoveAllDecorations();
            actor.Doll.AddDecoration(DollPart.EYES, MALE_EYES[roller.Roll(0, MALE_EYES.Length)]);
            actor.Doll.AddDecoration(DollPart.SKIN, MALE_SKINS[roller.Roll(0, MALE_SKINS.Length)]);
            actor.Doll.AddDecoration(DollPart.HEAD, MALE_HEADS[roller.Roll(0, MALE_HEADS.Length)]);
            actor.Doll.AddDecoration(DollPart.HEAD, GameImages.POLICE_HAT);
            actor.Doll.AddDecoration(DollPart.TORSO, GameImages.POLICE_UNIFORM);
            actor.Doll.AddDecoration(DollPart.LEGS, GameImages.POLICE_PANTS);
            actor.Doll.AddDecoration(DollPart.FEET, GameImages.POLICE_SHOES);
        }

        public void DressBiker(DiceRoller roller, Actor actor)
        {
            actor.Doll.RemoveAllDecorations();
            actor.Doll.AddDecoration(DollPart.EYES, MALE_EYES[roller.Roll(0, MALE_EYES.Length)]);
            actor.Doll.AddDecoration(DollPart.SKIN, MALE_SKINS[roller.Roll(0, MALE_SKINS.Length)]);
            actor.Doll.AddDecoration(DollPart.HEAD, BIKER_HEADS[roller.Roll(0, BIKER_HEADS.Length)]);
            actor.Doll.AddDecoration(DollPart.LEGS, BIKER_LEGS[roller.Roll(0, BIKER_LEGS.Length)]);
            actor.Doll.AddDecoration(DollPart.FEET, BIKER_SHOES[roller.Roll(0, BIKER_SHOES.Length)]);
        }

        public void DressGangsta(DiceRoller roller, Actor actor)
        {
            actor.Doll.RemoveAllDecorations();
            actor.Doll.AddDecoration(DollPart.EYES, MALE_EYES[roller.Roll(0, MALE_EYES.Length)]);
            actor.Doll.AddDecoration(DollPart.SKIN, MALE_SKINS[roller.Roll(0, MALE_SKINS.Length)]);
            actor.Doll.AddDecoration(DollPart.TORSO, GameImages.GANGSTA_SHIRT);
            actor.Doll.AddDecoration(DollPart.HEAD, MALE_HEADS[roller.Roll(0, MALE_HEADS.Length)]);
            actor.Doll.AddDecoration(DollPart.HEAD, GameImages.GANGSTA_HAT);
            actor.Doll.AddDecoration(DollPart.LEGS, GameImages.GANGSTA_PANTS);
            actor.Doll.AddDecoration(DollPart.FEET, MALE_SHOES[roller.Roll(0, MALE_SHOES.Length)]);
        }
        
        public void DressCHARGuard(DiceRoller roller, Actor actor)
        {
            actor.Doll.RemoveAllDecorations();
            actor.Doll.AddDecoration(DollPart.EYES, MALE_EYES[roller.Roll(0, MALE_EYES.Length)]);
            actor.Doll.AddDecoration(DollPart.SKIN, MALE_SKINS[roller.Roll(0, MALE_SKINS.Length)]);
            actor.Doll.AddDecoration(DollPart.HEAD, CHARGUARD_HEADS[roller.Roll(0, CHARGUARD_HEADS.Length)]);
            actor.Doll.AddDecoration(DollPart.LEGS, CHARGUARD_LEGS[roller.Roll(0, CHARGUARD_LEGS.Length)]);
        }

        public void DressBlackOps(DiceRoller roller, Actor actor)
        {
            actor.Doll.RemoveAllDecorations();
            actor.Doll.AddDecoration(DollPart.EYES, MALE_EYES[roller.Roll(0, MALE_EYES.Length)]);
            actor.Doll.AddDecoration(DollPart.SKIN, MALE_SKINS[roller.Roll(0, MALE_SKINS.Length)]);
            actor.Doll.AddDecoration(DollPart.TORSO, GameImages.BLACKOP_SUIT);
        }

        public string RandomSkin(DiceRoller roller, bool isMale)
        {
            string[] skins = isMale ? MALE_SKINS : FEMALE_SKINS;
            return skins[roller.Roll(0, skins.Length)];
        }

        static readonly string[] MALE_FIRST_NAMES = 
        {
            "Alan", "Albert", "Alex", "Alexander", "Andrew", "Andy", "Anton", "Anthony", "Ashley", "Axel",
            "Ben", "Bill", "Bob", "Brad", "Brandon", "Brian", "Bruce",
            "Caine", "Carl", "Carlton", "Charlie", "Clark", "Cody", "Cris", "Cristobal",
            "Dan", "Danny", "Dave", "David", "Dirk", "Don", "Donovan", "Doug", "Dustin",
            "Ed", "Eddy", "Edward", "Elias", "Elie", "Elmer", "Elton", "Eric", "Eugene",
            "Francis", "Frank", "Fred",
            "Garry", "Georges", "Greg", "Guy", "Gordon",
            "Hank", "Harold", "Harvey", "Henry", "Hubert",
            "Indy",
            "Jack", "Jake", "James", "Jarvis", "Jason", "Jeff", "Jeffrey", "Jeremy", "Jessie", "Jesus", "Jim", "John", "Johnny", "Jonas", "Joseph", "Julian",
            "Karl", "Keith", "Ken", 
            "Larry", "Lars", "Lee", "Lennie", "Lewis",
            "Mark", "Mathew", "Max", "Michael", "Mickey", "Mike", "Mitch",
            "Ned", "Neil", "Nick", "Norman",
            "Oliver", "Orlando", "Oscar",
            "Pablo", "Patrick", "Pete", "Peter", "Phil", "Philip", "Preston",
            "Quentin",
            "Randy", "Rick", "Rob", "Ron", "Ross", "Robert", "Roberto", "Rudy", "Ryan",
            "Sam", "Samuel", "Saul", "Scott", "Shane", "Shaun", "Stan", "Stanley", "Stephen", "Steve", "Stuart",
            "Ted", "Tim", "Toby", "Tom", "Tommy", "Tony", "Travis", "Trevor",
            "Ulrich",
            "Val", "Vince", "Vincent", "Vinnie",
            "Walter", "Wayne",
            "Xavier"
            // Y
            // Z
        };

        static readonly string[] FEMALE_FIRST_NAMES = 
        {
            "Abigail", "Amanda", "Ali", "Alice", "Alicia", "Alison", "Amy", "Angela", "Ann", "Annie", "Audrey",
            "Belinda", "Beth", "Brenda",
            "Carla", "Carolin", "Carrie", "Cassie", "Cherie", "Cheryl", "Claire", "Connie", "Cris", "Crissie", "Christina",
            "Dana", "Debbie", "Deborah", "Debrah", "Diana", "Dona",
            "Elayne", "Eleonor", "Elizabeth", "Ester",
            "Felicia", "Fiona", "Fran",
            "Gina", "Ginger", "Gloria", "Grace",
            "Helen", "Helena", "Hilary", "Holy",
            "Ingrid", "Isabela",
            "Jackie", "Jennifer", "Jess", "Jill", "Joana",
            "Kate", "Kathleen", "Kathy", "Katrin", "Kim", "Kira",
            "Leonor", "Leslie", "Linda", "Lindsay", "Lisa", "Liz", "Lorraine", "Lucia", "Lucy",
            "Maggie", "Margareth", "Maria", "Mary", "Mary-Ann", "Marylin", "Michelle", "Millie", "Molly", "Monica",
            "Nancy",
            "Ophelia",
            "Paquita", "Page", "Patricia", "Patty", "Paula",
            // Q
            "Rachel", "Raquel", "Regina", "Roberta", "Ruth",
            "Sabrina", "Samantha", "Sandra", "Sarah", "Sofia", "Sue", "Susan",
            "Tabatha", "Tanya", "Teresa", "Tess", "Tifany", "Tori",
            // U
            "Veronica", "Victoria", "Vivian",
            "Wendy", "Winona",
            // X
            // Y
            "Zora"
        };

        static readonly string[] LAST_NAMES = 
        {
            "Anderson", "Austin",
            "Bent", "Black", "Bradley", "Brown", "Bush",
            "Carpenter", "Carter", "Collins", "Cordell",
            "Dobbs",
            "Engels",
            "Finch", "Ford", "Forrester",
            "Gates",
            "Hewlett", "Holtz",
            "Irvin",
            "Jones",
            "Kennedy",
            "Lambert", "Lesaint", "Lee", "Lewis",
            "McAllister", "Malory", "McGready",
            "Norton",
            "O'Brien", "Oswald",
            "Patterson", "Paul", "Pitt",
            "Quinn",
            "Ramirez", "Reeves", "Rockwell", "Rogers", "Robertson",
            "Sanchez", "Smith", "Stevens", "Steward",
            "Tarver", "Taylor",
            "Ulrich",
            "Vance",
            "Washington", "Walters", "White"
            // X
            // Y
            // Z
        };

        public void GiveNameToActor(DiceRoller roller, Actor actor)
        {
            if (actor.Model.DollBody.IsMale)
                GiveNameToActor(roller, actor, MALE_FIRST_NAMES, LAST_NAMES);
            else
                GiveNameToActor(roller, actor, FEMALE_FIRST_NAMES, LAST_NAMES);
        }

        public void GiveNameToActor(DiceRoller roller, Actor actor, string[] firstNames, string[] lastNames)
        {
            actor.IsProperName = true;
            string randomName = firstNames[roller.Roll(0, firstNames.Length)] + " " + lastNames[roller.Roll(0, lastNames.Length)];
            actor.Name = randomName;
        }

        public void GiveRandomSkillsToActor(DiceRoller roller, Actor actor, int count)
        {
            for (int i = 0; i < count; i++)
                GiveRandomSkillToActor(roller, actor);
        }

        public void GiveRandomSkillToActor(DiceRoller roller, Actor actor)
        {
            Skills.IDs randomID;
            if (actor.Model.Abilities.IsUndead)
                randomID = Skills.RollUndead(roller);
            else
                randomID = Skills.RollLiving(roller);
            GiveStartingSkillToActor(actor, randomID);
        }

        public void GiveStartingSkillToActor(Actor actor, Skills.IDs skillID)
        {
            if (actor.Sheet.SkillTable.GetSkillLevel((int)skillID) >= Skills.MaxSkillLevel(skillID))
                return;

            actor.Sheet.SkillTable.AddOrIncreaseSkill((int)skillID);

            // recompute starting stats.
            RecomputeActorStartingStats(actor);
        }

        public void RecomputeActorStartingStats(Actor actor)
        {
            actor.HitPoints = m_Rules.ActorMaxHPs(actor);
            actor.StaminaPoints = m_Rules.ActorMaxSTA(actor);
            actor.FoodPoints = m_Rules.ActorMaxFood(actor);
            actor.SleepPoints = m_Rules.ActorMaxSleep(actor);
            actor.Sanity = m_Rules.ActorMaxSanity(actor);
            if (actor.Inventory != null)
                actor.Inventory.MaxCapacity = m_Rules.ActorMaxInv(actor);
        }
    }
}