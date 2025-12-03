// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Extensions;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Database;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Graphics.UserInterfaceV2;
using osu.Game.Input.Bindings;
using osu.Game.Localisation;
using osu.Game.Online.Multiplayer;
using osu.Game.Screens.Edit;
using osu.Game.Screens.OnlinePlay.Match.Components;
using osu.Game.Skinning;
using osuTK;
using osuTK.Graphics;
using Realms;

namespace osu.Game.Overlays.SkinEditor
{
    public partial class ExternalEditOverlay : OsuFocusedOverlayContainer
    {
        private const double transition_duration = 300;
        private FillFlowContainer flow = null!;

        [Cached]
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Blue);

        [Resolved]
        private GameHost gameHost { get; set; } = null!;

        [Resolved]
        private SkinManager skinManager { get; set; } = null!;

        [Resolved]
        private BeatmapManager beatmapManager { get; set; } = null!;

        private Editor? editor;

        private IExternalEditOperation? editOperation;
        private TaskCompletionSource? taskCompletionSource;
        private bool finishingEdit;

        protected override bool DimMainContent => false;

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.Both;
            InternalChild = new Container
            {
                RelativeSizeAxes = Axes.Both,
                Masking = true,
                Children = new Drawable[]
                {
                    // Since we're drawing this overlay on top of another overlay (SkinEditor), the dimming effect isn't applied. So we need to add a dimming effect manually.
                    new Box
                    {
                        Colour = Color4.Black.Opacity(0.5f),
                        RelativeSizeAxes = Axes.Both,
                    },
                    new Container
                    {
                        Masking = true,
                        CornerRadius = 20,
                        AutoSizeAxes = Axes.Both,
                        AutoSizeDuration = 500,
                        AutoSizeEasing = Easing.OutQuint,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Children = new Drawable[]
                        {
                            new Box
                            {
                                Colour = colourProvider.Background5,
                                RelativeSizeAxes = Axes.Both,
                            },
                            flow = new FillFlowContainer
                            {
                                Margin = new MarginPadding(20),
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Vertical,
                                Anchor = Anchor.TopCentre,
                                Origin = Anchor.TopCentre,
                                Spacing = new Vector2(15),
                            }
                        }
                    }
                }
            };

