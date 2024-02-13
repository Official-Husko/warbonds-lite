using HarmonyLib;
using Verse;

namespace rimstocks;

public class harmonyPatch_core : Mod
{
    public harmonyPatch_core(ModContentPack content) : base(content)
    {
        var harmony = new Harmony("yayo.rimstocks.1");
        /*
        harmony.Patch(
            AccessTools.Method(typeof(DefGenerator), "GenerateImpliedDefs_PreResolve"),
            null,
            new HarmonyMethod(typeof(rimstocks.harmonyPatch_core), nameof(rimstocks.harmonyPatch_core.Patch_DefGenerator_GenerateImpliedDefs_PreResolve))
        );
        */
        harmony.PatchAll();
    }
    /*
    static public void Patch_DefGenerator_GenerateImpliedDefs_PreResolve()
    {
        rimstocks.Core.patchDef();
    }
    */
}