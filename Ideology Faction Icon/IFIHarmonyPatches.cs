using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using HarmonyLib;

namespace Ideology_Faction_Icon
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            Harmony harmony = new Harmony("nuff.rimworld.ideology_faction_icon");
            harmony.PatchAll();
        }

        [HarmonyPatch(typeof(Settlement))]
        [HarmonyPatch("ExpandingIcon", MethodType.Getter)]
        public static class Settlement_ExpandingIcon_Patch
        {
            public static void Postfix(Settlement __instance, ref Texture2D __result)
            {
                if (IFIListHolder.chosenForward.Contains(__instance.Faction))
                {
                    __result = __instance.Faction.ideos.PrimaryIdeo.Icon;
                }
            }
        }

        [HarmonyPatch(typeof(RimWorld.PawnColumnWorker_Faction), "GetIconFor")]
        public static class PawnColumnWorker_Faction_GetIconFor_Patch
        {
            public static void Postfix(Pawn pawn, ref Texture2D __result)
            {
                if (pawn != null && (__result != null))
                {
                    if (IFIListHolder.chosenForward.Contains(pawn.Faction))
                    {
                        __result = pawn.Faction.ideos.PrimaryIdeo.Icon;
                    }
                }
            }
        }

        public static void HandleResult(Texture2D result)
        {

        }
    }

   
}
