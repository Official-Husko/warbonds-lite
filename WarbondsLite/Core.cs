using System;
using System.Collections.Generic;
using System.Linq;
using RimWar.Planet;
using RimWorld;
using UnityEngine;
using Verse;

namespace WarbondsLite;

public class Core : MapComponent
{
    public enum en_graphStyle
    {
        small,
        normal,
        big
    }

    public static readonly List<ThingDef> ar_warbondDef = new();
    public static readonly List<FactionDef> ar_faction = new();
    public static readonly List<en_graphStyle> ar_graphStyle = new();

    public static readonly float basicPrice = 500f;
    private readonly float maxPrice = 10000f;

    private readonly float minPrice = 1f;


    public Core(Map map) : base(map)
    {
    }

    public static int AbsTickGame => Find.TickManager.TicksGame + GenDate.GameStartHourOfDay * GenDate.TicksPerHour;


    public static bool isWarbondFaction(FactionDef f)
    {
        if (f.pawnGroupMakers == null ||
            f.hidden ||
            f.isPlayer)
            return false;

        if (!f.naturalEnemy &&
            !f.mustStartOneEnemy &&
            !f.permanentEnemy)
            return true;

        if (ModBase.UseEnemyFaction)
            if (!f.modContentPack.PackageId.Contains("ludeon"))
                return true;

        if (!ModBase.UseVanillaEnemyFaction) return f.defName == "Pirate";

        if (f.modContentPack.PackageId.Contains("ludeon")) return true;

        return f.defName == "Pirate";
    }

