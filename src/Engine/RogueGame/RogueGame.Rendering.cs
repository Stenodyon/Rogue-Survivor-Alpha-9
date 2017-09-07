using System;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Collections.Generic;

using djack.RogueSurvivor.Data;
using djack.RogueSurvivor.Engine.MapObjects;
using djack.RogueSurvivor.Engine.Items;
using djack.RogueSurvivor.Gameplay;
using djack.RogueSurvivor.UI.Components;

namespace djack.RogueSurvivor.Engine
{
    /* This part of RogueGame handles rendering (to refactor into an actual
    UI system) */
    partial class RogueGame
    {
#region View
        public void ComputeViewRect(Point mapCenter)
        {
            int left = mapCenter.X - HALF_VIEW_WIDTH;
            int right = mapCenter.X + HALF_VIEW_WIDTH;

            int top = mapCenter.Y - HALF_VIEW_HEIGHT;
            int bottom = mapCenter.Y + HALF_VIEW_HEIGHT;

            m_MapViewRect = new Rectangle(left, top, 1 + right - left, 1 + bottom - top);
        }

        public bool IsInViewRect(Point mapPosition)
        {
            return m_MapViewRect.Contains(mapPosition);
        }
#endregion

        public void RedrawPlayScreen()
        {
            // get mutex.
            Monitor.Enter(m_UI);

            m_UI.UI_Clear(Color.Black);
            {
                // map & minimap
                Color mapTint = Color.White; // disabled changing brightness bad for the eyes TintForDayPhase(m_Session.WorldTime.Phase);
                m_UI.UI_DrawLine(Color.DarkGray, RIGHTPANEL_X, 0, RIGHTPANEL_X, MESSAGES_Y);
                DrawMap(m_Session.CurrentMap, mapTint);

                m_UI.UI_DrawLine(Color.DarkGray, RIGHTPANEL_X, MINIMAP_Y - 4, CANVAS_WIDTH, MINIMAP_Y - 4);
                DrawMiniMap(m_Session.CurrentMap);

                // messages
                m_UI.UI_DrawLine(Color.DarkGray, MESSAGES_X, MESSAGES_Y - 1, CANVAS_WIDTH, MESSAGES_Y - 1);
                //m_MessageManager.Draw(m_UI, m_Session.LastTurnPlayerActed, MESSAGES_X, MESSAGES_Y);

                gameLayout.Draw(m_UI);

                // character skills.
                //if (m_Player != null && m_Player.Sheet.SkillTable != null && m_Player.Sheet.SkillTable.CountSkills > 0)
                //    DrawActorSkillTable(m_Player, RIGHTPANEL_TEXT_X, SKILLTABLE_Y);

                // overlays
                Monitor.Enter(m_Overlays);
                foreach (Overlay o in m_Overlays)
                    o.Draw(m_UI);
                Monitor.Exit(m_Overlays);

                // DEV STATS
                #if DEBUG
                if (s_Options.DEV_ShowActorsStats)
                {
                    int countLiving, countUndead;
                    countLiving = CountLivings(m_Session.CurrentMap);
                    countUndead = CountUndeads(m_Session.CurrentMap);
                    m_UI.UI_DrawString(Color.White, String.Format("Living {0} vs {1} Undead", countLiving, countUndead), RIGHTPANEL_TEXT_X, SKILLTABLE_Y - 32);
                }
                #endif
            }

            m_UI.UI_Repaint();

            // release mutex.
            Monitor.Exit(m_UI);
        }

