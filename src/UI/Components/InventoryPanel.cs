using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;

using djack.RogueSurvivor.Data;
using djack.RogueSurvivor.Gameplay;
using djack.RogueSurvivor.Engine;
using djack.RogueSurvivor.Engine.Items;

namespace djack.RogueSurvivor.UI.Components
{
    /* This UI component is responsible for rendering the inventory panel */
    class InventoryPanel : UIComponent
    {
        const int INVENTORY_SLOTS_PER_LINE = 10;

        RogueGame game;

        public InventoryPanel(Rectangle area, RogueGame game)
            : base(area)
        {
            this.game = game;
        }

        public override void Draw(IRogueUI ui)
        {
            if(game.Player != null)
                DrawPanel(ui);
        }

        private void DrawPanel(IRogueUI ui)
        {
            Actor player = game.Player;
            if(player.Inventory != null && player.Model.Abilities.HasInventory)
                DrawInventory(ui, player.Inventory, "Inventory", 0);
            Location playerLocation = game.Player.Location;
            DrawInventory(
                ui,
                playerLocation.Map.GetItemsAt(playerLocation.Position),
                "Items on ground",
                64);
            DrawCorpseList(
                ui,
                playerLocation.Map.GetCorpsesAt(playerLocation.Position),
                128);
        }

        private void DrawInventory(IRogueUI ui, Inventory inventory,
                                   string name, int y0)
        {
            SetColor(Color.White);
            DrawStringBold(ui, name, 0, y0 - BOLD_LINE_SPACING);
            if(inventory == null)
                inventory = new Inventory(10);
            for(int item_index = 0; item_index < inventory.MaxCapacity; item_index++)
            {
                Item item = inventory[item_index];
                int slot_x = RogueGame.TILE_SIZE * (item_index % INVENTORY_SLOTS_PER_LINE);
                int slot_y = RogueGame.TILE_SIZE * (item_index / INVENTORY_SLOTS_PER_LINE) + y0;
                DrawSlot(ui, item, slot_x, slot_y);

                // Slot number
                DrawString(
                    ui, (item_index + 1).ToString(),
                    slot_x + 4, slot_y + RogueGame.TILE_SIZE);
            }
        }

        private void DrawCorpseList(IRogueUI ui, List<Corpse> corpseList, int y0)
        {
            SetColor(Color.White);
            int corpseCount = corpseList == null ? 0 : corpseList.Count;
            string title = String.Format("Corpses on ground: {0}", corpseCount);
            DrawStringBold(ui, title, 0, y0 - BOLD_LINE_SPACING);
            if(corpseList == null)
                corpseList = new List<Corpse>();
            for(int item_index = 0; item_index < 10; item_index++)
            {
                Corpse corpse = item_index < corpseList.Count ? corpseList[item_index] : null;
                int slot_x = RogueGame.TILE_SIZE * (item_index % INVENTORY_SLOTS_PER_LINE);
                int slot_y = RogueGame.TILE_SIZE * (item_index / INVENTORY_SLOTS_PER_LINE) + y0;
                DrawCorpse(ui, corpse, slot_x, slot_y);
            }
        }

        private void DrawCorpse(IRogueUI ui, Corpse corpse, int x0, int y0)
        {
            DrawImage(ui, GameImages.ITEM_SLOT, x0, y0);
            if(corpse == null)
                return; // Nothing else to do

            float rotation = corpse.Rotation;
            float scale = corpse.Scale;
            int offset = 0;// TILE_SIZE / 2;

            Actor actor = corpse.DeadGuy;

            x0 += RogueGame.ACTOR_OFFSET + offset;
            y0 += RogueGame.ACTOR_OFFSET + offset;
            
            // model.
            if (actor.Model.ImageID != null)
                DrawImageTransform(ui, actor.Model.ImageID, x0, y0, rotation, scale);

            // skinning/clothing.
            DrawActorDecoration(ui, actor, x0, y0, DollPart.SKIN, rotation, scale);
            DrawActorDecoration(ui, actor, x0, y0, DollPart.FEET,  rotation, scale);
            DrawActorDecoration(ui, actor, x0, y0, DollPart.LEGS, rotation, scale);
            DrawActorDecoration(ui, actor, x0, y0, DollPart.TORSO, rotation, scale);
            DrawActorDecoration(ui, actor, x0, y0, DollPart.TORSO, rotation, scale);
            DrawActorDecoration(ui, actor, x0, y0, DollPart.EYES, rotation, scale);
            DrawActorDecoration(ui, actor, x0, y0, DollPart.HEAD, rotation, scale);

            x0 -= RogueGame.ACTOR_OFFSET + offset;
            y0 -= RogueGame.ACTOR_OFFSET + offset;

            // rotting.
            int rotLevel = game.Rules.CorpseRotLevel(corpse);
            string img = null;
            switch (rotLevel)
            {
                case 5: 
                case 4: 
                case 3: 
                case 2:
                case 1: img = "rot" + rotLevel + "_"; break;
                case 0: break;
                default: throw new Exception("unhandled rot level");
            }
            if (img != null)
            {
                // anim frame.
                img += 1 + (game.Session.WorldTime.TurnCounter % 2);
                // a bit of offset for a nice flies movement effect.
                int rotdx = (game.Session.WorldTime.TurnCounter % 5) - 2;
                int rotdy = ((game.Session.WorldTime.TurnCounter / 3) % 5) - 2;
                DrawImage(ui, img, x0 + rotdx, y0 + rotdy);
            }
        }

