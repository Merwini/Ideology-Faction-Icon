using System;
using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;

namespace nuff.Ideology_Faction_Icon
{
    public class GameComponent_FactionLists : GameComponent
    {
        public static GameComponent_FactionLists Instance { get; private set; }

        public IdeoFactIconSettings.OverallBehavior overallBehavior;

        internal Dictionary<Faction, bool> iconDictionary;
        internal Dictionary<Faction, bool> colorDictionary;

        List<Faction> iconFactListTmp;
        List<bool> iconBehaviorListTmp;

        List<Faction> colorFactListTmp;
        List<bool> colorBehaviorListTmp;

        public Dictionary<Faction, Texture2D> iconCacheDict;
        public Dictionary<Faction, Color?> colorCacheDict;
        public bool needRecache = true;

        public GameComponent_FactionLists(Game game)
        {
            iconDictionary = new Dictionary<Faction, bool>();
            colorDictionary = new Dictionary<Faction, bool>();
            iconCacheDict = new Dictionary<Faction, Texture2D>();
            colorCacheDict = new Dictionary<Faction, Color?>();
            overallBehavior = IdeoFactIconSettings.defaultBehavior;

            Instance = this;
        }

        public override void StartedNewGame()
        {
            needRecache = true;
        }

        public override void LoadedGame()
        {
            needRecache = true;
        }

        public override void FinalizeInit()
        {
            base.FinalizeInit();
            
            LongEventHandler.ExecuteWhenFinished(() =>
            {
                if (Current.Game?.GetComponent<GameComponent_FactionLists>() != this)
                    return;

                Instance = this;
                RecacheDictionaries();
            });
        }

        public override void GameComponentTick()
        {
            if (needRecache)
            {
                RecacheDictionaries();
            }
        }

        public void PopulateIconDictionary()
        {
            iconDictionary ??= new Dictionary<Faction, bool>();
            colorDictionary ??= new Dictionary<Faction, bool>();

            foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
            {
                TryAddFaction(faction);
            }
            needRecache = true;
        }

        public void RecacheDictionaries()
        {
            PopulateIconDictionary();

            Dictionary<Faction, Texture2D> newIconDict = new Dictionary<Faction, Texture2D>();
            foreach (var entry in iconDictionary)
            {
                RecacheIconSingle(entry.Key, newIconDict);
            }
            iconCacheDict = newIconDict;

            Dictionary<Faction, Color?> newColorDict = new Dictionary<Faction, Color?>();
            foreach (var entry in colorDictionary)
            {
                RecacheColorSingle(entry.Key, newColorDict);
            }
            colorCacheDict = newColorDict;

            needRecache = false;
        }

        private void RecacheIconSingle(Faction faction, Dictionary<Faction, Texture2D> newDict)
        {
            bool behavior;
            switch (overallBehavior)
            {
                case IdeoFactIconSettings.OverallBehavior.IFI_All:
                    behavior = true;
                    break;
                case IdeoFactIconSettings.OverallBehavior.IFI_Just_Player:
                    behavior = faction.IsPlayer;
                    break;
                default:
                    behavior = iconDictionary.TryGetValue(faction, out bool selected) && selected;
                    break;
            }

            Texture2D tex = null;
            if (behavior)
            {
                tex = faction.ideos?.PrimaryIdeo?.Icon;
            }
            if (tex == null) // covers !behavior and null Icon
            {
                tex = faction.def.FactionIcon;
            }

            newDict.Add(faction, tex);
        }

        private void RecacheColorSingle(Faction faction, Dictionary<Faction, Color?> newDict)
        {
            bool behavior;
            switch (overallBehavior)
            {
                case IdeoFactIconSettings.OverallBehavior.IFI_Choose:
                    behavior = colorDictionary.TryGetValue(faction, out bool selected) && selected;
                    break;
                default:
                    behavior = false;
                    break;
            }

            Color? color = null;

            if (behavior)
            {
                color = faction.ideos?.PrimaryIdeo?.Color;
            }

            newDict.Add(faction, color);
        }

        internal void TryAddFaction(Faction faction)
        {
            needRecache = true;

            if (!iconDictionary.ContainsKey(faction))
            {
                iconDictionary[faction] = true;
            }
            if (!colorDictionary.ContainsKey(faction))
            {
                colorDictionary[faction] = false;
            }
        }

        internal void TryRemoveFaction(Faction faction)
        {
            needRecache = true;

            if (iconDictionary.ContainsKey(faction))
            {
                iconDictionary.Remove(faction);
            }
            if (colorDictionary.ContainsKey(faction))
            {
                colorDictionary.Remove(faction);
            }
        }

        internal Texture2D GetTextureForFaction(Faction faction)
        {
            if (faction == null)
                return null;

            if (iconCacheDict != null && iconCacheDict.TryGetValue(faction, out Texture2D tex) && tex != null)
                return tex;

            return faction.def.FactionIcon;
        }

        internal Color? GetColorForFaction(Faction faction)
        {
            if (faction == null)
                return default;

            if (colorCacheDict != null && colorCacheDict.TryGetValue(faction, out Color? col) && col != default)
                return col;

            return null;
        }

        public override void ExposeData()
        {
            Scribe_Collections.Look(ref iconDictionary, "iconDictionary", LookMode.Reference, LookMode.Value, ref iconFactListTmp, ref iconBehaviorListTmp);
            Scribe_Collections.Look(ref colorDictionary, "colorDictionary", LookMode.Reference, LookMode.Value, ref colorFactListTmp, ref colorBehaviorListTmp);
            Scribe_Values.Look(ref overallBehavior, "overallBehavior", IdeoFactIconSettings.defaultBehavior);
        }
    }
}