        /// <summary>
        /// OBSOLETE
        /// </summary>
        /// <param name="phase"></param>
        /// <returns></returns>
        Color TintForDayPhase(DayPhase phase)
        {
            switch (phase)
            {
                case DayPhase.MORNING:
                case DayPhase.MIDDAY:
                case DayPhase.AFTERNOON:
                    return TINT_DAY;

                case DayPhase.SUNRISE:
                    return TINT_SUNRISE;

                case DayPhase.SUNSET:
                    return TINT_SUNSET;

                case DayPhase.MIDNIGHT:
                    return TINT_MIDNIGHT;

                case DayPhase.DEEP_NIGHT:
                    return TINT_NIGHT;

                case DayPhase.EVENING:
                    return TINT_EVENING;
                default:
                    throw new ArgumentOutOfRangeException("unhandled dayphase");
            }
        }

#region Drawing elements
        public void DrawMap(Map map, Color tint)
        {
            // trim to outer map bounds.
            int left = Math.Max(-1, m_MapViewRect.Left);
            int right = Math.Min(map.Width + 1, m_MapViewRect.Right);
            int top = Math.Max(-1, m_MapViewRect.Top);
            int bottom = Math.Min(map.Height + 1, m_MapViewRect.Bottom);

            // get weather image.
            string weatherImage;
            switch (m_Session.World.Weather)
            {
                case Weather.RAIN:
                    weatherImage = (m_Session.WorldTime.TurnCounter % 2 == 0 ? GameImages.WEATHER_RAIN1 : GameImages.WEATHER_RAIN2);
                    break;
                case Weather.HEAVY_RAIN:
                    weatherImage = (m_Session.WorldTime.TurnCounter % 2 == 0 ? GameImages.WEATHER_HEAVY_RAIN1 : GameImages.WEATHER_HEAVY_RAIN2);
                    break;

                default:
                    weatherImage = null;
                    break;
            }

            ///////////////////////////////////////////
            // Layered draw:
            // 1. Tiles.
            // 2. Corpses.
            // 3. (Target statut), Map objects.
            // 4. Scents.
            // 5. Items, Actors (if visible).
            // 6. Water cover.
            // 7. Weather (if visible and not inside).
            ///////////////////////////////////////////
            Point position = new Point();
            bool isUndead = m_Player.Model.Abilities.IsUndead;
            bool hasSmell = m_Player.Model.StartingSheet.BaseSmellRating > 0;
            int playerSmellTheshold = m_Rules.ActorSmellThreshold(m_Player);
            for (int x = left; x < right; x++)
            {
                position.X = x;
                for (int y = top; y < bottom; y++)
                {
                    position.Y = y;
                    Point toScreen = MapToScreen(x, y);
                    bool isVisible = IsVisibleToPlayer(map, position);
                    bool drawWater = false;
                    Tile tile = map.IsInBounds(x, y) ? map.GetTileAt(x, y) : null;

                    // 1. Tile
                    if (map.IsInBounds(x, y))
                        DrawTile(tile, toScreen, tint);
                    else if (map.IsMapBoundary(x, y))
                    {
                        if(map.GetExitAt(position) != null)
                            DrawExit(toScreen);
                    }
                    #if DEBUG
                    DrawTileDev(map, tile, x, y, toScreen);
                    #endif

                    // 2. Corpses
                    if (isVisible)
                    {
                        List<Corpse> corpses = map.GetCorpsesAt(x, y);
                        if (corpses != null)
                        {
                            foreach (Corpse c in corpses)
                                DrawCorpse(c, toScreen.X, toScreen.Y, tint);
                        }
                    }

                    // 3. (TargetStatus), Map objects
                    if (s_Options.ShowPlayerTargets && !m_Player.IsSleeping && m_Player.Location.Position == position)
                        DrawPlayerActorTargets(m_Player);
                    MapObject mapObj = map.GetMapObjectAt(x, y);
                    if (mapObj != null)
                    {
                        DrawMapObject(mapObj, toScreen, tint);
                        drawWater = true;
                    }

                    // 4. Scents
                    #region
                    if (!m_Player.IsSleeping && map.IsInBounds(x, y) && m_Rules.GridDistance(m_Player.Location.Position, position) <= 1)
                    {
                        // scents alpha is low to be able to see objects behind them (eg: scent on a door)
                        // squaring alpha helps increase discrimination for player.

                        if (isUndead)
                        {
                            // Undead can see living & zm scents.
                            if (hasSmell)
                            {
                                // living scent?
                                int livingScent = map.GetScentByOdorAt(Odor.LIVING, position);
                                if (livingScent >= playerSmellTheshold)
                                {
                                    float alpha = 0.90f * (float)livingScent / (float)OdorScent.MAX_STRENGTH;
                                    alpha *= alpha;
                                    m_UI.UI_DrawTransparentImage(alpha, GameImages.ICON_SCENT_LIVING, toScreen.X, toScreen.Y);
                                }

                                // zombie master scent?
                                int masterScent = map.GetScentByOdorAt(Odor.UNDEAD_MASTER, position);
                                if (masterScent >= playerSmellTheshold)
                                {
                                    float alpha = 0.90f * (float)masterScent / (float)OdorScent.MAX_STRENGTH;
                                    alpha *= alpha;
                                    m_UI.UI_DrawTransparentImage(alpha, GameImages.ICON_SCENT_ZOMBIEMASTER, toScreen.X, toScreen.Y);
                                }
                            }
                        }
                        else
                        {
                            // Living can see some perfumes.
                            // perfume: living suppressor?
                            int livingSupr = map.GetScentByOdorAt(Odor.PERFUME_LIVING_SUPRESSOR, position);
                            if (livingSupr > 0)
                            {
                                float alpha = 0.90f * (float)livingSupr / (float)OdorScent.MAX_STRENGTH;
                                //alpha *= alpha;
                                m_UI.UI_DrawTransparentImage(alpha, GameImages.ICON_SCENT_LIVING_SUPRESSOR, toScreen.X, toScreen.Y);
                            }
                        }
                    }
                    #endregion

                    // 5. Items, Actors (if visible)
                    if (isVisible)
                    {
                        // 4.2. Items
                        Inventory inv = map.GetItemsAt(x, y);
                        if (inv != null)
                        {
                            DrawItemsStack(inv, toScreen.X, toScreen.Y, tint);
                            drawWater = true;
                        }

                        // 4.3. Actors
                        Actor actor = map.GetActorAt(x, y);
                        if (actor != null)
                        {
                            DrawActorSprite(actor, toScreen, tint);
                            drawWater = true;
                        }
                    }

                    // 6. Water cover.
                    if (tile != null && tile.HasDecorations)
                        drawWater = true;
                    if (drawWater && tile.Model.IsWater)
                        DrawTileWaterCover(tile, toScreen, tint);


                    // 7. Weather (if visible and not inside).
                    if (isVisible && weatherImage != null && tile != null && !tile.IsInside)
                        m_UI.UI_DrawImage(weatherImage, toScreen.X, toScreen.Y);
                }
            }

            // DEV: scents
                #if false
                for (int x = left; x < right; x++)
                    for (int y = top; y < bottom; y++)
                {
                    if (map.IsInBounds(x, y))
                    {
                        int scent = map.GetScentByOdorAt(Odor.LIVING, new Point(x, y));
                        if (scent > 0)
                        {
                            m_UI.UI_DrawString(Color.White, String.Format("{0}", scent), MapToScreen(x, y).X, MapToScreen(x, y).Y);
                        }
                    }
                }
                #endif
        }

        string MovingWaterImage(TileModel model, int turnCount)
        {
            if (model == m_GameTiles.FLOOR_SEWER_WATER)
            {
                int i = turnCount % 3;
                switch (i)
                {
                    case 0: return GameImages.TILE_FLOOR_SEWER_WATER_ANIM1;
                    case 1: return GameImages.TILE_FLOOR_SEWER_WATER_ANIM2;
                    default: return GameImages.TILE_FLOOR_SEWER_WATER_ANIM3;
                }
            }

            return null;
        }

