using HarmonyLib;
using RimWorld;

namespace WarbondsLite
{
    /// <summary>
    /// Harmony patch for modifying Tradeable.InitPriceDataIfNeeded method.
    /// </summary>
    [HarmonyPatch(typeof(Tradeable), "InitPriceDataIfNeeded")]
    internal class PatchTradeableInitPriceDataIfNeeded
    {
        /// <summary>
        /// Postfix method to modify trade prices for items with the "warbond" trade tag.
        /// </summary>
        [HarmonyPostfix]
        private static void Postfix(Tradeable __instance, ref float ___pricePlayerBuy, ref float ___pricePlayerSell)
        {
            // Check if the item has the "warbond" trade tag
            if (__instance.ThingDef.tradeTags != null && __instance.ThingDef.tradeTags.Contains("warbond"))
            {
                // Adjust buy and sell prices based on specified criteria
                ___pricePlayerBuy = ___pricePlayerSell = __instance.ThingDef.BaseMarketValue * ModBase.SellPrice *
                                     (__instance.AnyThing.HitPoints / (float)__instance.AnyThing.MaxHitPoints);
            }
        }
    }
}
