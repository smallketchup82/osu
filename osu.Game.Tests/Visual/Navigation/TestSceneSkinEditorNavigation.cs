// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input;
using osu.Framework.Screens;
using osu.Framework.Testing;
using osu.Framework.Threading;
using osu.Game.Online.API;
using osu.Game.Beatmaps;
using osu.Game.Overlays.Mods;
using osu.Game.Overlays.Settings;
using osu.Game.Overlays.SkinEditor;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Mods;
using osu.Game.Screens.Edit.Components;
using osu.Game.Screens.Play;
using osu.Game.Screens.Play.HUD;
using osu.Game.Screens.Play.HUD.HitErrorMeters;
using osu.Game.Screens.SelectV2;
using osu.Game.Skinning;
using osu.Game.Tests.Beatmaps.IO;
using osuTK;
using osuTK.Input;

namespace osu.Game.Tests.Visual.Navigation
{
    public partial class TestSceneSkinEditorNavigation : OsuGameTestScene
    {
        private SoloSongSelect songSelect;
        private ModSelectOverlay modSelect => songSelect.ChildrenOfType<ModSelectOverlay>().First();

        private SkinEditor skinEditor => Game.ChildrenOfType<SkinEditor>().FirstOrDefault();

        [Test]
        public void TestEditComponentFromGameplayScene()
        {
            advanceToSongSelect();
            openSkinEditor();

            AddStep("import beatmap", () => BeatmapImportHelper.LoadQuickOszIntoOsu(Game).WaitSafely());
            AddUntilStep("wait for selected", () => !Game.Beatmap.IsDefault);

            switchToGameplayScene();

            BarHitErrorMeter hitErrorMeter = null;

            AddUntilStep("select bar hit error blueprint", () =>
            {
                var blueprint = skinEditor.ChildrenOfType<SkinBlueprint>().FirstOrDefault(b => b.Item is BarHitErrorMeter);

                if (blueprint == null)
                    return false;

                hitErrorMeter = (BarHitErrorMeter)blueprint.Item;
                skinEditor.SelectedComponents.Clear();
                skinEditor.SelectedComponents.Add(blueprint.Item);
                return true;
            });

            AddAssert("value is default", () => hitErrorMeter.JudgementLineThickness.IsDefault);

            AddStep("hover first slider", () =>
            {
                InputManager.MoveMouseTo(
                    skinEditor.ChildrenOfType<SkinSettingsToolbox>().First()
                              .ChildrenOfType<SettingsSlider<float>>().First()
                              .ChildrenOfType<SliderBar<float>>().First()
                );
            });

            AddStep("adjust slider via keyboard", () => InputManager.Key(Key.Left));

            AddAssert("value is less than default", () => hitErrorMeter.JudgementLineThickness.Value < hitErrorMeter.JudgementLineThickness.Default);
        }

        [Test]
        public void TestMutateProtectedSkinDuringGameplay()
        {
            advanceToSongSelect();
            AddStep("set default skin", () => Game.Dependencies.Get<SkinManager>().CurrentSkinInfo.SetDefault());

            AddStep("import beatmap", () => BeatmapImportHelper.LoadQuickOszIntoOsu(Game).WaitSafely());
            AddUntilStep("wait for selected", () => !Game.Beatmap.IsDefault);

            AddStep("enable NF", () => Game.SelectedMods.Value = new[] { new OsuModNoFail() });
            AddStep("enter gameplay", () => InputManager.Key(Key.Enter));

            AddUntilStep("wait for player", () =>
            {
                DismissAnyNotifications();
                return Game.ScreenStack.CurrentScreen is Player;
            });

            openSkinEditor();
            AddUntilStep("current skin is mutable", () => !Game.Dependencies.Get<SkinManager>().CurrentSkin.Value.SkinInfo.Value.Protected);
        }

