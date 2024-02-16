using System.Linq;
using HugsLib;
using HugsLib.Settings;
using RimWorld;
using UnityEngine;
using Verse;

namespace WarbondsLite;

[EarlyInit]
public class ModBase : HugsLib.ModBase
{
    public static bool UseEnemyFaction;
    public static bool UseVanillaEnemyFaction;
    public static bool RimwarLink;
    public static float RimwarPriceFactor;
    public static float SellPrice;
    public static float DividendPer;
    public static float MaxReward;
    public static float DelistingPrice;
    public static int LimitDate;
    public static int MilitaryAidCost;
    public static float MilitaryAidMultiply;
    public static float PriceEventMultiply;

    public static readonly bool ExistRimWar;

    private SettingHandle<float> _delistingPriceSetting;
    private SettingHandle<float> _dividendPerSetting;
    private SettingHandle<int> _limitDateSetting;
    private SettingHandle<float> _maxRewardSetting;
    private SettingHandle<int> _militaryAidCostSetting;
    private SettingHandle<float> _militaryAidMultiplySetting;
    private SettingHandle<float> _priceEventMultiplySetting;
    private SettingHandle<bool> _rimwarLinkSetting;
    private SettingHandle<float> _rimwarPriceFactorSetting;
    private SettingHandle<float> _sellPriceSetting;
    private SettingHandle<bool> _useEnemyFactionSetting;
    private SettingHandle<bool> _useVanillaEnemyFactionSetting;

    static ModBase()
    {
        if (ModsConfig.ActiveModsInLoadOrder.Any(mod => mod.PackageId.ToLower().Contains("Torann.RimWar".ToLower())))
            ExistRimWar = true;
    }

    public override string ModIdentifier => "husko.WarbondsLite";
    protected override bool HarmonyAutoPatch => false;
    public static bool use_rimwar => ExistRimWar && RimwarLink;

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
        _useEnemyFactionSetting = Settings.GetHandle<bool>("UseEnemyFaction", "Mods Enemy Faction (Restart)",
            "(Need Restart Game)\nMods Enemy faction use warbond");
        UseEnemyFaction = _useEnemyFactionSetting.Value;

        _useVanillaEnemyFactionSetting = Settings.GetHandle<bool>("UseVanillaEnemyFaction", "Vanilla Enemy Faction (Restart)",
            "(Need Restart Game)\nVanillia Enemy faction use warbond");
        UseVanillaEnemyFaction = _useVanillaEnemyFactionSetting.Value;

        _limitDateSetting = Settings.GetHandle<int>("LimitDate", "Bond limit date (Restart)",
            "(Need Restart Game)\nBond limit date");
        LimitDate = _limitDateSetting.Value;
    }

    public void setupOption2()
    {
        _rimwarLinkSetting = Settings.GetHandle("RimwarLink", "RimwarLink.t".Translate(), "RimwarLink.d".Translate(), true);
        _rimwarPriceFactorSetting = Settings.GetHandle("RimwarPriceFactor", "RimwarPriceFactor.t".Translate(),
            "RimwarPriceFactor.d".Translate(), 0.33f);

        _sellPriceSetting = Settings.GetHandle("SellPrice", "SellPrice.t".Translate(), "SellPrice.d".Translate(), 0.92f);
        _dividendPerSetting =
            Settings.GetHandle("DividendPer", "DividendPer.t".Translate(), "DividendPer.d".Translate(), 0.08f);
        _maxRewardSetting =
            Settings.GetHandle("MaxReward", "MaxReward.t".Translate(), "MaxReward.d".Translate(), 20000f);
        _delistingPriceSetting = Settings.GetHandle("DelistingPrice", "DelistingPrice.t".Translate(),
            "DelistingPrice.d".Translate(), 100f);

        _militaryAidCostSetting = Settings.GetHandle("MilitaryAidCost", "MilitaryAidCost.t".Translate(),
            "MilitaryAidCost.d".Translate(), 5);
        _militaryAidMultiplySetting = Settings.GetHandle("MilitaryAidMultiply", "MilitaryAidMultiply.t".Translate(),
            "MilitaryAidMultiply.d".Translate(), 1f);
        _priceEventMultiplySetting = Settings.GetHandle("PriceEventMultiply", "PriceEventMultiply.t".Translate(),
            "PriceEventMultiply.d".Translate(), 1f);

        SettingsChanged();

        Core.patchDef2();
    }

    public override void SettingsChanged()
    {
        UseEnemyFaction = _useEnemyFactionSetting.Value;
        UseVanillaEnemyFaction = _useVanillaEnemyFactionSetting.Value;

        RimwarLink = _rimwarLinkSetting.Value;
        RimwarPriceFactor = _rimwarPriceFactorSetting.Value;

        SellPrice = Mathf.Clamp(_sellPriceSetting.Value, 0.01f, 1f);
        DividendPer = Mathf.Clamp(_dividendPerSetting.Value, 0f, 5f);
        MaxReward = Mathf.Clamp(_maxRewardSetting.Value, 0f, 500000f);
        DelistingPrice = Mathf.Clamp(_delistingPriceSetting.Value, 1f, 1000f);
        LimitDate = _limitDateSetting.Value;
        MilitaryAidCost = _militaryAidCostSetting.Value;
        MilitaryAidMultiply = _militaryAidMultiplySetting.Value;
        PriceEventMultiply = _priceEventMultiplySetting.Value;

        Core.patchIncident();
    }


    public override void MapLoaded(Map map)
    {
        base.MapLoaded(map);
        // 마지막 채권가격 불러오기, 아이템 가격에 적용
        for (var i = 0; i < Core.ar_warbondDef.Count; i++)
        {
            var f = Core.ar_faction[i];
            var lastPrice = WorldComponentPriceSaveLoad.LoadPrice(f, Core.AbsTickGame);
            Core.ar_warbondDef[i].SetStatBaseValue(StatDefOf.MarketValue, lastPrice);
        }
    }
}