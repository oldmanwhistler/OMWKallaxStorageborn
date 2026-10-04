using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;
using Verse.AI;

namespace OMWKallaxStorageborn
{
    public class CompProperties_TargetEffectResurrectBlahaj : CompProperties_TargetEffectResurrect
    {
        public CompProperties_TargetEffectResurrectBlahaj() { compClass = typeof(CompTargetEffect_ResurrectBlahaj); }
    }

    public class CompTargetEffect_ResurrectBlahaj : CompTargetEffect_Resurrect
    {
        public override void DoEffectOn(Pawn user, Thing target)
        {
            if (!user.IsColonistPlayerControlled) return;
            Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("OMW_ResurrectBlahaj"), target, parent);
            job.count = 1;
            job.playerForced = true;
            user.jobs.TryTakeOrderedJob(job, JobTag.Misc);
        }
    }

    public class JobDriver_ResurrectBlahaj : JobDriver
    {
        private const TargetIndex CorpseInd = TargetIndex.A;
        private const TargetIndex ItemInd = TargetIndex.B;
        private Mote warmupMote;
        private Corpse Corpse => (Corpse)job.GetTarget(CorpseInd).Thing;
        private Thing Item => job.GetTarget(ItemInd).Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Corpse, job, 1, -1, null, errorOnFailed) && pawn.Reserve(Item, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            yield return Toils_Goto.GotoThing(ItemInd, PathEndMode.Touch).FailOnDespawnedOrNull(ItemInd).FailOnDespawnedOrNull(CorpseInd);
            yield return Toils_Haul.StartCarryThing(ItemInd);
            yield return Toils_Goto.GotoThing(CorpseInd, PathEndMode.Touch).FailOnDespawnedOrNull(CorpseInd);
            Toil wait = Toils_General.Wait(600);
            wait.WithProgressBarToilDelay(CorpseInd);
            wait.FailOnDespawnedOrNull(CorpseInd);
            wait.FailOnCannotTouch(CorpseInd, PathEndMode.Touch);
            wait.tickAction = () =>
            {
                CompUsable usable = Item.TryGetComp<CompUsable>();
                if (usable != null && warmupMote == null && usable.Props.warmupMote != null)
                    warmupMote = MoteMaker.MakeAttachedOverlay(Corpse, usable.Props.warmupMote, Vector3.zero);
                warmupMote?.Maintain();
            };
            yield return wait;
            yield return Toils_General.Do(Resurrect);
        }

        private void Resurrect()
        {
            Pawn resurrected = Corpse.InnerPawn;
            CompTargetEffect_Resurrect comp = Item.TryGetComp<CompTargetEffect_Resurrect>();
            if (ResurrectionUtility.TryResurrect(resurrected))
            {
                GeneDef blahaj = DefDatabase<GeneDef>.GetNamed("OMW_StoragebornBodyIkeaBlahaj");
                if (resurrected.genes != null && !resurrected.genes.HasGene(blahaj))
                {
                    // remove storageborn genes to trigger the body type to blahaj
                    foreach (Gene gene in resurrected.genes.GenesListForReading)
                    {
                        if (gene.def.defName.Contains("OMW_StorageBody"))
                            resurrected.genes?.RemoveGene(gene);
                    }
                    resurrected.genes.AddGene(blahaj, false);
                }

                SoundDefOf.MechSerumUsed.PlayOneShot(SoundInfo.InMap(resurrected));
                Messages.Message("MessagePawnResurrected".Translate(resurrected), resurrected, MessageTypeDefOf.PositiveEvent);
                if (comp.Props.moteDef != null) MoteMaker.MakeAttachedOverlay(resurrected, comp.Props.moteDef, Vector3.zero);
                if (comp.Props.addsHediff != null) resurrected.health.AddHediff(comp.Props.addsHediff);
            }
            Item.SplitOff(1).Destroy();
        }
    }
}
