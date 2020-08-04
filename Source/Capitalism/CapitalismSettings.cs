using Verse;

namespace Capitalism
{
    public class CapitalismSettings : ModSettings
    {
        public int MaxSupplyDemandChangePercent = 300;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref MaxSupplyDemandChangePercent, "maxSupplyDemandChangePercent");
        }
    }
}
