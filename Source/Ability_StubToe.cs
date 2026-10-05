using System;
using System.Linq;
using RimWorld;
using Verse;

namespace OMWKallaxStorageborn
{
    public class CompProperties_AbilityStubToe : CompProperties_AbilityGiveHediff
    {
        public CompProperties_AbilityStubToe()
        {
            compClass = typeof(CompAbilityEffect_StubToe);
        }
    }

    public class CompAbilityEffect_StubToe : CompAbilityEffect_GiveHediff
    {
        public override void Apply(LocalTargetInfo target, LocalTargetInfo dest)
        {
            Pawn victim = target.Pawn;
            if (victim != null && victim.health != null)
            {
                BodyPartRecord part = victim.health.hediffSet.GetNotMissingParts()
                    .FirstOrDefault(p => p.def.defName.Equals("Foot", StringComparison.OrdinalIgnoreCase));
                if (part == null)
                {
                    part = victim.health.hediffSet.GetNotMissingParts()
                        .FirstOrDefault(p => p.def == BodyPartDefOf.Leg);
                }

                if (part != null)
                {
                    DamageInfo damage = new DamageInfo(DamageDefOf.Cut, 1f, 0f, -1f, parent.pawn, part);
                    victim.health.AddHediff(Props.hediffDef, part);
                    victim.TakeDamage(damage);
                }
            }

            base.Apply(target, dest);
        }
    }
}