    public override void MapComponentTick()
    {
        base.MapComponentTick();
        if (map != Find.AnyPlayerHomeMap) return;
        // 틱 - 매일

        if (AbsTickGame % GenDate.TicksPerDay == 0)
        {
            if (ModBase.UseRimWar)
            {
                // 림워
                try
                {
                    ((Action)(() =>
                    {
                        // 이벤트에 따른 변화
                        foreach (var gc in Find.World.gameConditionManager.ActiveConditions)
                            switch (gc.def.defName)
                            {
                                case "rs_warbond_rise":
                                    // 주가 급상승
                                    ChangeRimwarAllFactionPower(new FloatRange(0.1f, 0.4f), 0.8f);
                                    break;
                                case "rs_warbond_fall":
                                    // 주가 급하강
                                    ChangeRimwarAllFactionPower(new FloatRange(0.1f, 0.4f), 0.2f);
                                    break;
                                case "rs_warbond_change":
                                    // 주가 급변동
                                    ChangeRimwarAllFactionPower(new FloatRange(0.1f, 0.4f), 0.5f);
                                    break;
                            }

                        // 현재 Faction power를 가격으로 적용
                        foreach (var fd in ar_faction)
                        {
                            var price = GetRimwarPriceByDef(fd);
                            WorldComponentPriceSaveLoad.SaveTrend(fd, AbsTickGame, price);
                            WorldComponentPriceSaveLoad.SavePrice(fd, AbsTickGame, price);
                            ar_warbondDef[ar_faction.IndexOf(fd)].SetStatBaseValue(StatDefOf.MarketValue, price);
                        }
                    }))();
                }
                catch (TypeLoadException)
                {
                }
            }
            else
            {
                // 일반

                // 채권 가격변동
                var tickGap = GenDate.TicksPerDay;


                for (var i = 0; i < ar_faction.Count; i++)
                {
                    var f = ar_faction[i];
                    var style = ar_graphStyle[i];
                    var prevTrend = WorldComponentPriceSaveLoad.LoadTrend(f, AbsTickGame - tickGap);
                    var prevTrend2 = WorldComponentPriceSaveLoad.LoadTrend(f, AbsTickGame - tickGap * 2);

                    // 추세 각도
                    float slope;
                    switch (style)
                    {
                        case en_graphStyle.small:
                            slope = prevTrend / prevTrend2 * Rand.Range(0.85f, 1.15f);
                            break;
                        default:
                            slope = prevTrend / prevTrend2 * Rand.Range(0.96f, 1.04f);
                            break;
                        case en_graphStyle.big:
                            slope = prevTrend / prevTrend2 * Rand.Range(0.995f, 1.005f);
                            break;
                    }

                    // 진동
                    var shake = 1f + Rand.Range(-0.05f, 0.05f);

                    // 상한 하한에서 튕겨 내려오기
                    if (prevTrend <= minPrice && Rand.Chance(0.2f)) slope += 1f / slope;

                    // 낮은확률로 그래프 꺽기
                    switch (style)
                    {
                        case en_graphStyle.small:
                            if (Rand.Chance(0.15f)) slope = 1f / slope;

                            break;
                        default:
                            if (Rand.Chance(0.12f)) slope = 1f / slope;

                            break;
                        case en_graphStyle.big:
                            if (Rand.Chance(0.1f)) slope = 1f / slope;

                            break;
                    }


                    // 각도가 클 수록 완만해질 확률 증가
                    switch (style)
                    {
                        case en_graphStyle.small:
                            if (Rand.Chance(Mathf.Abs(slope - 1f) * 0.8f))
                                slope = 1f + (slope - 1f) * Rand.Range(0.1f, 0.4f);

                            break;
                        default:
                            if (Rand.Chance(Mathf.Abs(slope - 1f) * 1.2f))
                                slope = 1f + (slope - 1f) * Rand.Range(0.1f, 0.4f);

                            break;
                        case en_graphStyle.big:
                            if (Rand.Chance(Mathf.Abs(slope - 1f) * 2.4f))
                                slope = 1f + (slope - 1f) * Rand.Range(0.1f, 0.4f);

                            break;
                    }


                    // 이벤트에 따른 변화
                    var eventDir = 0;
                    foreach (var gc in Find.World.gameConditionManager.ActiveConditions)
                        switch (gc.def.defName)
                        {
                            case "rs_warbond_rise":
                                // 주가 급상승
                                if (Rand.Chance(0.8f))
                                    eventDir = 1;
                                else
                                    eventDir = -1;

                                break;
                            case "rs_warbond_fall":
                                // 주가 급하강
                                if (Rand.Chance(0.2f))
                                    eventDir = 1;
                                else
                                    eventDir = -1;

                                break;
                            case "rs_warbond_change":
                                // 주가 급변동
                                if (Rand.Chance(0.5f))
                                    eventDir = 1;
                                else
                                    eventDir = -1;

                                break;
                        }

                    if (eventDir != 0)
                        switch (style)
                        {
                            case en_graphStyle.small:
                                slope = 1f + Rand.Range(0.1f, 0.5f) * eventDir;
                                break;
                            default:
                                slope = 1f + Rand.Range(0.07f, 0.35f) * eventDir;
                                break;
                            case en_graphStyle.big:
                                slope = 1f + Rand.Range(0.04f, 0.2f) * eventDir;
                                break;
                        }


                    // 가격이 높을수록 꺽여 내려올 확률 증가
                    switch (style)
                    {
                        case en_graphStyle.small:
                            if (slope > 1f && Rand.Chance(prevTrend / 700f * 0.2f))
                                slope = 1f / slope * Rand.Range(0.9f, 0.95f);

                            break;
                        default:
                            if (slope > 1f && Rand.Chance(Mathf.Max(0f, prevTrend - 2000f) / 2000f * 0.2f))
                                slope = 1f / slope * Rand.Range(0.9f, 0.95f);

                            break;
                        case en_graphStyle.big:
                            if (slope > 1f && Rand.Chance(Mathf.Max(0f, prevTrend - 4000f) / 3500f * 0.2f))
                                slope = 1f / slope * Rand.Range(0.9f, 0.95f);

                            break;
                    }


                    // 가격이 낮을수록 꺽여 올라갈 확률 증가
                    switch (style)
                    {
                        case en_graphStyle.small:
                            if (slope < 1f && Rand.Chance(Mathf.Clamp((400f - prevTrend) / 400f * 0.02f, 0f, 1f)))
                                slope *= Rand.Range(1.05f, 1.1f);

                            break;
                        default:
                            if (slope < 1f && Rand.Chance(Mathf.Clamp((400f - prevTrend) / 400f * 0.015f, 0f, 1f)))
                                slope *= Rand.Range(1.05f, 1.1f);

                            break;
                        case en_graphStyle.big:
                            if (slope < 1f && Rand.Chance(Mathf.Clamp((400f - prevTrend) / 400f * 0.015f, 0f, 1f)))
                                slope *= Rand.Range(1.05f, 1.1f);

                            break;
                    }


                    var newTrend = Mathf.Clamp(prevTrend * slope, minPrice, maxPrice);
                    var newPrice = Mathf.Clamp(newTrend * shake, minPrice, maxPrice);
                    ar_warbondDef[i].SetStatBaseValue(StatDefOf.MarketValue, newPrice);
                    WorldComponentPriceSaveLoad.SaveTrend(f, AbsTickGame, newTrend);
                    WorldComponentPriceSaveLoad.SavePrice(f, AbsTickGame, newPrice);


                    // 상장폐지
                    if (!(newPrice < ModBase.DelistingPrice)) continue;

                    WorldComponentPriceSaveLoad.SaveTrend(f, AbsTickGame - GenDate.TicksPerDay,
                        GetDefaultPrice(f));
                    WorldComponentPriceSaveLoad.SaveTrend(f, AbsTickGame, GetDefaultPrice(f));
                    WorldComponentPriceSaveLoad.SavePrice(f, AbsTickGame, GetDefaultPrice(f));

                    if (Util.RemoveAllThingByDef(ar_warbondDef[i]))
                        Messages.Message(new Message(
                            "bond.delisting.destroy".Translate(ar_warbondDef[i].label, ModBase.DelistingPrice),
                            MessageTypeDefOf.ThreatSmall));
                    else
                        Messages.Message(new Message(
                            "bond.delisting".Translate(ar_warbondDef[i].label, ModBase.DelistingPrice),
                            MessageTypeDefOf.ThreatSmall));
                }
            }
        }


        // 틱 - 분기

        if (Find.TickManager.TicksAbs % GenDate.TicksPerQuadrum != GenDate.TicksPerHour) return;

        // 배당금 지급
        for (var i = 0; i < ar_faction.Count; i++) Util.GiveDividend(ar_faction[i], ar_warbondDef[i]);
    }


