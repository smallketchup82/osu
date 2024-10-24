// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Game.Graphics.UserInterface;

namespace osu.Game.Overlays.SkinSelector
{
    public partial class SkinSearchTextBox : BasicSearchTextBox
    {
        protected override bool AllowCommit => true;
    }
}
