// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using osu.Framework;
using osu.Framework.Allocation;
using osu.Framework.Localisation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input;
using osu.Game.Graphics;
using osu.Game.Localisation;
using osu.Game.Overlays.Settings.Sections.Audio;

namespace osu.Game.Overlays.Settings.Sections
{
    public partial class AudioSection : SettingsSection
    {
        public override LocalisableString Header =>
            RuntimeInfo.IsMobile
                ? AudioSettingsStrings.MobileAudioSectionHeader
                : AudioSettingsStrings.AudioSectionHeader;

        public override Drawable CreateIcon() => new SpriteIcon
        {
            Icon = OsuIcon.Audio
        };

        public override IEnumerable<LocalisableString> FilterTerms => base.FilterTerms.Concat(new LocalisableString[] { "sound" });

        [BackgroundDependencyLoader]
        private void load(HapticManager hapticManager)
        {
            Add(new AudioDevicesSettings());
            Add(new VolumeSettings());

            if (RuntimeInfo.IsMobile && hapticManager.SupportsHaptics)
                Add(new HapticSettings());

            Add(new OffsetSettings());
        }
    }
}
