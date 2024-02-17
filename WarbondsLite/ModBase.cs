using System.Linq;
using HugsLib;
using HugsLib.Settings;
using RimWorld;
using UnityEngine;
using Verse;

namespace WarbondsLite
{
    /// <summary>
    /// Main class for the Warbonds Lite mod.
    /// </summary>
    [EarlyInit]
    public class ModBase : HugsLib.ModBase
    {
        // Static variables for mod settings
        public static bool UseEnemyFaction { get; private set; }
        public static bool UseVanillaEnemyFaction { get; private set; }
        public static bool RimWarLink { get; private set; }
        public static float RimwarPriceFactor { get; private set; }
        public static float SellPrice { get; private set; }
        public static float DividendPer { get; private set; }
        public static float MaxReward { get; private set; }
        public static float DelistingPrice { get; private set; }
        public static int LimitDate { get; private set; }
        public static int MilitaryAidCost { get; private set; }
        public static float MilitaryAidMultiply { get; private set; }
        public static float PriceEventMultiply { get; private set; }

        // Check if the RimWar mod is active
        private static readonly bool ExistRimWar = ModsConfig.ActiveModsInLoadOrder
            .Any(mod => mod.PackageId.ToLower().Contains("Torann.RimWar".ToLower()));

        // Setting handles for mod settings
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

        // Static constructor
        static ModBase()
        {
            // Check if RimWar mod is active and set ExistRimWar accordingly
            ExistRimWar = ModsConfig.ActiveModsInLoadOrder
                .Any(mod => mod.PackageId.ToLower().Contains("Torann.RimWar".ToLower()));
        }

        /// <summary>
        /// Mod identifier used by HugsLib.
        /// </summary>
        public override string ModIdentifier => "husko.WarbondsLite";

        /// <summary>
        /// Indicates whether Harmony should auto-patch methods.
        /// </summary>
        protected override bool HarmonyAutoPatch => false;

        /// <summary>
        /// Determines if RimWar is active and linked.
        /// </summary>
        public static bool UseRimWar => ExistRimWar && RimWarLink;

        /// <summary>
        /// Initializes mod settings during early game loading.
        /// </summary>
        public override void EarlyInitialize()
        {
            SetupOption();
        }

        /// <summary>
        /// Initializes additional mod settings after RimWorld's defs are loaded.
        /// </summary>
        public override void DefsLoaded()
        {
            SetupOption2();
        }

        /// <summary>
        /// Sets up the initial mod settings.
        /// </summary>
        private void SetupOption()
        {
            _useEnemyFactionSetting = Settings.GetHandle<bool>("UseEnemyFaction", "Mods Enemy Faction (Restart)",
                "(Need Restart Game)\nMods Enemy faction use warbond");
            UseEnemyFaction = _useEnemyFactionSetting.Value;

            _useVanillaEnemyFactionSetting = Settings.GetHandle<bool>("UseVanillaEnemyFaction", "Vanilla Enemy Faction (Restart)",
                "(Need Restart Game)\nVanilla Enemy faction use warbond");
            UseVanillaEnemyFaction = _useVanillaEnemyFactionSetting.Value;

            _limitDateSetting = Settings.GetHandle<int>("LimitDate", "Bond limit date (Restart)",
                "(Need Restart Game)\nBond limit date");
            LimitDate = _limitDateSetting.Value;
        }

        /// <summary>
        /// Sets up additional mod settings.
        /// </summary>
        private void SetupOption2()
        {
            _rimwarLinkSetting = Settings.GetHandle("RimWarLink", "RimWarLink.t".Translate(), "RimWarLink.d".Translate(), true);
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

        /// <summary>
        /// Handles mod settings changes.
        /// </summary>
        public override void SettingsChanged()
        {
            UseEnemyFaction = _useEnemyFactionSetting.Value;
            UseVanillaEnemyFaction = _useVanillaEnemyFactionSetting.Value;

            RimWarLink = _rimwarLinkSetting.Value;
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

        /// <summary>
        /// Called when a map is loaded, retrieves the last bond price and applies it to the item price.
        /// </summary>
        public override void MapLoaded(Map map)
        {
            base.MapLoaded(map);

            // Retrieve the last bond price and apply it to the item price
            for (var i = 0; i < Core.ar_warbondDef.Count; i++)
            {
                var f = Core.ar_faction[i];
                var lastPrice = WorldComponentPriceSaveLoad.LoadPrice(f, Core.AbsTickGame);
                Core.ar_warbondDef[i].SetStatBaseValue(StatDefOf.MarketValue, lastPrice);
            }
        }
    }
}
