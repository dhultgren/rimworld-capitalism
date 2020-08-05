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
            Settings.DoSettingsWindowContents(inRect);
        }

        public override string SettingsCategory() => "Capitalism";
    }
}
