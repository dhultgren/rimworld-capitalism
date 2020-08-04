using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Capitalism.Patches
{
    [HarmonyPatch(typeof(ThingWithComps), "PreTraded")]
    public class Patch_RegisterTrades
    {
        static void Postfix(TradeAction action, Pawn playerNegotiator, ITrader trader, ThingDef ___def, int ___stackCount)
        {
            if (TradeSession.trader.TraderKind.orbital) return;

            var comp = Find.World.GetComponent<CapitalismWorldComponent>();
            if (comp != null && ___def != ThingDefOf.Silver)
            {
                comp.RegisterTrade(trader.Faction, trader as Settlement, ___def, action == TradeAction.PlayerSells ? -___stackCount : ___stackCount);
            }
        }
    }
}
