using System.Collections.Generic;
using RimWorld;
using Verse;
using UnityEngine;
using RimWorld.Planet;

namespace WarbondsLite
{
    /// <summary>
    /// WorldComponent for saving and loading faction price data in the world.
    /// </summary>
    public class WorldComponentPriceSaveLoad : WorldComponent
    {
        // Singleton instance for easy access
        private static WorldComponentPriceSaveLoad _staticInstance;

        // Dictionary to store faction price data
        private readonly Dictionary<string, FactionPriceData> _factionToPriceData = new Dictionary<string, FactionPriceData>();

        // Flag to track initialization status
        private bool _initialized;

        public WorldComponentPriceSaveLoad(World world) : base(world)
        {
            _staticInstance = this;
        }

        /// <summary>
        /// Save price for a faction at a specific tick.
        /// </summary>
        public static void SavePrice(FactionDef faction, float tick, float price)
        {
            _staticInstance.GetFactionPriceDataFrom(faction).SavePrice(tick, price);
        }

        /// <summary>
        /// Load price for a faction at a specific tick.
        /// </summary>
        public static float LoadPrice(FactionDef faction, float tick)
        {
            return _staticInstance.GetFactionPriceDataFrom(faction).LoadPrice(tick);
        }

        /// <summary>
        /// Save trend for a faction at a specific tick.
        /// </summary>
        public static void SaveTrend(FactionDef faction, float tick, float price)
        {
            _staticInstance.GetFactionPriceDataFrom(faction).SaveTrend(tick, price);
        }

        /// <summary>
        /// Load trend for a faction at a specific tick.
        /// </summary>
        public static float LoadTrend(FactionDef faction, float tick)
        {
            return _staticInstance.GetFactionPriceDataFrom(faction).LoadTrend(tick);
        }

        /// <summary>
        /// Get or create faction price data for the given faction.
        /// </summary>
        private FactionPriceData GetFactionPriceDataFrom(FactionDef f)
        {
            var key = Util.FactionDefNameToKey(f.defName);

            // Try to get existing data or create a new one if not found
            if (!_factionToPriceData.TryGetValue(key, out var value))
            {
                value = new FactionPriceData
                {
                    defname = f.defName,
                    label = f.label,
                    // Get the first color from the colorSpectrum list or default to Color.white
                    color = f.colorSpectrum is { Count: > 0 } ? f.colorSpectrum[0] : Color.white
                };

                _factionToPriceData[key] = value;
            }

            return value;
        }

        /// <summary>
        /// Finalize initialization, including saving default prices for factions.
        /// </summary>
        public override void FinalizeInit()
        {
            if (!_initialized)
            {
                _initialized = true;
                float ticksNow = Core.AbsTickGame;

                // Save default prices for factions during initialization
                foreach (var f in DefDatabase<FactionDef>.AllDefs)
                {
                    if (Core.isWarbondFaction(f))
                    {
                        SavePrice(f, ticksNow, Core.getRimwarPriceByDef(f));
                    }
                }
            }
            else
            {
                // Update faction data during subsequent initialization
                foreach (var f in Core.ar_faction)
                {
                    var key = Util.FactionDefNameToKey(f.defName);
                    if (_factionToPriceData.TryGetValue(key, out var rs))
                    {
                        rs.defname = Util.KeyToFactionDefName(key);
                    }
                }
            }
        }
    }
}