        [Test]
        public void TestMutateProtectedSkinFromMainMenu_UndoToInitialStateIsCorrect()
        {
            AddStep("set default skin", () => Game.Dependencies.Get<SkinManager>().CurrentSkinInfo.SetDefault());
            AddStep("import beatmap", () => BeatmapImportHelper.LoadQuickOszIntoOsu(Game).WaitSafely());

            openSkinEditor();
            AddUntilStep("current skin is mutable", () => !Game.Dependencies.Get<SkinManager>().CurrentSkin.Value.SkinInfo.Value.Protected);

            AddUntilStep("wait for player", () =>
            {
                DismissAnyNotifications();
                return Game.ScreenStack.CurrentScreen is Player;
            });

            string state = string.Empty;

            AddUntilStep("wait for accuracy counter", () => Game.ChildrenOfType<ArgonAccuracyCounter>().Any(counter => counter.Position != new Vector2()));
            AddStep("dump state of accuracy meter", () => state = JsonConvert.SerializeObject(Game.ChildrenOfType<ArgonAccuracyCounter>().First().CreateSerialisedInfo()));
            AddStep("add any component", () => Game.ChildrenOfType<SkinComponentToolbox.ToolboxComponentButton>().First().TriggerClick());
            AddStep("undo", () =>
            {
                InputManager.PressKey(Key.ControlLeft);
                InputManager.Key(Key.Z);
                InputManager.ReleaseKey(Key.ControlLeft);
            });
            AddUntilStep("only one accuracy meter left",
                () => Game.ChildrenOfType<Player>().Single().ChildrenOfType<ArgonAccuracyCounter>().Count(),
                () => Is.EqualTo(1));
            AddAssert("accuracy meter state unchanged",
                () => JsonConvert.SerializeObject(Game.ChildrenOfType<ArgonAccuracyCounter>().First().CreateSerialisedInfo()),
                () => Is.EqualTo(state));
        }

        [Test]
        public void TestMutateProtectedSkinFromPlayer_UndoToInitialStateIsCorrect()
        {
            AddStep("set default skin", () => Game.Dependencies.Get<SkinManager>().CurrentSkinInfo.SetDefault());
            AddStep("import beatmap", () => BeatmapImportHelper.LoadQuickOszIntoOsu(Game).WaitSafely());
            advanceToSongSelect();
            AddUntilStep("wait for selected", () => !Game.Beatmap.IsDefault);

            AddStep("enable NF", () => Game.SelectedMods.Value = new[] { new OsuModNoFail() });
            AddStep("enter gameplay", () => InputManager.Key(Key.Enter));

            AddUntilStep("wait for player", () =>
            {
                DismissAnyNotifications();
                return Game.ScreenStack.CurrentScreen is Player;
            });
            openSkinEditor();

            string state = string.Empty;

            AddUntilStep("wait for accuracy counter", () => Game.ChildrenOfType<ArgonAccuracyCounter>().Any(counter => counter.Position != new Vector2()));
            AddStep("dump state of accuracy meter", () => state = JsonConvert.SerializeObject(Game.ChildrenOfType<ArgonAccuracyCounter>().First().CreateSerialisedInfo()));
            AddStep("add any component", () => Game.ChildrenOfType<SkinComponentToolbox.ToolboxComponentButton>().First().TriggerClick());
            AddStep("undo", () =>
            {
                InputManager.PressKey(Key.ControlLeft);
                InputManager.Key(Key.Z);
                InputManager.ReleaseKey(Key.ControlLeft);
            });
            AddUntilStep("only one accuracy meter left",
                () => Game.ChildrenOfType<Player>().Single().ChildrenOfType<ArgonAccuracyCounter>().Count(),
                () => Is.EqualTo(1));
            AddAssert("accuracy meter state unchanged",
                () => JsonConvert.SerializeObject(Game.ChildrenOfType<ArgonAccuracyCounter>().First().CreateSerialisedInfo()),
                () => Is.EqualTo(state));
        }

        [Test]
        public void TestComponentsDeselectedOnSkinEditorHide()
        {
            advanceToSongSelect();
            openSkinEditor();

            AddStep("import beatmap", () => BeatmapImportHelper.LoadQuickOszIntoOsu(Game).WaitSafely());
            AddUntilStep("wait for selected", () => !Game.Beatmap.IsDefault);

            switchToGameplayScene();

            AddUntilStep("wait for components", () => skinEditor.ChildrenOfType<SkinBlueprint>().Any());

            AddStep("select all components", () =>
            {
                InputManager.PressKey(Key.ControlLeft);
                InputManager.Key(Key.A);
                InputManager.ReleaseKey(Key.ControlLeft);
            });

            AddUntilStep("components selected", () => skinEditor.SelectedComponents.Count > 0);

            toggleSkinEditor();

            AddUntilStep("no components selected", () => skinEditor.SelectedComponents.Count == 0);
        }

