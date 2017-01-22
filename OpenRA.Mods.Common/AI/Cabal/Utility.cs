using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.Common.AI.Cabal
{
    public class Utility
    {
        public static void BotDebug(string s, params object[] args)
        {
            if (Game.Settings.Debug.BotDebug)
                Game.Debug("Cabal: " + s, args);
        }
        public static void BotDebug(Color c, string s, params object[] args)
        {
            if (Game.Settings.Debug.BotDebug)
                Game.AddChatLine(c, "Cabal", s.FormatWith(args));
        }
    }
}
