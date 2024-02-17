using HarmonyLib;
using UnityEngine;
using Verse;

namespace WarbondsLite
{
    /// <summary>
    /// Harmony patch for the TryAbsorbStack method in the Thing class.
    /// </summary>
    [HarmonyPatch(typeof(Thing), nameof(Thing.TryAbsorbStack))]
    internal class PatchThingTryAbsorbStack
    {
        /// <summary>
        /// Harmony postfix for the TryAbsorbStack method.
        /// </summary>
        /// <param name="__instance">The current instance of the Thing class.</param>
        /// <param name="__result">The result of the TryAbsorbStack method.</param>
        /// <param name="other">The other Thing to absorb.</param>
        /// <param name="respectStackLimit">Flag indicating whether to respect stack limits.</param>
        /// <returns>Returns true to continue with the original method logic.</returns>
        [HarmonyPostfix]
        public static bool Prefix(Thing __instance, ref bool __result, Thing other, bool respectStackLimit)
        {
            // Check if the current instance has CompLifespan
            if (!TryGetCompLifespan(__instance, out var cp) || !TryGetCompLifespan(other, out var cp_other))
                return true;

            // Calculate the number of items to absorb and update the age based on weighted averages
            var num = ThingUtility.TryAbsorbStackNumToTake(__instance, other, respectStackLimit);
            cp.age = Mathf.CeilToInt((cp.age * __instance.stackCount + cp_other.age * num) / (float)(__instance.stackCount + num));

            return true;
        }

        /// <summary>
        /// Helper method to try getting CompLifespan from a Thing and check for null.
        /// </summary>
        /// <param name="thing">The Thing instance to check for CompLifespan.</param>
        /// <param name="compLifespan">Output parameter to store the CompLifespan if found.</param>
        /// <returns>Returns true if CompLifespan is found; otherwise, returns false.</returns>
        private static bool TryGetCompLifespan(Thing thing, out CompLifespan compLifespan)
        {
            compLifespan = thing.TryGetComp<CompLifespan>();
            return compLifespan != null;
        }
    }
}
