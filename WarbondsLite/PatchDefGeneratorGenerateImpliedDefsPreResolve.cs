using HarmonyLib;
using RimWorld;

namespace WarbondsLite
{
    /// <summary>
    /// Harmony patch to modify the behavior of DefGenerator.GenerateImpliedDefs_PreResolve method.
    /// </summary>
    [HarmonyPatch(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve")]
    public static class PatchDefGeneratorGenerateImpliedDefsPreResolve
    {
        /// <summary>
        /// Prefix method called before the original GenerateImpliedDefs_PreResolve method.
        /// </summary>
        [HarmonyPrefix]
        public static void Prefix() => Core.PatchDef();
    }
}
