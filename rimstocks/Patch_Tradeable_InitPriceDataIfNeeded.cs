using HarmonyLib;
using RimWorld;

namespace rimstocks;

[HarmonyPatch(typeof(Tradeable), "InitPriceDataIfNeeded")]
internal class Patch_Tradeable_InitPriceDataIfNeeded
{
    [HarmonyPostfix]
    private static void Postfix(Tradeable __instance, ref float ___pricePlayerBuy, ref float ___pricePlayerSell)
    {
        if (__instance.ThingDef.tradeTags == null || !__instance.ThingDef.tradeTags.Contains("warbond"))
        {
            return;
        }

        ___pricePlayerBuy = __instance.ThingDef.BaseMarketValue;
        ___pricePlayerSell = __instance.ThingDef.BaseMarketValue * modBase.sellPrice *
                             (__instance.AnyThing.HitPoints / (float)__instance.AnyThing.MaxHitPoints);
    }
}