using System;
using System.Collections.Generic;
using System.Linq;
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
        static readonly MethodInfo getHelper = AccessTools.Method(typeof(HarmonyPatches), nameof(Get_FactionIcon_Helper));
        static readonly FieldInfo factionDefField = AccessTools.Field(typeof(Faction), nameof(Faction.def));
        static readonly MethodInfo factionIconGetter = AccessTools.PropertyGetter(typeof(FactionDef), nameof(FactionDef.FactionIcon));

        static HarmonyPatches()
        {
            Harmony harmony = new Harmony("nuff.rimworld.ideology_faction_icon");
            harmony.PatchAll();
        }

        private static bool ReplaceRangeWithCall(List<CodeInstruction> codes, int startIndex, int endIndex, MethodInfo helper)
        {
            if (helper == null || startIndex < 0 || endIndex < startIndex || endIndex >= codes.Count)
            {
                Log.Error("Ideoligion Icon as Faction Icon: invalid transpiler replacement range or helper; leaving instructions unchanged. Please send your error log to the developer.");
                return false;
            }

            // Check instructions for whether they jump or are jumped to, don't replace them if so
            for (int i = startIndex; i <= endIndex; i++)
            {
                FlowControl flow = codes[i].opcode.FlowControl;
                if ((i > startIndex && (codes[i].labels.Count > 0 || codes[i].blocks.Count > 0))
                    || flow == FlowControl.Branch || flow == FlowControl.Cond_Branch
                    || flow == FlowControl.Return || flow == FlowControl.Throw)
                {
                    Log.Error($"Ideoligion Icon as Faction Icon: unsafe replacement for {helper.Name}; leaving instructions unchanged. Please send your error log to the developer.");
                    return false;
                }
            }

            // Mutate existing objects so labels and exception boundaries retain their positions
            codes[startIndex].opcode = OpCodes.Call;
            codes[startIndex].operand = helper;
            for (int i = startIndex + 1; i <= endIndex; i++)
            {
                codes[i].opcode = OpCodes.Nop;
                codes[i].operand = null;
            }
            return true;
        }

        

        //World with factions is generated and displayed while selecting a tile, before icons have a chance to be cached
        [HarmonyPatch(typeof(Page_SelectStartingSite))]
        [HarmonyPatch("PreOpen")]
        public static class Page_SelectStartingSite_PreOpen_Postfix
        {
            public static void Postfix()
            {
                //need to recahce right away so that settlements display correctly when player goes to choose a landing site
                Current.Game.GetComponent<GameComponent_FactionLists>().RecacheDictionaries();
            }
        }

        [HarmonyPatch(typeof(Page_ChooseIdeoPreset))]
        [HarmonyPatch("PostOpen")]
        public static class Page_ChooseIdeoPreset_PostOpen_Prefix
        {
            public static void Prefix()
            {
                //have to recache here as well because new Odyssey scenario / ScenPart_ForcedMap skip Page_SelectStartingSite
                Current.Game.GetComponent<GameComponent_FactionLists>().RecacheDictionaries();
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

        // public static void GUI_DrawTexture_Helper(Rect position, Faction faction)
        // {
        //     GUI.DrawTexture(position, GameComponent_FactionLists.Instance.GetTextureSafe(faction));
        // }

        public static Texture2D Get_FactionIcon_Helper(Faction faction)
        {
            return GameComponent_FactionLists.Instance.GetTextureForFaction(faction);
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

        [HarmonyPatch(typeof(Faction), nameof(Faction.Color), MethodType.Getter)]
        public static class Faction_Color_Postfix
        {
            public static void Postfix(Faction __instance, ref Color __result)
            {
                Color? color = GameComponent_FactionLists.Instance.GetColorForFaction(__instance);
                if (color.HasValue)
                    __result = color ?? __result;
            }
        }

        //RimWorld.InteractionDef.GetSymbol(Faction, Ideo)
        //todo test
        [HarmonyPatch(typeof(InteractionDef), nameof(InteractionDef.GetSymbol))]
        public static class InteractionDef_GetSymbol_Postfix
        {
            public static void Postfix(InteractionDef __instance, ref Texture2D __result, Faction initiatorFaction)
            {
                if (__instance.symbolSource == InteractionSymbolSource.InitiatorFaction && initiatorFaction != null)
                {
                    __result = GameComponent_FactionLists.Instance.GetTextureForFaction(initiatorFaction);
                }
            }
        }

        //RimWorld.Faction.CommFloatMenuOption(Building_CommsConsole, Pawn)
        //Tested: works
        [HarmonyPatch(typeof(Faction), nameof(Faction.CommFloatMenuOption))]
        public static class Faction_CommFloatMenuOption_Postfix
        {
            public static void Postfix(Faction __instance, ref FloatMenuOption __result)
            {
                if (__result != null)
                {
                    __result.iconTex = GameComponent_FactionLists.Instance.GetTextureForFaction(__instance);
                }
            }
        }

        //RimWorld.CompUsable.Icon
        //Only used for Summon Diabolus, looks like, so doesn't need patch

        //RimWorld.Tradeable_RoyalFavor.DrawIcon(Rect iconRect)
        //Tested: Works
        [HarmonyPatch(typeof(Tradeable_RoyalFavor), nameof(Tradeable_RoyalFavor.DrawIcon))]
        public static class Tradeable_RoyalFavor_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = instructions.ToList();
                bool foundMatch = false;

                for (int i = 0; i + 1 < codes.Count; i++)
                {
                    if (codes[i].LoadsField(factionDefField) && codes[i + 1].Calls(factionIconGetter))
                    {
                        foundMatch = true;
                        ReplaceRangeWithCall(codes, i, i + 1, getHelper);
                    }
                }

                if (!foundMatch)
                {
                    Log.Error("Ideoligion Icon as Faction Icon: faction icon lookup not found in Tradeable_RoyalFavor.DrawIcon; leaving instructions unchanged. Please send your error log to the developer.");
                }

                return codes;
            }
        }
        // public static void Tradeable_RoyalFavor_Helper(Rect iconRect, Faction faction)
        // {
        //     Widgets.DrawTextureRotated(iconRect, GameComponent_FactionLists.Instance.GetTextureSafe(faction), 0f);
        // }

        //RimWorld.ColonistBarColonistDrawer.DrawIcons(Rect rect, Pawn colonist)
        //Tested: works
        [HarmonyPatch(typeof(ColonistBarColonistDrawer), nameof(ColonistBarColonistDrawer.DrawIcons))]
        public static class ColonistBarColonistDrawer_DrawIcons_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = new List<CodeInstruction>(instructions);
                bool foundMatch = false;

                for (int i = 0; i + 1 < codes.Count; i++)
                {
                    if (codes[i].LoadsField(factionDefField) && codes[i + 1].Calls(factionIconGetter))
                    {
                        foundMatch = true;
                        ReplaceRangeWithCall(codes, i, i + 1, getHelper);
                    }
                }

                if (!foundMatch)
                {
                    Log.Error("Ideoligion Icon as Faction Icon: faction icon lookup not found in ColonistBarColonistDrawer.DrawIcons; leaving instructions unchanged. Please send your error log to the developer.");
                }

                return codes;
            }
        }

        //RimWorld.Dialog_BeginRitual.CalculatePawnPortraitIcons(Pawn)
        //todo no longer exists?

        //RimWorld.FactionUIUtility.DrawFactionRow(Faction faction, float rowY, Rect fillRect)
        //Tested: works
        [HarmonyPatch(typeof(FactionUIUtility), nameof(FactionUIUtility.DrawFactionRow))]
        public static class FactionUIUtility_DrawFactionRow_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = instructions.ToList();
                bool foundMatch = false;

                for (int i = 0; i + 1 < codes.Count; i++)
                {
                    if (codes[i].LoadsField(factionDefField) && codes[i + 1].Calls(factionIconGetter))
                    {
                        foundMatch = true;
                        ReplaceRangeWithCall(codes, i, i + 1, getHelper);
                    }
                }

                if (!foundMatch)
                {
                    Log.Error("Ideoligion Icon as Faction Icon: faction icon lookup not found in FactionUIUtility.DrawFactionRow; leaving instructions unchanged. Please send your error log to the developer.");
                }

                return codes;
            }
        }

        //RimWorld.FactionUIUtility.DrawFactionIconWithTooltip(Rect, Faction)
        [HarmonyPatch(typeof(FactionUIUtility), nameof(FactionUIUtility.DrawFactionIconWithTooltip))]
        public static class FactionUIUtility_DrawFactionIconWithTooltip_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = instructions.ToList();
                bool foundMatch = false;

                for (int i = 0; i + 1 < codes.Count; i++)
                {
                    if (codes[i].LoadsField(factionDefField) && codes[i + 1].Calls(factionIconGetter))
                    {
                        foundMatch = true;
                        ReplaceRangeWithCall(codes, i, i + 1, getHelper);
                    }
                }

                if (!foundMatch)
                {
                    Log.Error("Ideoligion Icon as Faction Icon: faction icon lookup not found in FactionUIUtility.DrawFactionIconWithTooltip; leaving instructions unchanged. Please send your error log to the developer.");
                }

                return codes;
            }
        }

        //RimWorld.PermitsCardUtility.DrawRecordsCard(Rect, Pawn)
        //Only shows if you have multiple factions with titles
        //TODO test, need another faction that can have titles like DMS or Moyo Cartel to be updated to 1.6
        [HarmonyPatch(typeof(PermitsCardUtility), nameof(PermitsCardUtility.DrawRecordsCard))]
        public static class PermitsCardUtility_DrawRecordsCard_Patch
        {
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = instructions.ToList();
                bool foundMatch = false;

                for (int i = 0; i + 1 < codes.Count; i++)
                {
                    if (codes[i].LoadsField(factionDefField) && codes[i + 1].Calls(factionIconGetter))
                    {
                        foundMatch = true;
                        ReplaceRangeWithCall(codes, i, i + 1, getHelper);
                    }
                }

                if (!foundMatch)
                {
                    Log.Error("Ideoligion Icon as Faction Icon: faction icon lookup not found in PermitsCardUtility.DrawRecordsCard; leaving instructions unchanged. Please send your error log to the developer.");
                }

                return codes;
            }
        }

        //RimWorld.PawnColumnWorker_Faction.GetIconFor(Pawn pawn)
        //untested: not sure what this is in-game
        [HarmonyPatch(typeof(PawnColumnWorker_Faction), nameof(PawnColumnWorker_Faction.GetIconFor))]
        public static class PawnColumnWorker_Faction_GetIconFor_Postfix
        {
            public static void Postfix(Pawn pawn, ref Texture2D __result)
            {
                if (pawn != null && (__result != null))
                {
                    __result = Get_FactionIcon_Helper(pawn.Faction);
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
            static MethodBase TargetMethod()
            {
                var outerType = typeof(Reward_Goodwill);

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
            */
            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = instructions.ToList();
                bool foundMatch = false;

                for (int i = 0; i + 1 < codes.Count; i++)
                {
                    if (codes[i].LoadsField(factionDefField) && codes[i + 1].Calls(factionIconGetter))
                    {
                        foundMatch = true;
                        ReplaceRangeWithCall(codes, i, i + 1, getHelper);
                    }
                }

                if (!foundMatch)
                {
                    Log.Error("Ideoligion Icon as Faction Icon: faction icon lookup not found in Reward_Goodwill.StackElements; leaving instructions unchanged. Please send your error log to the developer.");
                }

                return codes;
            }
        }

        //RimWorld.Planet.EscapeShip.ExpandingIcon
        //don't think patch is needed here

        //RimWorld.Planet.Settlement.ExpandingIcon
        //Tested: works
        [HarmonyPatch(typeof(Settlement), nameof(Settlement.ExpandingIcon), MethodType.Getter)]
        public static class Settlement_ExpandingIcon_Postfix
        {
            public static void Postfix(Settlement __instance, ref Texture2D __result)
            {
                __result = Get_FactionIcon_Helper(__instance.Faction);
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

                foreach (FloatMenuOption fmo in __result)
                {
                    fmo.iconTex = Get_FactionIcon_Helper(faction);

                    yield return fmo;
                }
            }
        }

        //RimWorld.Pawn_RoyaltyTracker.<GetGizmos>d__70.MoveNext()
        //Tested; Works
        [HarmonyPatch(typeof(Pawn_RoyaltyTracker))]
        public static class Pawn_RoyaltyTracker_GetGizmos_Patch
        {

            static MethodBase TargetMethod()
            {
                var outerType = typeof(Pawn_RoyaltyTracker);

                var innerType = outerType
                    .GetNestedTypes(BindingFlags.NonPublic)
                    .FirstOrDefault(t => t.Name == "<GetGizmos>d__70");

                MethodInfo targetMethod = innerType
                    .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "MoveNext");

                return targetMethod;
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = instructions.ToList();
                bool foundMatch = false;

                for (int i = 0; i + 1 < codes.Count; i++)
                {
                    if (codes[i].LoadsField(factionDefField) && codes[i + 1].Calls(factionIconGetter))
                    {
                        foundMatch = true;
                        ReplaceRangeWithCall(codes, i, i + 1, getHelper);
                    }
                }

                if (!foundMatch)
                {
                    Log.Error("Ideoligion Icon as Faction Icon: faction icon lookup not found in Pawn_RoyaltyTracker.GetGizmos; leaving instructions unchanged. Please send your error log to the developer.");
                }

                return codes;
            }
        }

        //RimWorld.CharacterCardUtility.<>c__DisplayClass41_0.<DoTopStack>b__10(Rect)
        //RimWorld.CharacterCardUtility.<>c__DisplayClass41_1.<DoTopStack>b__11(Rect)
        //RimWorld.CharacterCardUtility.<>c__DisplayClass41_5.<DoTopStack>b__15(Rect)
        //The above were 41 when I started working on this months ago. Now are 42 as I work on this in 1.6
        //Tested: Works
        [HarmonyPatch]
        public static class CharacterCardUtility_DoTopStack_Patch
        {
            static IEnumerable<MethodBase> TargetMethods()
            {
                var parentType = typeof(CharacterCardUtility);

                foreach (var nestedType in parentType.GetNestedTypes(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                {
                    // Filter out unnecessary methods. Seems like the # after DisplayClass can change, so can't rely on it. Decompiler said 41 when I started working on this months ago, is 42 now that I come back to it
                    if (!nestedType.Name.StartsWith("<>c__DisplayClass"))
                        continue;

                    // Select methods that call the exact faction icon getter.
                    foreach (var method in nestedType.GetMethods(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
                    {
                        var instructions = HarmonyLib.PatchProcessor.GetCurrentInstructions(method, out _);

                        if (instructions.Any(instr => instr.Calls(factionIconGetter)))
                        {
                            yield return method;
                        }
                    }
                }
            }

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = instructions.ToList();
                bool foundMatch = false;

                for (int i = 0; i + 1 < codes.Count; i++)
                {
                    if (codes[i].LoadsField(factionDefField) && codes[i + 1].Calls(factionIconGetter))
                    {
                        foundMatch = true;
                        ReplaceRangeWithCall(codes, i, i + 1, getHelper);
                    }
                }

                if (!foundMatch)
                {
                    Log.Error("Ideoligion Icon as Faction Icon: faction icon lookup not found in CharacterCardUtility.DoTopStack; leaving instructions unchanged. Please send your error log to the developer.");
                }

                return codes;
            }
        }

        //RimWorld.Reward_BestowingCeremony.<get_StackElements>d__7.MoveNext()
        [HarmonyPatch]
        public static class Reward_BestowingCeremony_StackElements_Patch
        {
            static MethodBase TargetMethod()
            {
                var outerType = typeof(Reward_BestowingCeremony);

                var nestedType = outerType
                    .GetNestedTypes(BindingFlags.NonPublic)
                    .FirstOrDefault(t => t.Name.StartsWith("<get_StackElements>"));

                MethodInfo targetMethod = nestedType
                    .GetMethods(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "MoveNext");

                return targetMethod;
            }

            /*
             *  Looking for:
             	    IL_00ab: ldloc.1
			        IL_00ac: ldfld class RimWorld.Faction RimWorld.Reward_BestowingCeremony::awardingFaction
			        IL_00b1: ldfld class RimWorld.FactionDef RimWorld.Faction::def
			        IL_00b6: callvirt instance class [UnityEngine.CoreModule]UnityEngine.Texture2D RimWorld.FactionDef::get_FactionIcon()
            */

            static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                var codes = instructions.ToList();
                bool foundMatch = false;

                for (int i = 0; i + 1 < codes.Count; i++)
                {
                    if (codes[i].LoadsField(factionDefField) && codes[i + 1].Calls(factionIconGetter))
                    {
                        foundMatch = true;
                        ReplaceRangeWithCall(codes, i, i + 1, getHelper);
                    }
                }

                if (!foundMatch)
                {
                    Log.Error("Ideoligion Icon as Faction Icon: faction icon lookup not found in Reward_BestowingCeremony.StackElements; leaving instructions unchanged. Please send your error log to the developer.");
                }

                return codes;
            }
        }

        //Settlement colors
        [HarmonyPatch(typeof(Settlement), nameof(Settlement.Material), MethodType.Getter)]
        public static class Settlement_Material_Postfix
        {
            public static void Postfix(Material __result, Settlement __instance)
            {
                var comp = Current.Game?.GetComponent<GameComponent_FactionLists>();
                if (comp == null || __instance?.Faction == null)
                    return;

                Faction faction = __instance.Faction;

                bool useIdeoColor = comp.overallBehavior == IdeoFactIconSettings.OverallBehavior.IFI_Choose
                    && comp.colorDictionary.TryGetValue(faction, out bool use) && use;

                Color targetColor = useIdeoColor
                    ? (faction.ideos?.PrimaryIdeo?.Color ?? faction.Color)
                    : faction.Color;

                __result.color = targetColor;
            }
        }

        [HarmonyPatch(typeof(FactionManager), nameof(FactionManager.Add))]
        public static class FactionManager_Add_Postfix
        {
            public static void Postfix(Faction faction)
            {
                var comp = Current.Game?.GetComponent<GameComponent_FactionLists>();
                if (comp != null)
                {
                    comp.TryAddFaction(faction);
                    //GameComponent_FactionLists.RecacheIcons();
                    comp.needRecache = true;
                }
            }
        }

        [HarmonyPatch(typeof(FactionManager), nameof(FactionManager.Remove))]
        public static class FactionManager_Remove_Postfix
        {
            public static void Postfix(Faction faction)
            {
                var comp = Current.Game?.GetComponent<GameComponent_FactionLists>();
                if (comp != null)
                {
                    comp.TryRemoveFaction(faction);
                }
            }
        }

        [HarmonyPatch(typeof(Page_ChooseIdeoPreset), nameof(Page_ChooseIdeoPreset.DoClassic))]
        public static class Page_ChooseIdeoPreset_DoClassic_Postfix
        {
            public static void Postfix()
            {
                var comp = Current.Game?.GetComponent<GameComponent_FactionLists>();
                if (comp != null)
                {
                    List<Faction> iconKeys = comp.iconDictionary.Keys.ToList();
                    foreach (var key in iconKeys)
                    {
                        comp.iconDictionary[key] = false;
                    }

                    //doing this instead of adding to above behavior, in case dictionaries are somehow desynced
                    List<Faction> colorKeys = comp.colorDictionary.Keys.ToList();
                    foreach (var key in colorKeys)
                    {
                        comp.colorDictionary[key] = false;
                    }
                }
            }
        }

        [HarmonyPatch(typeof(Dialog_ChooseIdeoSymbols), nameof(Dialog_ChooseIdeoSymbols.TryAccept))]
        public static class Dialog_ChooseIdeoSymbols_TryAccept_Postfix
        {
            public static void Postfix()
            {
                GameComponent_FactionLists comp = Current.Game?.GetComponent<GameComponent_FactionLists>();
                if (comp != null)
                {
                    comp.needRecache = true;
                }
            }
        }

        [HarmonyPatch(typeof(FactionIdeosTracker), nameof(FactionIdeosTracker.RecalculateIdeosBasedOnPlayerPawns))]
        public static class FactionIdeosTracker_RecalculateIdeosBasedOnPlayerPawns_Patches
        {
            public static void Prefix(FactionIdeosTracker __instance, out Ideo __state)
            {
                __state = __instance.primaryIdeo;
            }

            public static void Postfix(FactionIdeosTracker __instance, Ideo __state)
            {
                if (__instance.primaryIdeo != __state)
                {
                    Current.Game.GetComponent<GameComponent_FactionLists>().RecacheDictionaries();
                }
            }
        }
    }
}
