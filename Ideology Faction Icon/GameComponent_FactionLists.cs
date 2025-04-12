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
        internal Dictionary<Faction, bool> iconDictionary;
        internal Dictionary<Faction, bool> colorDictionary;

        List<Faction> iconFactListTmp;
        List<bool> iconBehaviorListTmp;

        public static Dictionary<Faction, Texture2D> iconCacheDict;
        public bool needRecache = true;

        public GameComponent_FactionLists(Game game)
        {
        }

        public override void StartedNewGame()
        {
            needRecache = true;
        }

        public override void LoadedGame()
        {
            PopulateIconDictionary();
        }

        public override void GameComponentTick()
        {
            if (needRecache)
            {
                RecacheIcons();
                needRecache = false;
            }
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref iconDictionary, "iconDictionary", LookMode.Reference, LookMode.Value, ref iconFactListTmp, ref iconBehaviorListTmp);
        }

        public void PopulateIconDictionary()
        {
            if (iconDictionary == null)
            {
                iconDictionary = new Dictionary<Faction, bool>();
            }
            //colorDictionary = new Dictionary<Faction, bool>();

            IdeoFactIconSettings.CustomizeSettings setting = IdeoFactIconSettings.ideoAsFact;

            foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
            {
                if (setting == IdeoFactIconSettings.CustomizeSettings.All)
                {
                    iconDictionary[faction] = true;
                    continue;
                }

                //could have made this an || in the previous if statement but is cleaner to read this way
                else if (setting == IdeoFactIconSettings.CustomizeSettings.Just_Player)
                {
                    if (faction.IsPlayer)
                    {
                        iconDictionary[faction] = true;
                    }
                    else
                    {
                        iconDictionary[faction] = false;
                    }
                }

                else
                {
                    //account for newly-added factions or those somehow missing, leave existing entries untouched
                    if (!iconDictionary.ContainsKey(faction))
                    {
                        iconDictionary[faction] = false;
                    }
                }
            }
            needRecache = true;
        }

        public static void RecacheIcons()
        {
            if (Current.Game == null)
            {
                Log.Error("Ideologion Icon as Faction Icon tried to recache faction icons with no Game in progress. Please send your error log to the developer.");
                return;
            }

            GameComponent_FactionLists comp = Current.Game.GetComponent<GameComponent_FactionLists>();

            if (comp == null)
            {
                Log.Error("Ideologion Icon as Faction Icon tried to recache faction icons but couldn't find the GameComponent. Please send your error log to the developer.");
                return;
            }

            if (comp.iconDictionary.NullOrEmpty())
            {
                comp.PopulateIconDictionary();
            }

            iconCacheDict = new Dictionary<Faction, Texture2D>();
            foreach (var entry in comp.iconDictionary)
            {
                comp.RecacheIconSingle(entry.Key, entry.Value);
            }
        }

        private void RecacheIconSingle(Faction faction, bool behavior)
        {
            Texture2D tex = null;
            if (behavior)
            {
                tex = faction.ideos?.PrimaryIdeo?.Icon;
            }
            if (tex == null)
            {
                tex = faction.def.FactionIcon;
            }

            iconCacheDict.Add(faction, tex);
        }

    }
}