            gameHost.ExitRequested += tryFinishOnExit;
        }

        public async Task<Task> Begin(IHasGuidPrimaryKey realmObject, Editor? providedEditor = null)
        {
            if (taskCompletionSource != null)
                throw new InvalidOperationException("Cannot start multiple concurrent external edits!");

            Show();

            switch (realmObject)
            {
                case SkinInfo:
                    showSpinner("Mounting external skin...");
                    setGlobalSkinDisabled(true);
                    break;

                case BeatmapSetInfo:
                    showSpinner("Mounting external beatmap...");
                    break;

                default:
                    throw new ArgumentException($"Unsupported skin info type: {realmObject.GetType().FullName}", nameof(realmObject));
            }

            editor = providedEditor;

            await Task.Delay(500).ConfigureAwait(true);

            try
            {
                switch (realmObject)
                {
                    case SkinInfo skinInfo:
                        editOperation = new ExternalEditWrapper<SkinInfo>(await skinManager.BeginExternalEditing(skinInfo).ConfigureAwait(false));
                        break;

                    case BeatmapSetInfo beatmapSetInfo:
                        editOperation = new ExternalEditWrapper<BeatmapSetInfo>(await beatmapManager.BeginExternalEditing(beatmapSetInfo).ConfigureAwait(false));
                        break;

                    default:
                        throw new ArgumentException($"Unsupported realm object type: {realmObject.GetType().FullName}", nameof(realmObject));
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to initialize external edit operation: {ex}", LoggingTarget.Database, LogLevel.Error);
                setGlobalSkinDisabled(false);
                Schedule(() => showSpinner("Export failed!"));
                Scheduler.AddDelayed(Hide, 1000);
                return Task.FromException(ex);
            }

            Schedule(() =>
            {
                flow.Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Text = "Item is mounted externally",
                        Font = OsuFont.Default.With(size: 30),
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                    },
                    new OsuTextFlowContainer
                    {
                        Padding = new MarginPadding(5),
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Width = 350,
                        AutoSizeAxes = Axes.Y,
                        Text = "Any changes made to the exported folder will be imported to the game, including file additions, modifications and deletions.",
                    },
                    new PurpleRoundedButton
                    {
                        Text = "Open folder",
                        Width = 350,
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Action = openDirectory,
                        Enabled = { Value = false }
                    },
                    new DangerousRoundedButton
                    {
                        Text = EditorStrings.FinishEditingExternally,
                        Width = 350,
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Action = () => finish().FireAndForget(),
                        Enabled = { Value = false }
                    }
                };
            });

            Scheduler.AddDelayed(() =>
            {
                foreach (var b in flow.ChildrenOfType<RoundedButton>())
                    b.Enabled.Value = true;
                openDirectory();
            }, 1000);
            return (taskCompletionSource = new TaskCompletionSource()).Task;
        }

        private void openDirectory()
        {
            if (editOperation == null)
                return;

            gameHost.OpenFileExternally(editOperation.MountedPath.TrimDirectorySeparator() + Path.DirectorySeparatorChar);
        }

        private void tryFinishOnExit()
        {
            if (editOperation != null && !finishingEdit)
                finish().FireAndForget(onSuccess: () => Schedule(() => finishingEdit = false));
        }

        private async Task finish()
        {
            if (finishingEdit)
                return;

            finishingEdit = true;

            Debug.Assert(taskCompletionSource != null);

            string? originalDifficulty = editor?.Beatmap.Value.Beatmap.BeatmapInfo.DifficultyName;

            showSpinner("Cleaning up...");
            await Task.Delay(500).ConfigureAwait(true);

            try
            {
                await editOperation!.Finish().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to finish external edit operation: {ex}", LoggingTarget.Database, LogLevel.Error);
                showSpinner("Import failed!");
                Scheduler.AddDelayed(Hide, 1000);
                setGlobalSkinDisabled(false);
                taskCompletionSource.SetException(ex);
                taskCompletionSource = null;
                return;
            }

            switch (editOperation)
            {
                case ExternalEditWrapper<SkinInfo>:
                    Schedule(() =>
                    {
                        var oldSkin = skinManager.CurrentSkin!.Value;
                        var newSkinInfo = oldSkin.SkinInfo.PerformRead(s => s);

                        // Create a new skin instance to ensure the skin is reloaded
                        // If there's a better way to reload the skin, this should be replaced with it.
                        setGlobalSkinDisabled(false);
                        skinManager.CurrentSkin.Value = newSkinInfo.CreateInstance(skinManager);

                        oldSkin.Dispose();
                        Hide();
                    });
                    break;

                case ExternalEditWrapper<BeatmapSetInfo> beatmapEdit:
                    Schedule(() =>
                    {
                        Live<BeatmapSetInfo>? beatmap = beatmapEdit.Result;

                        if (beatmap == null)
                        {
                            Schedule(Hide);
                            return;
                        }

                        Debug.Assert(editor != null);

                        beatmap.PerformWrite(s =>
                        {
                            if (s.Status == BeatmapOnlineStatus.None)
                                s.Status = BeatmapOnlineStatus.LocallyModified;
                            foreach (var difficulty in s.Beatmaps.Where(b => b.Status == BeatmapOnlineStatus.None))
                                difficulty.Status = BeatmapOnlineStatus.LocallyModified;
                        });

                        var closestMatchingBeatmap =
                            beatmap.Value.Beatmaps.FirstOrDefault(b => b.DifficultyName == originalDifficulty)
                            ?? beatmap.Value.Beatmaps.First();

                        editor!.SwitchToDifficulty(closestMatchingBeatmap);
                        Hide();
                    });
                    break;
            }

            taskCompletionSource.SetResult();
            taskCompletionSource = null;
        }

        private void setGlobalSkinDisabled(bool disabled)
        {
            skinManager.CurrentSkin.Disabled = disabled;
            skinManager.CurrentSkinInfo.Disabled = disabled;
        }

        protected override void PopIn()
        {
            this.FadeIn(transition_duration, Easing.OutQuint);
        }

        protected override void PopOut()
        {
            this.FadeOut(transition_duration, Easing.OutQuint).Finally(_ =>
            {
                // Set everything to a clean state
                editOperation = null;
                finishingEdit = false;
                flow.Children = Array.Empty<Drawable>();
            });
        }

        public override bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
        {
            if (e.Repeat)
                return false;

            switch (e.Action)
            {
                case GlobalAction.Back:
                case GlobalAction.Select:
                    if (editOperation == null)
                        return false;

                    finish().FireAndForget();
                    return true;
            }

            return base.OnPressed(e);
        }

        private void showSpinner(string text)
        {
            foreach (var b in flow.ChildrenOfType<RoundedButton>())
                b.Enabled.Value = false;

            flow.Children = new Drawable[]
            {
                new OsuSpriteText
                {
                    Text = text,
                    Font = OsuFont.Default.With(size: 30),
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                },
                new LoadingSpinner
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    State = { Value = Visibility.Visible }
                },
            };
        }

        protected override void Dispose(bool isDisposing)
        {
            if (gameHost.IsNotNull())
                gameHost.ExitRequested -= tryFinishOnExit;

            base.Dispose(isDisposing);
        }
    }

    internal interface IExternalEditOperation
    {
        string MountedPath { get; }
        Task Finish();
    }

    internal abstract class ExternalEditWrapper : IExternalEditOperation
    {
        public abstract string MountedPath { get; }
        public abstract Task Finish();
    }

    internal class ExternalEditWrapper<T> : ExternalEditWrapper where T : class, IHasGuidPrimaryKey, IRealmObject
    {
        private readonly ExternalEditOperation<T> operation;

        public Live<T>? Result { get; private set; }

        public ExternalEditWrapper(ExternalEditOperation<T> operation)
        {
            this.operation = operation;
        }

        public override string MountedPath => operation.MountedPath;

        public override async Task Finish()
        {
            Result = await operation.Finish().ConfigureAwait(false);
        }
    }
}
