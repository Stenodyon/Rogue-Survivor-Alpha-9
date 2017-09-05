using System;
using System.Text;
using System.Drawing;

using djack.RogueSurvivor.Data;

namespace djack.RogueSurvivor.Engine
{
    /* This part of RogueGame provides messaging helpers */
    partial class RogueGame
    {
        public void AddMessage(Message msg)
        {
            // ignore empty messages
            if (msg.Text.Length == 0)
                return;

            // Clear if too much messages.
            if (m_MessageManager.Count >= MAX_MESSAGES)
                m_MessageManager.Clear();

            // Format message: <turn> <Text>           
            msg.Text = String.Format("{0} {1}", m_Session.WorldTime.TurnCounter, Capitalize(msg.Text));

            // Add.
            m_MessageManager.Add(msg);
        }

        /// <summary>
        /// Adds the message if it is audible by the player and redraws the screen.
        /// </summary>
        public void AddMessageIfAudibleForPlayer(Location location, Message msg)
        {
            if (msg == null)
                throw new ArgumentNullException("msg");

            // 1. Audible to player?
            if (m_Player != null)
            {
                // if sleeping can't hear.
                if (m_Player.IsSleeping)
                    return;

                // can't hear if not same map.
                if (location.Map != m_Player.Location.Map)
                    return;

                // can hear if close enough.
                if (m_Rules.StdDistance(m_Player.Location.Position, location.Position) <= m_Player.AudioRange)
                {
                    // hear.
                    msg.Color = PLAYER_AUDIO_COLOR;
                    AddMessage(msg);

                    // if waiting, interupt.
                    if (m_IsPlayerLongWait)
                        m_IsPlayerLongWaitForcedStop = true;

                    // redraw.
                    RedrawPlayScreen();
                }
            }
        }

        /// <summary>
        /// Make a message with the text: [eventText] DISTANCE tiles to the DIRECTION.
        /// </summary>
        /// <param name="eventText"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        Message MakePlayerCentricMessage(string eventText, Point position)
        {
            Point vDir = new Point(position.X - m_Player.Location.Position.X, position.Y - m_Player.Location.Position.Y);
            string text = String.Format("{0} {1} tiles to the {2}.", eventText, (int)m_Rules.StdDistance(vDir), Direction.ApproximateFromVector(vDir));
            return new Message(text, m_Session.WorldTime.TurnCounter);
        }

        Message MakeErrorMessage(string text)
        {
            return new Message(text, m_Session.WorldTime.TurnCounter, Color.Red);
        }

        Message MakeYesNoMessage(string question)
        {
            return new Message(String.Format("{0}? Y to confirm, N to cancel", question), m_Session.WorldTime.TurnCounter, Color.Yellow);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="actor"></param>
        /// <returns>"someone" if not visible to the player; TheName if visible.</returns>
        string ActorVisibleIdentity(Actor actor)
        {
            return IsVisibleToPlayer(actor) ? actor.TheName : "someone";
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mapObj"></param>
        /// <returns>"someone" if not visible to the player; TheName if visible.</returns>
        string ObjectVisibleIdentity(MapObject mapObj)
        {
            return IsVisibleToPlayer(mapObj) ? mapObj.TheName : "something";
        }

        Message MakeMessage(Actor actor, string doWhat)
        {
            return MakeMessage(actor, doWhat, OTHER_ACTION_COLOR);
        }

        Message MakeMessage(Actor actor, string doWhat, Color color)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(ActorVisibleIdentity(actor));
            sb.Append(" ");
            sb.Append(doWhat);

            Message msg = new Message(sb.ToString(), m_Session.WorldTime.TurnCounter);
            if (actor.IsPlayer)
                msg.Color = PLAYER_ACTION_COLOR;
            else
                msg.Color = color;

            return msg;
        }

        Message MakeMessage(Actor actor, string doWhat, Actor target)
        {
            return MakeMessage(actor, doWhat, target, ".");
        }

        Message MakeMessage(Actor actor, string doWhat, Actor target, string phraseEnd)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(ActorVisibleIdentity(actor));
            sb.Append(" ");
            sb.Append(doWhat);
            sb.Append(" ");
            sb.Append(ActorVisibleIdentity(target));
            sb.Append(phraseEnd);

            Message msg = new Message(sb.ToString(), m_Session.WorldTime.TurnCounter);
            if (actor.IsPlayer || target.IsPlayer)
                msg.Color = PLAYER_ACTION_COLOR;
            else
                msg.Color = OTHER_ACTION_COLOR;

            return msg;
        }

