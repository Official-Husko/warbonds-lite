using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace WarbondsLite
{
    /// <summary>
    /// Utility class containing various static methods for managing warbonds and related functionality.
    /// </summary>
    public static class Util
    {
        // List to store reward items
        private static List<Thing> rewards = new List<Thing>();

        /// <summary>
        /// Removes all instances of a specific warbond thing definition from player-owned maps and caravans.
        /// </summary>
        /// <param name="def">The definition of the warbond thing to be removed.</param>
        /// <returns>True if any instances were removed; otherwise, false.</returns>
        public static bool RemoveAllThingByDef(ThingDef def)
        {
            var removed = false;
            
            // Get all player-owned maps
            var playerMaps = Find.Maps.Where(m => m.ParentFaction == Find.FactionManager.OfPlayer).ToList();

            // Iterate through player-owned maps
            foreach (var map in playerMaps)
            {
                // Get all warbond things in the map with the specified definition
                var ar = GetAllWarbondInMap(map, def);
                
                // Remove each warbond thing from the map
                ar.ForEach(t => { t.Kill(); removed = true; });
            }

            // Get all caravans in the world
            var caravans = Find.WorldObjects.Caravans.ToList();
            
            // Iterate through caravans
            foreach (var cv in caravans)
            {
                // Get all warbond things in the caravan with the specified definition
                var ar = GetAllWarbondInCaravan(cv, def);
                
                // Remove each warbond thing from the caravan
                ar.ForEach(t => { t.Kill(); removed = true; });
            }

            return removed;
        }

        /// <summary>
        /// Distributes dividends for a specific warbond definition and faction to all player-owned maps.
        /// </summary>
        /// <param name="f">The faction definition receiving dividends.</param>
        /// <param name="warbondDef">The definition of the warbond thing.</param>
        public static void GiveDividend(FactionDef f, ThingDef warbondDef)
        {
            var rewards = new List<Thing>();

            foreach (var map in Find.Maps.Where(m => m.ParentFaction == Find.FactionManager.OfPlayer))
            {
                var bondCount = AmountWarbondForDividend(map, warbondDef);
                var marketValue = bondCount * warbondDef.BaseMarketValue * ModBase.DividendPer;
                var intVec = DropCellFinder.TradeDropSpot(map);

                var arThingDef = GetRewardThingDefs(f);

                if (arThingDef.Count == 0) continue;

                marketValue = Math.Min(marketValue, ModBase.MaxReward);
                rewards = MakeThings(marketValue, arThingDef, f);

                if (rewards.Count == 0) continue;

                DropPodUtility.DropThingsNear(intVec, map, rewards, 110, false, false, false, false);
                rewards.Clear();

                var translatedMessage = "bond.dividendArrived".Translate(warbondDef.label, bondCount, marketValue);
                Messages.Message(new Message(translatedMessage, MessageTypeDefOf.NeutralEvent));
            }
        }

        private static List<ThingDef> GetRewardThingDefs(FactionDef f)
        {
            var arThingDef = new List<ThingDef>();

            if (!(f.modContentPack?.IsOfficialMod == true))
            {
                arThingDef.AddRange(DefDatabase<ThingDef>.AllDefs
                    .Where(t => BasicThingCheck(t) && t.modContentPack?.PackageId == f.modContentPack.PackageId));
            }

            if (arThingDef.Count == 0)
            {
                arThingDef.AddRange(GetThingDefsByTechLevel(f.techLevel));
            }

            if (arThingDef.Count == 0)
            {
                arThingDef.AddRange(DefDatabase<ThingDef>.AllDefs
                    .Where(t => BasicThingCheck(t) && t.modContentPack?.IsOfficialMod == true));
            }

            return arThingDef;
        }

        private static IEnumerable<ThingDef> GetThingDefsByTechLevel(TechLevel techLevel)
        {
            return DefDatabase<ThingDef>.AllDefs
                .Where(t => BasicThingCheck(t) && t.techLevel == techLevel && t.modContentPack?.IsOfficialMod == true);
        }

        /// <summary>
        /// Performs a basic check on a <see cref="ThingDef"/> to determine if it can be used in trading.
        /// </summary>
        /// <param name="t">The ThingDef to check.</param>
        /// <returns>True if the ThingDef is tradable; otherwise, false.</returns>
        private static bool BasicThingCheck(ThingDef t)
        {
            return t.tradeability != Tradeability.None && t.race == null && !t.IsBuildingArtificial;
        }

        /// <summary>
        /// Creates a list of things based on specified market value, thing definitions, and faction definition.
        /// </summary>
        /// <param name="marketValue">The desired total market value of the created things.</param>
        /// <param name="arDef">The list of ThingDefs to use for creating things.</param>
        /// <param name="f">The faction definition.</param>
        /// <returns>A list containing things with a combined market value close to the specified value.</returns>
        private static List<Thing> MakeThings(float marketValue, List<ThingDef> arDef, FactionDef f)
        {
            var arThing = new List<Thing>();

            // Sort the ThingDefs in descending order based on base market value
            arDef.SortByDescending(e => e.BaseMarketValue);

            foreach (var def in arDef)
            {
                // Calculate the number of things needed to reach the desired market value
                var needCount = Mathf.FloorToInt(marketValue / def.BaseMarketValue);
                // Determine the maximum stack count based on the ThingDef's stack limit
                var count = Mathf.Min(def.stackLimit, needCount);

                // Check if the ThingDef is made from stuff
                if (def.MadeFromStuff)
                {
                    while (needCount > 0)
                    {
                        // Try to find a random stuff for the ThingDef based on faction tech level
                        if (GenStuff.TryRandomStuffFor(def, out var stuff, f.techLevel))
                        {
                            var thing = ThingMaker.MakeThing(def, stuff);
                            thing.stackCount = count;
                            arThing.Add(thing);
                            // Adjust the remaining market value and count
                            marketValue -= def.BaseMarketValue * thing.stackCount;
                            needCount -= count;
                        }
                    }
                }
                else
                {
                    while (needCount > 0)
                    {
                        // Create the Thing without stuff
                        var thing = ThingMaker.MakeThing(def);
                        thing.stackCount = count;
                        arThing.Add(thing);
                        // Adjust the remaining market value and count
                        marketValue -= def.BaseMarketValue * thing.stackCount;
                        needCount -= count;
                    }
                }
            }

            // If there is still remaining market value, add silver to the list
            if (marketValue >= 1f)
            {
                var silver = ThingMaker.MakeThing(ThingDefOf.Silver);
                silver.stackCount = Mathf.FloorToInt(marketValue);
                arThing.Add(silver);
            }

            // Remove null or zero-count things from the list
            arThing.RemoveAll(t => t == null || t.stackCount == 0);

            return arThing;
        }

        /// <summary>
        /// Retrieves all instances of a specific <see cref="Thing"/> within a given <see cref="Caravan"/>.
        /// </summary>
        /// <param name="cv">The caravan to search for the things.</param>
        /// <param name="td">The definition of the desired thing.</param>
        /// <returns>A list containing all instances of the specified thing in the caravan.</returns>
        private static List<Thing> GetAllWarbondInCaravan(Caravan cv, ThingDef td)
        {
            var arThing = new List<Thing>();

            // Collect things in the caravan with the specified definition
            arThing.AddRange(cv.AllThings.Where(t => t.def == td));

            // Check if there are pawns in the caravan
            if (cv.pawns?.Any<Pawn>() == true)
            {
                // Collect things in the inventories of pawns with the specified definition
                arThing.AddRange(cv.pawns
                    .SelectMany((Pawn p) => p.inventory?.innerContainer ?? Enumerable.Empty<Thing>(), (p, t) => t)
                    .Where((Thing t2) => t2.def == td)
                    .ToList());
            }

            return arThing;
        }

        /// <summary>
        /// Retrieves all instances of a specific <see cref="Thing"/> within a given <see cref="Map"/>.
        /// </summary>
        /// <param name="map">The map to search for the things.</param>
        /// <param name="td">The definition of the desired thing.</param>
        /// <returns>A list containing all instances of the specified thing in the map.</returns>
        private static List<Thing> GetAllWarbondInMap(Map map, ThingDef td)
        {
            var arThing = new List<Thing>();

            // Collect things on the ground with the specified definition
            arThing.AddRange(map.listerThings.AllThings.Where(t => t.def == td));

            // Collect things in transporters with the specified definition
            foreach (var t in map.listerThings.AllThings.Where(t => t.TryGetComp<CompTransporter>() != null))
            {
                var cp = t.TryGetComp<CompTransporter>();
                arThing.AddRange(cp.innerContainer.Where(t2 => t2.def == td));
            }

            // Collect things in the inventories of free colonists and prisoners with the specified definition
            arThing.AddRange(map.mapPawns.FreeColonistsAndPrisoners
                                .Where(p => p.inventory?.innerContainer != null)
                                .SelectMany(p => p.inventory.innerContainer.Where(t2 => t2.def == td)));

            return arThing;
        }

        /// <summary>
        /// Calculates the total amount of a specific warbond thing available for dividends in the colony.
        /// </summary>
        /// <param name="map">The map representing the colony.</param>
        /// <param name="td">The definition of the warbond thing.</param>
        /// <returns>The total amount of the specified warbond thing available for dividends.</returns>
        private static int AmountWarbondForDividend(Map map, ThingDef td)
        {
            return CaravanFormingUtility.AllReachableColonyItems(map)
                                    .Where(t => t.def == td)
                                    .Sum(t => t.stackCount);
        }

        /// <summary>
        /// Calculates the total amount of a specific warbond thing that can be sent for trading.
        /// </summary>
        /// <param name="map">The map representing the colony.</param>
        /// <param name="td">The definition of the warbond thing.</param>
        /// <returns>The total amount of the specified warbond thing available for trading.</returns>
        private static int AmountSendableWarbond(Map map, ThingDef td)
        {
            return TradeUtility.AllLaunchableThingsForTrade(map)
                            .Where(t => t.def == td)
                            .Sum(t => t.stackCount);
        }

        /// <summary>
        /// Generates a DiaOption for requesting military aid through warbond in the communicator menu.
        /// </summary>
        /// <param name="map">The map where the request is made.</param>
        /// <param name="faction">The faction to which the aid request is directed.</param>
        /// <param name="negotiator">The negotiator pawn initiating the request.</param>
        /// <returns>A DiaOption representing the military aid request option.</returns>
        public static DiaOption RequestMilitaryAidOptionWarbond(Map map, Faction faction, Pawn negotiator)
        {
            // Retrieve the warbond ThingDef specific to the faction
            var warbondDef = ThingDef.Named($"oh_warbond_{faction.def.defName}");

            // Construct the option text with the translation and warbond label
            string text = "warbond.requestMilitaryAid".Translate(ModBase.MilitaryAidCost, warbondDef.label);

            // Check if there is not enough warbond available for the request
            if (AmountSendableWarbond(map, warbondDef) < ModBase.MilitaryAidCost)
            {
                var diaOption = new DiaOption(text);
                diaOption.Disable("warbond.noCost".Translate());
                return diaOption;
            }

            // Check if the map temperature is outside the allowed range for the faction
            if (!faction.def.allowedArrivalTemperatureRange.ExpandedBy(-4f).Includes(map.mapTemperature.SeasonalTemp))
            {
                var diaOption2 = new DiaOption(text);
                diaOption2.Disable("BadTemperature".Translate());
                return diaOption2;
            }

            // Check if there is a waiting time before the faction can make another military aid request
            var num = faction.lastMilitaryAidRequestTick + 60000 - Find.TickManager.TicksGame;
            if (num > 0)
            {
                var diaOption3 = new DiaOption(text);
                diaOption3.Disable("WaitTime".Translate(num.ToStringTicksToPeriod()));
                return diaOption3;
            }

            // Check if the faction is not hostile and there are blocking hostile lords present
            if (faction.PlayerRelationKind != FactionRelationKind.Hostile &&
                NeutralGroupIncidentUtility.AnyBlockingHostileLord(map, faction))
            {
                var diaOption4 = new DiaOption(text);
                diaOption4.Disable("HostileVisitorsPresent".Translate());
                return diaOption4;
            }

            // Construct the main DiaOption for military aid request
            var diaOption5 = new DiaOption(text);

            // Check if the faction's tech level is less than 4
            if ((int)faction.def.techLevel < 4)
            {
                diaOption5.link = FactionDialogMaker.CantMakeItInTime(faction, negotiator);
            }
            else
            {
                // Find hostile targets that are threats to the player and not hostile to the faction
                var source = map.attackTargetsCache.TargetsHostileToColony
                    .Where(x => GenHostility.IsActiveThreatToPlayer(x))
                    .Select(x => ((Thing)x).Faction)
                    .Where(x => x != null && !x.HostileTo(faction))
                    .Distinct();

                if (source.Any())
                {
                    // Construct a DiaNode to confirm the request due to mutual enemies
                    var diaNode = new DiaNode("MilitaryAidConfirmMutualEnemy".Translate(faction.Name,
                        source.Select(fa => fa.Name).ToCommaList(true)));
                    var diaOption6 = new DiaOption("CallConfirm".Translate())
                    {
                        // Set the action to call for military aid and link to FightersSent outcome
                        action = delegate { CallForAidWarBond(map, faction, warbondDef); },
                        link = FactionDialogMaker.FightersSent(faction, negotiator)
                    };
                    var diaOption7 = new DiaOption("CallCancel".Translate())
                    {
                        // Set the link to reset the dialog to the root
                        linkLateBind = FactionDialogMaker.ResetToRoot(faction, negotiator)
                    };
                    diaNode.options.Add(diaOption6);
                    diaNode.options.Add(diaOption7);
                    diaOption5.link = diaNode;
                }
                else
                {
                    // Set the action to call for military aid and link to FightersSent outcome
                    diaOption5.action = delegate { CallForAidWarBond(map, faction, warbondDef); };
                    diaOption5.link = FactionDialogMaker.FightersSent(faction, negotiator);
                }
            }

            return diaOption5;
        }

        /// <summary>
        /// Initiates a call for military aid using war bonds.
        /// </summary>
        /// <param name="map">The map where the aid request is made.</param>
        /// <param name="faction">The faction from which military aid is requested.</param>
        /// <param name="warbondDef">The ThingDef representing the war bonds.</param>
        private static void CallForAidWarBond(Map map, Faction faction, ThingDef warbondDef)
        {
            // Launch warbonds using TradeUtility with specified cost
            TradeUtility.LaunchThingsOfType(warbondDef, ModBase.MilitaryAidCost, map, null);

            // Set up parameters for the military aid incident
            var incidentParms = new IncidentParms
            {
                target = map,
                faction = faction,
                raidArrivalModeForQuickMilitaryAid = true,
                // Calculate raid points based on warbond value and multiplier
                points = warbondDef.BaseMarketValue * ModBase.MilitaryAidMultiply * 2f
            };

            // Adjust raid arrival mode for hostile factions
            if (faction.PlayerRelationKind == FactionRelationKind.Hostile)
                incidentParms.raidArrivalMode = PawnsArrivalModeDefOf.EdgeWalkIn;

            // Update the last military aid request tick for the faction
            faction.lastMilitaryAidRequestTick = Find.TickManager.TicksGame;

            // Execute the military aid incident
            IncidentDefOf.RaidFriendly.Worker.TryExecute(incidentParms);
        }

        /// <summary>
        /// Converts a faction definition name to a unique key for warbond prices.
        /// </summary>
        /// <param name="defname">The faction definition name to be converted.</param>
        /// <returns>A unique key for warbond prices based on the faction definition name.</returns>
        public static string FactionDefNameToKey(string defname)
        {
            // Combine a prefix with the faction definition name to create a unique key
            return $"warbondPrice_{defname}";
        }

        /// <summary>
        /// Converts a unique key for warbond prices to a faction definition name.
        /// </summary>
        /// <param name="key">The unique key for warbond prices to be converted.</param>
        /// <returns>The faction definition name extracted from the unique key.</returns>
        public static string KeyToFactionDefName(string key)
        {
            // Extract faction definition name from the key by removing the prefix
            return key.Substring(key.IndexOf('_') + 1, key.Length - key.IndexOf('_') - 1);
        }
    }
}