        public void DrawActorDecoration(IRogueUI ui, Actor actor, int x0, int y0,
                                        DollPart part,
                                        float rotation, float scale)
        {
            List<string> decos = actor.Doll.GetDecorations(part);
            if (decos == null)
                return;

            foreach (string imageID in decos)
                DrawImageTransform(ui, imageID, x0, y0, rotation, scale);
        }

        private void DrawSlot(IRogueUI ui, Item item, int x0, int y0)
        {
            DrawImage(ui, GameImages.ITEM_SLOT, x0, y0);
            if(item == null)
                return; // Nothing else to do

            if (item.IsEquipped)
                DrawImage(ui, GameImages.ITEM_EQUIPPED, x0, y0);
            if (item is ItemRangedWeapon)
            {
                ItemRangedWeapon w = item as ItemRangedWeapon;
                if (w.Ammo <= 0)
                    DrawImage(ui, GameImages.ICON_OUT_OF_AMMO, x0, y0);
                DrawItemBar(
                    ui,
                    w.Ammo, (w.Model as ItemRangedWeaponModel).MaxAmmo,
                    Color.Blue, x0, y0);
            }
            else if (item is ItemSprayPaint)
            {
                ItemSprayPaint sp = item as ItemSprayPaint;
                DrawItemBar(
                    ui,
                    sp.PaintQuantity, (sp.Model as ItemSprayPaintModel).MaxPaintQuantity,
                    Color.Gold, x0, y0);
            }
            else if (item is ItemSprayScent)
            {
                ItemSprayScent sp = item as ItemSprayScent;
                DrawItemBar(
                    ui,
                    sp.SprayQuantity, (sp.Model as ItemSprayScentModel).MaxSprayQuantity,
                    Color.Cyan, x0, y0);
            }
            else if (item is ItemLight)
            {
                ItemLight lt = item as ItemLight;
                if (lt.Batteries <= 0)
                    DrawImage(ui, GameImages.ICON_OUT_OF_BATTERIES, x0, y0);
                DrawItemBar(
                    ui,
                    lt.Batteries, (lt.Model as ItemLightModel).MaxBatteries,
                    Color.Yellow, x0, y0);
            }
            else if (item is ItemTracker)
            {
                ItemTracker tr = item as ItemTracker;
                if (tr.Batteries <= 0)
                    DrawImage(ui, GameImages.ICON_OUT_OF_BATTERIES, x0, y0);
                DrawItemBar(
                    ui,
                    tr.Batteries, (tr.Model as ItemTrackerModel).MaxBatteries,
                    Color.Pink, x0, y0);
            }
            else if (item is ItemFood)
            {
                ItemFood food = item as ItemFood;
                if (game.Rules.IsFoodExpired(food, game.Session.WorldTime.TurnCounter))
                    DrawImage(ui, GameImages.ICON_EXPIRED_FOOD, x0, y0);
                else if (game.Rules.IsFoodSpoiled(food, game.Session.WorldTime.TurnCounter))
                    DrawImage(ui, GameImages.ICON_SPOILED_FOOD, x0, y0);
            }
            else if (item is ItemTrap)
            {
                ItemTrap trap = item as ItemTrap;
                if (trap.IsTriggered)
                    DrawImage(ui, GameImages.ICON_TRAP_TRIGGERED, x0, y0);
                else if (trap.IsActivated)
                    DrawImage(ui, GameImages.ICON_TRAP_ACTIVATED, x0, y0);
            }
            else if (item is ItemEntertainment)
            {
                if (game.Player !=null && game.Player.IsBoredOf(item))
                    DrawImage(ui, GameImages.ICON_BORING_ITEM, x0, y0);
            }
            DrawItem(ui, item, x0, y0);
        }

        private void DrawItemBar(IRogueUI ui, int value, int maxValue, Color color, int x0, int y0)
        {
            Rectangle itemBar = MakeItemBar(x0, y0);
            int split = (int)(itemBar.Width * ((float)value / (float)maxValue));
            DrawSplitBar(ui, itemBar, split, color, Color.DarkGray);
        }

        private Rectangle MakeItemBar(int x0, int y0)
        {
            return new Rectangle(2 + x0, 27 + y0, 28, 3);
        }

        private void DrawItem(IRogueUI ui, Item item, int x0, int y0)
        {
            Color tint = Color.White; // Might need to get rid of this
            DrawImage(ui, item.ImageID, x0, y0, tint);

            if (item.Model.IsStackable)
            {
                string q = string.Format("{0}", item.Quantity);
                int tx = x0 + RogueGame.TILE_SIZE - 10;
                if (item.Quantity > 100)
                    tx -= 10;
                else if (item.Quantity > 10)
                    tx -= 4;
                DrawString(ui, Color.DarkGray, q, tx + 1, y0 + 1);
                DrawString(ui, Color.White, q, tx, y0);
            }
            if (item is ItemTrap)
            {
                ItemTrap trap = item as ItemTrap;
                if (trap.IsTriggered)
                    DrawImage(ui, GameImages.ICON_TRAP_TRIGGERED, x0, y0);
                else if (trap.IsActivated)
                    DrawImage(ui, GameImages.ICON_TRAP_ACTIVATED, x0, y0);
            }
        }
    }
}