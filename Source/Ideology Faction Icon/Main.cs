using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using Verse;
using UnityEngine;

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
            return "IFI_ModName".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            GameComponent_FactionLists comp = Current.Game?.GetComponent<GameComponent_FactionLists>();
            bool inGame = comp != null;
            IdeoFactIconSettings.OverallBehavior behavior = inGame ? comp.overallBehavior : IdeoFactIconSettings.defaultBehavior;

            Text.Font = GameFont.Medium;

            Listing_Standard listingStandard = new Listing_Standard { maxOneColumn = true };
            listingStandard.Begin(inRect);

            listingStandard.Label("IFI_Menu_1".Translate());
            listingStandard.Label("IFI_Menu_2".Translate());
            listingStandard.EnumSelector(ref behavior, "", "", "");
            if (inGame)
            {
                if (comp.overallBehavior != behavior)
                {
                    comp.overallBehavior = behavior;
                    comp.needRecache = true;
                }
            }
            else
            {
                IdeoFactIconSettings.defaultBehavior = behavior;
            }

            if (behavior == IdeoFactIconSettings.OverallBehavior.IFI_Choose)
            {
                if (inGame)
                {
                    Text.Font = GameFont.Small;

                    float availableHeight = Mathf.Max(0f, inRect.height - listingStandard.CurHeight - 10f);
                    float viewportHeight = Mathf.Min(availableHeight, Mathf.Max(200f, inRect.height * 0.7f));
                    Rect outRect = listingStandard.GetRect(viewportHeight);
                    Widgets.DrawBox(outRect);

                    Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, Mathf.Max(outRect.height, ifiSettings.calculatedScrollHeight));
                    Widgets.BeginScrollView(outRect, ref ifiSettings.scrollPosition, viewRect);

                    Listing_Standard scrollList = new Listing_Standard {maxOneColumn = true};
                    scrollList.Begin(new Rect(10f, 0f, viewRect.width - 10f, viewRect.height));

                    foreach (var faction in comp.iconDictionary.Keys.ToList())
                    {
                        IdeoFactIconSettings.IconBehavior iconBehavior = comp.iconDictionary[faction]
                            ? IdeoFactIconSettings.IconBehavior.IFI_UseIdeoForFaction
                            : IdeoFactIconSettings.IconBehavior.IFI_Default;

                        IdeoFactIconSettings.ColorBehavior colorBehavior = comp.colorDictionary[faction]
                            ? IdeoFactIconSettings.ColorBehavior.IFI_UseIdeoligionColor
                            : IdeoFactIconSettings.ColorBehavior.IFI_UseFactionColor;

                        scrollList.Label(faction.Name);
                        if (faction.ideos?.PrimaryIdeo != null)
                        {
                            scrollList.EnumSelector(ref iconBehavior, "", "");
                            scrollList.EnumSelector(ref colorBehavior, "", "");
                        }
                        else
                        {
                            scrollList.Label("IFI_Menu_3".Translate());
                        }

                        comp.iconDictionary[faction] = iconBehavior == IdeoFactIconSettings.IconBehavior.IFI_UseIdeoForFaction;
                        comp.colorDictionary[faction] = colorBehavior == IdeoFactIconSettings.ColorBehavior.IFI_UseIdeoligionColor;
                        scrollList.Gap();
                    }

                    ifiSettings.calculatedScrollHeight = scrollList.CurHeight + 10f;
                    scrollList.End();
                    Widgets.EndScrollView();
                }
                else
                {
                    listingStandard.Label("IFI_Menu_4".Translate());
                }
            }
            listingStandard.Gap();

            listingStandard.End();
        }

        public override void WriteSettings()
        {
            GameComponent_FactionLists comp = Current.Game?.GetComponent<GameComponent_FactionLists>();
            if (comp != null)
            {
                comp.needRecache = true; 
            }

            base.WriteSettings();
        }

    }

    public class IdeoFactIconSettings : ModSettings
    {
        public enum OverallBehavior
        {
            IFI_Just_Player,
            IFI_All,
            IFI_Choose,
        }

        public enum IconBehavior
        {
            IFI_Default,
            IFI_UseIdeoForFaction
        }

        public enum ColorBehavior
        {
            IFI_UseFactionColor,
            IFI_UseIdeoligionColor
        }

        public static OverallBehavior defaultBehavior = OverallBehavior.IFI_Just_Player;

        public float calculatedScrollHeight = 0f;
        public Vector2 scrollPosition = new Vector2();

        public override void ExposeData()
        {
            Scribe_Values.Look(ref defaultBehavior, "defaultBehavior");
        }
    }
}
