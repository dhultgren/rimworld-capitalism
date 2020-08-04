using HarmonyLib;
using System.Reflection;
using UnityEngine;
using Verse;

namespace Capitalism
{
    [StaticConstructorOnStartup]
    public class Capitalism : Mod
    {
        public static CapitalismSettings Settings;

        static Capitalism()
        {
            new Harmony("Capitalism").PatchAll(Assembly.GetExecutingAssembly());
        }

        public Capitalism(ModContentPack content) : base(content)
        {
            Settings = GetSettings<CapitalismSettings>();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.ColumnWidth = 400f;
            listing.Begin(inRect);

            string buffer = Settings.MaxSupplyDemandChangePercent.ToString();
            listing.Label("Limit how much the price will fluctuate with supply and demand, in percent.");
            listing.IntEntry(ref Settings.MaxSupplyDemandChangePercent, ref buffer);

            listing.End();
            base.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory() => "Capitalism";
    }
}