        [Test]
        public void TestSwitchScreenWhileDraggingComponent()
        {
            Vector2 firstBlueprintCentre = Vector2.Zero;
            ScheduledDelegate movementDelegate = null;

            advanceToSongSelect();

            openSkinEditor();

            AddStep("add skinnable component", () =>
            {
                skinEditor.ChildrenOfType<SkinComponentToolbox.ToolboxComponentButton>().First().TriggerClick();
            });

            AddUntilStep("newly added component selected", () => skinEditor.SelectedComponents.Count == 1);

            AddStep("start drag", () =>
            {
                firstBlueprintCentre = skinEditor.ChildrenOfType<SkinBlueprint>().First().ScreenSpaceDrawQuad.Centre;

                InputManager.MoveMouseTo(firstBlueprintCentre);
                InputManager.PressButton(MouseButton.Left);
            });

            AddStep("start movement", () => movementDelegate = Scheduler.AddDelayed(() => { InputManager.MoveMouseTo(firstBlueprintCentre += new Vector2(1)); }, 10, true));

            toggleSkinEditor();
            AddStep("exit song select", () => songSelect.Exit());

            AddUntilStep("wait for blueprints removed", () => !skinEditor.ChildrenOfType<SkinBlueprint>().Any());

            AddStep("stop drag", () =>
            {
                InputManager.ReleaseButton(MouseButton.Left);
                movementDelegate?.Cancel();
            });
        }

        [Test]
        public void TestAutoplayCompatibleModsRetainedOnEnteringGameplay()
        {
            advanceToSongSelect();
            openSkinEditor();
            AddStep("select DT", () => Game.SelectedMods.Value = new Mod[] { new OsuModDoubleTime() });

            AddStep("import beatmap", () => BeatmapImportHelper.LoadQuickOszIntoOsu(Game).WaitSafely());
            AddUntilStep("wait for selected", () => !Game.Beatmap.IsDefault);

            switchToGameplayScene();

            AddAssert("DT still selected", () => ((Player)Game.ScreenStack.CurrentScreen).Mods.Value.Single() is OsuModDoubleTime);
        }

        [Test]
        public void TestAutoplayIncompatibleModsRemovedOnEnteringGameplay()
        {
            advanceToSongSelect();
            openSkinEditor();
            AddStep("select relax and spun out", () => Game.SelectedMods.Value = new Mod[] { new OsuModRelax(), new OsuModSpunOut() });

            AddStep("import beatmap", () => BeatmapImportHelper.LoadQuickOszIntoOsu(Game).WaitSafely());
            AddUntilStep("wait for selected", () => !Game.Beatmap.IsDefault);

            switchToGameplayScene();

            AddAssert("no mod selected", () => !((Player)Game.ScreenStack.CurrentScreen).Mods.Value.Any());
        }

        [Test]
        public void TestDuplicateAutoplayModRemovedOnEnteringGameplay()
        {
            advanceToSongSelect();
            openSkinEditor();
            AddStep("select autoplay", () => Game.SelectedMods.Value = new Mod[] { new OsuModAutoplay() });

            AddStep("import beatmap", () => BeatmapImportHelper.LoadQuickOszIntoOsu(Game).WaitSafely());
            AddUntilStep("wait for selected", () => !Game.Beatmap.IsDefault);

            switchToGameplayScene();

            AddAssert("no mod selected", () => !((Player)Game.ScreenStack.CurrentScreen).Mods.Value.Any());
        }

        [Test]
        public void TestGameplaySettingsDoesNotExpandWhenSkinOverlayPresent()
        {
            advanceToSongSelect();
            openSkinEditor();
            AddStep("select autoplay", () => Game.SelectedMods.Value = new Mod[] { new OsuModAutoplay() });
            AddStep("import beatmap", () => BeatmapImportHelper.LoadQuickOszIntoOsu(Game).WaitSafely());
            AddUntilStep("wait for selected", () => !Game.Beatmap.IsDefault);
            switchToGameplayScene();

            AddUntilStep("wait for settings", () => getPlayerSettingsOverlay() != null);
            AddAssert("settings not visible", () => getPlayerSettingsOverlay().DrawWidth, () => Is.EqualTo(0));

            AddStep("move cursor to right of screen", () => InputManager.MoveMouseTo(InputManager.ScreenSpaceDrawQuad.TopRight));
            AddAssert("settings not visible", () => getPlayerSettingsOverlay().DrawWidth, () => Is.EqualTo(0));

            toggleSkinEditor();

            AddStep("move cursor slightly", () => InputManager.MoveMouseTo(InputManager.ScreenSpaceDrawQuad.TopRight + new Vector2(1)));
            AddUntilStep("settings visible", () => getPlayerSettingsOverlay().DrawWidth, () => Is.GreaterThan(0));

            AddStep("move cursor to right of screen too far", () => InputManager.MoveMouseTo(InputManager.ScreenSpaceDrawQuad.TopRight + new Vector2(10240, 0)));
            AddUntilStep("settings not visible", () => getPlayerSettingsOverlay().DrawWidth, () => Is.EqualTo(0));

            PlayerSettingsOverlay getPlayerSettingsOverlay() => ((Player)Game.ScreenStack.CurrentScreen).ChildrenOfType<PlayerSettingsOverlay>().SingleOrDefault();
        }

