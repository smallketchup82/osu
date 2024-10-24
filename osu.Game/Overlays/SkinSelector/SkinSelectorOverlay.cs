// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Testing;
using osu.Game.Database;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.UserInterface;
using osu.Game.Input.Bindings;
using osu.Game.Skinning;
using osuTK;
using Realms;

namespace osu.Game.Overlays.SkinSelector
{
    public partial class SkinSelectorOverlay : OsuFocusedOverlayContainer
    {
        [Resolved]
        private SkinManager skinManager { get; set; } = null!;

        [Resolved]
        private RealmAccess realm { get; set; } = null!;

        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Blue);

        protected override bool DimMainContent => true;

        private readonly BasicSearchTextBox searchTextBox;

        private readonly SearchContainer searchContainer;

        private readonly OsuScrollContainer scrollContainer;

        private readonly Bindable<SkinItem> selectedItem = new Bindable<SkinItem>();

        private readonly List<Live<SkinInfo>> skinItems = new List<Live<SkinInfo>>();

        private const int transition_duration = 200;

        public SkinSelectorOverlay()
        {
            Anchor = Anchor.Centre;
            Scale = new Vector2(0.75f);
            Origin = Anchor.Centre;
            RelativeSizeAxes = Axes.Both;
            Width = 0.4f;
            Height = 0.4f;
            CornerRadius = 10;
            Masking = true;
            Children = new Drawable[]
            {
                new Box
                {
                    Colour = colourProvider.Background6,
                    RelativeSizeAxes = Axes.Both,
                },
                // This is the main content of the overlay
                new FillFlowContainer
                {
                    Direction = FillDirection.Vertical,
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                    Padding = new MarginPadding(10),
                    RelativeSizeAxes = Axes.Both,
                    Spacing = new Vector2(0, 10),
                    Children = new Drawable[]
                    {
                        // The search text box
                        searchTextBox = new SkinSearchTextBox
                        {
                            RelativeSizeAxes = Axes.Both,
                            Height = 0.12f,
                            HoldFocus = true
                        },
                        // The scroll container for the skin list
                        scrollContainer = new OsuScrollContainer
                        {
                            RelativeSizeAxes = Axes.Both,
                            Height = 0.84f,
                            // The skin list
                            Child = searchContainer = new SearchContainer
                            {
                                Direction = FillDirection.Vertical,
                                AutoSizeAxes = Axes.Y,
                                RelativeSizeAxes = Axes.X,
                                Spacing = new Vector2(0, 10),
                            }
                        }
                    }
                }
            };

            searchTextBox.Current.BindValueChanged(_ =>
            {
                searchContainer.SearchTerm = searchTextBox.Text;

                // Clear the selection when the search term changes since the selected item could be filtered out
                selectedItem.Value = null!;

                // Scroll to the top of the list when the search term changes
                scrollContainer.ScrollTo(0);

                // TODO: Show a message when no results are found
            });

            searchTextBox.OnCommit += (_, _) =>
            {
                if (selectedItem.Value == null)
                {
                    searchContainer.ChildrenOfType<SkinItem>().FirstOrDefault(item => item.MatchingFilter)?.TriggerClick();
                    selectedItem.Value = null!;
                }
                else
                    selectedItem.Value.TriggerClick();

                searchTextBox.Text = string.Empty;
            };
        }

        protected override void LoadComplete()
        {
            var realmSubscription = realm.RegisterForNotifications(_ => realm.Realm.All<SkinInfo>()
                                                                             .Where(s => !s.DeletePending)
                                                                             .OrderBy(s => s.Name, StringComparer.OrdinalIgnoreCase), skinsChanged!);

            selectedItem.BindValueChanged(change => Scheduler.AddOnce(() =>
            {
                if (change.OldValue != null) change.OldValue.BorderThickness = 0;

                if (change.NewValue != null)
                {
                    change.NewValue.BorderThickness = 2;
                    change.NewValue.BorderColour = colourProvider.Highlight1;
                }

                scrollToSelection();
            }));
            base.LoadComplete();
        }

        private void skinsChanged(IRealmCollection<SkinInfo> sender, ChangeSet changes)
        {
            if (!sender.Any())
                return;

            skinItems.Clear();

            skinItems.Add(sender.Single(s => s.ID == SkinInfo.ARGON_SKIN).ToLive(realm));
            skinItems.Add(sender.Single(s => s.ID == SkinInfo.ARGON_PRO_SKIN).ToLive(realm));
            skinItems.Add(sender.Single(s => s.ID == SkinInfo.TRIANGLES_SKIN).ToLive(realm));
            skinItems.Add(sender.Single(s => s.ID == SkinInfo.CLASSIC_SKIN).ToLive(realm));

            foreach (var skin in sender.Where(s => !s.Protected))
                skinItems.Add(skin.ToLive(realm));

            Schedule(() =>
            {
                searchContainer.Clear();

                searchContainer.AddRange(skinItems.Select(skin => new SkinItem
                {
                    SkinInfo = skin.Value,
                    Action = () =>
                    {
                        skinManager.CurrentSkinInfo.Value = skin;
                        Hide();
                    }
                }));
            });
        }

        private void scrollToSelection()
        {
            if (selectedItem.Value == null)
                return;

            var drawableItem = searchContainer.Children.FirstOrDefault(s => s == selectedItem.Value);

            if (drawableItem == null)
                return;

            if (!drawableItem.IsLoaded)
                drawableItem.OnLoadComplete += _ => scrollContainer.ScrollIntoView(drawableItem);
            else
                scrollContainer.ScrollIntoView(drawableItem);
        }

        private void selectNext(int direction)
        {
            var visibleItems = searchContainer.ChildrenOfType<SkinItem>().AsEnumerable().Where(r => r.IsPresent);

            SkinItem? item;

            if (selectedItem.Value == null)
                item = visibleItems.FirstOrDefault();
            else
            {
                if (direction == -1)
                    visibleItems = visibleItems.Reverse();

                item = visibleItems.SkipWhile(r => r != selectedItem.Value).Skip(1).FirstOrDefault();

                // If the user is scrolling up and the current item is the first one, reverse again, then select the last item.
                if (item == null)
                {
                    item = direction switch
                    {
                        -1 => visibleItems.Reverse().LastOrDefault(),
                        1 => visibleItems.FirstOrDefault(),
                        _ => item
                    };
                }
            }

            if (item != null)
                selectedItem.Value = item;
        }

        public override bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
        {
            switch (e.Action)
            {
                case GlobalAction.SelectNext:
                    selectNext(1);
                    return true;

                case GlobalAction.SelectPrevious:
                    selectNext(-1);
                    return true;
            }

            if (e.Repeat)
                return false;

            switch (e.Action)
            {
                case GlobalAction.Back:
                    Hide();
                    return true;

                case GlobalAction.Select:
                    if (selectedItem.Value != null)
                        selectedItem.Value.TriggerClick();
                    else
                        return false;

                    return true;
            }

            return false;
        }

        protected override void PopIn()
        {
            this
                .FadeIn(transition_duration, Easing.InOutCubic)
                .ScaleTo(1, transition_duration, Easing.InOutCubic);
        }

        protected override void PopOut()
        {
            this.FadeOut(transition_duration, Easing.InOutCubic).ScaleTo(0.75f, transition_duration, Easing.InOutCubic).Finally(_ =>
            {
                searchTextBox.Text = string.Empty;
            });
        }
    }
}
