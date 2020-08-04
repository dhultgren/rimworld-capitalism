using HarmonyLib;
using System.Reflection;
using Verse;

namespace Capitalism
{
    [StaticConstructorOnStartup]
    public class Capitalism
    {
        static Capitalism()
        {
            new Harmony("Capitalism").PatchAll(Assembly.GetExecutingAssembly());
        }
    }
}
