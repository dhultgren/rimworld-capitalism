using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace Capitalism.Patches
{
    [HarmonyPatch(typeof(Settlement_TraderTracker), "RegenerateStock")]
    public class Patch_ForgetTradesOnRegenerateStock
    {
        static void Postfix(Settlement ___settlement)
        {
            var comp = Find.World.GetComponent<CapitalismWorldComponent>();
            if (comp != null)
            {
                comp.ForgetTradesFor(___settlement);
            }
        }
    }
}
