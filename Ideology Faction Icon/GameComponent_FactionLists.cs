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
    public class GameComponent_FactionLists : GameComponent
    {
        public HashSet<Faction> knownFactions;
        public List<Faction> chosenForward;
        public List<Faction> chosenReverse;
        public List<Faction> unchosenForward;
        public List<Faction> unchosenReverse;

        public GameComponent_FactionLists(Game game)
        {

        }

        public override void StartedNewGame()
        {
            base.StartedNewGame();

            knownFactions = new HashSet<Faction>();
            chosenForward = new List<Faction>();
            chosenReverse = new List<Faction>();
            unchosenForward = new List<Faction>();
            unchosenReverse = new List<Faction>();

            foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
            {
                knownFactions.Add(faction);
                unchosenForward.Add(faction);
                unchosenReverse.Add(faction);
            }

            this.LoadedGame();
        }

        public override void LoadedGame()
        {
            base.LoadedGame();

            //check for new factions
            foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
            {
                if (knownFactions.Add(faction))
                {
                    Log.Message("A faction has been added. Adding to unchosen lists.");
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

            //update static lists
            IFIListHolder.knownFactions = knownFactions;
            IFIListHolder.chosenForward = chosenForward;
            IFIListHolder.chosenReverse = chosenReverse;
            IFIListHolder.unchosenForward = unchosenForward;
            IFIListHolder.unchosenReverse = unchosenReverse;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref knownFactions, "knownFactions", LookMode.Reference);
            Scribe_Collections.Look(ref chosenForward, "chosenForward", LookMode.Reference);
            Scribe_Collections.Look(ref chosenReverse, "chosenReverse", LookMode.Reference);
            Scribe_Collections.Look(ref unchosenForward, "unchosenForward", LookMode.Reference);
            Scribe_Collections.Look(ref unchosenReverse, "unchosenReverse", LookMode.Reference);
        }
    }
}
