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

namespace nuff.Ideology_Faction_Icon
{
    [StaticConstructorOnStartup]
    public static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            Harmony harmony = new Harmony("nuff.rimworld.ideology_faction_icon");
            harmony.PatchAll();
        }

        public static Texture2D GetIconForFaction(Faction faction)
        {
            return GameComponent_FactionLists.iconCacheDict[faction];
        }

        //World with factions is generated and displayed while selecting a tile, before icons have a chance to be cached
        [HarmonyPatch(typeof(Page_SelectStartingSite))]
        [HarmonyPatch("PreOpen")]
        public static class Page_SelectStartingSite_PreOpen_Postfix
        {
            public static void Postfix()
            {
                //need to recahce right away so that settlements display correctly when player goes to choose a landing site
                GameComponent_FactionLists.RecacheIcons();
            }
        }

        //Call for icon recache when scenario description window opens
        [HarmonyPatch(typeof(ScenPart_GameStartDialog))]
        [HarmonyPatch("PostGameStart")]
        public static class ScenPart_GameStartDialog_PostGameStart_Postfix
        {
            public static void Postfix()
            {
                Current.Game.GetComponent<GameComponent_FactionLists>().needRecache = true;
            }
        }

        public static void GUI_DrawTexture_Helper(Rect position, Faction faction)
        {
            GUI.DrawTexture(position, GameComponent_FactionLists.iconCacheDict[faction]);
        }

        //Verse.Widgets.CanDrawIconFor(Def)
        //Doesn't need a patch

        //Verse.Widgets.DefIcon(Rect, Def, ThingDef, float, ThingStyleDef, bool, Color?, Mateiral, int?)
        //not sure this is necessary?
        //[HarmonyPatch(typeof(Widgets))]
        //[HarmonyPatch("DefIcon")]
        //public static class Widgets_DefIcon_Patch
        //{
        //    public static bool Prefix(Def def)
        //    {
        //        if (def is FactionDef)
        //        {
        //            Log.Warning("Widgets.DefIcon called for a FactionDef"); //TODO need to go up one or more method calls to properly patch this method, since it has no ref to the faction itself
        //        }
        //        return true;
        //    }
        //}

        //RimWorld.InteractionDef.GetSymbol(Faction, Ideo)
        //todo test
        [HarmonyPatch(typeof(InteractionDef))]
        [HarmonyPatch("GetSymbol")]
        public static class InteractionDef_GetSymbol_Postfix
        {
            public static void Postfix(InteractionDef __instance, ref Texture2D __result, Faction initiatorFaction)
            {
                if (__instance.symbolSource == InteractionSymbolSource.InitiatorFaction && initiatorFaction != null)
                {
                    __result = GameComponent_FactionLists.iconCacheDict[initiatorFaction];
                }
            }
        }

        //RimWorld.Faction.CommFloatMenuOption(Building_CommsConsole, Pawn)
        //Tested: works
        [HarmonyPatch(typeof(Faction))]
        [HarmonyPatch("CommFloatMenuOption")]
        public static class Faction_CommFloatMenuOption_Postfix
        {
            public static void Postfix(Faction __instance, ref FloatMenuOption __result)
            {
                if (__result != null)
                {
                    FieldInfo itemIconField = typeof(FloatMenuOption).GetField("iconTex", BindingFlags.Instance | BindingFlags.NonPublic);
                    itemIconField.SetValue(__result, GameComponent_FactionLists.iconCacheDict[__instance]);
                }
            }
        }

        //RimWorld.CompUsable.Icon
        //Only used for Summon Diabolus, looks like, so doesn't need patch

        //RimWorld.Tradeable_RoyalFavor.DrawIcon(Rect iconRect)
        //todo test
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
            Widgets.DrawTextureRotated(iconRect, GameComponent_FactionLists.iconCacheDict[faction], 0f);
        }

        //RimWorld.ColonistBarColonistDrawer.DrawIcons(Rect rect, Pawn colonist)
        //Tested: works
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
            return GameComponent_FactionLists.iconCacheDict[faction];
        }

        //RimWorld.Dialog_BeginRitual.CalculatePawnPortraitIcons(Pawn)
        //todo no longer exists?

        //RimWorld.FactionUIUtility.DrawFactionRow(Faction faction, float rowY, Rect fillRect)
        //Tested: works
        [HarmonyPatch(typeof(FactionUIUtility))]
        [HarmonyPatch("DrawFactionRow")]
        public static class FactionUIUtility_DrawFactionRow_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo drawHelper = typeof(HarmonyPatches).GetMethod(nameof(GUI_DrawTexture_Helper), BindingFlags.Public | BindingFlags.Static);

                bool foundEndIndex = false;
                bool foundStartIndex = false;
                int startIndex = -1;
                int endIndex = -1;

                var codes = new List<CodeInstruction>(instructions);
                //var callDrawTexture = typeof(GUI).GetMethod("DrawTexture", new[] { typeof(Rect), typeof(Texture) });

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

        //RimWorld.FactionUIUtility.DrawFactionIconWithTooltip(Rect, Faction)
        [HarmonyPatch(typeof(FactionUIUtility))]
        [HarmonyPatch("DrawFactionIconWithTooltip")]
        public static class FactionUIUtility_DrawFactionIconWithTooltip_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                MethodInfo drawHelper = typeof(HarmonyPatches).GetMethod(nameof(GUI_DrawTexture_Helper), BindingFlags.Public | BindingFlags.Static);

                bool foundEndIndex = false;
                bool foundStartIndex = false;
                int startIndex = -1;
                int endIndex = -1;

                var codes = new List<CodeInstruction>(instructions);
                //var callDrawTexture = typeof(GUI).GetMethod("DrawTexture", new[] { typeof(Rect), typeof(Texture) });

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
        //public static void FactionUIUtility_DrawFactionIconWithTooltip_Helper(Rect r, Faction faction)
        //{
        //    GUI.DrawTexture(r, GameComponent_FactionLists.iconCacheDict[faction]);
        //}

        //RimWorld.PermitsCardUtility.DrawRecordsCard(Rect, Pawn)

        //RimWorld.PawnColumnWorker_Faction.GetIconFor(Pawn pawn)
        //untested: not sure what this is in-game
        [HarmonyPatch(typeof(PawnColumnWorker_Faction))]
        [HarmonyPatch("GetIconFor")]
        public static class PawnColumnWorker_Faction_GetIconFor_Postfix
        {
            public static void Postfix(Pawn pawn, ref Texture2D __result)
            {
                if (pawn != null && (__result != null))
                {
                    __result = GetIconForFaction(pawn.Faction);
                }
            }
        }

        //RimWorld.PawnPortraitIconsDrawer.CalculatePawnPortraitIcons(Pawn pawn, bool required, bool showIdeoIcon)

        //RimWorld.Reward_Goodwill.<get_StackElements>b__3_0(Rect)
        //This is what displays the faction icon next to goodwill quest rewards
        //Tested: works
        [HarmonyPatch]
        public static class Reward_Goodwill_StackElements_Patch
        {
            static MethodInfo drawHelper = typeof(HarmonyPatches).GetMethod(nameof(GUI_DrawTexture_Helper), BindingFlags.Static | BindingFlags.Public);
            static MethodInfo drawTexture = AccessTools.Method(typeof(GUI), nameof(GUI.DrawTexture), new[] { typeof(Rect), typeof(Texture) });

            static MethodBase TargetMethod()
            {
                var outerType = typeof(Reward_Goodwill);

                MethodInfo[] methods = outerType.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                var innerType = outerType.GetNestedTypes(BindingFlags.NonPublic).FirstOrDefault(t => t.Name.Contains("get_StackElements"));

                MethodInfo outerTargetMethod = outerType
                    .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "<get_StackElements>b__3_0");

                return outerTargetMethod;
            }

            /* 
             * Original IL:
            		IL_0012: ldfld class RimWorld.Faction RimWorld.Reward_Goodwill::faction
		            IL_0017: ldfld class RimWorld.FactionDef RimWorld.Faction::def
		            IL_001c: callvirt instance class [UnityEngine.CoreModule]UnityEngine.Texture2D RimWorld.FactionDef::get_FactionIcon()
		            IL_0021: call void [UnityEngine.IMGUIModule]UnityEngine.GUI::DrawTexture(valuetype [UnityEngine.CoreModule]UnityEngine.Rect, class [UnityEngine.CoreModule]UnityEngine.Texture)

                Need:
                    ldfld class RimWorld.Faction RimWorld.Reward_Goodwill::faction
                    call helper method instead
            */
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = instructions.ToList();

                for (int i = 0; i < codes.Count; i++)
                {
                    //Find operation tagged IL_0021 above
                    if (codes[i].Calls(drawTexture))
                    {
                        // Replace that call with a call to my helper
                        codes[i] = new CodeInstruction(OpCodes.Call, drawHelper);

                        // Remove the instructions that consume the Faction in favor of a FactionDef, then consume that in favor of a Texture2D. IL_0017 and IL_001c above
                        codes.RemoveRange(i - 2, 2);
                    }
                }

                return codes;
            }
        }

        //RimWorld.Planet.EscapeShip.ExpandingIcon
        //don't think patch is needed here

        //RimWorld.Planet.Settlement.ExpandingIcon
        //todo test
        [HarmonyPatch(typeof(Settlement))]
        [HarmonyPatch("ExpandingIcon", MethodType.Getter)]
        public static class Settlement_ExpandingIcon_Postfix
        {
            public static void Postfix(Settlement __instance, ref Texture2D __result)
            {
                __result = GetIconForFaction(__instance.Faction);
            }
        }

        //RimWorld.Planet.WorldFactionsUIUtility.DoWindowContents(Rect, List<FactionDef>, bool)
        //I think this is actually the screen during world setup, 
        //[HarmonyPatch(typeof(WorldFactionsUIUtility))]
        //[HarmonyPatch("DoWindowContents")]
        //public static class WorldFactionsUIUtility_DoWindowContents_Patch
        //{
        //    static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        //    {
        //        MethodInfo drawHelper = typeof(HarmonyPatches).GetMethod("WorldFactionsUIUtility_DoWindowContents_Helper", BindingFlags.Public | BindingFlags.Static);
        //        var codes = new List<CodeInstruction>(instructions);
        //        int startIndex = -1;
        //        int endIndex = -1;
        //        //int defIndex = -1;
        //        bool foundStartIndex = false;
        //        bool foundEndIndex = false;

        //        //bool foundDefIndex = false;

        //        for (int i = 0; i < codes.Count; i++)
        //        {
        //            if (foundStartIndex && foundEndIndex)
        //                break;

        //            if (foundEndIndex)
        //            {
        //                for (int j = endIndex; j >= 0; j--)
        //                {
        //                    if (codes[j].opcode == OpCodes.Ldfld
        //                        && codes[j].operand.ToString().Contains("def"))
        //                    {
        //                        startIndex = j;
        //                        foundStartIndex = true;
        //                        break;
        //                    }
        //                }
        //                continue;
        //            }

        //            if (!foundEndIndex
        //                && codes[i].opcode == OpCodes.Callvirt
        //                && codes[i].operand.ToString().Contains("get_FactionIcon"))
        //            {
        //                endIndex = i;
        //                foundEndIndex = true;
        //                continue;
        //            }
        //        }

        //        if (startIndex >= 0 && endIndex >= 0)
        //        {
        //            //make new instructions
        //            List<CodeInstruction> newCodes = new List<CodeInstruction>();
        //            newCodes.Add(new CodeInstruction(OpCodes.Call, drawHelper));

        //            //remove old instructions
        //            codes.RemoveRange(startIndex, (endIndex - startIndex + 1));

        //            //add new instructions
        //            codes.InsertRange(startIndex, newCodes);
        //        }

        //        return codes.AsEnumerable();
        //    }
        //}
        //public static Texture2D WorldFactionsUIUtility_DoWindowContents_Helper(Faction faction)
        //{
        //    return GameComponent_FactionLists.iconCacheDict[faction];
        //}

        //World setup, pre-ideo generation
        //RimWorld.Planet.WorldFactionsUIUtility.DoRow(Rect, FactionDef, List<FactionDef>, int)

        [HarmonyPatch]
        public static class RoyalTitlePermitWorkers_Postfix
        {
            public static IEnumerable<MethodBase> TargetMethods()
            {
                yield return AccessTools.Method(typeof(RoyalTitlePermitWorker_CallAid), "GetRoyalAidOptions");
                yield return AccessTools.Method(typeof(RoyalTitlePermitWorker_CallLaborers), "GetRoyalAidOptions");
                yield return AccessTools.Method(typeof(RoyalTitlePermitWorker_CallShuttle), "GetRoyalAidOptions");
                yield return AccessTools.Method(typeof(RoyalTitlePermitWorker_DropResources), "GetRoyalAidOptions");
                yield return AccessTools.Method(typeof(RoyalTitlePermitWorker_OrbitalStrike), "GetRoyalAidOptions");
            }

            public static IEnumerable<FloatMenuOption> Postfix(IEnumerable<FloatMenuOption> __result, Faction faction)
            {
                FieldInfo itemIconField = typeof(FloatMenuOption).GetField("itemIcon", BindingFlags.Instance | BindingFlags.NonPublic);

                foreach (FloatMenuOption fmo in __result)
                {
                    itemIconField.SetValue(fmo, GetIconForFaction(faction));

                    yield return fmo;
                }
            }
        }

        //RimWorld.Pawn_RoyaltyTracker.<GetGizmos>d__70.MoveNext()

        //RimWorld.CharacterCardUtility.<>c__DisplayClass41_0.<DoTopStack>b__10(Rect)

        //RimWorld.CharacterCardUtility.<>c__DisplayClass41_1.<DoTopStack>b__11(Rect)

        //RimWorld.CharacterCardUtility.<>c__DisplayClass41_5.<DoTopStack>b__15(Rect)

        //RimWorld.Reward_BestowingCeremony.<get_StackElements>d__7.MoveNext()

        //Settlement colors
        [HarmonyPatch(typeof(Settlement))]
        [HarmonyPatch("Material", MethodType.Getter)]
        public static class Settlement_Material_Postfix
        {
            public static void Postfix(Material __result, Settlement __instance)
            {
                var comp = Current.Game?.GetComponent<GameComponent_FactionLists>();
                if (comp == null || __instance?.Faction == null)
                    return;

                Faction faction = __instance.Faction;

                bool useIdeoColor = comp.colorDictionary.TryGetValue(faction, out bool use) && use;

                Color targetColor = useIdeoColor
                    ? (faction.ideos?.PrimaryIdeo?.Color ?? faction.Color)
                    : faction.Color;

                __result.color = targetColor;
            }
        }
    }
}
