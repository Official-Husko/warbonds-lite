﻿using HarmonyLib;
using RimWorld;

namespace WarbondsLite;

[HarmonyPatch(typeof(Tradeable), "InitPriceDataIfNeeded")]
internal class Patch_Tradeable_InitPriceDataIfNeeded
{
    [HarmonyPostfix]
    private static void Postfix(Tradeable __instance, ref float ___pricePlayerBuy, ref float ___pricePlayerSell)
    {
        if (__instance.ThingDef.tradeTags == null || !__instance.ThingDef.tradeTags.Contains("warbond")) return;

        ___pricePlayerBuy = __instance.ThingDef.BaseMarketValue;
        ___pricePlayerSell = __instance.ThingDef.BaseMarketValue * ModBase.SellPrice *
                             (__instance.AnyThing.HitPoints / (float)__instance.AnyThing.MaxHitPoints);
    }
}