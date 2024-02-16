using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace WarbondsLite;

public class FactionPriceData : IExposable
{
    public const int modularTicksUnit = 60000;
    public Color color;
    public string defname;
    public bool graphEnabled = true;
    public string label;
    public Dictionary<int, float> timeToPriceData = new();

    public Dictionary<int, float> timeToTrendData = new();

    public void ExposeData()
    {
        Scribe_Values.Look(ref graphEnabled, "graphEnabled", true);
        Scribe_Values.Look(ref defname, "defname", "defname");
        Scribe_Values.Look(ref label, "label", "FACTIONNAME");
        Scribe_Values.Look(ref color, "color");
        Scribe_Collections.Look(ref timeToPriceData, "timeToPriceData", LookMode.Value, LookMode.Value);
        Scribe_Collections.Look(ref timeToTrendData, "timeToTrendData", LookMode.Value, LookMode.Value);
    }

    public void SavePrice(float tick, float price)
    {
        var unitTime = Mathf.FloorToInt(tick / modularTicksUnit);
        if (timeToPriceData.ContainsKey(unitTime)) timeToPriceData.Remove(unitTime);

        timeToPriceData.Add(unitTime, price);
    }

    public float LoadPrice(float tick)
    {
        var unitTime = Mathf.FloorToInt(tick / modularTicksUnit);
        if (timeToPriceData.TryGetValue(unitTime, out var price)) return price;

        if (modBase.use_rimwar && FactionDef.Named(defname) != null)
            return Core.getRimwarPriceByDef(FactionDef.Named(defname));

        return FactionDef.Named(defname) != null
            ? Core.getDefaultPrice(FactionDef.Named(defname))
            : Rand.Range(200f, 6000f);
    }

    public void SaveTrend(float tick, float trend)
    {
        var unitTime = Mathf.FloorToInt(tick / modularTicksUnit);
        if (timeToTrendData.ContainsKey(unitTime)) timeToTrendData.Remove(unitTime);

        timeToTrendData.Add(unitTime, trend);
    }

    public float LoadTrend(float tick)
    {
        var unitTime = Mathf.FloorToInt(tick / modularTicksUnit);
        if (timeToTrendData.TryGetValue(unitTime, out var trend)) return trend;

        if (modBase.use_rimwar && FactionDef.Named(defname) != null)
            return Core.getRimwarPriceByDef(FactionDef.Named(defname));

        return FactionDef.Named(defname) != null
            ? Core.getDefaultPrice(FactionDef.Named(defname))
            : Rand.Range(200f, 6000f);
    }
}