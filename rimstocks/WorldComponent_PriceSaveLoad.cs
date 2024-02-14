using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace rimstocks;

public class WorldComponent_PriceSaveLoad : WorldComponent
{
    public static WorldComponent_PriceSaveLoad staticInstance;
    public Dictionary<string, FactionPriceData> factionToPriceData = new();
    public bool initialized;

    public WorldComponent_PriceSaveLoad(World world) : base(world)
    {
        staticInstance = this;
    }

    public static void savePrice(FactionDef faction, float tick, float price)
    {
        staticInstance.getFactionPriceDataFrom(faction).savePrice(tick, price);
    }

    public static float loadPrice(FactionDef faction, float tick)
    {
        return staticInstance.getFactionPriceDataFrom(faction).loadPrice(tick);
    }

    public static void saveTrend(FactionDef faction, float tick, float price)
    {
        staticInstance.getFactionPriceDataFrom(faction).saveTrend(tick, price);
    }

    public static float loadTrend(FactionDef faction, float tick)
    {
        return staticInstance.getFactionPriceDataFrom(faction).loadTrend(tick);
    }

    public FactionPriceData getFactionPriceDataFrom(FactionDef f)
    {
        var Key = util.factionDefNameToKey(f.defName);
        if (factionToPriceData.TryGetValue(Key, out var from)) return from;

        var fpdn = new FactionPriceData
        {
            defname = f.defName,
            label = f.label,
            color = f.colorSpectrum is { Count: > 0 } ? f.colorSpectrum[0] : Color.white
        };

        factionToPriceData.Add(Key, fpdn);
        return factionToPriceData[Key];
    }

    public FactionPriceData func_289013(string Key)
    {
        return !factionToPriceData.ContainsKey(Key) ? null : factionToPriceData[Key];
    }

    public override void FinalizeInit()
    {
        if (!initialized)
        {
            initialized = true;
            float ticksNow = Core.AbsTickGame;
            foreach (var f in from f in DefDatabase<FactionDef>.AllDefs
                     where
                         Core.isWarbondFaction(f)
                     select f)
                if (modBase.use_rimwar)
                    savePrice(f, ticksNow, Core.getRimwarPriceByDef(f));
                else if (f != null)
                    savePrice(f, ticksNow, Core.getDefaultPrice(f));
                else
                    savePrice(null, ticksNow, Rand.Range(200f, 6000f));
        }
        else
        {
            foreach (var f in Core.ar_faction)
            {
                var key = util.factionDefNameToKey(f.defName);
                if (!staticInstance.factionToPriceData.Keys.Contains(key)) continue;

                var rs = staticInstance.func_289013(key);
                rs.defname = util.keyToFactionDefName(key);
            }
        }
    }
}