using HarmonyLib;
using RimWorld;
using Verse;

namespace rimstocks;

[HarmonyPatch(typeof(FactionDialogMaker), "FactionDialogFor")]
internal class Patch_FactionDialogMaker_FactionDialogFor
{
    private static void Postfix(ref DiaNode __result, Pawn negotiator, Faction faction)
    {
        DiaOption opt;

        // 채권 군사요청
        if (!Core.isWarbondFaction(faction.def)) return;

        opt = util.RequestMilitaryAidOptionWarbond(negotiator.Map, faction, negotiator);
        if (negotiator.skills.GetSkill(SkillDefOf.Social).TotallyDisabled)
            opt.Disable("WorkTypeDisablesOption".Translate(SkillDefOf.Social.label));

        __result.options.Insert(__result.options.Count - 1, opt);
    }
}