        [Test]
        public void TestCinemaModRemovedOnEnteringGameplay()
        {
            advanceToSongSelect();
            openSkinEditor();
            AddStep("select cinema", () => Game.SelectedMods.Value = new Mod[] { new OsuModCinema() });

            AddStep("import beatmap", () => BeatmapImportHelper.LoadQuickOszIntoOsu(Game).WaitSafely());
            AddUntilStep("wait for selected", () => !Game.Beatmap.IsDefault);

            switchToGameplayScene();

            AddAssert("no mod selected", () => !((Player)Game.ScreenStack.CurrentScreen).Mods.Value.Any());
        }

        [Test]
        public void TestModOverlayClosesOnOpeningSkinEditor()
        {
            advanceToSongSelect();
            AddStep("open mod overlay", () => modSelect.Show());

            openSkinEditor();
            AddUntilStep("mod overlay closed", () => modSelect.State.Value == Visibility.Hidden);
        }

        [Test]
        public void TestChangeToNonSkinnableScreen()
        {
            advanceToSongSelect();
            openSkinEditor();
            AddAssert("blueprint container present", () => skinEditor.ChildrenOfType<SkinBlueprintContainer>().Count(), () => Is.EqualTo(1));
            AddAssert("placeholder not present", () => skinEditor.ChildrenOfType<NonSkinnableScreenPlaceholder>().Count(), () => Is.Zero);
            AddAssert("editor sidebars not empty", () => skinEditor.ChildrenOfType<EditorSidebar>().SelectMany(sidebar => sidebar.Children).Count(), () => Is.GreaterThan(0));

            AddStep("add skinnable component", () =>
            {
                skinEditor.ChildrenOfType<SkinComponentToolbox.ToolboxComponentButton>().First().TriggerClick();
            });
            AddUntilStep("newly added component selected", () => skinEditor.SelectedComponents, () => Has.Count.EqualTo(1));

            AddStep("exit to main menu", () => Game.ScreenStack.CurrentScreen.Exit());
            AddAssert("selection cleared", () => skinEditor.SelectedComponents, () => Has.Count.Zero);
            AddAssert("blueprint container not present", () => skinEditor.ChildrenOfType<SkinBlueprintContainer>().Count(), () => Is.Zero);
            AddAssert("placeholder present", () => skinEditor.ChildrenOfType<NonSkinnableScreenPlaceholder>().Count(), () => Is.EqualTo(1));
            AddAssert("editor sidebars empty", () => skinEditor.ChildrenOfType<EditorSidebar>().SelectMany(sidebar => sidebar.Children).Count(), () => Is.Zero);

            advanceToSongSelect();
            AddAssert("blueprint container present", () => skinEditor.ChildrenOfType<SkinBlueprintContainer>().Count(), () => Is.EqualTo(1));
            AddAssert("placeholder not present", () => skinEditor.ChildrenOfType<NonSkinnableScreenPlaceholder>().Count(), () => Is.Zero);
            AddAssert("editor sidebars not empty", () => skinEditor.ChildrenOfType<EditorSidebar>().SelectMany(sidebar => sidebar.Children).Count(), () => Is.GreaterThan(0));
        }

        [Test]
        public void TestOpenSkinEditorGameplaySceneOnBeatmapWithNoObjects()
        {
            AddStep("set dummy beatmap", () => Game.Beatmap.SetDefault());
            advanceToSongSelect();

            AddStep("create empty beatmap", () => Game.BeatmapManager.CreateNew(new OsuRuleset().RulesetInfo, new GuestUser()));
            AddUntilStep("wait for selected", () => !Game.Beatmap.IsDefault);

            openSkinEditor();
            switchToGameplayScene();
        }

