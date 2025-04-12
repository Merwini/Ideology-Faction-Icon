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
    // ROADMAP
    // TODO: CUSTOMIZABLE SETTINGS
    // TODO: CHANGE TO ONLY OCCUR WHEN WORLD MAP IS OPENED INSTEAD OF EVERY TICK
    // TODO: ACCOUNT FOR NULLS

    [StaticConstructorOnStartup]
    public class IdeoFactIcon : Mod
    {
        IdeoFactIconSettings ifiSettings;
        public IdeoFactIcon(ModContentPack content) : base(content)
        {
            this.ifiSettings = GetSettings<IdeoFactIconSettings>();
        }

        /*
       ///  <param name = "inRect" >
        public override void DoSettingsWindowContents(Rect inRect)
        {
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);
            listingStandard.CheckboxLabeled("Change Player Faction Icon", ref IdeoFactIconSettings.changePlayerIcon, "Changes the player's faction settlement icon on the world map to match their primary ideoligion's icon.");
            listingStandard.CheckboxLabeled("Change Player Faction Color", ref IdeoFactIconSettings.changePlayerIconColor, "Change the player's faction settlement icon color to match the primary ideoligion's color.");
            listingStandard.CheckboxLabeled("Change Nonplayer Faction Icons", ref IdeoFactIconSettings.changeNonplayerIcons, "Changes all other factions' settlement icons to match their primary ideoligion's icon.");
            listingStandard.CheckboxLabeled("Change Nonplayer Faction Colors", ref IdeoFactIconSettings.changeNonplayerColors, "Change all other factions' settlement icons to match their primary ideoligion's color.");
            listingStandard.End();
            base.DoSettingsWindowContents(inRect);
        }
        */



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

                    float scrollContentHeight = comp.iconDictionary.Count * 50f + 10f; // Better spacing
                    Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, scrollContentHeight);
                    Widgets.BeginScrollView(outRect, ref ifiSettings.scrollPosition, viewRect);

                    Listing_Standard scrollList = new Listing_Standard();
                    scrollList.Begin(viewRect);

                    foreach (var faction in comp.iconDictionary.Keys.ToList())
                    {
                        IdeoFactIconSettings.Behavior behavior = comp.iconDictionary[faction]
                            ? IdeoFactIconSettings.Behavior.UseIdeoForFaction
                            : IdeoFactIconSettings.Behavior.Default;

                        scrollList.Label(faction.Name);
                        if (faction.ideos?.PrimaryIdeo != null)
                        {
                            scrollList.EnumSelector(ref behavior, "", "");
                        }
                        else
                        {
                            scrollList.Label("has no ideoligion");
                        }

                        comp.iconDictionary[faction] = (behavior == IdeoFactIconSettings.Behavior.UseIdeoForFaction);
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
                //if (comp != null)
                //{
                //    if (IdeoFactIconSettings.ideoAsFact == IdeoFactIconSettings.CustomizeSettings.All)
                //    {
                //        foreach (var key in comp.iconDictionary.Keys.ToList())
                //        {
                //            comp.iconDictionary[key] = true;
                //        }
                //    }
                //    else if (IdeoFactIconSettings.ideoAsFact == IdeoFactIconSettings.CustomizeSettings.Just_Player)
                //    {
                //        foreach (var key in comp.iconDictionary.Keys.ToList())
                //        {
                //            if (key.IsPlayer)
                //            {
                //                comp.iconDictionary[key] = true;
                //            }
                //            else
                //            {
                //                comp.iconDictionary[key] = false;
                //            }
                //        }
                //    }
                //    comp.needRecache = true;
                //}
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

        public static CustomizeSettings ideoAsFact = CustomizeSettings.Just_Player;
        public CustomizeSettings factAsIdeo = CustomizeSettings.Just_Player;

        public Vector2 scrollPosition = new Vector2();

        public override void ExposeData()
        {
            Scribe_Values.Look(ref ideoAsFact, "ideoBehavior");
        }

    }
}
