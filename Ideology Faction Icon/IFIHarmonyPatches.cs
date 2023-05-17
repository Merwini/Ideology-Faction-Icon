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

        #region VanillaCode
        /*
          
         *  GUI.DrawTexture(position, faction.def.FactionIcon);
          
         * 	IL_00b6: ldloc.0
	     *  IL_00b7: ldfld class RimWorld.Faction RimWorld.FactionUIUtility/'<>c__DisplayClass14_0'::faction
	     *  IL_00bc: ldfld class RimWorld.FactionDef RimWorld.Faction::def
	     *  IL_00c1: callvirt instance class [UnityEngine.CoreModule]UnityEngine.Texture2D RimWorld.FactionDef::get_FactionIcon()
	     *  IL_00c6: call void [UnityEngine.IMGUIModule]UnityEngine.GUI::DrawTexture(valuetype [UnityEngine.CoreModule]UnityEngine.Rect, class [UnityEngine.CoreModule]UnityEngine.Texture)

         */
        #endregion

        #region GoalCode
        /*
          
         * if (IFIListHolder.chosenForward.Contains(faction)
         * {
         *      GUI.DrawTexture(position, faction.ideos.PrimaryIdeo.Icon);
         * }
         * else
         * {
         *      GUI.DrawTexture(position, faction.def.FactionIcon);
         * }
          
         */
        #endregion

        [HarmonyPatch(typeof(FactionUIUtility))]
        [HarmonyPatch("DrawFactionRow")]
        public static class FactionUIUtility_DrawFactionRow_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilGen)
            {
                var callDrawTexture = typeof(GUI).GetMethod("DrawTexture", new[] { typeof(Rect), typeof(Texture) });
                //var codeDrawTexture = new CodeInstruction(OpCodes.Call, callDrawTexture);

                var chosenForwardGetter = AccessTools.Property(typeof(IFIListHolder), "chosenForward").GetGetMethod();
                var primaryIdeoGetter = AccessTools.Property(typeof(Ideo), "PrimaryIdeo").GetGetMethod();
                var iconGetter = AccessTools.Property(typeof(Ideo), "Icon").GetGetMethod();


                bool foundDrawTexture = false;
                int startIndex = -1;
                int endIndex = -1;

                var codes = new List<CodeInstruction>(instructions);
                //loop through code instructions
                for (int i = 0; i < codes.Count; i++)
                {
                    //end the loop if the call has been found
                    if (foundDrawTexture)
                    {
                        break;
                    }
                    //if the faction is put on the stack
                    if (codes[i].opcode == OpCodes.Ldloc_0)
                    {
                        //start a subloop looking through the subsequent instructions
                        for (int j = i + 1; j < codes.Count; j++)
                        {
                            //if the DrawTexture call is found
                            if (codes[j].opcode == OpCodes.Call)
                            {
                                var methOperand = codes[j].operand as System.Reflection.MethodInfo;
                                if (methOperand == callDrawTexture)
                                {
                                    foundDrawTexture = true;
                                    startIndex = i;
                                    endIndex = j;
                                    break;
                                }
                            }
                            //if the faction is put on the stack again without finding the DrawTexture call, the main loop starts back up where the subloop left off
                            else if (codes[j].opcode == OpCodes.Ldloc_0)
                            {
                                i = j;
                                break;
                            }
                        }
                    }
                }

                //only modify the instructions if the above loop was able to set the startIndex and endIndex
                if (startIndex > 1 && endIndex > 1)
                {
                    //TODO generate the new instructions
                    List<CodeInstruction> newCodes = new List<CodeInstruction>();

                    //make some labels
                    Label ifLabel = ilGen.DefineLabel();
                    Label elseLabel = ilGen.DefineLabel();

                    // load faction onto the stack twice
                    newCodes.Add(new CodeInstruction(OpCodes.Ldloc_0));
                    newCodes.Add(new CodeInstruction(OpCodes.Ldloc_0));





                    //replace the old instructions with the new ones
                    codes.InsertRange(startIndex, newCodes);
                }

                return codes.AsEnumerable();
            }
        }

    }
}
