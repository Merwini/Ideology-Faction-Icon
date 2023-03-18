using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RimWorld.Planet;
using RimWorld;
using Verse;
using UnityEngine;

namespace Ideology_Faction_Icon
{
    public class GameComponent_FactionLists : GameComponent
    {
        public List<Faction> knownFactions;
        public List<Faction> chosenForward;
        public List<Faction> chosenReverse;
        public List<Faction> unchosenForward;
        public List<Faction> unchosenReverse;

        public GameComponent_FactionLists(Game game)
        {

        }

        public override void StartedNewGame()
        {
            knownFactions = new List<Faction>();
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

            base.StartedNewGame();

            this.LoadedGame();
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
