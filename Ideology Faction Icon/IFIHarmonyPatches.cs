using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Verse;
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
            public static bool Prefix(Settlement __instance, ref Texture2D __result)
            {
                // Your if-else statement goes here
                // Make sure to set the value of __result accordingly
                if (IFIListHolder.chosenForward.Contains(__instance.Faction))
                {
                    __result = __instance.Faction.ideos.PrimaryIdeo.Icon;
                    return false;
                }

                return true;
            }
        }



        public static void HandleResult(Texture2D result)
        {

        }
    }

   
}
