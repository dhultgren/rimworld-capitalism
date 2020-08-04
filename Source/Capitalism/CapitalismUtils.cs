using RimWorld;
using Verse;

namespace Capitalism
{
    public static class CapitalismUtils
    {
        public static float? GetPriceModifierIfExists(ThingDef thingDef)
        {
            var comp = Find.World.GetComponent<CapitalismWorldComponent>();
            if (comp != null)
            {
                var modifiers = comp.PriceModifiers;
                if (modifiers.ContainsKey(thingDef))
                {
                    return modifiers[thingDef];
                }
            }
            return null;
        }

        public static string AddPriceModifierText(ThingDef thingDef)
        {
            if (TradeSession.trader.TraderKind.orbital) return string.Empty;

            var modifier = CapitalismUtils.GetPriceModifierIfExists(thingDef);
            if (modifier.HasValue)
            {
                return "\n  x " + modifier.Value.ToString("F2") + " (supply/demand)";
            }
            return string.Empty;
        }
    }
}
