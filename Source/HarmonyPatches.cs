using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace MarketValuePer
{
    [StaticConstructorOnStartup]
    static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            Log.Message("[MarketValuePer] Looking for DubsMintMenus");
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.FullName.StartsWith("DubsMintMenus"))
                {
                    new Harmony("doug.MarketValuePer").Patch(AccessTools.Method("DubsMintMenus.Patch_BillStack_DoListing:DoRow"), 
                        transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(Hook)));
                    Log.Message("[MarketValuePer] Patched DubsMintMenus.Patch_BillStack_DoListing:DoRow");
                }
            }
        }

        private static IEnumerable<CodeInstruction> Hook(IEnumerable<CodeInstruction> instructions)
        {
            var inst = instructions.ToArray();
            LocalBuilder rectVariable = null;
            for (var index = 0; index < inst.Length; index++)
            {
                if (inst[index].opcode == OpCodes.Ldloc_S &&
                    inst[index].operand is LocalBuilder l && l.LocalType == typeof(Rect) &&
                    inst[index + 1].opcode == OpCodes.Ldsfld &&
                    inst[index + 1].operand is FieldInfo f && f.Name == "work_icon")
                {
                    rectVariable = inst[index].operand as LocalBuilder;
                    break;
                }
            }

            if (rectVariable == null)
                throw new InvalidOperationException("Couldn't find rectangle for drawing");

            for (var index = 0; index < inst.Length; index++)
            {
                if (index > 3 &&
                    inst[index - 3].opcode == OpCodes.Ldfld &&
                    inst[index - 3].operand is FieldInfo f && f.Name == "ShowBillWorkIcons" &&
                    inst[index - 2].opcode == OpCodes.Brfalse_S &&
                    inst[index - 1].opcode == OpCodes.Ldloc_S &&
                    inst[index].opcode == OpCodes.Ldc_I4_1 &&
                    inst[index + 1].opcode == OpCodes.Add)
                {
                    yield return new CodeInstruction(OpCodes.Ldc_I4_2);
                    continue;
                }
                else if (inst[index].opcode == OpCodes.Ldarg_0 &&
                    inst[index + 1].opcode == OpCodes.Ldnull &&
                    inst[index + 2].opcode == OpCodes.Callvirt &&
                    inst[index + 2].operand is MethodInfo m && m.Name == "WorkAmountTotal")
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldloca_S, rectVariable);
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method("MarketValuePer.HarmonyPatches:AddMarketValuePer"));
                }
                yield return inst[index];
            }
        }

        public static void AddMarketValuePer(RecipeDef recipe, ref Rect rect5)
        {
            try
            {
                if (recipe.ProducedThingDef != null)
                {
                    var productIsStuffableWithFancy = false;

                    var costOfIngredientsSimple = 0f;
                    var costOfIngredientsFancy = 0f;
                    var fancy = ThingDefOf.Steel;
                    if (recipe.ingredients != null)
                    {
                        foreach (var ingredientCount in recipe.ingredients)
                        {
                            if (ingredientCount.IsFixedIngredient)
                            {
                                var countRequiredOfFor = ingredientCount.CountRequiredOfFor(ingredientCount.FixedIngredient, recipe);
                                var v = countRequiredOfFor * ingredientCount.FixedIngredient.GetStatValueAbstract(StatDefOf.MarketValue);
                                costOfIngredientsSimple += v;
                                costOfIngredientsFancy += v;
                            }
                            else
                            {
                                var found = false;
                                foreach (var thingDef in new[] {ThingDef.Named("DevilstrandCloth"), ThingDefOf.Gold})
                                {
                                    if (ingredientCount.filter.Allows(thingDef))
                                    {
                                        costOfIngredientsFancy += thingDef.GetStatValueAbstract(StatDefOf.MarketValue) * ingredientCount.GetBaseCount() / thingDef.VolumePerUnit;
                                        productIsStuffableWithFancy = true;
                                        fancy = thingDef;
                                        found = true;
                                        break;
                                    }
                                }

                                if (!found)
                                    costOfIngredientsFancy += 2 * ingredientCount.GetBaseCount();
                                costOfIngredientsSimple += 2 * ingredientCount.GetBaseCount();
                            }
                        }
                    }

                    var marketValueWithSimple = recipe.ProducedThingDef.GetStatValueAbstract(StatDefOf.MarketValue, ThingDefOf.Steel);
                    var marketValueWithFancy = recipe.ProducedThingDef.GetStatValueAbstract(StatDefOf.MarketValue, fancy);
                    var marketValueWithSimpleGood = marketValueWithSimple;
                    var marketValueWithSimpleLegendary = marketValueWithSimple;
                    var marketValueWithFancyGood = marketValueWithFancy;
                    var marketValueWithFancyLegendary = marketValueWithFancy;
                    var productHasQuality = recipe.ProducedThingDef.HasComp(typeof(CompQuality));
                    if (productHasQuality)
                    {
                        StatDefOf.MarketValue.GetStatPart<StatPart_Quality>().TransformValue(StatRequest.For(recipe.ProducedThingDef, null, QualityCategory.Good), ref marketValueWithSimpleGood);
                        StatDefOf.MarketValue.GetStatPart<StatPart_Quality>().TransformValue(StatRequest.For(recipe.ProducedThingDef, null, QualityCategory.Good), ref marketValueWithFancyGood);
                        StatDefOf.MarketValue.GetStatPart<StatPart_Quality>().TransformValue(StatRequest.For(recipe.ProducedThingDef, null, QualityCategory.Legendary), ref marketValueWithSimpleLegendary);
                        StatDefOf.MarketValue.GetStatPart<StatPart_Quality>().TransformValue(StatRequest.For(recipe.ProducedThingDef, null, QualityCategory.Legendary), ref marketValueWithFancyLegendary);
                    }

                    var valuePerWorkWithSimpleGood = marketValueWithSimpleGood / recipe.WorkAmountTotal(null);
                    var valuePerWorkWithFancyGood = marketValueWithFancyGood / recipe.WorkAmountTotal(null);
                    var valuePerWorkWithSimpleLegendary = marketValueWithSimpleLegendary / recipe.WorkAmountTotal(null);
                    var valuePerWorkWithFancyLegendary = marketValueWithFancyLegendary / recipe.WorkAmountTotal(null);

                    var valuePerIngredientWithSimpleGood = marketValueWithSimpleGood / costOfIngredientsSimple;
                    var valuePerIngredientWithFancyGood = marketValueWithFancyGood / costOfIngredientsFancy;
                    var valuePerIngredientWithSimpleLegendary = marketValueWithSimpleLegendary / costOfIngredientsSimple;
                    var valuePerIngredientWithFancyLegendary = marketValueWithFancyLegendary / costOfIngredientsFancy;

                    if (productIsStuffableWithFancy)
                        GUI.color = productHasQuality ? Color.yellow : Color.magenta;
                    else
                        GUI.color = productHasQuality ? Color.green : Color.white;

                    Text.Anchor = TextAnchor.UpperCenter;
                    Widgets.Label(rect5, valuePerWorkWithSimpleGood.ToString("F3"));
                    Text.Anchor = TextAnchor.LowerCenter;
                    Widgets.Label(rect5, valuePerIngredientWithSimpleGood.ToString("F3"));
                    Text.Anchor = TextAnchor.UpperLeft;
                    if (productIsStuffableWithFancy && productHasQuality)
                    {
                        TooltipHandler.TipRegion(rect5, "Simple (steel, etc):\n\n" +
                                                        "Value/Work if Good: " + valuePerWorkWithSimpleGood.ToString("F3") + "\n" +
                                                        "Value/Work if Legendary: " + valuePerWorkWithSimpleLegendary.ToString("F3") + "\n" +
                                                        "Value/Ingredient if Good: " + valuePerIngredientWithSimpleGood.ToString("F3") + "\n" +
                                                        "Value/Ingredient if Legendary: " + valuePerIngredientWithSimpleLegendary.ToString("F3"));
                        TooltipHandler.TipRegion(rect5, "Fancy (gold, devilstrand):\n\n" +
                                                        "Value/Work if Good/Fancy: " + valuePerWorkWithFancyGood.ToString("F3") + "\n" +
                                                        "Value/Work if Legendary/Fancy: " + valuePerWorkWithFancyLegendary.ToString("F3") + "\n" +
                                                        "Value/Ingredient if Good/Fancy: " + valuePerIngredientWithFancyGood.ToString("F3") + "\n" +
                                                        "Value/Ingredient if Legendary/Fancy: " + valuePerIngredientWithFancyLegendary.ToString("F3"));
                    }
                    else if (productHasQuality)
                        TooltipHandler.TipRegion(rect5, "Value/Work if Good: " + valuePerWorkWithSimpleGood.ToString("F3") + "\n" +
                                                        "Value/Work if Legendary: " + valuePerWorkWithSimpleLegendary.ToString("F3") + "\n" +
                                                        "Value/Ingredient if Good: " + valuePerIngredientWithSimpleGood.ToString("F3") + "\n" +
                                                        "Value/Ingredient if Legendary: " + valuePerIngredientWithSimpleLegendary.ToString("F3"));
                    else
                        TooltipHandler.TipRegion(rect5, "Value/Work: " + valuePerWorkWithSimpleGood.ToString("F3") + "\n" + "Value/Ingredient: " + valuePerIngredientWithSimpleGood.ToString("F3"));

                    rect5.x -= rect5.width;
                    GUI.color = Color.white;
                }
            }
            catch
            {
            }
        }
    }
}
