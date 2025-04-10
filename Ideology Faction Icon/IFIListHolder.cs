using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;

namespace nuff.Ideology_Faction_Icon
{
    [StaticConstructorOnStartup]
    public static class IFIListHolder
    {
        public static HashSet<Faction> knownFactions;
        public static List<Faction> chosenForward;
        public static List<Faction> chosenReverse;
        public static List<Faction> unchosenForward;
        public static List<Faction> unchosenReverse;
    }
}