        public void DrawTile(Tile tile, Point screen, Color tint)
        {
            if (tile.IsInView)  // visible
            {
                // tile.
                m_UI.UI_DrawImage(tile.Model.ImageID, screen.X, screen.Y, tint);

                // animation layer.
                string movingWater = MovingWaterImage(tile.Model, m_Session.WorldTime.TurnCounter);
                if (movingWater != null)
                    m_UI.UI_DrawImage(movingWater, screen.X, screen.Y, tint);

                // decorations.
                if (tile.HasDecorations)
                    foreach (string deco in tile.Decorations)
                        m_UI.UI_DrawImage(deco, screen.X, screen.Y, tint);
            }
            else if (tile.IsVisited && !IsPlayerSleeping()) // memorized
            {
                // tile.
                m_UI.UI_DrawGrayLevelImage(tile.Model.ImageID, screen.X, screen.Y);

                // animation layer.
                string movingWater = MovingWaterImage(tile.Model, m_Session.WorldTime.TurnCounter);
                if (movingWater != null)
                    m_UI.UI_DrawGrayLevelImage(movingWater, screen.X, screen.Y);

                // deocrations.
                if (tile.HasDecorations)
                    foreach (string deco in tile.Decorations)
                        m_UI.UI_DrawGrayLevelImage(deco, screen.X, screen.Y);
            }
        }

        public void DrawTileWaterCover(Tile tile, Point screen, Color tint)
        {
            if (tile.IsInView)  // visible
            {
                // tile.
                m_UI.UI_DrawImage(tile.Model.WaterCoverImageID, screen.X, screen.Y, tint);
            }
            else if (tile.IsVisited && !IsPlayerSleeping()) // memorized
            {
                // tile.
                m_UI.UI_DrawGrayLevelImage(tile.Model.WaterCoverImageID, screen.X, screen.Y);
            }
        }

        public void DrawExit(Point screen)
        {
            m_UI.UI_DrawImage(GameImages.MAP_EXIT, screen.X, screen.Y);
        }

        public void DrawTileRectangle(Point mapPosition, Color color)
        {
            m_UI.UI_DrawRect(color, new Rectangle(MapToScreen(mapPosition), new Size(TILE_SIZE, TILE_SIZE)));
        }

        public void DrawMapObject(MapObject mapObj, Point screen, Color tint)
        {
            // pushables objects in water floating animation.
            if (mapObj.IsMovable && mapObj.Location.Map.GetTileAt(mapObj.Location.Position.X, mapObj.Location.Position.Y).Model.IsWater)
            {
                int yDrift = (mapObj.Location.Position.X + m_Session.WorldTime.TurnCounter) % 2 == 0 ? -2 : 0;
                screen.Y -= yDrift;
            }

            if (IsVisibleToPlayer(mapObj))
            {
                DrawMapObject(mapObj, screen, mapObj.ImageID, (imageID, gx, gy) => m_UI.UI_DrawImage(imageID, gx, gy, tint));

                if (mapObj.HitPoints < mapObj.MaxHitPoints && mapObj.HitPoints > 0)
                    DrawMapHealthBar(mapObj.HitPoints, mapObj.MaxHitPoints, screen.X, screen.Y);

                DoorWindow door = mapObj as DoorWindow;
                if (door != null && door.BarricadePoints > 0)
                {
                    DrawMapHealthBar(door.BarricadePoints, Rules.BARRICADING_MAX, screen.X, screen.Y, Color.Green);
                    m_UI.UI_DrawImage(GameImages.EFFECT_BARRICADED, screen.X, screen.Y, tint);
                }
            }
            else if (IsKnownToPlayer(mapObj) && !IsPlayerSleeping())
            {
                DrawMapObject(mapObj, screen, mapObj.HiddenImageID, (imageID, gx, gy) => m_UI.UI_DrawGrayLevelImage(imageID, gx, gy));
            }
        }

        void DrawMapObject(MapObject mapObj, Point screen, string imageID, Action<string, int, int> drawFn)
        {
            // draw image.
            drawFn(imageID, screen.X, screen.Y);

            // draw effects.
            if (mapObj.IsOnFire)
                drawFn(GameImages.EFFECT_ONFIRE, screen.X, screen.Y);
        }

