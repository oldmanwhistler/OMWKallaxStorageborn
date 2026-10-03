using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace OMWKallaxStorageborn
{
    public class Ability_BlahajBlast : Ability
    {
        public Ability_BlahajBlast(Pawn pawn, AbilityDef def) : base(pawn, def) { }
        public Ability_BlahajBlast(Pawn pawn, Precept sourcePrecept, AbilityDef def) : base(pawn, sourcePrecept, def) { }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            bool activated = base.Activate(target, dest);
            Pawn victim = target.Thing as Pawn;
            if (!activated || victim == null || !victim.Spawned || victim.Map != pawn.Map) return activated;

            DrawBeam(victim);
            DefDatabase<SoundDef>.GetNamed("OMW_BlahajBlastSound").PlayOneShot(SoundInfo.InMap(pawn));
            victim.TakeDamage(new DamageInfo(DamageDefOf.Bullet, 15f, 0.35f, -1f, pawn));

            GeneDef gene = DefDatabase<GeneDef>.GetNamed("VU_Hermaphromorph", false);
            if (gene != null && victim.genes != null && !victim.genes.HasGene(gene))
                victim.genes.AddGene(gene, true);
            return true;
        }

        private void DrawBeam(Pawn victim)
        {
            Map map = pawn.Map;
            Vector3 start = pawn.DrawPos;
            Vector3 end = victim.DrawPos;
            Vector3 delta = end - start;
            float distance = delta.magnitude;
            if (distance < 0.01f) return;
            Vector3 direction = delta / distance;
            Vector3 side = new Vector3(-direction.z, 0f, direction.x);
            // Three aligned fleck ribbons create the requested nested white/pink/blue beam.
            int segments = Mathf.Max(1, Mathf.CeilToInt(distance * 3f));
            for (int i = 0; i <= segments; i++)
            {
                Vector3 point = start + direction * (distance * i / segments);
                ThrowBeamFleck(point, map, side, 0.42f, new Color(85f / 255f, 205f / 255f, 252f / 255f, 1f));
                ThrowBeamFleck(point, map, side, 0.23f, new Color(247f / 255f, 168f / 255f, 184f / 255f, 1f));
                ThrowBeamFleck(point, map, side, 0.08f, Color.white);
            }
        }

        private static void ThrowBeamFleck(Vector3 point, Map map, Vector3 side, float width, Color color)
        {
            FleckDef beamFleck = DefDatabase<FleckDef>.GetNamed("OMW_BlahajBeamFleck");
            FleckCreationData data = FleckMaker.GetDataStatic(point, map, beamFleck, width);
            data.instanceColor = color;
            data.exactScale = new Vector3(width, 1f, width);
            data.airTimeLeft = 0.4f;
            data.scale = width;
            data.velocitySpeed = 0f;
            map.flecks.CreateFleck(data);
        }
    }
}
