using HarmonyLib;
using RimWorld;
using Verse;

namespace WarbondsLite
{
    /// <summary>
    /// Harmony patch for modifying faction dialog options.
    /// </summary>
    [HarmonyPatch(typeof(FactionDialogMaker), "FactionDialogFor")]
    internal class PatchFactionDialogMakerFactionDialogFor
    {
        /// <summary>
        /// Postfix method to modify faction dialog options.
        /// </summary>
        /// <param name="__result">The original dialog node result.</param>
        /// <param name="negotiator">The pawn initiating the negotiation.</param>
        /// <param name="faction">The faction involved in the negotiation.</param>
        private static void Postfix(ref DiaNode __result, Pawn negotiator, Faction faction)
        {
            // Check if the faction is a warbond faction
            if (!Core.isWarbondFaction(faction.def)) return;

            // Create a military aid option for warbond factions
            var opt = Util.RequestMilitaryAidOptionWarbond(negotiator.Map, faction, negotiator);

            // Check if the negotiator's social skill is not totally disabled
            SkillRecord socialSkill = negotiator.skills.GetSkill(SkillDefOf.Social);
            if (!socialSkill.TotallyDisabled)
            {
                // Disable the option if the social skill is not totally disabled
                opt.Disable("WorkTypeDisablesOption".Translate(SkillDefOf.Social.label));
            }

            // Insert the military aid option at the end of the dialog options
            __result.options.Insert(__result.options.Count, opt);
        }
    }
}