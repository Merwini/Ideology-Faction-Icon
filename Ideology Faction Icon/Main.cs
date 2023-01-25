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
    [StaticConstructorOnStartup]
    public partial class Base
    {
        public static List<Texture2D> ideoIcon = new List<Texture2D>();
    }

    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        private static readonly Type patchType = typeof(HarmonyPatches);
        static HarmonyPatches()
        {
            Harmony harmony = new Harmony(id: "rimWorld.nuff.ideofacticon");
            harmony.Patch(AccessTools.Method(typeof(WorldInterface), nameof(WorldInterface.WorldInterfaceUpdate)), prefix: new HarmonyMethod(patchType, nameof(WorldInterfaceUpdatePrefix))); 

            /*
            HarmonyInstance harmonyInstance = HarmonyInstance.Create("your_instance_name");
            harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
            */
        }

        public static void WorldInterfaceUpdatePrefix()
        {
            Faction pFaction = Find.FactionManager.FirstFactionOfDef(DefDatabase<FactionDef>.GetNamed("PlayerColony", true));
            bool flag = pFaction.ideos.PrimaryIdeo != null;
            if (flag)
            {
                SetFactionIcon(ref pFaction, GetIdeoIcon(ref pFaction));
            }
        }

        private static Texture2D GetIdeoIcon(ref Faction pFaction)
        {
            Ideo mainIdeo = pFaction.ideos.PrimaryIdeo;
            Texture2D ideoIcon = mainIdeo.iconDef.Icon;
            return ideoIcon;
        }


        private static void SetFactionIcon(ref Faction pFaction, Texture2D newIcon)
        {
            Type reflectionSucks = typeof(FactionDef);
            FieldInfo reflectionBlows = reflectionSucks.GetField("factionIcon", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            reflectionBlows.SetValue(pFaction.def, newIcon);

            //pFaction.color = pFaction.ideos.PrimaryIdeo.colorDef.color;

            Color ideoColor = pFaction.ideos.PrimaryIdeo.colorDef.color;
            Color newColor = new Color(ideoColor.r, ideoColor.g, ideoColor.b);

            foreach (RimWorld.Planet.Settlement settlement in Find.WorldObjects.SettlementBases)
            {
                if (settlement.Faction.IsPlayer)
                {
                    settlement.Material.color = newColor;
                    //settlement.Material.color = pFaction.ideos.PrimaryIdeo.colorDef.color;
                }
            }
        }
    }
}
