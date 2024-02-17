using HarmonyLib;
using Verse;

namespace WarbondsLite
{
    /// <summary>
    /// Main class for WarbondsLite mod, responsible for applying Harmony patches.
    /// </summary>
    public class HarmonyPatchCore : Mod
    {
        /// <summary>
        /// Constructor for HarmonyPatchCore class.
        /// </summary>
        /// <param name="content">The mod's content pack.</param>
        public HarmonyPatchCore(ModContentPack content) : base(content)
        {
            // Create a new Harmony instance with a specific identifier.
            var harmony = new Harmony("husko.WarbondsLite.1");

            // Apply all Harmony patches for this mod.
            harmony.PatchAll();
        }
    }
}
