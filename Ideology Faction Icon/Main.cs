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

namespace Ideology_Faction_Icon
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
            GameComponent_FactionLists factionListHolder = null;

            Text.Font = GameFont.Medium;

            if (Current.Game != null)
            {
                factionListHolder = Current.Game.GetComponent<GameComponent_FactionLists>();
            }
            Listing_Standard listingStandard = new Listing_Standard();
            listingStandard.Begin(inRect);

            listingStandard.Label("Which factions should use their ideoligion icon as their faction icon?");
            listingStandard.EnumSelector(ref ifiSettings.ideoAsFact, "", "", "");
            if (ifiSettings.ideoAsFact == IdeoFactIconSettings.CustomizeSettings.Choose)
            {
                if (Current.Game != null)
                {
                    listingStandard.ListControl(inRect, ref factionListHolder.unchosenForward, ref factionListHolder.chosenForward, ref factionListHolder.chosenReverse,
                        ref ifiSettings.searchTerm1, ref ifiSettings.leftScrollPosition1, ref ifiSettings.rightScrollPosition1, ref ifiSettings.leftSelectedObject1, ref ifiSettings.rightSelectedObject1,
                        "Enable Icon Changing for:", 0.3f);
                }
                else
                {
                    listingStandard.Label("Specific factions can only be chosen while in-game. Please load your save first.");
                }
            }
            listingStandard.Gap();

            listingStandard.Label("Which factions should use their faction icon as their ideoligion icon? (reverse)");
            listingStandard.EnumSelector(ref ifiSettings.factAsIdeo, "", "", "");
            if (ifiSettings.factAsIdeo == IdeoFactIconSettings.CustomizeSettings.Choose)
            {
                if (Current.Game != null)
                {
                    listingStandard.ListControl(inRect, ref factionListHolder.unchosenReverse, ref factionListHolder.chosenReverse, ref factionListHolder.chosenForward,
                        ref ifiSettings.searchTerm2, ref ifiSettings.leftScrollPosition2, ref ifiSettings.rightScrollPosition2, ref ifiSettings.leftSelectedObject2, ref ifiSettings.rightSelectedObject2, 
                        "Enable Icon Changing for:", 0.3f);
                }
                else
                {
                    listingStandard.Label("Specific factions can only be chosen while in-game. Please load your save first.");
                }
            }
            listingStandard.Gap();
            listingStandard.CheckboxLabeled("Change Player Faction Color", ref IdeoFactIconSettings.changePlayerIconColor, "Change the player's faction settlement icon color to match the primary ideoligion's color.");
            listingStandard.Gap();
            listingStandard.CheckboxLabeled("Change Nonplayer Faction Colors", ref IdeoFactIconSettings.changeNonplayerColors, "Change all other factions' settlement icons to match their primary ideoligion's color.");
            listingStandard.End();
        }
    }

    public class IdeoFactIconSettings : ModSettings
    {
        public enum CustomizeSettings
        {
            All,
            Choose,
            None,
        }

        public static bool changePlayerIcon = true;
        public static bool changePlayerIconColor = true;
        public static bool changeNonplayerIcons = false;
        public static bool changeNonplayerColors = false;
        public static bool reverseIcon = false;

        public CustomizeSettings ideoAsFact = CustomizeSettings.None;
        public CustomizeSettings factAsIdeo = CustomizeSettings.None;

        public string searchTerm1 = "";
        public Vector2 leftScrollPosition1 = new Vector2();
        public Vector2 rightScrollPosition1 = new Vector2();
        public Faction leftSelectedObject1 = null;
        public Faction rightSelectedObject1 = null;

        public string searchTerm2 = "";
        public Vector2 leftScrollPosition2 = new Vector2();
        public Vector2 rightScrollPosition2 = new Vector2();
        public Faction leftSelectedObject2 = null;
        public Faction rightSelectedObject2 = null;

        public override void ExposeData()
        {
            Scribe_Values.Look(ref changePlayerIcon, "changePlayerIcon");
            Scribe_Values.Look(ref changePlayerIconColor, "changePlayerIconColor");
            Scribe_Values.Look(ref changeNonplayerIcons, "changeNonplayerIcons");
            Scribe_Values.Look(ref changeNonplayerColors, "changeNonplayerColors");
            Scribe_Values.Look(ref ideoAsFact, "ideoAsFact");
            Scribe_Values.Look(ref factAsIdeo, "factAsIdeo");
            base.ExposeData();
        }
                
    }

    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        private static readonly Type patchType = typeof(HarmonyPatches);
        static HarmonyPatches()
        {
            Harmony harmony = new Harmony(id: "rimworld.nuff.ideofacticon");
            harmony.Patch(AccessTools.Method(typeof(WorldInterface), nameof(WorldInterface.WorldInterfaceUpdate)), prefix: new HarmonyMethod(patchType, nameof(WorldInterfaceUpdatePrefix)));
        }

        public static void WorldInterfaceUpdatePrefix()
        {
            if (IdeoFactIconSettings.changePlayerIcon)
            {
                Faction pFaction = Find.FactionManager.OfPlayer;
                bool flag1 = pFaction.ideos != null;
                bool flag2 = false;
                if (flag1)
                {
                    flag2 = pFaction.ideos.PrimaryIdeo != null;
                }
                if (flag2)
                {
                    SetFactionIcon(ref pFaction, GetIdeoIcon(ref pFaction));
                }
            }

            if (IdeoFactIconSettings.changePlayerIconColor)
            {
                //Faction pFaction = Find.FactionManager.FirstFactionOfDef(DefDatabase<FactionDef>.GetNamed("PlayerColony", true));
                Faction pFaction = Find.FactionManager.OfPlayer;
                bool flag1 = pFaction.ideos != null;
                bool flag2 = false;
                if (flag1)
                {
                    flag2 = pFaction.ideos.PrimaryIdeo != null;
                }

                if (flag2)
                {
                    Color pFactionColor = GetColor(ref pFaction);
                    foreach (RimWorld.Planet.Settlement settlement in Find.WorldObjects.SettlementBases)
                    {
                        if (settlement.Faction.IsPlayer)
                        {
                            SetSettlementColor(settlement, pFactionColor);
                        }
                    }
                }
            }

            if (IdeoFactIconSettings.changeNonplayerIcons)
            {
                List<Faction> npcFactions = new List<Faction>();
                foreach (Faction faction in Find.FactionManager.AllFactionsListForReading)
                {
                    if (!faction.IsPlayer)
                    {
                        npcFactions.Add(faction);
                    }
                }

                Faction aFaction;
                foreach (Faction faction in npcFactions)
                {
                    aFaction = faction;
                    bool flag1 = aFaction.ideos != null;
                    bool flag2 = false;
                    if (flag1)
                    {
                        flag2 = aFaction.ideos.PrimaryIdeo != null;
                    }
                    if (flag2)
                    {
                        SetFactionIcon(ref aFaction, GetIdeoIcon(ref aFaction));
                    }
                }
            }

            if (IdeoFactIconSettings.changeNonplayerColors)
            {
                Faction aFaction;
                foreach (RimWorld.Planet.Settlement settlement in Find.WorldObjects.SettlementBases)
                {
                    if (!settlement.Faction.IsPlayer)
                    {
                        aFaction = settlement.Faction;
                        bool flag1 = aFaction.ideos != null;
                        bool flag2 = false;
                        if (flag1)
                        {
                            flag2 = aFaction.ideos.PrimaryIdeo != null;
                        }
                        if (flag2)
                        {
                            SetSettlementColor(settlement, GetColor(ref aFaction));
                        }
                    }
                }
            }
        }

        private static Texture2D GetIdeoIcon(ref Faction faction)
        {
            Ideo mainIdeo = faction.ideos.PrimaryIdeo;
            Texture2D ideoIcon = mainIdeo.iconDef.Icon;
            return ideoIcon;
        }

        private static Color GetColor(ref Faction faction)
        {
            Color ideoColor = faction.ideos.PrimaryIdeo.colorDef.color;
            //Color newColor = new Color(ideoColor.r, ideoColor.g, ideoColor.b);
            return new Color(ideoColor.r, ideoColor.g, ideoColor.b);
        }

        private static void SetFactionIcon(ref Faction faction, Texture2D newIcon)
        {
            Type reflectionSucks = typeof(FactionDef);
            FieldInfo reflectionBlows = reflectionSucks.GetField("factionIcon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            reflectionBlows.SetValue(faction.def, newIcon);
        }

        private static void SetSettlementColor(RimWorld.Planet.Settlement settlement, Color color)
        {
            settlement.Material.color = color;
        }
    }
}
