using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;

namespace WarbondsLite
{
    /// <summary>
    /// Represents data related to faction prices and trends over time.
    /// </summary>
    public class FactionPriceData : IExposable
    {
        /// <summary>
        /// The time unit used for modular calculations (in ticks).
        /// </summary>
        private readonly int _modularTicksUnit = 60000;

        /// <summary>
        /// Color associated with the faction.
        /// </summary>
        public Color Color;

        /// <summary>
        /// The definition name of the faction.
        /// </summary>
        public string DefName;

        /// <summary>
        /// Indicates whether the faction graph is enabled.
        /// </summary>
        private bool _graphEnabled = true;

        /// <summary>
        /// Label or name associated with the faction.
        /// </summary>
        public string Label;

        /// <summary>
        /// Dictionary mapping time units to faction prices over time.
        /// </summary>
        private Dictionary<int, float> _timeToPriceData = new();

        /// <summary>
        /// Dictionary mapping time units to faction trends over time.
        /// </summary>
        private Dictionary<int, float> _timeToTrendData = new();

        /// <summary>
        /// Exposes data for saving and loading purposes.
        /// </summary>
        public void ExposeData()
        {
            // Saving and loading essential data using RimWorld's Scribe system.
            Scribe_Values.Look(ref _graphEnabled, "_graphEnabled", true);
            Scribe_Values.Look(ref DefName, "defname", "defname");
            Scribe_Values.Look(ref Label, "label", "FACTIONNAME");
            Scribe_Values.Look(ref Color, "color");
            Scribe_Collections.Look(ref _timeToPriceData, "_timeToPriceData", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref _timeToTrendData, "_timeToTrendData", LookMode.Value, LookMode.Value);
        }

        /// <summary>
        /// Saves the specified faction price at the given time.
        /// </summary>
        /// <param name="tick">The current game tick.</param>
        /// <param name="price">The faction price to be saved.</param>
        public void SavePrice(float tick, float price)
        {
            // Calculate the unit time and save the faction price.
            var unitTime = Mathf.FloorToInt(tick / _modularTicksUnit);
            _timeToPriceData[unitTime] = price;
        }

        /// <summary>
        /// Saves the specified faction trend at the given time.
        /// </summary>
        /// <param name="tick">The current game tick.</param>
        /// <param name="trend">The faction trend to be saved.</param>
        public void SaveTrend(float tick, float trend)
        {
            // Calculate the unit time and save the faction trend.
            var unitTime = Mathf.FloorToInt(tick / _modularTicksUnit);
            _timeToTrendData[unitTime] = trend;
        }

        /// <summary>
        /// Loads faction data at the specified time from the provided data dictionary.
        /// </summary>
        /// <param name="tick">The current game tick.</param>
        /// <param name="data">The dictionary containing faction data over time.</param>
        /// <returns>The faction data at the specified time.</returns>
        public float LoadData(float tick, Dictionary<int, float> data)
        {
            // Calculate the unit time and try to get the faction data at that time.
            var unitTime = Mathf.FloorToInt(tick / _modularTicksUnit);
            if (data.TryGetValue(unitTime, out var value)) return value;

            // If the data is not available, check RimWar integration and default to a random value.
            var factionDef = FactionDef.Named(DefName);
            if (factionDef != null)
            {
                if (ModBase.UseRimWar)
                    return Core.getRimwarPriceByDef(factionDef);

                return Core.getDefaultPrice(factionDef);
            }

            return Rand.Range(200f, 6000f);
        }

        /// <summary>
        /// Loads the faction price at the specified time.
        /// </summary>
        /// <param name="tick">The current game tick.</param>
        /// <returns>The faction price at the specified time.</returns>
        public float LoadPrice(float tick)
        {
            // Delegate loading to the common LoadData method for faction prices.
            return LoadData(tick, _timeToPriceData);
        }

        /// <summary>
        /// Loads the faction trend at the specified time.
        /// </summary>
        /// <param name="tick">The current game tick.</param>
        /// <returns>The faction trend at the specified time.</returns>
        public float LoadTrend(float tick)
        {
            // Delegate loading to the common LoadData method for faction trends.
            return LoadData(tick, _timeToTrendData);
        }
    }
}