        [Test]
        public void TestOpenSkinEditorGameplaySceneWhenDummyBeatmapActive()
        {
            AddStep("set dummy beatmap", () => Game.Beatmap.SetDefault());

            openSkinEditor();
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void TestOpenSkinEditorGameplaySceneWhenDifferentRulesetActive(int rulesetId)
        {
            BeatmapSetInfo beatmapSet = null!;

            AddStep("import beatmap", () => beatmapSet = BeatmapImportHelper.LoadQuickOszIntoOsu(Game).GetResultSafely());
            AddStep($"select difficulty for ruleset w/ ID {rulesetId}", () =>
            {
                var beatmap = beatmapSet.Beatmaps.First(b => b.Ruleset.OnlineID == rulesetId);
                Game.Beatmap.Value = Game.BeatmapManager.GetWorkingBeatmap(beatmap);
            });

            openSkinEditor();
            switchToGameplayScene();
        }

        [Test]
        public void TestRulesetInputDisabledWhenSkinEditorOpen()
        {
            advanceToSongSelect();
            openSkinEditor();

            AddStep("import beatmap", () => BeatmapImportHelper.LoadQuickOszIntoOsu(Game).WaitSafely());
            AddUntilStep("wait for selected", () => !Game.Beatmap.IsDefault);

            switchToGameplayScene();
            AddUntilStep("nested input disabled", () => ((Player)Game.ScreenStack.CurrentScreen).ChildrenOfType<PassThroughInputManager>().All(manager => !manager.UseParentInput));

            toggleSkinEditor();
            AddUntilStep("nested input enabled", () => ((Player)Game.ScreenStack.CurrentScreen).ChildrenOfType<PassThroughInputManager>().Any(manager => manager.UseParentInput));

            toggleSkinEditor();
            AddUntilStep("nested input disabled", () => ((Player)Game.ScreenStack.CurrentScreen).ChildrenOfType<PassThroughInputManager>().All(manager => !manager.UseParentInput));
        }

        [Test]
        public void TestSkinSavesOnChange()
        {
            advanceToSongSelect();
            openSkinEditor();

            Guid editedSkinId = Guid.Empty;
            AddStep("save skin id", () => editedSkinId = Game.Dependencies.Get<SkinManager>().CurrentSkinInfo.Value.ID);
            AddStep("add skinnable component", () =>
            {
                skinEditor.ChildrenOfType<SkinComponentToolbox.ToolboxComponentButton>().First().TriggerClick();
            });

            AddStep("change to triangles skin", () => Game.Dependencies.Get<SkinManager>().SetSkinFromConfiguration(SkinInfo.TRIANGLES_SKIN.ToString()));
            AddUntilStep("components loaded", () => Game.ChildrenOfType<SkinnableContainer>().All(c => c.ComponentsLoaded));
            // sort of implicitly relies on song select not being skinnable.
            // TODO: revisit if the above ever changes
            AddUntilStep("skin changed", () => !skinEditor.ChildrenOfType<SkinBlueprint>().Any());

            AddStep("change back to modified skin", () => Game.Dependencies.Get<SkinManager>().SetSkinFromConfiguration(editedSkinId.ToString()));
            AddUntilStep("components loaded", () => Game.ChildrenOfType<SkinnableContainer>().All(c => c.ComponentsLoaded));
            AddUntilStep("changes saved", () => skinEditor.ChildrenOfType<SkinBlueprint>().Any());
        }

        private void advanceToSongSelect()
        {
            PushAndConfirm(() => songSelect = new SoloSongSelect());
            AddUntilStep("wait for song select", () => songSelect.CarouselItemsPresented);
        }

        private void openSkinEditor()
        {
            toggleSkinEditor();
            AddUntilStep("skin editor loaded", () => skinEditor != null);
        }

        private void toggleSkinEditor()
        {
            AddStep("toggle skin editor", () =>
            {
                InputManager.PressKey(Key.ControlLeft);
                InputManager.PressKey(Key.ShiftLeft);
                InputManager.Key(Key.S);
                InputManager.ReleaseKey(Key.ControlLeft);
                InputManager.ReleaseKey(Key.ShiftLeft);
            });
        }

        private void switchToGameplayScene()
        {
            AddStep("Click gameplay scene button", () =>
            {
                InputManager.MoveMouseTo(skinEditor.ChildrenOfType<SkinEditorSceneLibrary.SceneButton>().First(b => b.Text.ToString() == "Gameplay"));
                InputManager.Click(MouseButton.Left);
            });

            AddUntilStep("wait for player", () =>
            {
                DismissAnyNotifications();
                return Game.ScreenStack.CurrentScreen is Player;
            });
        }
    }
}
