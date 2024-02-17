using System.Linq;
using HarmonyLib;
using RimWorld;

namespace WarbondsLite
{
    /// <summary>
    /// Harmony patch for handling quest completion and updating faction relationships.
    /// </summary>
    [HarmonyPatch(typeof(Quest), "End")]
    internal class PatchQuestEnd
    {
        /// <summary>
        /// Postfix method called after the completion of a quest.
        /// </summary>
        /// <param name="__instance">The Quest instance.</param>
        /// <param name="outcome">The outcome of the quest.</param>
        private static void Postfix(Quest __instance, QuestEndOutcome outcome)
        {
            FactionDef f = null;
            FactionDef f2 = null;

            // Check if there are at least two involved factions
            if (__instance.InvolvedFactions != null && __instance.InvolvedFactions.Count() >= 2)
            {
                // Retrieve faction definitions for the two factions involved
                var factions = __instance.InvolvedFactions.ToList();
                f = factions[0].def;
                f2 = factions[1].def;
            }
            // Check if there is only one involved faction
            else if (__instance.InvolvedFactions != null && __instance.InvolvedFactions.Count() == 1)
            {
                // Iterate through quest parts to determine involved factions
                foreach (var part in __instance.PartsListForReading)
                {
                    switch (part)
                    {
                        // Handle QuestPart_SpawnWorldObject for site world objects
                        case QuestPart_SpawnWorldObject o when o.worldObject.def == WorldObjectDefOf.Site && o.worldObject.Faction != null:
                            f2 = o.worldObject.Faction.def;
                            break;

                        // Handle QuestPart_Incident for raid incidents
                        case QuestPart_Incident p2 when p2.incident == IncidentDefOf.RaidEnemy:
                            f2 = __instance.InvolvedFactions.ToList()[0].def;
                            break;

                        // Handle other quest parts
                        default:
                            // Check for involved factions in non-specific quest parts
                            if (part is not QuestPart_InvolvedFactions && part.InvolvedFactions.Any())
                                f = part.InvolvedFactions.ToList()[0].def;
                            break;
                    }
                }
            }

            // Update faction relationships based on quest outcome
            Core.OnQuestResult(f, f2, outcome != QuestEndOutcome.Fail, __instance.points);
        }
    }
}