        public void DrawActorSprite(Actor actor, Point screen, Color tint)
        {
            int gx = screen.X;
            int gy = screen.Y;

            // player follower?
            if (actor.Leader != null && actor.Leader == m_Player)
            {
                if (m_Rules.HasActorBondWith(actor, m_Player))
                    m_UI.UI_DrawImage(GameImages.PLAYER_FOLLOWER_BOND, gx, gy, tint);
                else if (m_Rules.IsActorTrustingLeader(actor))
                    m_UI.UI_DrawImage(GameImages.PLAYER_FOLLOWER_TRUST, gx, gy, tint);
                else
                    m_UI.UI_DrawImage(GameImages.PLAYER_FOLLOWER, gx, gy, tint);
            }

            gx += ACTOR_OFFSET;
            gy += ACTOR_OFFSET;

            // model
            if (actor.Model.ImageID != null)
                m_UI.UI_DrawImage(actor.Model.ImageID, gx, gy, tint);

            // skinning/clothing and body equipment.
            DrawActorDecoration(actor, gx, gy, DollPart.SKIN, tint);
            DrawActorDecoration(actor, gx, gy, DollPart.FEET, tint);
            DrawActorDecoration(actor, gx, gy, DollPart.LEGS, tint);
            DrawActorDecoration(actor, gx, gy, DollPart.TORSO, tint);
            DrawActorDecoration(actor, gx, gy, DollPart.TORSO, tint);    
            if (actor.GetEquippedItem(DollPart.TORSO) != null)
                DrawActorEquipment(actor, gx - ACTOR_OFFSET, gy - ACTOR_OFFSET, DollPart.TORSO, tint);
            DrawActorDecoration(actor, gx, gy, DollPart.EYES, tint);
            DrawActorDecoration(actor, gx, gy, DollPart.HEAD, tint);

            // hands equipment
            DrawActorEquipment(actor, gx - ACTOR_OFFSET, gy - ACTOR_OFFSET, DollPart.LEFT_HAND, tint);
            DrawActorEquipment(actor, gx - ACTOR_OFFSET, gy - ACTOR_OFFSET, DollPart.RIGHT_HAND, tint);

            gx -= ACTOR_OFFSET;
            gy -= ACTOR_OFFSET;

            // personal enemy?
            if (m_Player != null)
            {
                bool imSelfDefence = m_Player.IsSelfDefenceFrom(actor);
                bool imTheAggressor = m_Player.IsAggressorOf(actor);
                bool indirectEnemies = m_Player.AreIndirectEnemies(actor);
                if (imSelfDefence)
                    m_UI.UI_DrawImage(GameImages.ICON_SELF_DEFENCE, gx, gy, tint);
                else if (imTheAggressor)
                    m_UI.UI_DrawImage(GameImages.ICON_AGGRESSOR, gx, gy, tint);
                else if (indirectEnemies)
                    m_UI.UI_DrawImage(GameImages.ICON_INDIRECT_ENEMIES, gx, gy, tint);
            }

            // activity
            #region
            switch (actor.Activity)
            {
                case Activity.IDLE:
                    break;

                case Activity.CHASING:
                case Activity.FIGHTING:
                    if (actor.IsPlayer)
                        break;
                    if (actor.TargetActor == null)
                        break;

                    if (actor.TargetActor != null && actor.TargetActor == m_Player)
                        m_UI.UI_DrawImage(GameImages.ACTIVITY_CHASING_PLAYER, gx, gy, tint);
                    else
                        m_UI.UI_DrawImage(GameImages.ACTIVITY_CHASING, gx, gy, tint);
                    break;

                case Activity.TRACKING:
                    if (actor.IsPlayer)
                        break;

                    m_UI.UI_DrawImage(GameImages.ACTIVITY_TRACKING, gx, gy, tint);
                    break;

                case Activity.FLEEING:
                    if (actor.IsPlayer)
                        break;

                    m_UI.UI_DrawImage(GameImages.ACTIVITY_FLEEING, gx, gy, tint);
                    break;

                case Activity.FLEEING_FROM_EXPLOSIVE:
                    if (actor.IsPlayer)
                        break;

                    m_UI.UI_DrawImage(GameImages.ACTIVITY_FLEEING_FROM_EXPLOSIVE, gx, gy, tint);
                    break;

                case Activity.FOLLOWING:
                    if (actor.IsPlayer)
                        break;
                    if (actor.TargetActor == null)
                        break;

                    if (actor.TargetActor.IsPlayer)
                        m_UI.UI_DrawImage(GameImages.ACTIVITY_FOLLOWING_PLAYER, gx, gy);
                    else
                        m_UI.UI_DrawImage(GameImages.ACTIVITY_FOLLOWING, gx, gy);
                    break;

                case Activity.FOLLOWING_ORDER:
                    m_UI.UI_DrawImage(GameImages.ACTIVITY_FOLLOWING_ORDER, gx, gy);
                    break;

                case Activity.SLEEPING:
                    m_UI.UI_DrawImage(GameImages.ACTIVITY_SLEEPING, gx, gy);
                    break;

                default:
                    throw new InvalidOperationException("unhandled activity " + actor.Activity);
            }
            #endregion

            // health bar.
            int maxHP = m_Rules.ActorMaxHPs(actor);
            if (actor.HitPoints < maxHP)
            {
                DrawMapHealthBar(actor.HitPoints, maxHP, gx, gy);
            }

            // run/tired icon.
            #region
            if (actor.IsRunning)
                m_UI.UI_DrawImage(GameImages.ICON_RUNNING, gx, gy, tint);
            else if (actor.Model.Abilities.CanRun && !m_Rules.CanActorRun(actor))
                m_UI.UI_DrawImage(GameImages.ICON_CANT_RUN, gx, gy, tint);
            #endregion

            // sleepy, hungry & insane icons.
            #region
            if (actor.Model.Abilities.HasToSleep)
            {
                if (m_Rules.IsActorExhausted(actor))
                    m_UI.UI_DrawImage(GameImages.ICON_SLEEP_EXHAUSTED, gx, gy, tint);
                else if (m_Rules.IsActorSleepy(actor))
                    m_UI.UI_DrawImage(GameImages.ICON_SLEEP_SLEEPY, gx, gy, tint);
                else if (m_Rules.IsAlmostSleepy(actor))
                    m_UI.UI_DrawImage(GameImages.ICON_SLEEP_ALMOST_SLEEPY, gx, gy, tint);
            }

            if (actor.Model.Abilities.HasToEat)
            {
                if (m_Rules.IsActorStarving(actor))
                    m_UI.UI_DrawImage(GameImages.ICON_FOOD_STARVING, gx, gy, tint);
                else if (m_Rules.IsActorHungry(actor))
                    m_UI.UI_DrawImage(GameImages.ICON_FOOD_HUNGRY, gx, gy, tint);
                else if (IsAlmostHungry(actor))
                    m_UI.UI_DrawImage(GameImages.ICON_FOOD_ALMOST_HUNGRY, gx, gy, tint);
            }
            else if (actor.Model.Abilities.IsRotting)
            {
                if (m_Rules.IsRottingActorStarving(actor))
                    m_UI.UI_DrawImage(GameImages.ICON_ROT_STARVING, gx, gy, tint);
                else if (m_Rules.IsRottingActorHungry(actor))
                    m_UI.UI_DrawImage(GameImages.ICON_ROT_HUNGRY, gx, gy, tint);
                else if (IsAlmostRotHungry(actor))
                    m_UI.UI_DrawImage(GameImages.ICON_ROT_ALMOST_HUNGRY, gx, gy, tint);
            }

            if (actor.Model.Abilities.HasSanity)
            {
                if (m_Rules.IsActorInsane(actor))
                    m_UI.UI_DrawImage(GameImages.ICON_SANITY_INSANE, gx, gy, tint);
                else if (m_Rules.IsActorDisturbed(actor))
                    m_UI.UI_DrawImage(GameImages.ICON_SANITY_DISTURBED, gx, gy, tint);
            }
            #endregion

            // can trade with player icon.
            if (m_Player != null && m_Rules.CanActorInitiateTradeWith(m_Player, actor))
                m_UI.UI_DrawImage(GameImages.ICON_CAN_TRADE, gx, gy, tint);

            // sleep-healing icon.
            if (actor.IsSleeping && (m_Rules.IsOnCouch(actor) || m_Rules.ActorHealChanceBonus(actor) > 0))
                m_UI.UI_DrawImage(GameImages.ICON_HEALING, gx, gy, tint);

            // is a leader icon.
            if (actor.CountFollowers > 0)
                m_UI.UI_DrawImage(GameImages.ICON_LEADER, gx, gy, tint);

            // combat assitant helper.
            if (s_Options.IsCombatAssistantOn)
            {
                if (actor != m_Player && m_Player != null && m_Rules.IsEnemyOf(actor, m_Player))
                {
                    if (m_Rules.WillActorActAgainBefore(m_Player, actor))
                        m_UI.UI_DrawImage(GameImages.ICON_THREAT_SAFE, gx, gy, tint);
                    else if (m_Rules.WillOtherActTwiceBefore(m_Player, actor))
                        m_UI.UI_DrawImage(GameImages.ICON_THREAT_HIGH_DANGER, gx, gy, tint);
                    else
                        m_UI.UI_DrawImage(GameImages.ICON_THREAT_DANGER, gx, gy, tint);
                }
            }
        }

