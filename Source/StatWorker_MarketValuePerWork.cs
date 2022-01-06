using RimWorld;
using UnityEngine;

namespace MarketValuePer
{
    public class StatWorker_MarketValuePerWork : StatWorker
    {
        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            if (req.BuildableDef != null)
            {
                var marketValue = req.BuildableDef.GetStatValueAbstract(StatDefOf.MarketValue, req.StuffDef);
                var work = Mathf.Max(req.BuildableDef.GetStatValueAbstract(StatDefOf.WorkToMake, req.StuffDef), req.BuildableDef.GetStatValueAbstract(StatDefOf.WorkToBuild, req.StuffDef));

                return marketValue / work;
            }
			
            return -1f;
        }
        
        /*public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            return "";
        }*/
    }
}