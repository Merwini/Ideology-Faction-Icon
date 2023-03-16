using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld.Planet;
using RimWorld;
using Verse;
using UnityEngine;

namespace nuff.Ideology_Faction_Icon
{
    [StaticConstructorOnStartup]
    public class GameComponent_FactionLists : GameComponent
    {
        List<Faction> knownFactions;
        List<Faction> chosenForward;
        List<Faction> chosenReverse;
        List<Faction> unchosenForward;
        List<Faction> unchosenReverse;

        public override void StartedNewGame()
        {
            knownFactions = Find.FactionManager.AllFactionsListForReading;
            base.StartedNewGame();
        }

        public override void LoadedGame()
        {
            //check for new factions
            foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
            {
                if (!knownFactions.Contains(faction))
                {
                    Log.Message("A faction has been added. Adding to unchosen lists.");
                    knownFactions.Add(faction);
                    unchosenForward.Add(faction);
                    unchosenReverse.Add(faction);
                }
            }

            //check for removed factions
            foreach (Faction faction in knownFactions)
            {
                if (!Find.FactionManager.AllFactionsListForReading.Contains(faction))
                {
                    Log.Message("A faction has been removed. Removing from lists.");
                    knownFactions.Remove(faction);
                    chosenForward.Remove(faction);
                    chosenReverse.Remove(faction);
                    unchosenForward.Remove(faction);
                    unchosenReverse.Remove(faction);
                }
            }
            base.LoadedGame();
        }
    }
}
