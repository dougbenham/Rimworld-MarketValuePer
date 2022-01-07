using RimWorld;

namespace SellValuePer
{
    public class StatWorker_SellValuePerWork : StatWorker
    {
        public override float GetValueUnfinalized(StatRequest req, bool applyPostProcess = true)
        {
            if (req.BuildableDef != null)
                return req.GetSellValuePerWork();

            return -1f;
        }


        /*public override string GetExplanationUnfinalized(StatRequest req, ToStringNumberSense numberSense)
        {
            return "";
        }*/
    }
}