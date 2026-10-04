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
        private Effecter anomalyRitualEffecter;
        private Sustainer anomalyHumanRitualSound;
        private Sustainer anomalyRitualSound;
        private int handlingTicks;
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
                bool supportsBlahaj = Corpse.InnerPawn != null && Corpse.InnerPawn.RaceProps.Humanlike && Corpse.InnerPawn.genes != null;
                if (supportsBlahaj && anomalyRitualEffecter == null)
                {
                    EffecterDef ritualEffect = DefDatabase<EffecterDef>.GetNamedSilentFail("PsychicRitual_Sustained");
                    if (ritualEffect != null) anomalyRitualEffecter = ritualEffect.Spawn(Corpse, Corpse);
                }
                if (supportsBlahaj)
                {
                    anomalyRitualEffecter?.EffectTick(Corpse, Corpse);
                    TargetInfo soundTarget = new TargetInfo(Corpse.PositionHeld, Corpse.MapHeld);
                    SoundDef humanRitualSound = DefDatabase<SoundDef>.GetNamedSilentFail("PsychicRitual_Ongoing_Human");
                    if (anomalyHumanRitualSound == null && humanRitualSound != null && humanRitualSound.sustain)
                        anomalyHumanRitualSound = humanRitualSound.TrySpawnSustainer(SoundInfo.InMap(soundTarget, MaintenanceType.PerTick));
                    anomalyHumanRitualSound?.Maintain();
                    handlingTicks++;
                    if (handlingTicks >= 360)
                    {
                        SoundDef ritualSound = DefDatabase<SoundDef>.GetNamedSilentFail("PsychicRitual_Ongoing");
                        if (anomalyRitualSound == null && ritualSound != null && ritualSound.sustain)
                            anomalyRitualSound = ritualSound.TrySpawnSustainer(SoundInfo.InMap(soundTarget, MaintenanceType.PerTick));
                        anomalyRitualSound?.Maintain();
                    }
                }
            };
            wait.AddFinishAction(() =>
            {
                anomalyRitualEffecter?.Cleanup();
                anomalyRitualEffecter = null;
                anomalyHumanRitualSound?.End();
                anomalyHumanRitualSound = null;
                anomalyRitualSound?.End();
                anomalyRitualSound = null;
            });
            yield return wait;
            yield return Toils_General.Do(Resurrect);
        }

        private void Resurrect()
        {
            Pawn resurrected = Corpse.InnerPawn;
            IntVec3 originalPosition = Corpse.PositionHeld;
            Map originalMap = Corpse.MapHeld;
            CompTargetEffect_Resurrect comp = Item.TryGetComp<CompTargetEffect_Resurrect>();
            if (ResurrectionUtility.TryResurrect(resurrected))
            {
                if (resurrected.RaceProps.Humanlike && resurrected.genes != null)
                {
                    SpawnDessicatedRemains(resurrected, originalPosition, originalMap);
                    GeneDef blahaj = DefDatabase<GeneDef>.GetNamed("OMW_StoragebornBodyIkeaBlahaj");
                    if (!resurrected.genes.HasGene(blahaj))
                    {
                        // Remove any prior Storageborn body gene before assigning the Blåhaj body.
                        List<Gene> genesToRemove = new List<Gene>();
                        foreach (Gene gene in resurrected.genes.GenesListForReading)
                            if (gene.def.defName.Contains("OMW_StorageBody")) genesToRemove.Add(gene);
                        foreach (Gene gene in genesToRemove) resurrected.genes.RemoveGene(gene);
                        resurrected.genes.AddGene(blahaj, false);
                    }
                }

                SoundDefOf.MechSerumUsed.PlayOneShot(SoundInfo.InMap(resurrected));
                Messages.Message("MessagePawnResurrected".Translate(resurrected), resurrected, MessageTypeDefOf.PositiveEvent);
                if (comp.Props.moteDef != null) MoteMaker.MakeAttachedOverlay(resurrected, comp.Props.moteDef, Vector3.zero);
                if (comp.Props.addsHediff != null) resurrected.health.AddHediff(comp.Props.addsHediff);
            }
            Item.SplitOff(1).Destroy();
        }

        private static void SpawnDessicatedRemains(Pawn originalPawn, IntVec3 position, Map map)
        {
            if (map == null || !position.IsValid) return;
            // Match the Corrupted Obelisk procedure so the remains retain the original pawn's full identity and genes.
            Pawn remains = Find.PawnDuplicator.Duplicate(originalPawn);
            remains.forceNoDeathNotification = true;
            GenSpawn.Spawn(remains, position, map);
            remains.Kill(null);
            Corpse remainsCorpse = remains.Corpse;
            CompRottable rottable = remainsCorpse?.GetComp<CompRottable>();
            if (rottable != null) rottable.RotProgress = rottable.PropsRot.TicksToDessicated + 1f;
        }
    }
}
