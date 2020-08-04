using HarmonyLib;
using RimWorld;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;

namespace Capitalism.Patches
{
    // Add the supply/demand row to price tooltip
    [HarmonyPatch(typeof(Tradeable), "GetPriceTooltip")]
    public class Patch_GetPriceTooltip
    {
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var newCall = typeof(CapitalismUtils).GetMethod("AddPriceModifierText", BindingFlags.Static | BindingFlags.Public);
            var getThingDef = typeof(Tradeable).GetMethod("get_ThingDef", BindingFlags.Public | BindingFlags.Instance);
            var concat = typeof(string).GetMethod("Concat", new[] { typeof(string), typeof(string) });

            var instructionList = instructions.ToList();
            for (var i = 0; i < instructionList.Count; i++)
            {
                var instruction = instructionList[i];
                yield return instruction;

                if (MatchesInstructions(instructionList, new List<string> { "ldloc.0", "ldstr \"\\n\"", "String::Concat"}, i))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0); // Load instance
                    yield return new CodeInstruction(OpCodes.Callvirt, getThingDef); // Load function parameter
                    yield return new CodeInstruction(OpCodes.Callvirt, newCall); // Call function generating new text
                    yield return new CodeInstruction(OpCodes.Call, concat); // Concat text
                    yield return new CodeInstruction(OpCodes.Stloc_0); // Save textr

                    yield return new CodeInstruction(OpCodes.Ldloc_0); // return to previous state
                }
            }
        }

        private static bool MatchesInstructions(List<CodeInstruction> instructions, List<string> instructionsToMatch, int index)
        {
            if (instructions.Count < index + instructionsToMatch.Count) return false;

            for (var i = 0; i < instructionsToMatch.Count; i++)
            {
                if (!instructions[index + i].ToString().Contains(instructionsToMatch[i])) return false;
            }
            return true;
        }
    }
}