        public void DrawActorDecoration(Actor actor, int gx, int gy, DollPart part, Color tint)
        {
            List<string> decos = actor.Doll.GetDecorations(part);
            if (decos == null)
                return;

            foreach (string imageID in decos)
                m_UI.UI_DrawImage(imageID, gx, gy, tint);
        }

        public void DrawActorDecoration(Actor actor, int gx, int gy, DollPart part, float rotation, float scale)
        {
            List<string> decos = actor.Doll.GetDecorations(part);
            if (decos == null)
                return;

            foreach (string imageID in decos)
                m_UI.UI_DrawImageTransform(imageID, gx, gy, rotation, scale);
        }

        public void DrawActorEquipment(Actor actor, int gx, int gy, DollPart part, Color tint)
        {
            Item it = actor.GetEquippedItem(part);
            if (it == null)
                return;

            m_UI.UI_DrawImage(it.ImageID, gx, gy, tint);
        }

        public void DrawCorpse(Corpse c, int gx, int gy, Color tint)
        {
            float rotation = c.Rotation;
            float scale = c.Scale;
            int offset = 0;// TILE_SIZE / 2;

            Actor actor = c.DeadGuy;

            gx += ACTOR_OFFSET + offset;
            gy += ACTOR_OFFSET + offset;
            
            // model.
            if (actor.Model.ImageID != null)
                m_UI.UI_DrawImageTransform(actor.Model.ImageID, gx, gy, rotation, scale);

            // skinning/clothing.
            DrawActorDecoration(actor, gx, gy, DollPart.SKIN, rotation, scale);
            DrawActorDecoration(actor, gx, gy, DollPart.FEET,  rotation, scale);
            DrawActorDecoration(actor, gx, gy, DollPart.LEGS, rotation, scale);
            DrawActorDecoration(actor, gx, gy, DollPart.TORSO, rotation, scale);
            DrawActorDecoration(actor, gx, gy, DollPart.TORSO, rotation, scale);
            DrawActorDecoration(actor, gx, gy, DollPart.EYES, rotation, scale);
            DrawActorDecoration(actor, gx, gy, DollPart.HEAD, rotation, scale);

            gx -= ACTOR_OFFSET + offset;
            gy -= ACTOR_OFFSET + offset;

            // rotting.
            int rotLevel = Rules.CorpseRotLevel(c);
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
                img += 1 + (m_Session.WorldTime.TurnCounter % 2);
                // a bit of offset for a nice flies movement effect.
                int rotdx = (m_Session.WorldTime.TurnCounter % 5) - 2;
                int rotdy = ((m_Session.WorldTime.TurnCounter / 3) % 5) - 2;
                m_UI.UI_DrawImage(img, gx + rotdx, gy + rotdy);
            }
        }

        /// <summary>
        /// add overlays
        /// </summary>
        /// <param name="actor"></param>
        public void DrawActorTargets(Actor actor)
        {
            Point offset = new Point(TILE_SIZE / 2, TILE_SIZE / 2);

            if (actor.TargetActor != null && !actor.TargetActor.IsDead && IsVisibleToPlayer(actor.TargetActor))
            {
                AddOverlay(new OverlayImage(MapToScreen(actor.TargetActor.Location.Position), GameImages.ICON_IS_TARGET));
            }
            foreach (Actor a in actor.Location.Map.Actors)
            {
                if (a == actor || a.IsDead || !IsVisibleToPlayer(a))
                    continue;
                if (a.TargetActor == actor && (a.Activity == Activity.CHASING || a.Activity == Activity.FIGHTING))
                {
                    AddOverlay(new OverlayImage(MapToScreen(actor.Location.Position), GameImages.ICON_IS_TARGETTED));
                    break;
                }
            }
        }

