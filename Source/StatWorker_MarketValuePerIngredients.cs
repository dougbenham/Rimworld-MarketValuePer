using RimWorld;

namespace MarketValuePer
{
    public class StatWorker_MarketValuePerIngredients : StatWorker
    {
        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
		{
            if (req.BuildableDef != null)
            {
                var marketValue = req.HasThing 
                    ? req.Thing.GetStatValue(StatDefOf.MarketValue) 
                    : req.BuildableDef.GetStatValueAbstract(StatDefOf.MarketValue, req.StuffDef);

                var ingredientCost = 0f;
                if (req.BuildableDef.CostList != null)
                {
                    foreach (var thingDefCountClass in req.BuildableDef.CostList)
                        ingredientCost += thingDefCountClass.count * thingDefCountClass.thingDef.BaseMarketValue;
                }
                if (req.BuildableDef.CostStuffCount > 0)
                {
                    if (req.StuffDef != null)
                        ingredientCost += req.BuildableDef.CostStuffCount / req.StuffDef.VolumePerUnit * req.StuffDef.GetStatValueAbstract(StatDefOf.MarketValue);
                    else
                        ingredientCost += req.BuildableDef.CostStuffCount * 2f;
                }
                
                return marketValue / ingredientCost;
            }
			
			return -1f;
		}
        
		/*public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
		{
			return "";
		}*/
    }
}