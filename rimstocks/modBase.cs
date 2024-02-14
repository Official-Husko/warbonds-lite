using System.Linq;
using HugsLib;
using HugsLib.Settings;
using RimWorld;
using UnityEngine;
using Verse;

namespace rimstocks;

[EarlyInit]
public class modBase : ModBase
{
    public static bool useEnemyFaction;
    public static bool useVanillaEnemyFaction;
    public static bool rimwarLink;
    public static float rimwarPriceFactor;
    public static float sellPrice;
    public static float dividendPer;
    public static float maxReward;
    public static float DelistingPrice;
    public static int limitDate;
    public static int militaryAid_cost;
    public static float militaryAid_multiply;
    public static float priceEvent_multiply;

    public static readonly bool exist_rimWar;

    private SettingHandle<float> DelistingPrice_s;

    private SettingHandle<float> dividendPer_s;

    private SettingHandle<int> limitDate_s;

    private SettingHandle<float> maxReward_s;

    private SettingHandle<int> militaryAid_cost_s;

    private SettingHandle<float> militaryAid_multiply_s;

    private SettingHandle<float> priceEvent_multiply_s;

    private SettingHandle<bool> rimwarLink_s;

    private SettingHandle<float> rimwarPriceFactor_s;

    private SettingHandle<float> sellPrice_s;

    private SettingHandle<bool> useEnemyFaction_s;

    private SettingHandle<bool> useVanillaEnemyFaction_s;

    static modBase()
    {
        if (ModsConfig.ActiveModsInLoadOrder.Any(mod => mod.PackageId.ToLower().Contains("Torann.RimWar".ToLower())))
            exist_rimWar = true;
    }

    public override string ModIdentifier => "yayo.rimstocks";
    protected override bool HarmonyAutoPatch => false;
    public static bool use_rimwar => exist_rimWar && rimwarLink;

    public override void EarlyInitialize()
    {
        setupOption();
    }

    public override void DefsLoaded()
    {
        setupOption2();
    }

    public void setupOption()
    {
        useEnemyFaction_s = Settings.GetHandle<bool>("useEnemyFaction", "Mods Enemy Faction (Restart)",
            "(Need Restart Game)\nMods Enemy faction use warbond");
        useEnemyFaction = useEnemyFaction_s.Value;

        useVanillaEnemyFaction_s = Settings.GetHandle<bool>("useVanillaEnemyFaction", "Vanilla Enemy Faction (Restart)",
            "(Need Restart Game)\nVanillia Enemy faction use warbond");
        useVanillaEnemyFaction = useVanillaEnemyFaction_s.Value;

        limitDate_s = Settings.GetHandle<int>("limitDate", "Bond limit date (Restart)",
            "(Need Restart Game)\nBond limit date");
        limitDate = limitDate_s.Value;
    }

    public void setupOption2()
    {
        rimwarLink_s = Settings.GetHandle("rimwarLink", "rimwarLink.t".Translate(), "rimwarLink.d".Translate(), true);
        rimwarPriceFactor_s = Settings.GetHandle("rimwarPriceFactor", "rimwarPriceFactor.t".Translate(),
            "rimwarPriceFactor.d".Translate(), 0.33f);

        sellPrice_s = Settings.GetHandle("sellPrice", "sellPrice.t".Translate(), "sellPrice.d".Translate(), 0.92f);
        dividendPer_s =
            Settings.GetHandle("dividendPer", "dividendPer.t".Translate(), "dividendPer.d".Translate(), 0.08f);
        maxReward_s =
            Settings.GetHandle("maxReward", "maxReward.t".Translate(), "maxReward.d".Translate(), 20000f);
        DelistingPrice_s = Settings.GetHandle("DelistingPrice", "DelistingPrice.t".Translate(),
            "DelistingPrice.d".Translate(), 100f);

        militaryAid_cost_s = Settings.GetHandle("militaryAid_cost", "militaryAid_cost.t".Translate(),
            "militaryAid_cost.d".Translate(), 5);
        militaryAid_multiply_s = Settings.GetHandle("militaryAid_multiply", "militaryAid_multiply.t".Translate(),
            "militaryAid_multiply.d".Translate(), 1f);
        priceEvent_multiply_s = Settings.GetHandle("priceEvent_multiply", "priceEvent_multiply.t".Translate(),
            "priceEvent_multiply.d".Translate(), 1f);

        SettingsChanged();

        Core.patchDef2();
    }

    public override void SettingsChanged()
    {
        useEnemyFaction = useEnemyFaction_s.Value;
        useVanillaEnemyFaction = useVanillaEnemyFaction_s.Value;

        rimwarLink = rimwarLink_s.Value;
        rimwarPriceFactor = rimwarPriceFactor_s.Value;

        sellPrice = Mathf.Clamp(sellPrice_s.Value, 0.01f, 1f);
        dividendPer = Mathf.Clamp(dividendPer_s.Value, 0f, 5f);
        maxReward = Mathf.Clamp(maxReward_s.Value, 0f, 500000f);
        DelistingPrice = Mathf.Clamp(DelistingPrice_s.Value, 1f, 1000f);
        limitDate = limitDate_s.Value;
        militaryAid_cost = militaryAid_cost_s.Value;
        militaryAid_multiply = militaryAid_multiply_s.Value;
        priceEvent_multiply = priceEvent_multiply_s.Value;

        Core.patchIncident();
    }


    public override void MapLoaded(Map map)
    {
        base.MapLoaded(map);
        // 마지막 채권가격 불러오기, 아이템 가격에 적용
        for (var i = 0; i < Core.ar_warbondDef.Count; i++)
        {
            var f = Core.ar_faction[i];
            var lastPrice = WorldComponent_PriceSaveLoad.loadPrice(f, Core.AbsTickGame);
            Core.ar_warbondDef[i].SetStatBaseValue(StatDefOf.MarketValue, lastPrice);
        }
    }
}