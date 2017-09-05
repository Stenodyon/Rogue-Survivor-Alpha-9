using System;
using System.Text;
using System.Drawing;
using System.Collections.Generic;

using djack.RogueSurvivor.Data;
using djack.RogueSurvivor.Engine;

namespace djack.RogueSurvivor.UI.Components
{
    class LocationPanel : UIComponent
    {
        readonly Color NIGHT_COLOR = Color.Cyan;
        readonly Color DAY_COLOR = Color.Gold;

        RogueGame game;

        public LocationPanel(Rectangle area, RogueGame game)
            : base(area)
        {
            this.game = game;
        }

        public override void Draw(IRogueUI ui)
        {
            //  <map name>
            //  <zone name>
            //  <day>        <dayphase>
            //  <hour>       <weather>/<lighting>
            //  <turn>       <scoring>@<difficulty> <mode>
            //  <life>/<lives>
            //  <murders>
            Color timeColor = game.Session.WorldTime.IsNight ? NIGHT_COLOR : DAY_COLOR;
            int col1 = 4, col2 = col1 + 128;

            int curY = 4;
            SetColor(Color.DarkGray);
            DrawLine(ui, 0, 0, 0, w);

            SetColor(Color.White);
            DrawString(ui, game.Session.CurrentMap.Name, col1, curY);

            curY += LINE_SPACING;
            DrawString(ui, LocationText(game.Session.CurrentMap, game.Player), col1, curY);

            curY += LINE_SPACING;
            DrawString(ui, String.Format("Day  {0}", game.Session.WorldTime.Day), col1, curY);
            DrawString(ui, timeColor, RogueGame.DescribeDayPhase(game.Session.WorldTime.Phase), col2, curY);

            curY += LINE_SPACING;
            DrawString(ui, String.Format("Hour {0}", game.Session.WorldTime.Hour), col1, curY);
            DrawWeather(ui, col2, curY);

            curY += LINE_SPACING;
            DrawString(ui, String.Format("Turn {0}", game.Session.WorldTime.TurnCounter), col1, curY);
            int difficultyRating =
                (int)(100*Scoring.ComputeDifficultyRating(
                    RogueGame.Options,
                    game.Session.Scoring.Side,
                    game.Session.Scoring.ReincarnationNumber));
            DrawString(ui, String.Format(
                "Score   {0}@{1}% {2}",
                game.Session.Scoring.TotalPoints,
                difficultyRating,
                Session.DescShortGameMode(game.Session.GameMode)),
                col2, curY);

            curY += LINE_SPACING;
            DrawString(ui, String.Format(
                "Avatar  {0}/{1}",
                (1 + game.Session.Scoring.ReincarnationNumber),
                (1 + RogueGame.Options.MaxReincarnations)),
                col2, curY);

            curY += LINE_SPACING;
            if (game.Player.MurdersCounter > 0)
                DrawString(ui, String.Format("Murders {0}", game.Player.MurdersCounter), col1, curY);                
        }

        void DrawWeather(IRogueUI ui, int x, int y)
        {
            Color weatherOrLightingColor;
            string weatherOrLightingString;
            switch(game.Session.CurrentMap.Lighting)
            {
                case Lighting.OUTSIDE:
                    weatherOrLightingColor = WeatherColor(game.Session.World.Weather);
                    weatherOrLightingString = RogueGame.DescribeWeather(game.Session.World.Weather);
                    break;
                case Lighting.DARKNESS:
                    weatherOrLightingColor = Color.Blue;
                    weatherOrLightingString = "Darkness";
                    break;
                case Lighting.LIT:
                    weatherOrLightingColor = Color.Yellow;
                    weatherOrLightingString = "Lit";
                    break;
                default:
                    throw new ArgumentOutOfRangeException("unhandled lighting");
            }
            DrawString(ui, weatherOrLightingColor, weatherOrLightingString, x, y);
        }

        string LocationText(Map map, Actor actor)
        {
            if (map == null || actor == null)
                return "";

            StringBuilder sb = new StringBuilder(String.Format("({0},{1}) ", actor.Location.Position.X, actor.Location.Position.Y));

            List<Zone> zones = map.GetZonesAt(actor.Location.Position.X, actor.Location.Position.Y);
            if (zones == null || zones.Count == 0)
                return sb.ToString();

            foreach (Zone z in zones)
                sb.Append(String.Format("{0} ", z.Name));

            return sb.ToString();
        }

        Color WeatherColor(Weather weather)
        {
            switch (weather)
            {
                case Weather.CLOUDY: return Color.Gray;
                case Weather.HEAVY_RAIN: return Color.Blue;
                case Weather.RAIN: return Color.LightBlue;
                case Weather.CLEAR: return Color.Yellow;

                default:
                    throw new ArgumentOutOfRangeException("unhandled weather");
            }
        }

    }
}