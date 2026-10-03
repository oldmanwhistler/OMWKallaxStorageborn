using System.Collections.Generic;
using LudeonTK;
using RimWorld;
using Verse;
using Verse.AI;

namespace OMWKallaxStorageborn
{
    public class CompFlatpack : ThingComp
    {
        public override IEnumerable<FloatMenuOption> CompFloatMenuOptions(Pawn selPawn)
        {
            foreach (FloatMenuOption option in base.CompFloatMenuOptions(selPawn)) yield return option;
            if (selPawn == null || !parent.Spawned || selPawn.Faction != Faction.OfPlayer) yield break;
            if (!selPawn.CanReach(parent, PathEndMode.Touch, Danger.Some))
            {
                yield return new FloatMenuOption("Assemble Flatpack (unreachable)", null);
                yield break;
            }
            yield return new FloatMenuOption("Assemble Flatpack", () =>
            {
                Job job = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("OMW_AssembleFlatpack"), parent);
                selPawn.jobs.TryTakeOrderedJob(job);
            });
        }

        [DebugAction("OMW Storageborn", "Spawn 10 OMW_SB_Flatpack", allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void SpawnTenFlatpacks()
        {
            Map map = Find.CurrentMap;
            if (map == null) return;
            IntVec3 origin = Find.Selector.SelectedObjects.Find(x => x is Pawn) is Pawn pawn
                ? pawn.Position : map.Center;
            for (int i = 0; i < 10; i++)
            {
                IntVec3 cell = CellFinder.RandomClosewalkCellNear(origin, map, 5);
                Thing flatpack = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("OMW_SB_Flatpack"));
                GenPlace.TryPlaceThing(flatpack, cell, map, ThingPlaceMode.Near);
            }
        }

    }

    public class CompProperties_Flatpack : CompProperties
    {
        public CompProperties_Flatpack() { compClass = typeof(CompFlatpack); }
    }

    public class JobDriver_AssembleFlatpack : JobDriver
    {
        private const int AssemblyTicks = 3600;
        private Thing Flatpack => job.targetA.Thing;

        public override bool TryMakePreToilReservations(bool errorOnFailed)
        {
            return pawn.Reserve(Flatpack, job, 1, -1, null, errorOnFailed);
        }

        protected override IEnumerable<Toil> MakeNewToils()
        {
            this.FailOnDespawnedOrNull(TargetIndex.A);
            yield return Toils_Goto.GotoThing(TargetIndex.A, PathEndMode.Touch);
            int duration = Prefs.DevMode ? 300 : AssemblyTicks;
            Toil assemble = Toils_General.Wait(duration, TargetIndex.A);
            assemble.WithProgressBarToilDelay(TargetIndex.A);
            assemble.defaultCompleteMode = ToilCompleteMode.Delay;
            yield return assemble;
            yield return Toils_General.Do(() =>
            {
                if (Flatpack == null || Flatpack.Destroyed || !Flatpack.Spawned) return;
                Map map = Flatpack.Map;
                IntVec3 spawnCell = Flatpack.Position;
                Flatpack.Destroy(DestroyMode.Vanish);
                var genes = new List<GeneDef>
                {
                    DefDatabase<GeneDef>.GetNamed("OMW_StoragebornBodyIkeaKallax"),
                    DefDatabase<GeneDef>.GetNamed("OMW_StoragebornBodyIkeaBilly"),
                    DefDatabase<GeneDef>.GetNamed("OMW_StoragebornBodyIkeaLack")
                };
                var request = new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer,
                    PawnGenerationContext.NonPlayer, forceGenerateNewPawn: true,
                    forcedXenotype: XenotypeDefOf.Baseliner, developmentalStages: DevelopmentalStage.Adult,
                    biologicalAgeRange: new FloatRange(18f, 40f), forceRecruitable: true,
                    forceNoGear: true);
                Pawn newPawn = PawnGenerator.GeneratePawn(request);
                newPawn.genes.AddGene(genes.RandomElement(), true);
                GenSpawn.Spawn(newPawn, CellFinder.RandomClosewalkCellNear(spawnCell, map, 2), map);
            });
        }
    }
}