    public static void OnQuestResult(FactionDef f, FactionDef f2, bool success, float point)
    {
        var targetTime = AbsTickGame;
        float changeScale;
        int index;
        float change;

        void resetChangeScale()
        {
            changeScale = Rand.Range(0.10f, 0.25f);
        }


        if (ModBase.UseRimWar)
        {
            // 림워
            try
            {
                ((Action)(() =>
                {
                    float price;
                    if (f != null)
                    {
                        index = ar_faction.IndexOf(f);
                        if (index >= 0)
                        {
                            price = 0;
                            foreach (var faction in Find.FactionManager.AllFactions)
                            {
                                if (faction.def != f) continue;

                                var data =
                                    WorldUtility.GetRimWarDataForFaction(faction);
                                if (data == null) continue;

                                change = 1f;
                                resetChangeScale();
                                changeScale *= Mathf.Min(1f,
                                    1500f * ModBase.RimwarPriceFactor / GetRimwarPriceByDef(f));
                                if (success)
                                {
                                    change = 1f + changeScale;
                                    Messages.Message(new Message(
                                        "bond.quest.up".Translate(ar_warbondDef[index].label,
                                            (changeScale * 100f).ToString("0.#")),
                                        MessageTypeDefOf.ThreatSmall));
                                }
                                else
                                {
                                    change = 1f - changeScale;
                                    Messages.Message(new Message(
                                        "bond.quest.down".Translate(ar_warbondDef[index].label,
                                            "-" + (changeScale * 100f).ToString("0.#")),
                                        MessageTypeDefOf.ThreatSmall));
                                }

                                foreach (var st in data.WarSettlementComps)
                                    st.RimWarPoints = Mathf.RoundToInt(st.RimWarPoints * change);

                                price += data.TotalFactionPoints;
                            }

                            price *= ModBase.RimwarPriceFactor;
                            WorldComponentPriceSaveLoad.SaveTrend(f, AbsTickGame, price);
                            WorldComponentPriceSaveLoad.SavePrice(f, AbsTickGame, price);
                            ar_warbondDef[ar_faction.IndexOf(f)].SetStatBaseValue(StatDefOf.MarketValue, price);
                        }
                    }

                    if (f2 == null) return;

                    index = ar_faction.IndexOf(f2);
                    if (index < 0) return;

                    price = 0;
                    foreach (var faction in Find.FactionManager.AllFactions)
                    {
                        if (faction.def != f2) continue;

                        var data =
                            WorldUtility.GetRimWarDataForFaction(faction);
                        if (data == null) continue;

                        change = 1f;
                        resetChangeScale();
                        changeScale *= Mathf.Min(1f,
                            1500f * ModBase.RimwarPriceFactor / GetRimwarPriceByDef(f));
                        if (!success)
                        {
                            change = 1f + changeScale;
                            Messages.Message(new Message(
                                "bond.quest.up".Translate(ar_warbondDef[index].label,
                                    (changeScale * 100f).ToString("0.#")),
                                MessageTypeDefOf.ThreatSmall));
                        }
                        else
                        {
                            change = 1f - changeScale;
                            Messages.Message(new Message(
                                "bond.quest.down".Translate(ar_warbondDef[index].label,
                                    "-" + (changeScale * 100f).ToString("0.#")),
                                MessageTypeDefOf.ThreatSmall));
                        }

                        foreach (var st in data.WarSettlementComps)
                            st.RimWarPoints = Mathf.RoundToInt(st.RimWarPoints * change);

                        price += data.TotalFactionPoints;
                    }

                    price *= ModBase.RimwarPriceFactor;
                    WorldComponentPriceSaveLoad.SaveTrend(f2, AbsTickGame, price);
                    WorldComponentPriceSaveLoad.SavePrice(f2, AbsTickGame, price);
                    ar_warbondDef[ar_faction.IndexOf(f2)].SetStatBaseValue(StatDefOf.MarketValue, price);
                }))();
            }
            catch (TypeLoadException)
            {
            }

            return;
        }

        // 일반
        float prev;
        if (f != null)
        {


    /// <summary>
    /// Patch the definition of warbond items for each warbond faction.
    /// </summary>
    public static void PatchDef()
    {
        // Iterate through all faction definitions and create warbond items
        foreach (var factionDef in DefDatabase<FactionDef>.AllDefs.Where(isWarbondFaction))
        {
            // Create a new ThingDef for warbond item
            var thingDef = new ThingDef
            {
                // Basic properties
                thingClass = typeof(ThingWithComps),
                category = ThingCategory.Item,
                resourceReadoutPriority = ResourceCountPriority.Middle,
                selectable = true,
                altitudeLayer = AltitudeLayer.Item,
                comps = new List<CompProperties> { new CompProperties_Forbiddable() },
                alwaysHaulable = true,
                drawGUIOverlay = true,
                rotatable = false,
                pathCost = 14,

                // Detail properties
                defName = $"oh_warbond_{factionDef.defName}",
                label = string.Format("warbond_t".Translate(), factionDef.label),
                description = string.Format("warbond_d".Translate(), factionDef.label),
                graphicData = new GraphicData
                {
                    texPath = factionDef.factionIconPath,
                    // Use the first color from the colorSpectrum, or white if colorSpectrum is null
                    color = factionDef.colorSpectrum?.FirstOrDefault() ?? Color.white,
                    graphicClass = typeof(Graphic_Single)
                },
                soundInteract = SoundDef.Named("Silver_Drop"),
                soundDrop = SoundDef.Named("Silver_Drop"),

                // Stat modifications
                healthAffectsPrice = true,
                statBases = new List<StatModifier>(),
                useHitPoints = true,
                stackLimit = 999,

                // Trade and market properties
                tradeability = Tradeability.All,
                tradeTags = new List<string> { "warbond" },
                tickerType = TickerType.Rare
            };

            // SetStatBaseValues
            thingDef.SetStatBaseValue(StatDefOf.MaxHitPoints, 30f);
            thingDef.SetStatBaseValue(StatDefOf.Flammability, 1f);
            thingDef.SetStatBaseValue(StatDefOf.MarketValue, basicPrice);
            thingDef.SetStatBaseValue(StatDefOf.Mass, 0.008f);

            // Add lifespan component if ModBase.LimitDate is greater than 0
            if (ModBase.LimitDate > 0)
                thingDef.comps.Add(new CompProperties_Lifespan { lifespanTicks = GenDate.TicksPerDay });

            // Add warbond thing category
            var thingCategoryDef = ThingCategoryDef.Named("warbond");
            if (thingCategoryDef != null) thingDef.thingCategories.Add(thingCategoryDef);

            // Add ThingDef to relevant lists
            ar_warbondDef.Add(thingDef);
            ar_faction.Add(factionDef);

            // Determine the graph style based on faction properties
            switch (factionDef.defName)
            {
                default:
                    // Determine graph style based on package ID and natural enemy status
                    int switchResult = ar_graphStyle.Count % 4;
                    ar_graphStyle.Add(factionDef.modContentPack.PackageId.Contains("ludeon") ?
                                      (!factionDef.naturalEnemy ? en_graphStyle.normal : en_graphStyle.small) :
                                      switchResult switch
                                      {
                                          0 => en_graphStyle.big,
                                          2 => en_graphStyle.small,
                                          _ => en_graphStyle.normal
                                      });
                    break;
                case "Pirate":
                    // For "Pirate" faction, use small graph style
                    ar_graphStyle.Add(en_graphStyle.small);
                    break;
                case "Empire":
                    // For "Empire" faction, use big graph style
                    ar_graphStyle.Add(en_graphStyle.big);
                    break;
            }

            // Add implied definition for the ThingDef
            DefGenerator.AddImpliedDef(thingDef);
        }

        // Perform additional patching for incidents
        PatchIncident();
    }

    /// <summary>
    /// Applies a patch to update the lifespanTicks property of CompProperties_Lifespan for specified definitions.
    /// </summary>
    public static void PatchDef2()
    {
        foreach (var t in ar_warbondDef)
        {
            if (t.GetCompProperties<CompProperties_Lifespan>() is { } cp)
            {
                cp.lifespanTicks = GenDate.TicksPerDay * ModBase.LimitDate;
            }
        }
    }

    /// <summary>
    /// Applies a patch to update the baseChance property of IncidentDef instances containing "rs_warbond" in their defName.
    /// </summary>
    public static void PatchIncident()
    {
        foreach (var i in DefDatabase<IncidentDef>.AllDefs.Where(i => i.defName.Contains("rs_warbond")))
        {
            i.baseChance = 3f * ModBase.PriceEventMultiply;
        }
    }

    /// <summary>
    /// Retrieves the default price based on the faction's graphStyle.
    /// </summary>
    /// <param name="fd">The FactionDef for which to retrieve the default price.</param>
    /// <returns>The default price based on the faction's graphStyle.</returns>
    public static float GetDefaultPrice(FactionDef fd)
    {
        var index = ar_faction.IndexOf(fd);

        // If the faction is not found or the index is out of range, return a random value.
        if (index < 0 || index >= ar_graphStyle.Count)
        {
            return Rand.Range(200f, 6000f);
        }

        var style = ar_graphStyle[index];

        // Determine the default price based on the faction's graphStyle.
        return style switch
        {
            en_graphStyle.small => Rand.Range(350f, 450f),
            en_graphStyle.big => Rand.Range(4100f, 4500f),
            _ => Rand.Range(1750f, 2050f),
        };
    }


    /// <summary>
    /// Changes the RimWar points for all factions based on a random factor and scaling range.
    /// </summary>
    /// <param name="changeScaleRange">The range used to scale the RimWar points.</param>
    /// <param name="increasePer">The probability of increasing the RimWar points.</param>
    public static void ChangeRimwarAllFactionPower(FloatRange changeScaleRange, float increasePer)
    {
        // Check if RimWar is enabled
        if (!ModBase.UseRimWar) return;

        foreach (var faction in Find.FactionManager.AllFactions)
        {
            var data = WorldUtility.GetRimWarDataForFaction(faction);
            
            // Skip factions without RimWar data
            if (data == null) continue;

            var multiply = 1f;
            
            // Adjust RimWar points based on random chance and scaling range
            if (Rand.Chance(increasePer))
            {
                var nerfForTooMuchPowerful = Mathf.Min(1f, 1500f * ModBase.RimwarPriceFactor / GetRimwarPrice(faction));
                multiply += Rand.Range(changeScaleRange.min, changeScaleRange.max) * nerfForTooMuchPowerful;
            }
            else
            {
                multiply -= Rand.Range(changeScaleRange.min, changeScaleRange.max);
            }

            // Calculate the adjustment factor once outside the loop
            var adjustmentFactor = Mathf.RoundToInt(multiply);

            // Apply the adjustment factor to each settlement's RimWar points
            foreach (var st in data.WarSettlementComps) st.RimWarPoints *= adjustmentFactor;
        }
    }

    /// <summary>
    /// Gets the total RimWar points for factions with a specific faction definition.
    /// </summary>
    /// <param name="factionDef">The faction definition to filter factions.</param>
    /// <returns>The total RimWar points for factions with the specified definition.</returns>
    public static float GetRimwarPriceByDef(FactionDef factionDef)
    {
        // Check if RimWar is enabled
        if (!ModBase.UseRimWar) return -1f;

        // Calculate and return the total RimWar points for factions with the specified definition
        var price = Find.FactionManager.AllFactions
            .Where(f => f.def == factionDef)
            .Sum(f => WorldUtility.GetRimWarDataForFaction(f)?.TotalFactionPoints ?? 0) * ModBase.RimwarPriceFactor;

        return Mathf.Max(1f, price);
    }

    /// <summary>
    /// Gets the total RimWar points for a specific faction.
    /// </summary>
    /// <param name="faction">The faction to retrieve RimWar points for.</param>
    /// <returns>The total RimWar points for the specified faction.</returns>
    public static float GetRimwarPrice(Faction faction)
    {
        // Check if RimWar is enabled
        if (!ModBase.UseRimWar) return -1f;

        // Calculate and return the total RimWar points for the specified faction
        var factionData = WorldUtility.GetRimWarDataForFaction(faction);
        var price = (factionData?.TotalFactionPoints ?? 0) * ModBase.RimwarPriceFactor;

        return Mathf.Max(1f, price);
    }

}