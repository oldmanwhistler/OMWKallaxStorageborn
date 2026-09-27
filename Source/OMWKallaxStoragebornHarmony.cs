using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace OMWKallaxStorageborn
{
    [StaticConstructorOnStartup]
    public static class OMWKallaxStoragebornHarmony
    {
        static OMWKallaxStoragebornHarmony()
        {
            new Harmony("oldmanwhistler.OMWKallaxStorageborn").PatchAll();
        }
    }

    [HarmonyPatch(typeof(StoragebornXenotype.StoragebornController), nameof(StoragebornXenotype.StoragebornController.RandomizeBodyGene))]
    public static class RandomizeBodyGenePatch
    {
        public static bool Prefix(Pawn pawn)
        {
            if (pawn?.genes == null || !StoragebornXenotype.StoragebornController.HasStoragebornGene(pawn))
                return false;

            List<Gene> existing = pawn.genes.GenesListForReading
                .Where(g => g.def.defName.StartsWith("OMW_StorageBody") || g.def.defName == "OMW_KallaxStoragebornBody")
                .ToList();
            foreach (Gene gene in existing)
                pawn.genes.RemoveGene(gene);

            if (OMWKallaxStoragebornMod.Instance?.Settings?.KallaxEnabled != true)
                return false;

            GeneDef kallax = DefDatabase<GeneDef>.GetNamedSilentFail("OMW_KallaxStoragebornBody");
            if (kallax != null)
                pawn.genes.AddGene(kallax, xenogene: false);
            return false;
        }
    }
}
