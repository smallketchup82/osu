// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Localisation;
using osu.Framework.Screens;
using osu.Game.Localisation;
using osu.Game.Screens;
using osu.Game.Screens.Import;
using osu.Game.Screens.Utility;

namespace osu.Game.Overlays.Settings.Sections.DebugSettings
{
    public partial class GeneralSettings : SettingsSubsection
    {
        protected override LocalisableString Header => CommonStrings.General;

        private SettingsCheckbox associationSetting = null!;

        [BackgroundDependencyLoader]
        private void load(FrameworkDebugConfigManager config, FrameworkConfigManager frameworkConfig, IPerformFromScreenRunner? performer)
        {
            Children = new Drawable[]
            {
                associationSetting = new SettingsCheckbox
                {
                    LabelText = "Associate files with osu!",
                    Keywords = new[] { @"association", @"file", @"extension", @"osu", @"osz", @"osk" },
                    TooltipText = "Associate osu! related files with osu!(lazer)",
                    Current = new Bindable<bool>(true),
                    ClassicDefault = true
                },
                new SettingsCheckbox
                {
                    LabelText = DebugSettingsStrings.ShowLogOverlay,
                    Current = frameworkConfig.GetBindable<bool>(FrameworkSetting.ShowLogOverlay)
                },
                new SettingsCheckbox
                {
                    LabelText = DebugSettingsStrings.BypassFrontToBackPass,
                    Current = config.GetBindable<bool>(DebugSetting.BypassFrontToBackPass)
                },
                new SettingsButton
                {
                    Text = DebugSettingsStrings.ImportFiles,
                    Action = () => performer?.PerformFromScreen(menu => menu.Push(new FileImportScreen()))
                },
                new SettingsButton
                {
                    Text = DebugSettingsStrings.RunLatencyCertifier,
                    Action = () => performer?.PerformFromScreen(menu => menu.Push(new LatencyCertifierScreen()))
                }
            };

            associationSetting.SetNoticeText("This setting should only be disabled if you are having issues with associations. Use your operating system's settings to manage associations where possible.", true);
        }
    }
}
