using Verse;

namespace OMWKallaxStorageborn
{
    public class OMWKallaxStoragebornMod : Mod
    {
        public static OMWKallaxStoragebornMod Instance { get; private set; }
        public OMWKallaxStoragebornSettings Settings { get; }

        public OMWKallaxStoragebornMod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<OMWKallaxStoragebornSettings>();
        }

        public override string SettingsCategory()
        {
            return "OMWKallaxStoragebornSettingsCategory".Translate();
        }

        public override void DoSettingsWindowContents(UnityEngine.Rect inRect)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(inRect);
            listing.Label("OMWKallaxStoragebornSettingsTitle".Translate());
            listing.CheckboxLabeled("OMWKallaxStoragebornSettingsEnabled".Translate(), ref Settings.KallaxEnabled);
            listing.Gap();
            listing.Label("OMWKallaxStoragebornSettingsDescription".Translate());
            listing.End();
        }
    }
}