        /// <summary>
        /// immediate mode
        /// </summary>
        /// <param name="player"></param>
        public void DrawPlayerActorTargets(Actor player)
        {
            Point offset = new Point(TILE_SIZE / 2, TILE_SIZE / 2);

            if (player.TargetActor != null && !player.TargetActor.IsDead && IsVisibleToPlayer(player.TargetActor))
            {
                Point gpos = MapToScreen(player.TargetActor.Location.Position);
                m_UI.UI_DrawImage(GameImages.ICON_IS_TARGET, gpos.X, gpos.Y);
            }
            foreach (Actor a in player.Location.Map.Actors)
            {
                if (a == player || a.IsDead || !IsVisibleToPlayer(a))
                    continue;
                if (a.TargetActor == player && (a.Activity == Activity.CHASING || a.Activity == Activity.FIGHTING))
                {
                    Point gpos = MapToScreen(player.Location.Position);
                    m_UI.UI_DrawImage(GameImages.ICON_IS_TARGETTED, gpos.X, gpos.Y);
                    break;
                }
            }
        }

        public void DrawItemsStack(Inventory inventory, int gx, int gy, Color tint)
        {
            if (inventory == null)
                return;

            foreach (Item it in inventory.Items)
                DrawItem(it, gx, gy, tint);
        }

        public void DrawMapIcon(Point position, string imageID)
        {
            m_UI.UI_DrawImage(imageID, position.X * RogueGame.TILE_SIZE, position.Y * RogueGame.TILE_SIZE);
        }

        public void DrawMapHealthBar(int hitPoints, int maxHitPoints, int gx, int gy)
        {
            DrawMapHealthBar(hitPoints, maxHitPoints, gx, gy, Color.Red);
        }

        public void DrawMapHealthBar(int hitPoints, int maxHitPoints, int gx, int gy, Color barColor)
        {
            int hpX = gx + 4;
            int hpY = gy + TILE_SIZE - 4;
            int barLength = (int)(20 * (float)hitPoints / (float)maxHitPoints);
            m_UI.UI_FillRect(Color.Black, new Rectangle(hpX, hpY, 20, 4));
            if (barLength > 0)
                m_UI.UI_FillRect(barColor, new Rectangle(hpX + 1, hpY + 1, barLength, 2));

        }

