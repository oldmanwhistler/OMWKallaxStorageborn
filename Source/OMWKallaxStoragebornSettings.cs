using RimWorld;
using Verse;

namespace OMWKallaxStorageborn
{
    public class OMWKallaxStoragebornSettings : ModSettings
    {
        public bool KallaxEnabled = true;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref KallaxEnabled, "kallaxEnabled", true);
        }
    }
}
