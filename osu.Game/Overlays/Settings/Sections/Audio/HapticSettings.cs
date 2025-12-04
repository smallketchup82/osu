// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Localisation;

namespace osu.Game.Overlays.Settings.Sections.Audio
{
    public partial class HapticSettings : SettingsSubsection
    {
        protected override LocalisableString Header => "Haptic Settings";

        [BackgroundDependencyLoader]
        private void load(FrameworkConfigManager config)
        {
            Children = new Drawable[]
            {
                new SettingsCheckbox
                {
                    LabelText = "Enable haptics",
                    Current = config.GetBindable<bool>(FrameworkSetting.HapticsEnabled)
                }
            };
        }
    }
}
