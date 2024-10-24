// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using NUnit.Framework;
using osu.Game.Overlays.SkinSelector;

namespace osu.Game.Tests.Visual.UserInterface
{
    [TestFixture]
    public partial class TestSceneSkinSelect : OsuTestScene
    {
        private SkinSelectorOverlay skinSelectorOverlay1;

        [SetUp]
        public void SetUp()
        {
            skinSelectorOverlay1 = new SkinSelectorOverlay();
            Add(skinSelectorOverlay1);
            skinSelectorOverlay1.Show();
        }
    }
}
