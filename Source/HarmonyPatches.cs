using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;

namespace SellValuePer
{
    [StaticConstructorOnStartup]
    static class HarmonyPatches
    {
        static HarmonyPatches()
        {
            Log.Message("[SellValuePer] Looking for DubsMintMenus");
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm.FullName.StartsWith("DubsMintMenus"))
                {
                    new Harmony("doug.SellValuePer").Patch(AccessTools.Method("DubsMintMenus.Patch_BillStack_DoListing:DoRow"), 
                        transpiler: new HarmonyMethod(typeof(HarmonyPatches), nameof(Hook)));
                    Log.Message("[SellValuePer] Patched DubsMintMenus.Patch_BillStack_DoListing:DoRow");
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
                    yield return new CodeInstruction(OpCodes.Call, AccessTools.Method("SellValuePer.HarmonyPatches:Add"));
                }
                yield return inst[index];
            }
        }

        public static void Add(RecipeDef recipe, ref Rect rect5)
        {
            try
            {
                if (recipe.products != null && recipe.products.Count > 0)
                {
                    ThingDef fancyStuff = null;

                    float? coiLegendary_ = null;
                    var variables = recipe.GetVariableIngredients().ToArray();
                    foreach (var thingDef in new[] {ThingDefOf.Gold, ThingDef.Named("DevilstrandCloth")})
                    {
                        if (variables.Contains(thingDef))
                        {
                            coiLegendary_ = recipe.GetIngredientCost(thingDef);
                            fancyStuff = thingDef;
                        }
                    }
                    var coiLegendary = coiLegendary_ ?? recipe.GetIngredientCost() ?? throw new ArgumentNullException();
                    
                    var svLegendary = recipe.products.Sum(p => p.thingDef.GetSellValue(fancyStuff, QualityCategory.Legendary) * p.count);
                    var productHasQuality = recipe.products.Any(p => p.thingDef.HasComp(typeof(CompQuality)));
                    
                    var vpwLegendary = svLegendary / recipe.WorkAmountTotal(null);
                    var vpiLegendary = svLegendary / coiLegendary;
                    var svrLegendary = Extensions.GetSellValueRating(vpiLegendary, vpwLegendary);

                    if (fancyStuff != null)
                        GUI.color = productHasQuality ? Color.yellow : Color.magenta;
                    else
                        GUI.color = productHasQuality ? Color.green : Color.white;
                    
                    Text.Anchor = TextAnchor.MiddleCenter;
                    Widgets.Label(rect5, svrLegendary.ToString("F0"));
                    Text.Anchor = TextAnchor.UpperLeft;
                    if (variables.Length > 0 && productHasQuality)
                    {
                        TooltipHandler.TipRegion(rect5, "Legendary:\n\n" +
                                                        string.Join("\n", variables.Select(t =>
                                                        {
                                                            var sv = recipe.products.Sum(p => p.thingDef.GetSellValue(t, QualityCategory.Legendary) * p.count);
                                                            var vpw = sv / recipe.WorkAmountTotal(null);
                                                            var vpi = sv / recipe.GetIngredientCost(t);
                                                            var svr = Extensions.GetSellValueRating(vpi ?? throw new ArgumentNullException(), vpw);
                                                            
                                                            return $"{t.label} R: {svr:F1} W: {vpw:F3} I: {vpi:F3}";
                                                        })));
                    }
                    else if (productHasQuality)
                        TooltipHandler.TipRegion(rect5, $"Legendary:\n\nR: {svrLegendary:F1} | W: {vpwLegendary:F3} | I: {vpiLegendary:F3}");
                    else
                        TooltipHandler.TipRegion(rect5, $"R: {svrLegendary:F1} | W: {vpwLegendary:F3} | I: {vpiLegendary:F3}");

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
