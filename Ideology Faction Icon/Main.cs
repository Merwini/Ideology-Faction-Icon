using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using UnityEngine;
using HarmonyLib;
using System.Reflection;

namespace nuff.Ideology_Faction_Icon
{
    [StaticConstructorOnStartup]
    public class IdeoFactIcon : Mod
    {
        IdeoFactIconSettings ifiSettings;
        public IdeoFactIcon(ModContentPack content) : base(content)
        {
            this.ifiSettings = GetSettings<IdeoFactIconSettings>();
        }

        public override string SettingsCategory()
        {
            return "Ideoligion Icon as Faction Icon";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            GameComponent_FactionLists comp = null;

            Text.Font = GameFont.Medium;

            if (Current.Game != null)
            {
                comp = Current.Game.GetComponent<GameComponent_FactionLists>();
            }
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            listingStandard.Label("Which factions should use their ideoligion icon as their faction icon?");
            listingStandard.Label("(Icons won't update while the game is paused.)");
            listingStandard.EnumSelector(ref IdeoFactIconSettings.ideoAsFact, "", "", "");
            if (IdeoFactIconSettings.ideoAsFact == IdeoFactIconSettings.CustomizeSettings.Choose)
            {
                if (Current.Game != null && comp != null)
                {
                    Text.Font = GameFont.Small;

                    Rect outRect = listingStandard.GetRect(400f);
                    Widgets.DrawBox(outRect);

                    float colorCount = comp.iconDictionary.Values.Where(v => v == true).Count();

                    float scrollContentHeight = comp.iconDictionary.Count * 60f + colorCount * Text.LineHeight + 10f; // Better spacing
                    Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, scrollContentHeight);
                    Widgets.BeginScrollView(outRect, ref ifiSettings.scrollPosition, viewRect);

                    Listing_Standard scrollList = new Listing_Standard();
                    scrollList.Begin(viewRect);

                    foreach (var faction in comp.iconDictionary.Keys.ToList())
                    {
                        IdeoFactIconSettings.Behavior behavior = comp.iconDictionary[faction]
                            ? IdeoFactIconSettings.Behavior.UseIdeoForFaction
                            : IdeoFactIconSettings.Behavior.Default;

                        IdeoFactIconSettings.ColorBehavior colorBehavior = comp.colorDictionary[faction]
                            ? IdeoFactIconSettings.ColorBehavior.Ideoligion
                            : IdeoFactIconSettings.ColorBehavior.Faction;

                        scrollList.Label(faction.Name);
                        if (faction.ideos?.PrimaryIdeo != null)
                        {
                            scrollList.EnumSelector(ref behavior, "", "");
                            if (behavior == IdeoFactIconSettings.Behavior.UseIdeoForFaction)
                            {
                                scrollList.EnumSelector(ref colorBehavior, "", "");
                            }
                        }
                        else
                        {
                            scrollList.Label("has no ideoligion");
                        }

                        comp.iconDictionary[faction] = (behavior == IdeoFactIconSettings.Behavior.UseIdeoForFaction);
                        comp.colorDictionary[faction] = (colorBehavior == IdeoFactIconSettings.ColorBehavior.Ideoligion);
                        scrollList.Gap();
                    }

                    scrollList.End();
                    Widgets.EndScrollView();
                }
                else
                {
                    listingStandard.Label("Specific factions can only be chosen while in-game. Please load your save first.");
                }
            }
            listingStandard.Gap();

            listingStandard.End();
        }

        public override void WriteSettings()
        {
            if (Current.Game != null)
            {
                GameComponent_FactionLists comp = Current.Game.GetComponent<GameComponent_FactionLists>();
                comp.PopulateIconDictionary();
            }

            base.WriteSettings();
        }

    }

    public class IdeoFactIconSettings : ModSettings
    {
        public enum CustomizeSettings
        {
            Just_Player,
            All,
            Choose,
        }

        public enum Behavior
        {
            UseIdeoForFaction,
            Default
        }

        public enum ColorBehavior
        {
            Faction,
            Ideoligion
        }

        public static CustomizeSettings ideoAsFact = CustomizeSettings.Just_Player;

        public float calculatedScrollHeight = 0f;
        public Vector2 scrollPosition = new Vector2();

        public override void ExposeData()
        {
            Scribe_Values.Look(ref ideoAsFact, "ideoBehavior");
        }

    }
}
