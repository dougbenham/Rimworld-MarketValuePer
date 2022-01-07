using RimWorld;

namespace SellValuePer
{
    public class StatWorker_SellValueRating : StatWorker
    {
        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            if (req.BuildableDef != null)
                return Extensions.GetSellValueRating(req.GetSellValuePerIngredients(), req.GetSellValuePerWork());

            return -1f;
        }


        /*public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            return "";
        }*/
    }
}