        Message MakeMessage(Actor actor, string doWhat, MapObject target)
        {
            return MakeMessage(actor, doWhat, target, ".");
        }

        Message MakeMessage(Actor actor, string doWhat, MapObject target, string phraseEnd)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(ActorVisibleIdentity(actor));
            sb.Append(" ");
            sb.Append(doWhat);
            sb.Append(" ");
            sb.Append(ObjectVisibleIdentity(target));
            sb.Append(phraseEnd);

            Message msg = new Message(sb.ToString(), m_Session.WorldTime.TurnCounter);
            if (actor.IsPlayer)
                msg.Color = PLAYER_ACTION_COLOR;
            else
                msg.Color = OTHER_ACTION_COLOR;

            return msg;
        }

        Message MakeMessage(Actor actor, string doWhat, Item target)
        {
            return MakeMessage(actor, doWhat, target, ".");
        }

        Message MakeMessage(Actor actor, string doWhat, Item target, string phraseEnd)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(ActorVisibleIdentity(actor));
            sb.Append(" ");
            sb.Append(doWhat);
            sb.Append(" ");
            sb.Append(target.TheName);
            sb.Append(phraseEnd);

            Message msg = new Message(sb.ToString(), m_Session.WorldTime.TurnCounter);
            if (actor.IsPlayer)
                msg.Color = PLAYER_ACTION_COLOR;
            else
                msg.Color = OTHER_ACTION_COLOR;

            return msg;
        }    

        void ClearMessages()
        {
            m_MessageManager.Clear();
        }

        void ClearMessagesHistory()
        {
            m_MessageManager.ClearHistory();
        }

        void RemoveLastMessage()
        {
            m_MessageManager.RemoveLastMessage();
        }

        void AddMessagePressEnter()
        {
            AddMessage(new Message("<press ENTER>", m_Session.WorldTime.TurnCounter, Color.Yellow));
            RedrawPlayScreen();
            WaitEnter();
            RemoveLastMessage();
            RedrawPlayScreen();
        }

        string Conjugate(Actor actor, string verb)
        {
            return actor.IsProperName && !actor.IsPluralName ? verb + "s" : verb;
        }

        string Conjugate(Actor actor, Verb verb)
        {
            return actor.IsProperName && !actor.IsPluralName ? verb.HeForm : verb.YouForm;
        }

        string Capitalize(string text)
        {
            if (text == null)
                return "";
            if (text.Length == 1)
                return String.Format("{0}", Char.ToUpper(text[0]));

            return String.Format("{0}{1}", Char.ToUpper(text[0]), text.Substring(1));
        }

        string HisOrHer(Actor actor)
        {
            return actor.Model.DollBody.IsMale ? "his" : "her";
        }

        string HeOrShe(Actor actor)
        {
            return actor.Model.DollBody.IsMale ? "he" : "she";
        }

        string HimOrHer(Actor actor)
        {
            return actor.Model.DollBody.IsMale ? "him" : "her";
        }

        /// <summary>
        /// </summary>
        /// <returns>"a/an name"</returns>
        string AorAn(string name)
        {
            char c = name[0];
            return (c == 'a' || c == 'e' || c == 'i' || c == 'u' ? "an " : "a ") + name;
        }

        string TruncateString(string s, int maxLength)
        {
            return s.Length <= maxLength ? s : s.Substring(0, maxLength);
        }
    }
}