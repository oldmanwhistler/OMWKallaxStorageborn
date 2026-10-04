using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace OMWKallaxStorageborn
{
    public class PsychicRitualDef_BlahajAwakening : PsychicRitualDef_InvocationCircle
    {
        public SimpleCurve comaHoursFromQualityCurve;

        public override IEnumerable<string> BlockingIssues(PsychicRitualRoleAssignments assignments, Map map)
        {
            foreach (string issue in base.BlockingIssues(assignments, map)) yield return issue;
            Pawn victim = assignments.FirstAssignedPawn(TargetRole);
            Pawn invoker = assignments.FirstAssignedPawn(InvokerRole);
            if (victim == null || invoker == null) yield break;
            var plushFilter = new ThingFilter();
            plushFilter.SetAllow(ThingDef.Named("OMW_BlahajPlushApparel"), true);
            int count = 0;
            foreach (Thing thing in map.listerThings.AllThings)
                if (plushFilter.Allows(thing) && !thing.IsForbidden(invoker) && invoker.CanReserveAndReach(thing, PathEndMode.Touch, invoker.NormalMaxDanger())) count += thing.stackCount;
            if (count < 1) yield return "OMW_BlahajAwakeningRequiresPlush".Translate();
        }

        public override List<PsychicRitualToil> CreateToils(PsychicRitual ritual, PsychicRitualGraph parent)
        {
            List<PsychicRitualToil> toils = base.CreateToils(ritual, parent);
            toils.Add(new PsychicRitualToil_BlahajAwakening(this));
            return toils;
        }

        public float ComaHours(float quality) => comaHoursFromQualityCurve?.Evaluate(Mathf.Clamp01(quality)) ?? 8f;
    }

    public class PsychicRitualToil_BlahajAwakening : PsychicRitualToil
    {
        private PsychicRitualDef_BlahajAwakening def;
        public PsychicRitualToil_BlahajAwakening() { }
        public PsychicRitualToil_BlahajAwakening(PsychicRitualDef_BlahajAwakening def) { this.def = def; }
        public override void ExposeData() { base.ExposeData(); Scribe_Defs.Look(ref def, "def"); }

        public override void End(PsychicRitual ritual, PsychicRitualGraph parent, bool success)
        {
            base.End(ritual, parent, success);
            if (!success) return;
            Map map = ritual.Map;
            Pawn victim = ritual.assignments.FirstAssignedPawn(def.TargetRole);
            Pawn invoker = ritual.assignments.FirstAssignedPawn(def.InvokerRole);
            if (map == null || victim == null || invoker == null || victim.Dead) return;

            Thing plush = map.listerThings.AllThings.FirstOrDefault(t => t.def.defName == "OMW_BlahajPlushApparel" && !t.IsForbidden(invoker) && invoker.CanReserveAndReach(t, PathEndMode.Touch, invoker.NormalMaxDanger()));
            if (plush == null) return;
            plush.SplitOff(1).Destroy(DestroyMode.Vanish);

            IntVec3 corpseCell = victim.PositionHeld;
            victim.Kill(null, null);
            Corpse corpse = victim.Corpse;
            if (corpse != null)
            {
                CompRottable rottable = corpse.GetComp<CompRottable>();
                if (rottable != null) rottable.RotProgress = rottable.PropsRot.TicksToDessicated + 1f;
                corpse.Destroy(DestroyMode.Vanish);
            }
            Thing serum = ThingMaker.MakeThing(ThingDef.Named("OMW_ResurrectorBlahajSerum"));
            GenPlace.TryPlaceThing(serum, ritual.assignments.Target.Cell, map, ThingPlaceMode.Near);

            float hours = def.ComaHours(ritual.power);
            if (hours > 0f)
            {
                HediffDef coma = DefDatabase<HediffDef>.GetNamedSilentFail("PsychicComa");
                if (coma != null)
                {
                    Hediff hediff = HediffMaker.MakeHediff(coma, invoker);
                    invoker.health.AddHediff(hediff);
                    hediff.TryGetComp<HediffComp_Disappears>()?.SetDuration(Mathf.RoundToInt(hours * 2500f));
                }
            }
        }
    }
}
