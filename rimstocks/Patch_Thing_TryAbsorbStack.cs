using HarmonyLib;
using UnityEngine;
using Verse;

namespace rimstocks;

[HarmonyPatch(typeof(Thing), nameof(Thing.TryAbsorbStack))]
internal class Patch_Thing_TryAbsorbStack
{
    [HarmonyPostfix]
    public static bool Prefix(Thing __instance, ref bool __result, Thing other, bool respectStackLimit)
    {
        var cp = __instance.TryGetComp<CompLifespan>();
        if (cp == null)
        {
            return true;
        }

        var cp_other = other.TryGetComp<CompLifespan>();
        if (cp_other == null)
        {
            return true;
        }

        var num = ThingUtility.TryAbsorbStackNumToTake(__instance, other, respectStackLimit);
        cp.age = Mathf.CeilToInt(((cp.age * __instance.stackCount) + (cp_other.age * num)) /
                                 (float)(__instance.stackCount + num));

        return true;
    }
}