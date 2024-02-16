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

        if (modBase.useEnemyFaction)
            if (!f.modContentPack.PackageId.Contains("ludeon"))
                return true;

        if (!modBase.useVanillaEnemyFaction) return f.defName == "Pirate";

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
            if (modBase.use_rimwar)
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
                                    changeRimwarAllFactionPower(new FloatRange(0.1f, 0.4f), 0.8f);
                                    break;
                                case "rs_warbond_fall":
                                    // 주가 급하강
                                    changeRimwarAllFactionPower(new FloatRange(0.1f, 0.4f), 0.2f);
                                    break;
                                case "rs_warbond_change":
                                    // 주가 급변동
                                    changeRimwarAllFactionPower(new FloatRange(0.1f, 0.4f), 0.5f);
                                    break;
                            }

                        // 현재 Faction power를 가격으로 적용
                        foreach (var fd in ar_faction)
                        {
                            var price = getRimwarPriceByDef(fd);
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
                    if (!(newPrice < modBase.DelistingPrice)) continue;

                    WorldComponentPriceSaveLoad.SaveTrend(f, AbsTickGame - GenDate.TicksPerDay,
                        getDefaultPrice(f));
                    WorldComponentPriceSaveLoad.SaveTrend(f, AbsTickGame, getDefaultPrice(f));
                    WorldComponentPriceSaveLoad.SavePrice(f, AbsTickGame, getDefaultPrice(f));

                    if (util.removeAllThingByDef(ar_warbondDef[i]))
                        Messages.Message(new Message(
                            "bond.delisting.destroy".Translate(ar_warbondDef[i].label, modBase.DelistingPrice),
                            MessageTypeDefOf.ThreatSmall));
                    else
                        Messages.Message(new Message(
                            "bond.delisting".Translate(ar_warbondDef[i].label, modBase.DelistingPrice),
                            MessageTypeDefOf.ThreatSmall));
                }
            }
        }


        // 틱 - 분기

        if (Find.TickManager.TicksAbs % GenDate.TicksPerQuadrum != GenDate.TicksPerHour) return;

        // 배당금 지급
        for (var i = 0; i < ar_faction.Count; i++) util.giveDividend(ar_faction[i], ar_warbondDef[i]);
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


        if (modBase.use_rimwar)
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
                                    1500f * modBase.rimwarPriceFactor / getRimwarPriceByDef(f));
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

                            price *= modBase.rimwarPriceFactor;
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
                            1500f * modBase.rimwarPriceFactor / getRimwarPriceByDef(f));
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

                    price *= modBase.rimwarPriceFactor;
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
            index = ar_faction.IndexOf(f);
            if (index >= 0)
            {
                prev = WorldComponentPriceSaveLoad.LoadTrend(f, targetTime);
                resetChangeScale();
                changeScale *= Mathf.Min(1f, 1500f / prev);
                if (success)
                {
                    change = 1f + changeScale;
                    Messages.Message(new Message(
                        "bond.quest.up".Translate(ar_warbondDef[index].label, (changeScale * 100f).ToString("0.#")),
                        MessageTypeDefOf.ThreatSmall));
                }
                else
                {
                    change = 1f - changeScale;
                    Messages.Message(new Message(
                        "bond.quest.down".Translate(ar_warbondDef[index].label,
                            "-" + (changeScale * 100f).ToString("0.#")), MessageTypeDefOf.ThreatSmall));
                }

                WorldComponentPriceSaveLoad.SaveTrend(f, targetTime, change * prev);
                prev = WorldComponentPriceSaveLoad.LoadPrice(f, targetTime);
                WorldComponentPriceSaveLoad.SavePrice(f, targetTime, change * prev);
                ar_warbondDef[index].SetStatBaseValue(StatDefOf.MarketValue, change * prev);
            }
        }


        if (f2 == null) return;

        index = ar_faction.IndexOf(f2);
        if (index < 0) return;

        prev = WorldComponentPriceSaveLoad.LoadTrend(f2, targetTime);
        resetChangeScale();
        changeScale *= Mathf.Min(1f, 1500f / prev);
        if (!success)
        {
            change = 1f + changeScale;
            Messages.Message(new Message(
                "bond.quest.up".Translate(ar_warbondDef[index].label, (changeScale * 100f).ToString("0.#")),
                MessageTypeDefOf.ThreatSmall));
        }
        else
        {
            change = 1f - changeScale;
            Messages.Message(new Message(
                "bond.quest.down".Translate(ar_warbondDef[index].label,
                    "-" + (changeScale * 100f).ToString("0.#")), MessageTypeDefOf.ThreatSmall));
        }

        WorldComponentPriceSaveLoad.SaveTrend(f2, targetTime, change * prev);
        prev = WorldComponentPriceSaveLoad.LoadPrice(f2, targetTime);
        WorldComponentPriceSaveLoad.SavePrice(f2, targetTime, change * prev);
        ar_warbondDef[index].SetStatBaseValue(StatDefOf.MarketValue, change * prev);
    }


    public static void patchDef()
    {
        // 채권 아이템 DEF 생성
        foreach (var f in from f in DefDatabase<FactionDef>.AllDefs
                 where
                     isWarbondFaction(f)
                 select f)
        {
            var t = new ThingDef
            {
                // base
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
                // detail
                defName = $"oh_warbond_{f.defName}",
                label = string.Format("warbond_t".Translate(), f.label),
                description = string.Format("warbond_d".Translate(), f.label),
                graphicData = new GraphicData
                {
                    texPath = f.factionIconPath
                }
            };

            if (f.colorSpectrum is { Count: > 0 }) t.graphicData.color = f.colorSpectrum[0];

            t.graphicData.graphicClass = typeof(Graphic_Single);
            t.soundInteract = SoundDef.Named("Silver_Drop");
            t.soundDrop = SoundDef.Named("Silver_Drop");

            t.healthAffectsPrice = true;
            t.statBases = new List<StatModifier>();


            t.useHitPoints = true;
            t.SetStatBaseValue(StatDefOf.MaxHitPoints, 30f);
            t.SetStatBaseValue(StatDefOf.Flammability, 1f);

            t.SetStatBaseValue(StatDefOf.MarketValue, basicPrice);
            t.SetStatBaseValue(StatDefOf.Mass, 0.008f);

            t.thingCategories = new List<ThingCategoryDef>();

            t.stackLimit = 999;

            t.burnableByRecipe = true;
            t.smeltable = false;
            t.terrainAffordanceNeeded = TerrainAffordanceDefOf.Medium;

            var thingCategoryDef = ThingCategoryDef.Named("warbond");
            if (thingCategoryDef != null) t.thingCategories.Add(thingCategoryDef);

            t.tradeability = Tradeability.All;

            t.tradeTags = new List<string> { "warbond" };

            t.tickerType = TickerType.Rare;

            if (modBase.limitDate > 0)
            {
                var cp_lifespan = new CompProperties_Lifespan
                {
                    lifespanTicks = GenDate.TicksPerDay
                };
                t.comps.Add(cp_lifespan);
            }


            // 등록
            ar_warbondDef.Add(t);
            ar_faction.Add(f);
            switch (f.defName)
            {
                default:
                    if (f.modContentPack.PackageId.Contains("ludeon"))
                        ar_graphStyle.Add(!f.naturalEnemy ? en_graphStyle.normal : en_graphStyle.small);
                    else
                        switch (ar_graphStyle.Count % 4)
                        {
                            default:
                                ar_graphStyle.Add(en_graphStyle.normal);
                                break;
                            case 0:
                                ar_graphStyle.Add(en_graphStyle.big);
                                break;
                            case 2:
                                ar_graphStyle.Add(en_graphStyle.small);
                                break;
                        }

                    break;
                case "Pirate":
                    ar_graphStyle.Add(en_graphStyle.small);
                    break;
                case "Empire":
                    ar_graphStyle.Add(en_graphStyle.big);
                    break;
            }

            DefGenerator.AddImpliedDef(t);
        }

        patchIncident();
    }


    public static void patchDef2()
    {
        foreach (var t in ar_warbondDef)
        {
            var cp = t.GetCompProperties<CompProperties_Lifespan>();
            if (cp == null) continue;

            cp.lifespanTicks = GenDate.TicksPerDay * modBase.limitDate;
        }
    }

    public static void patchIncident()
    {
        foreach (var i in from i in DefDatabase<IncidentDef>.AllDefs
                 where
                     i.defName.Contains("rs_warbond")
                 select i)
            i.baseChance = 3f * modBase.priceEvent_multiply;
    }


    public static float getDefaultPrice(FactionDef fd)
    {
        var index = ar_faction.IndexOf(fd);
        if (index < 0 || index >= ar_graphStyle.Count) return Rand.Range(200f, 6000f);

        var style = ar_graphStyle[index];
        switch (style)
        {
            case en_graphStyle.small:
                return Rand.Range(350f, 450f);
            default:
                return Rand.Range(1750f, 2050f);
            case en_graphStyle.big:
                return Rand.Range(4100f, 4500f);
        }
    }

    public static void changeRimwarAllFactionPower(FloatRange changeScaleRange, float increasePer)
    {
        if (!modBase.use_rimwar) return;

        foreach (var f in Find.FactionManager.AllFactions)
        {
            var data = WorldUtility.GetRimWarDataForFaction(f);
            if (data == null) continue;

            var multiply = 1f;
            if (Rand.Chance(increasePer))
            {
                var nerfForTooMuchPowerful = Mathf.Min(1f, 1500f * modBase.rimwarPriceFactor / getRimwarPrice(f));
                multiply += Rand.Range(changeScaleRange.min, changeScaleRange.max) * nerfForTooMuchPowerful;
            }
            else
            {
                multiply -= Rand.Range(changeScaleRange.min, changeScaleRange.max);
            }

            foreach (var st in data.WarSettlementComps) st.RimWarPoints = Mathf.RoundToInt(st.RimWarPoints * multiply);
        }
    }

    public static float getRimwarPriceByDef(FactionDef fd)
    {
        var price = -1f;
        if (!modBase.use_rimwar) return price;

        // 림워
        try
        {
            ((Action)(() =>
            {
                price = 0;
                foreach (var f in Find.FactionManager.AllFactions)
                    if (f.def == fd)
                        price += WorldUtility.GetRimWarDataForFaction(f) != null
                            ? WorldUtility.GetRimWarDataForFaction(f).TotalFactionPoints
                            : 0;

                price *= modBase.rimwarPriceFactor;
                price = Mathf.Max(1f, price);
            }))();
        }
        catch (TypeLoadException)
        {
        }

        return price;
    }

    public static float getRimwarPrice(Faction f)
    {
        var price = -1f;
        if (!modBase.use_rimwar) return price;

        // 림워
        try
        {
            ((Action)(() =>
            {
                price = WorldUtility.GetRimWarDataForFaction(f) != null
                    ? WorldUtility.GetRimWarDataForFaction(f).TotalFactionPoints
                    : 0;
                price *= modBase.rimwarPriceFactor;
                price = Mathf.Max(1f, price);
            }))();
        }
        catch (TypeLoadException)
        {
        }

        return price;
    }
}