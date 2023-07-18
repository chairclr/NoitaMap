using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;

namespace NoitaMap.Game.Tests.Visual;

[TestFixture]
public partial class TestSceneFastLZ : NoitaMapTestScene
{
    public TestSceneFastLZ()
    {
        Add(new SpriteText
        {
            Anchor = Anchor.TopLeft,
            Font = FontUsage.Default.With(size: 26),
            Y = 10,
            Text = "hi"
        });
    }
}