        public void DrawMiniMap(Map map)
        {
            // clear minimap.
            if (s_Options.IsMinimapOn)
            {
                m_UI.UI_ClearMinimap(Color.Black);
            }

            // set visited tiles color.
            #region
            if (s_Options.IsMinimapOn)
            {
                Point pt = new Point();
                for (int x = 0; x < map.Width; x++)
                {
                    pt.X = x;
                    for (int y = 0; y < map.Height; y++)
                    {
                        pt.Y = y;
                        Tile tile = map.GetTileAt(x, y);
                        if (tile.IsVisited)
                        {
                            // exits override tile color.
                            if (map.GetExitAt(pt) != null)
                                m_UI.UI_SetMinimapColor(x, y, Color.HotPink);
                            else
                                m_UI.UI_SetMinimapColor(x, y, tile.Model.MinimapColor);
                        }
                    }
                }
            }
            #endregion

            // show minimap.
            if (s_Options.IsMinimapOn)
            {
                m_UI.UI_DrawMinimap(MINIMAP_X, MINIMAP_Y);
            }

            // show view rect.
            m_UI.UI_DrawRect(Color.White, new Rectangle(MINIMAP_X + m_MapViewRect.Left * MINITILE_SIZE, MINIMAP_Y + m_MapViewRect.Top * MINITILE_SIZE, m_MapViewRect.Width * MINITILE_SIZE, m_MapViewRect.Height * MINITILE_SIZE));

            // show player tags.
            #region
            if (s_Options.ShowPlayerTagsOnMinimap)
            {
                for (int x = 0; x < map.Width; x++)
                    for (int y = 0; y < map.Height; y++)
                    {
                        Tile tile = map.GetTileAt(x, y);
                        if (tile.IsVisited)
                        {
                            string minitag = null;
                            if (tile.HasDecoration(GameImages.DECO_PLAYER_TAG1))
                                minitag = GameImages.MINI_PLAYER_TAG1;
                            else if (tile.HasDecoration(GameImages.DECO_PLAYER_TAG2))
                                minitag = GameImages.MINI_PLAYER_TAG2;
                            else if (tile.HasDecoration(GameImages.DECO_PLAYER_TAG3))
                                minitag = GameImages.MINI_PLAYER_TAG3;
                            else if (tile.HasDecoration(GameImages.DECO_PLAYER_TAG4))
                                minitag = GameImages.MINI_PLAYER_TAG4;
                            if (minitag != null)
                            {
                                Point pos = new Point(MINIMAP_X + x * MINITILE_SIZE, MINIMAP_Y + y * MINITILE_SIZE);
                                m_UI.UI_DrawImage(minitag, pos.X - MINI_TRACKER_OFFSET, pos.Y - MINI_TRACKER_OFFSET);
                            }
                        }
                    }
            }
            #endregion

            // show player & tracked actors.
            // add tracked targets images out of player fov on the map.
            #region
            if (m_Player != null)
            {
                // tracker items.
                if (!m_Player.IsSleeping)
                {
                    ItemTracker tracker = m_Player.GetEquippedItem(DollPart.LEFT_HAND) as ItemTracker;

                    // tracking...
                    if (tracker != null && tracker.Batteries > 0)
                    {
                        // ...followers?
                        #region
                        if (m_Player.CountFollowers > 0 && tracker.CanTrackFollowersOrLeader)
                        {
                            foreach (Actor fo in m_Player.Followers)
                            {
                                // only track in same map.
                                if (fo.Location.Map != m_Player.Location.Map)
                                    continue;

                                ItemTracker foTracker = fo.GetEquippedItem(DollPart.LEFT_HAND) as ItemTracker;
                                if (foTracker != null && foTracker.CanTrackFollowersOrLeader)
                                {
                                    // show follower position.
                                    Point foMiniPos = new Point(MINIMAP_X + fo.Location.Position.X * MINITILE_SIZE, MINIMAP_Y + fo.Location.Position.Y * MINITILE_SIZE);
                                    m_UI.UI_DrawImage(GameImages.MINI_FOLLOWER_POSITION, foMiniPos.X - MINI_TRACKER_OFFSET, foMiniPos.Y - MINI_TRACKER_OFFSET);

                                    // if out of FoV but in view,, draw on map.
                                    if (IsInViewRect(fo.Location.Position) && !IsVisibleToPlayer(fo))
                                    {
                                        Point screenPos = MapToScreen(fo.Location.Position);
                                        m_UI.UI_DrawImage(GameImages.TRACK_FOLLOWER_POSITION, screenPos.X, screenPos.Y);
                                    }
                                }
                            }
                        }
                        #endregion

                        // ...undeads?
                        #region
                        if (tracker.CanTrackUndeads)
                        {
                            foreach (Actor other in map.Actors)
                            {
                                if (other == m_Player)
                                    continue;
                                if (!other.Model.Abilities.IsUndead)
                                    continue;
                                // only track in same map.
                                if (other.Location.Map != m_Player.Location.Map)
                                    continue;
                                if (m_Rules.GridDistance(other.Location.Position, m_Player.Location.Position) > Rules.ZTRACKINGRADIUS)
                                    continue;

                                // close undead, show it.
                                Point undeadPos = new Point(MINIMAP_X + other.Location.Position.X * MINITILE_SIZE, MINIMAP_Y + other.Location.Position.Y * MINITILE_SIZE);
                                m_UI.UI_DrawImage(GameImages.MINI_UNDEAD_POSITION, undeadPos.X - MINI_TRACKER_OFFSET, undeadPos.Y - MINI_TRACKER_OFFSET);

                                // if out of FoV but in view,, draw on map.
                                if (IsInViewRect(other.Location.Position) && !IsVisibleToPlayer(other))
                                {
                                    Point screenPos = MapToScreen(other.Location.Position);
                                    m_UI.UI_DrawImage(GameImages.TRACK_UNDEAD_POSITION, screenPos.X, screenPos.Y);
                                }
                            }
                        }
                        #endregion

                        // ...BlackOps?
                        #region
                        if (tracker.CanTrackBlackOps)
                        {
                            foreach (Actor other in map.Actors)
                            {
                                if (other == m_Player)
                                    continue;
                                if (other.Faction != GameFactions.TheBlackOps)
                                    continue;
                                // only track in same map.
                                if (other.Location.Map != m_Player.Location.Map)
                                    continue;

                                // blackop, show it.
                                Point boPos = new Point(MINIMAP_X + other.Location.Position.X * MINITILE_SIZE, MINIMAP_Y + other.Location.Position.Y * MINITILE_SIZE);
                                m_UI.UI_DrawImage(GameImages.MINI_BLACKOPS_POSITION, boPos.X - MINI_TRACKER_OFFSET, boPos.Y - MINI_TRACKER_OFFSET);

                                // if out of FoV but in view,, draw on map.
                                if (IsInViewRect(other.Location.Position) && !IsVisibleToPlayer(other))
                                {
                                    Point screenPos = MapToScreen(other.Location.Position);
                                    m_UI.UI_DrawImage(GameImages.TRACK_BLACKOPS_POSITION, screenPos.X, screenPos.Y);
                                }
                            }
                        }
                        #endregion

                        // ...Police?
                        if (tracker.CanTrackPolice)
                        {
                            foreach (Actor other in map.Actors)
                            {
                                if (other == m_Player)
                                    continue;
                                if (other.Faction != GameFactions.ThePolice)
                                    continue;
                                // only track in same map.
                                if (other.Location.Map != m_Player.Location.Map)
                                    continue;

                                // policeman, show it.
                                Point boPos = new Point(MINIMAP_X + other.Location.Position.X * MINITILE_SIZE, MINIMAP_Y + other.Location.Position.Y * MINITILE_SIZE);
                                m_UI.UI_DrawImage(GameImages.MINI_POLICE_POSITION, boPos.X - MINI_TRACKER_OFFSET, boPos.Y - MINI_TRACKER_OFFSET);

                                // if out of FoV but in view,, draw on map.
                                if (IsInViewRect(other.Location.Position) && !IsVisibleToPlayer(other))
                                {
                                    Point screenPos = MapToScreen(other.Location.Position);
                                    m_UI.UI_DrawImage(GameImages.TRACK_POLICE_POSITION, screenPos.X, screenPos.Y);
                                }
                            }
                        }
                    }
                }

                // player.
                Point pos = new Point(MINIMAP_X + m_Player.Location.Position.X * MINITILE_SIZE, MINIMAP_Y + m_Player.Location.Position.Y * MINITILE_SIZE);
                m_UI.UI_DrawImage(GameImages.MINI_PLAYER_POSITION, pos.X - MINI_TRACKER_OFFSET, pos.Y - MINI_TRACKER_OFFSET);
            }
            #endregion
        }

        public void DrawItem(Item it, int gx, int gy)
        {
            DrawItem(it, gx, gy, Color.White);
        }

