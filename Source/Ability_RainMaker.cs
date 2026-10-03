using RimWorld;
using Verse;

namespace OMWKallaxStorageborn
{
    public class Ability_RainMaker : Ability
    {
        public Ability_RainMaker(Pawn pawn, AbilityDef def) : base(pawn, def)
        {
        }

        public Ability_RainMaker(Pawn pawn, Precept sourcePrecept, AbilityDef def)
            : base(pawn, sourcePrecept, def)
        {
        }

        public override bool Activate(LocalTargetInfo target, LocalTargetInfo dest)
        {
            bool activated = base.Activate(target, dest);
            if (!activated || pawn?.Map == null)
                return activated;

            GameCondition condition = GameConditionMaker.MakeCondition(
                DefDatabase<GameConditionDef>.GetNamed("OMW_RainMakerCondition"), 60000);
            pawn.Map.gameConditionManager.RegisterCondition(condition);
            return true;
        }
    }
}
