using HarmonyLib;
using Verse;

namespace rimstocks;

public class harmonyPatch_core : Mod
{
    public harmonyPatch_core(ModContentPack content) : base(content)
    {
        var harmony = new Harmony("yayo.rimstocks.1");
        harmony.PatchAll();
    }
}