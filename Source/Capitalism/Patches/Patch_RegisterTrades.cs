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
            var comp = Find.World.GetComponent<CapitalismWorldComponent>();
            if (comp != null)
            {
                comp.RegisterTrade(trader as Settlement, ___def, action == TradeAction.PlayerSells ? -___stackCount : ___stackCount);
            }
        }
    }
}
