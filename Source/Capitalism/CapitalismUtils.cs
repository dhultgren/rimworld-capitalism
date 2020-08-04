using RimWorld;
using Verse;

namespace Capitalism
{
    public static class CapitalismUtils
    {
        public static float? GetPriceModifierIfExists(ThingDef thingDef)
        {
            var comp = Find.World.GetComponent<CapitalismWorldComponent>();
            var def = GroupCertainThingDefs(thingDef);
            if (comp != null)
            {
                var modifiers = comp.PriceModifiers;
                if (modifiers.ContainsKey(def))
                {
                    return modifiers[def];
                }
            }
            return null;
        }

        public static string AddPriceModifierText(ThingDef thingDef)
        {
            if (TradeSession.trader.TraderKind.orbital) return string.Empty;

            var modifier = GetPriceModifierIfExists(thingDef);
            if (modifier.HasValue)
            {
                return "\n  x " + modifier.Value.ToString("F2") + " (supply/demand)";
            }
            return string.Empty;
        }

        // Group certain things because there's just too many similar ones which can cause crazy multipliers
        public static ThingDef GroupCertainThingDefs(ThingDef thingDef)
        {
            if (thingDef.FirstThingCategory == ThingCategoryDefOf.MeatRaw) return ThingDefOf.Meat_Human;
            if (thingDef.FirstThingCategory == ThingCategoryDefOf.Leathers) return ThingDefOf.Leather_Plain;

            return thingDef;
        }
    }
}