        public void DrawItem(Item it, int gx, int gy, Color tint)
        {
            m_UI.UI_DrawImage(it.ImageID, gx, gy, tint);

            if (it.Model.IsStackable)
            {
                string q = string.Format("{0}", it.Quantity);
                int tx = gx + TILE_SIZE - 10;
                if (it.Quantity > 100)
                    tx -= 10;
                else if (it.Quantity > 10)
                    tx -= 4;
                m_UI.UI_DrawString(Color.DarkGray, q, tx + 1, gy + 1);
                m_UI.UI_DrawString(Color.White, q, tx, gy);
            }
            if (it is ItemTrap)
            {
                ItemTrap trap = it as ItemTrap;
                if (trap.IsTriggered)
                    m_UI.UI_DrawImage(GameImages.ICON_TRAP_TRIGGERED, gx, gy);
                else if (trap.IsActivated)
                    m_UI.UI_DrawImage(GameImages.ICON_TRAP_ACTIVATED, gx, gy);
            }
        }

        public void DrawActorSkillTable(Actor actor, int gx, int gy)
        {
            gy -= BOLD_LINE_SPACING;
            m_UI.UI_DrawStringBold(Color.White, "Skills", gx, gy);
            gy += BOLD_LINE_SPACING;

            IEnumerable<Skill> skills = actor.Sheet.SkillTable.Skills;
            if (skills == null)
                return;

            int x, y;
            int count = 0;
            x = gx; y = gy;
            foreach (Skill sk in skills)
            {
                m_UI.UI_DrawString(Color.White, String.Format("{0}-", sk.Level), x, y);
                x += 16;
                m_UI.UI_DrawString(Color.White, Skills.Name(sk.ID), x, y);
                x -= 16;

                if (++count >= SKILLTABLE_LINES)
                {
                    count = 0;
                    y = gy;
                    x += 120;
                }
                else
                    y += LINE_SPACING;
            }
        }
#endregion

#region Overlays
        void AddOverlay(Overlay o)
        {
            // get mutex
            Monitor.Enter(m_Overlays);

            m_Overlays.Add(o);

            // release mutex
            Monitor.Exit(m_Overlays);
        }

        void ClearOverlays()
        {
            // get mutex
            Monitor.Enter(m_Overlays);

            m_Overlays.Clear();

            // release mutex
            Monitor.Exit(m_Overlays);
        }
#endregion

#region Coordinates conversion
        Point MapToScreen(Point mapPosition)
        {
            return MapToScreen(mapPosition.X, mapPosition.Y);
        }

        Point MapToScreen(int x, int y)
        {
            return new Point((x - m_MapViewRect.Left) * RogueGame.TILE_SIZE, (y - m_MapViewRect.Top) * RogueGame.TILE_SIZE);
        }

        Point ScreenToMap(Point screenPosition)
        {
            return ScreenToMap(screenPosition.X, screenPosition.Y);
        }

        Point ScreenToMap(int gx, int gy)
        {
            return new Point(m_MapViewRect.Left + gx / RogueGame.TILE_SIZE, m_MapViewRect.Top + gy / RogueGame.TILE_SIZE);
        }

        Point MouseToMap(Point mousePosition)
        {
            return MouseToMap(mousePosition.X, mousePosition.Y);
        }

        Point MouseToMap(int mouseX, int mouseY)
        {
            mouseX = (int)(mouseX / m_UI.UI_GetCanvasScaleX());
            mouseY = (int)(mouseY / m_UI.UI_GetCanvasScaleY());
            return ScreenToMap(mouseX, mouseY);
        }

        Point MouseToInventorySlot(int invX, int invY, int mouseX, int mouseY)
        {
            mouseX = (int)(mouseX / m_UI.UI_GetCanvasScaleX());
            mouseY = (int)(mouseY / m_UI.UI_GetCanvasScaleY());

            return new Point((mouseX - invX) / 32, (mouseY - invY) / 32);
        }

        Point InventorySlotToScreen(int invX, int invY, int slotX, int slotY)
        {
            return new Point(invX + slotX * 32, invY + slotY * 32);
        }
#endregion

#region Visibility/Smell & memory helpers
        bool IsVisibleToPlayer(Location location)
        {
            return IsVisibleToPlayer(location.Map, location.Position);
        }

        bool IsVisibleToPlayer(Map map, Point position)
        {
            return m_Player != null
                && map == m_Player.Location.Map && map.IsInBounds(position.X, position.Y) 
                && map.GetTileAt(position.X, position.Y).IsInView;
        }

        bool IsVisibleToPlayer(Actor actor)
        {
            return actor == m_Player || IsVisibleToPlayer(actor.Location);
        }

        bool IsVisibleToPlayer(MapObject mapObj)
        {
            return IsVisibleToPlayer(mapObj.Location);
        }

        bool IsKnownToPlayer(Map map, Point position)
        {
            return map.IsInBounds(position.X, position.Y) && map.GetTileAt(position.X, position.Y).IsVisited;
        }

        bool IsKnownToPlayer(Location location)
        {
            return IsKnownToPlayer(location.Map, location.Position);
        }

        bool IsKnownToPlayer(MapObject mapObj)
        {
            return IsKnownToPlayer(mapObj.Location);
        }

        bool IsPlayerSleeping()
        {
            return m_Player != null && m_Player.IsSleeping;
        }
#endregion

#region Text helpers
        int FindLongestLine(string[] lines)
        {
            if (lines == null || lines.Length == 0)
                return 0;

            int max = Int32.MinValue;

            foreach (string s in lines)
            {
                if (s == null)  // sanity check.
                    continue;
                if (s.Length > max)
                    max = s.Length;
            }

            return max;
        }
#endregion
    }
}