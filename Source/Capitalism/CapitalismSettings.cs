using UnityEngine;
using Verse;

namespace Capitalism
{
    public class CapitalismSettings : ModSettings
    {
        public readonly int RememberSettlementMaxTime = 30000;
        public readonly int RememberCaravanTime = 30000;
        public readonly int RememberOrbitalTradersTime = 15000;

        public int MaxSupplyDemandChangePercent = 300;
        public float EffectMultiplier = 1f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref MaxSupplyDemandChangePercent, "maxSupplyDemandChangePercent");
            Scribe_Values.Look(ref EffectMultiplier, "effectMultiplier", 1f);
        }

        public void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.ColumnWidth = 500f;
            listing.Begin(inRect);

            string buffer = MaxSupplyDemandChangePercent.ToString();
            listing.Label("Limit how much the price can theoretically fluctuate with supply and demand, in percent.");
            listing.IntEntry(ref MaxSupplyDemandChangePercent, ref buffer);
            listing.Gap(40);

            listing.Label("Multiplier for how much supply/demand will affect prices (while staying within the limits). Current value: " + EffectMultiplier.ToString("F2") + ".");
            EffectMultiplier = listing.Slider(EffectMultiplier, 0f, 5f);

            listing.End();
        }
    }
}
