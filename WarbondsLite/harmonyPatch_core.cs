using HarmonyLib;
using Verse;

namespace WarbondsLite;

public class harmonyPatch_core : Mod
{
    public harmonyPatch_core(ModContentPack content) : base(content)
    {
        var harmony = new Harmony("husko.WarbondsLite.1");
        harmony.PatchAll();
    }
}