// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Events;
using osu.Framework.Localisation;
using osu.Game.Graphics.Containers;
using osu.Game.Graphics.Sprites;
using osu.Game.Graphics.UserInterface;
using osu.Game.Skinning;

namespace osu.Game.Overlays.SkinSelector
{
    public partial class SkinItem : OsuClickableContainer, IFilterable
    {
        private readonly OverlayColourProvider colourProvider = new OverlayColourProvider(OverlayColourScheme.Blue);
        public required SkinInfo SkinInfo { get; set; }

        private Box background = null!;

        public SkinItem()
            : base(HoverSampleSet.Button)
        {
            Enabled.Value = true;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.X;
            Height = 50;
            CornerRadius = 5;
            Masking = true;
            Children = new Drawable[]
            {
                background = new Box
                {
                    Colour = colourProvider.Background4,
                    RelativeSizeAxes = Axes.Both,
                },
                new TruncatingSpriteText
                {
                    Text = SkinInfo.Name,
                    Origin = Anchor.Centre,
                    Anchor = Anchor.Centre,
                    RelativeSizeAxes = Axes.X,
                    Padding = new MarginPadding { Horizontal = 10 },
                }
            };
        }

        protected override bool OnHover(HoverEvent e)
        {
            background.FadeColour(colourProvider.Background3, 200);
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            background.FadeColour(colourProvider.Background4, 200);
            base.OnHoverLost(e);
        }

        public IEnumerable<LocalisableString> FilterTerms => [SkinInfo.Name];

        private bool matchingFilter = true;

        public bool MatchingFilter
        {
            get => matchingFilter;
            set
            {
                if (value == matchingFilter)
                    return;

                matchingFilter = value;
                this.FadeTo(matchingFilter ? 1 : 0, 200);
            }
        }

        public bool FilteringActive { get; set; }
    }
}
