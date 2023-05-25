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

        //Verse.Widgets.CanDrawIconFor(Def)

        //Verse.Widgets.DefIcon(Rect, Def, ThingDef, float, ThingStyleDef, bool, Color?, Mateiral, int?)

        //untested: not sure what this is in-game
        [HarmonyPatch(typeof(InteractionDef))]
        [HarmonyPatch("GetSymbol")]
        public static class InteractionDef_GetSymbol_Postfix
        {
            public static void Postfix(InteractionDef __instance, ref Texture2D __result, Faction __0, Ideo __1)
            {
                if (__0 != null && __1 != null)
                {
                    if (IFIListHolder.chosenForward.Contains(__0) && __result == __0.def.FactionIcon)
                    {
                        __result = __1.Icon;
                    }
                }
            }
        }

        //tested: works
        [HarmonyPatch(typeof(Faction))]
        [HarmonyPatch("CommFloatMenuOption")]
        public static class Faction_CommFloatMenuOption_Postfix
        {
            public static void Postfix(Faction __instance, ref FloatMenuOption __result)
            {
                if (__result != null && IFIListHolder.chosenForward.Contains(__instance))
                {
                    FieldInfo itemIconField = typeof(FloatMenuOption).GetField("itemIcon", BindingFlags.Instance | BindingFlags.NonPublic);
                    itemIconField.SetValue(__result, __instance.ideos.PrimaryIdeo.Icon);
                }
            }
        }

        //RimWorld.CompUsable.Icon

        //tested: works
        [HarmonyPatch(typeof(Tradeable_RoyalFavor))]
        [HarmonyPatch("DrawIcon")]
        public static class Tradeable_RoyalFavor_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo drawHelper = typeof(HarmonyPatches).GetMethod("Tradeable_RoyalFavor_Helper", BindingFlags.Public | BindingFlags.Static);
                var codes = new List<CodeInstruction>(instructions);
                int startIndex = -1;
                int endIndex = -1;
                bool foundStartIndex = false;
                bool foundEndIndex = false;

                for (int i = 0; i < codes.Count; i++)
                {
                    if (foundStartIndex && foundEndIndex)
                        break;

                    if (foundEndIndex)
                    {
                        for (int j = endIndex; j >= 0; j--)
                        {
                            if (codes[j].opcode == OpCodes.Ldfld
                                && codes[j].operand.ToString().Contains("def"))
                            {
                                startIndex = j;
                                foundStartIndex = true;
                                break;
                            }
                        }
                    }

                    if (!foundEndIndex
                        && codes[i].opcode == OpCodes.Call
                        && codes[i].operand.ToString().Contains("DrawTextureRotated"))
                    {
                        endIndex = i;
                        foundEndIndex = true;
                        continue;
                    }
                }

                if (startIndex >= 0 && endIndex >= 0)
                {
                    //make new instructions
                    List<CodeInstruction> newCodes = new List<CodeInstruction>();
                    newCodes.Add(new CodeInstruction(OpCodes.Call, drawHelper));

                    //remove old instructions
                    codes.RemoveRange(startIndex, (endIndex - startIndex + 1));

                    //add new instructions
                    codes.InsertRange(startIndex, newCodes);
                }

                return codes.AsEnumerable();
            }
        }
        public static void Tradeable_RoyalFavor_Helper(Rect iconRect, Faction faction)
        {
            if (IFIListHolder.chosenForward.Contains(faction))
            {
                Widgets.DrawTextureRotated(iconRect, faction.ideos.PrimaryIdeo.Icon, 0f);
            }
            else
            {
                Widgets.DrawTextureRotated(iconRect, faction.def.FactionIcon, 0f);
            }
        }

        //tested: works
        [HarmonyPatch(typeof(ColonistBarColonistDrawer))]
        [HarmonyPatch("DrawIcons")]
        public static class ColonistBarColonistDrawer_DrawIcons_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo drawHelper = typeof(HarmonyPatches).GetMethod("ColonistBarColonistDrawer_DrawIcons_Helper", BindingFlags.Public | BindingFlags.Static);
                var codes = new List<CodeInstruction>(instructions);
                int startIndex = -1;
                int endIndex = -1;
                //int defIndex = -1;
                bool foundStartIndex = false;
                bool foundEndIndex = false;

                //bool foundDefIndex = false;

                for (int i = 0; i < codes.Count; i++)
                {
                    if (foundStartIndex && foundEndIndex)
                        break;

                    if (foundEndIndex)
                    {
                        for (int j = endIndex; j >= 0; j--)
                        {
                            if (codes[j].opcode == OpCodes.Ldfld
                                && codes[j].operand.ToString().Contains("def"))
                            {
                                startIndex = j;
                                foundStartIndex = true;
                                break;
                            }
                        }
                        continue;
                    }

                    if (!foundEndIndex 
                        && codes[i].opcode == OpCodes.Callvirt
                        && codes[i].operand.ToString().Contains("get_FactionIcon"))
                    {
                        endIndex = i;
                        foundEndIndex = true;
                        continue;
                    }

                    /* found a simpler way to do this
                    if (!foundDefIndex
                        && codes[i].opcode == OpCodes.Callvirt
                        && codes[i].operand.ToString().Contains("get_FactionIcon"))
                    {
                        defIndex = i;
                        foundDefIndex = true;
                        continue;
                    }
                    */
                }

                if (startIndex >= 0 && endIndex >= 0)
                {
                    //make new instructions
                    List<CodeInstruction> newCodes = new List<CodeInstruction>();
                    newCodes.Add(new CodeInstruction(OpCodes.Call, drawHelper));

                    //remove old instructions
                    codes.RemoveRange(startIndex, (endIndex - startIndex + 1));

                    //add new instructions
                    codes.InsertRange(startIndex, newCodes);
                }

                return codes.AsEnumerable();
            }
        }
        public static Texture2D ColonistBarColonistDrawer_DrawIcons_Helper(Faction faction)
        {
            Texture2D iconTexture;

            if (IFIListHolder.chosenForward.Contains(faction))
            {
                iconTexture = faction.ideos.PrimaryIdeo.Icon;
            }
            else
            {
                iconTexture = faction.def.FactionIcon;
            }

            return iconTexture;
        }

        /*
        public static void DrawIconsHelper2(Faction faction, List<object> iconDrawCallList, ColonistBarColonistDrawer cbcd)
        {
            // Assuming you have an instance of ColonistBarColonistDrawer called 'drawer'
            Type drawerType = typeof(ColonistBarColonistDrawer);
            Type iconDrawCallType = drawerType.GetNestedType("IconDrawCall", BindingFlags.NonPublic);

            // Create an instance of the IconDrawCall struct using Activator.CreateInstance
            object iconDrawCall = Activator.CreateInstance(iconDrawCallType, new object[] { null, null, null });

            // Get the fields of the IconDrawCall struct
            FieldInfo textureField = iconDrawCallType.GetField("texture", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo tooltipField = iconDrawCallType.GetField("tooltip", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo colorField = iconDrawCallType.GetField("color", BindingFlags.Instance | BindingFlags.Public);

            Texture2D texture;
            string tooltip = null;
            Color? color;

            if (IFIListHolder.chosenForward.Contains(faction))
            {
                texture = faction.ideos.PrimaryIdeo.Icon;
                color = faction.ideos.PrimaryIdeo.Color;
            }
            else
            {
                texture = faction.def.FactionIcon;
                color = faction.color;
            }

            // Set the values of the fields
            textureField.SetValue(iconDrawCall, texture);
            tooltipField.SetValue(iconDrawCall, tooltip);
            colorField.SetValue(iconDrawCall, color);

            iconDrawCallList.Add(iconDrawCall);
        }
        */

        //RimWorld.Dialog_BeginRitual.CalculatePawnPortraitIcons(Pawn)

        //tested: works
        [HarmonyPatch(typeof(FactionUIUtility))]
        [HarmonyPatch("DrawFactionRow")]
        public static class FactionUIUtility_DrawFactionRow_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo drawHelper = typeof(HarmonyPatches).GetMethod("FactionUIUtility_DrawFactionRow_Helper", BindingFlags.Public | BindingFlags.Static);

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
                if (startIndex >= 0 && endIndex >= 0)
                {
                    //generate the new instructions
                    List<CodeInstruction> newCodes = new List<CodeInstruction>();
                    //call my method
                    newCodes.Add(new CodeInstruction(OpCodes.Call, drawHelper));

                    //remove the old instruction
                    codes.RemoveRange(startIndex, (endIndex - startIndex + 1));
                    //insert the new instructions
                    codes.InsertRange(startIndex, newCodes);
                }

                return codes.AsEnumerable();
            }
        }
        public static void FactionUIUtility_DrawFactionRow_Helper(Rect position, Faction faction)
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

        //RimWorld.FactionUIUtility.DrawFactionIconWithTooltip(Rect, Faction)

        //RimWorld.PermitsCardUtility.DrawRecordsCard(Rect, Pawn)

        //untested: not sure what this is in-game
        [HarmonyPatch(typeof(PawnColumnWorker_Faction))]
        [HarmonyPatch("GetIconFor")]
        public static class PawnColumnWorker_Faction_GetIconFor_Postfix
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

        //RimWorld.Reward_Goodwill.<get_StackElements>b__3_0(Rect)

        //untested: not sure what this is in-game
        [HarmonyPatch(typeof(EscapeShip))]
        [HarmonyPatch("ExpandingIcon", MethodType.Getter)]
        public static class EscapeShip_ExpandingIcon_Postfix
        {
            public static void Postfix(EscapeShip __instance, ref Texture2D __result)
            {
                if (__instance.HasMap && __instance.Faction != null)
                {
                    if (IFIListHolder.chosenForward.Contains(__instance.Faction))
                    {
                        __result = __instance.Faction.ideos.PrimaryIdeo.Icon;
                    }
                }
            }
        }

        //tested: works
        [HarmonyPatch(typeof(Settlement))]
        [HarmonyPatch("ExpandingIcon", MethodType.Getter)]
        public static class Settlement_ExpandingIcon_Postfix
        {
            public static void Postfix(Settlement __instance, ref Texture2D __result)
            {
                if (IFIListHolder.chosenForward.Contains(__instance.Faction))
                {
                    __result = __instance.Faction.ideos.PrimaryIdeo.Icon;
                }
            }
        }

        //RimWorld.Planet.WorldFactionsUIUtility.DoWindowContents(Rect, List<FactionDef>, bool)

        //RimWorld.Planet.WorldFactionsUIUtility.DoRow(Rect, FactionDef, List<FactionDef>, int)

        //RimWorld.RoyalTitlePermitWorker_CallAid

        //RimWorld.RoyalTitlePermitWorker_CallLaborers

        //RimWorld.RoyalTitlePermitWorker_CallShuttle

        //RimWorld.RoyalTitlePermitWorker_DropResources

        //RimWorld.RoyalTitlePermitWorker_OrbitalStrike

        //RimWorld.Pawn_RoyaltyTracker.<GetGizmos>d__70.MoveNext()

        //RimWorld.CharacterCardUtility.<>c__DisplayClass41_0.<DoTopStack>b__10(Rect)

        //RimWorld.CharacterCardUtility.<>c__DisplayClass41_1.<DoTopStack>b__11(Rect)

        //RimWorld.CharacterCardUtility.<>c__DisplayClass41_5.<DoTopStack>b__15(Rect)

        //RimWorld.Reward_BestowingCeremony.<get_StackElements>d__7.MoveNext()

        
    }
}
