using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using System.Linq;
using Verse;

namespace Capitalism.Patches
{
    [HarmonyPatch(typeof(ThingWithComps), "PreTraded")]
    public class Patch_RegisterTrades
    {
        static void Postfix(TradeAction action, Pawn playerNegotiator, ITrader trader, ThingDef ___def, int ___stackCount)
        {
            if (CapitalismUtils.ShouldIgnoreTrade()) return;

            var comp = Find.World.GetComponent<CapitalismWorldComponent>();
            if (comp != null && ___def != ThingDefOf.Silver)
            {
                comp.RegisterTrade(trader.Faction, trader, ___def, action == TradeAction.PlayerSells ? -___stackCount : ___stackCount);

                if (trader is Caravan || trader is Pawn)
                {
                    comp.RegisterCaravanTrader(trader as Pawn, trader.TraderName, trader.Goods.ToList());
                }
                else if (trader is TradeShip)
                {
                    comp.RegisterOrbitalTrader(trader.TraderName, trader.Goods.ToList());
                }
            }
        }
    }
}
