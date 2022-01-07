using RimWorld;

namespace SellValuePer
{
    public class StatWorker_SellValuePerIngredients : StatWorker
    {
        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
		{
            if (req.BuildableDef != null)
                return req.GetSellValuePerIngredients();

            return -1f;
		}

        /*public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
		{
			return "";
		}*/
    }
}