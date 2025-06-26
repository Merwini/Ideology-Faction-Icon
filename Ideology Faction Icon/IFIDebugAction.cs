using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using LudeonTK;
using RimWorld;

namespace nuff.Ideology_Faction_Icon
{
    public static class IFIDebugAction
    {
        [DebugAction("Ideoligion Icon as Faction Icon", "List Factions", false, true, false, false, false, 0, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
        public static void ListFactions()
        {
            List<Faction> factions = Find.FactionManager.AllFactionsListForReading.ToList();
            foreach (Faction fact in factions)
            {
                Log.Message(fact.Name);
            }
        }

        [DebugAction("Ideoligion Icon as Faction Icon", "List Factions in Dict", false, true, false, false, false, 0, actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
        public static void ListFactionsDict()
        {
            if (Current.Game == null)
            {
                Log.Warning("No current game");
                return;
            }

            GameComponent_FactionLists comp = Current.Game.GetComponent<GameComponent_FactionLists>();

            if (comp == null)
            {
                Log.Warning("Comp is null");
                return;
            }

            if (comp.iconDictionary == null)
            {
                Log.Warning("Dictionary is null");
                return;
            }

            foreach (var faction in comp.iconDictionary.Keys.ToList())
            {
                Log.Message(faction.Name);
            }
        }
    }
}
