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
using System.Reflection;
using System.Reflection.Emit;

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

        //TODO untested
        [HarmonyPatch(typeof(RimWorld.PawnColumnWorker_Faction))]
        [HarmonyPatch("GetIconFor")]
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

        [HarmonyPatch(typeof(FactionUIUtility))]
        [HarmonyPatch("DrawFactionRow")]
        public static class FactionUIUtility_DrawFactionRow_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo forwardDrawHelper = typeof(HarmonyPatches).GetMethod("ForwardDrawHelper", BindingFlags.Public | BindingFlags.Static);

                bool foundEndIndex = false;
                bool foundStartIndex = false;
                int startIndex = -1;
                int endIndex = -1;

                var codes = new List<CodeInstruction>(instructions);
                var callDrawTexture = typeof(GUI).GetMethod("DrawTexture", new[] { typeof(Rect), typeof(Texture) });

                //loop through code instructions
                for (int i = 0; i < codes.Count; i++)
                {
                    if (foundEndIndex && foundStartIndex)
                        break;

                    if (foundEndIndex)
                    {
                        for (int j = endIndex; j > 0; j--)
                        {
                            if (codes[j].opcode == OpCodes.Ldfld
                                && codes[j].operand.ToString().Contains("FactionDef"))
                            {
                                startIndex = j;
                                foundStartIndex = true;
                                break;
                            }
                        }
                    }
                        
                    if (!foundEndIndex 
                        && codes[i].opcode == OpCodes.Call
                        && codes[i].operand.ToString().Contains("DrawTexture"))
                    {
                        endIndex = i;
                        foundEndIndex = true;
                        continue;
                    }
                }

                //only modify the instructions if the above loop was able to set the startIndex and endIndex
                if (startIndex > 1 && endIndex > 1)
                {
                    //generate the new instructions
                    List<CodeInstruction> newCodes = new List<CodeInstruction>();
                    //call my method
                    newCodes.Add(new CodeInstruction(OpCodes.Call, forwardDrawHelper));

                    //remove the old instruction
                    codes.RemoveRange(startIndex, (endIndex-startIndex+1));
                    //insert the new instructions
                    codes.InsertRange(startIndex, newCodes);
                }

                return codes.AsEnumerable();
            }
        }
        public static void ForwardDrawHelper(Rect position, Faction faction)
        {
            if (IFIListHolder.chosenForward.Contains(faction))
            {
                GUI.DrawTexture(position, faction.ideos.PrimaryIdeo.Icon);
            }
            else
            {
                GUI.DrawTexture(position, faction.def.FactionIcon);
            }
        }
    }
}
