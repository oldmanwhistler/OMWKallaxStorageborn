using RimWorld;
using Verse;
using Verse.Sound;

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

            SoundDefOf.Thunder_OffMap.PlayOneShotOnCamera();

            // Set the map weather immediately; the condition keeps rain forced for the full duration.
            WeatherDef rain = DefDatabase<WeatherDef>.GetNamed("Rain");
            pawn.Map.weatherManager.curWeather = rain;
            pawn.Map.weatherManager.lastWeather = rain;

            GameCondition condition = GameConditionMaker.MakeCondition(
                DefDatabase<GameConditionDef>.GetNamed("OMW_RainMakerCondition"), 10000);
            pawn.Map.gameConditionManager.RegisterCondition(condition);
            return true;
        }
    }
}
