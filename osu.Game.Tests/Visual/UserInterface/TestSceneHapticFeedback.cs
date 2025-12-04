// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;
using osu.Framework.Testing;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Overlays.Settings;
using osuTK;

namespace osu.Game.Tests.Visual.UserInterface
{
    public partial class TestSceneHapticFeedback : OsuManualInputManagerTestScene
    {
        [Resolved]
        private HapticManager hapticManager { get; set; } = null!;

        private readonly BindableBool continuousHapticEnabled = new BindableBool();
        private readonly BindableNumber<float> transientIntensity = new BindableNumber<float>(1.0f) { MinValue = 0.0f, MaxValue = 1.0f, Precision = 0.1f };
        private readonly BindableNumber<float> transientSharpness = new BindableNumber<float>(1.0f) { MinValue = 0.0f, MaxValue = 1.0f, Precision = 0.1f };
        private readonly BindableNumber<float> crashDuration = new BindableNumber<float>(1.0f) { MinValue = 0.1f, MaxValue = 10.0f, Precision = 0.1f };

        private readonly BindableBool toggleState = new BindableBool();

        [SetUpSteps]
        public virtual void SetUpSteps() => AddStep("Create components", () =>
        {
            RelativeSizeAxes = Axes.Both;
            Child = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Y,
                Width = 500,
                Spacing = new Vector2(0, 15),
                Direction = FillDirection.Vertical,
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Text = "Transient Haptics",
                        Font = OsuFont.Default.With(size: 30),
                        Origin = Anchor.TopCentre,
                        Anchor = Anchor.TopCentre,
                    },
                    new SettingsSlider<float>
                    {
                        LabelText = "Haptic Intensity",
                        Current = transientIntensity,
                        KeyboardStep = 0.10f
                    },
                    new SettingsSlider<float>
                    {
                        LabelText = "Haptic Sharpness",
                        Current = transientSharpness,
                        KeyboardStep = 0.10f
                    },
                    new SettingsSlider<float>
                    {
                        LabelText = "Crash Duration",
                        Current = crashDuration,
                        KeyboardStep = 0.10f
                    },
                    new SettingsButton
                    {
                        Text = "Trigger Transient Haptic",
                        Action = () => hapticManager.PlayTransient(transientIntensity.Value, transientSharpness.Value),
                        EnableHaptics = false
                    },
                    new SettingsButton
                    {
                        Text = "Toggle Continuous Haptic",
                        Action = () => continuousHapticEnabled.Value = !continuousHapticEnabled.Value,
                        EnableHaptics = false
                    },
                    new OsuSpriteText
                    {
                        Text = "Helpers",
                        Font = OsuFont.Default.With(size: 30),
                        Origin = Anchor.TopCentre,
                        Anchor = Anchor.TopCentre,
                    },
                    new SettingsButton
                    {
                        Text = "Button Press",
                        Action = () => hapticManager.ButtonPress(),
                        EnableHaptics = false
                    },
                    new SettingsButton
                    {
                        Text = "Selection Changed",
                        Action = () => hapticManager.SelectionChanged(),
                        EnableHaptics = false
                    },
                    new OsuCheckbox
                    {
                        LabelText = "Toggle On/Off",
                        Current = toggleState,
                        EnableHaptics = false
                    },
                    new SettingsButton
                    {
                        Text = "Success Notification",
                        Action = () => hapticManager.SuccessNotification(),
                        EnableHaptics = false
                    },
                    new SettingsButton
                    {
                        Text = "Warning Notification",
                        Action = () => hapticManager.WarningNotification(),
                        EnableHaptics = false
                    },
                    new SettingsButton
                    {
                        Text = "Error Notification",
                        Action = () => hapticManager.ErrorNotification(),
                        EnableHaptics = false
                    },
                    new SettingsButton
                    {
                        Text = "Crash",
                        Action = () => hapticManager.Crash(transientIntensity.Value, transientSharpness.Value, crashDuration.Value),
                        EnableHaptics = false
                    },
                }
            };

            // Ensure continuous haptic is disabled at the start of the test.
            continuousHapticEnabled.Value = false;

            continuousHapticEnabled.BindValueChanged(enabled =>
            {
                if (enabled.NewValue)
                    hapticManager.StartContinuous(transientIntensity.Value, transientSharpness.Value);
                else
                    hapticManager.ReleaseContinuous();
            });

            toggleState.BindValueChanged(state =>
            {
                if (state.NewValue)
                    hapticManager.ToggleOn();
                else
                    hapticManager.ToggleOff();
            });
        });
    }
}
