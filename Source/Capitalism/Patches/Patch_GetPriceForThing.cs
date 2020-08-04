using HarmonyLib;
using RimWorld;

namespace Capitalism.Patches
{
    [HarmonyPatch(typeof(Tradeable), "GetPriceFor")]
    public class Patch_GetPriceForThing
    {
        static void Postfix(Tradeable __instance, ref float __result)
        {
            if (TradeSession.trader.TraderKind.orbital) return;

            var modifier = CapitalismUtils.GetPriceModifierIfExists(__instance.ThingDef);
            if (modifier.HasValue) __result *= modifier.Value;
        }
    }
}
