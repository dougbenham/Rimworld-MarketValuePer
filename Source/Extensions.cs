using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace SellValuePer
{
    public static class Extensions
    {
        public static float GetSellValue(this StatRequest req)
        {
            return req.HasThing 
                ? GetSellValue(req.Thing) 
                : GetSellValue(req.BuildableDef, req.StuffDef);
        }

        public static float GetSellValue(this BuildableDef buildable, ThingDef stuff, QualityCategory? quality = null)
        {
            var r = buildable.GetStatValueAbstract(StatDefOf.MarketValue, stuff) *
                    buildable.GetStatValueAbstract(StatDefOf.SellPriceFactor, stuff);

            if (quality != null && buildable is ThingDef td && td.HasComp(typeof(CompQuality)))
                StatDefOf.MarketValue.GetStatPart<StatPart_Quality>().TransformValue(StatRequest.For(buildable, null, quality.Value), ref r);

            return r;
        }

        public static float GetSellValue(this Thing thing)
        {
            return thing.GetStatValue(StatDefOf.MarketValue) *
                   thing.GetStatValue(StatDefOf.SellPriceFactor);
        }

        public static float GetIngredientCost(this StatRequest req)
        {
            return req.BuildableDef.GetIngredientCost(req.StuffDef);
        }
        
        public static float GetIngredientCost(this BuildableDef buildable, ThingDef stuff = null)
        {
            var ingredientCost = 0f;
            if (buildable.CostList != null)
            {
                foreach (var thingDefCountClass in buildable.CostList)
                    ingredientCost += thingDefCountClass.count * thingDefCountClass.thingDef.BaseMarketValue;
            }

            if (buildable.CostStuffCount > 0)
            {
                if (stuff != null)
                    ingredientCost += buildable.CostStuffCount / stuff.VolumePerUnit * stuff.GetStatValueAbstract(StatDefOf.MarketValue);
                else
                    ingredientCost += buildable.CostStuffCount * 2f;
            }

            return ingredientCost;
        }

        public static float? GetIngredientCost(this RecipeDef recipe, ThingDef stuff = null)
        {
            var cost = 0f;

            var stuffable = false;
            if (recipe.ingredients != null)
            {
                foreach (var ingredientCount in recipe.ingredients)
                {
                    if (ingredientCount.IsFixedIngredient)
                    {
                        var countRequiredOfFor = ingredientCount.CountRequiredOfFor(ingredientCount.FixedIngredient, recipe);
                        var v = countRequiredOfFor * ingredientCount.FixedIngredient.GetStatValueAbstract(StatDefOf.MarketValue);
                        cost += v;
                    }
                    else
                    {
                        stuffable = true;
                        if (stuff == null)
                            cost += 2 * ingredientCount.GetBaseCount();
                        else
                        {
                            if (ingredientCount.filter.Allows(stuff))
                            {
                                cost += stuff.GetStatValueAbstract(StatDefOf.MarketValue) * ingredientCount.GetBaseCount() / stuff.VolumePerUnit;
                            }
                            else
                                return null; // can't put that stuff in this recipe
                        }
                    }
                }
            }

            if (stuffable != recipe.productHasIngredientStuff)
                Log.Error("Not accurate!");
            else
                Log.Message("Accurate!");

            return cost;
        }

        public static IEnumerable<ThingDef> GetVariableIngredients(this RecipeDef recipe)
        {
            if (recipe.ingredients != null)
            {
                foreach (var ingredientCount in recipe.ingredients)
                {
                    if (!ingredientCount.IsFixedIngredient)
                    {
                        return ingredientCount.filter.AllowedThingDefs;
                    }
                }
            }

            return Enumerable.Empty<ThingDef>();
        }

        public static float GetSellValuePerIngredients(this StatRequest req)
        {
            var ingredientCost = GetIngredientCost(req);
            if (ingredientCost > 0)
                return req.GetSellValue() / ingredientCost;
            return -1f;
        }

        public static float GetSellValuePerWork(this StatRequest req)
        {
            var work = GetWork(req);
            if (work > 0)
                return req.GetSellValue() / work;
            return -1;
        }

        public static float GetWork(this StatRequest req)
        {
            return GetWork(req.BuildableDef, req.StuffDef);
        }

        public static float GetWork(this BuildableDef buildable, ThingDef stuff)
        {
            return Mathf.Max(buildable.GetStatValueAbstract(StatDefOf.WorkToMake, stuff), buildable.GetStatValueAbstract(StatDefOf.WorkToBuild, stuff));
        }

        public static float GetSellValueRating(float sellValuePerIngredients, float sellValuePerWork)
        {
            return 100 * (sellValuePerIngredients * 0.25f + sellValuePerWork * 0.75f);
        }
    }
}