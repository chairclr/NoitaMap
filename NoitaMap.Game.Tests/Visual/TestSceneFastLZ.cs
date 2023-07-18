using osu.Framework.Graphics;
using NUnit.Framework;
using osu.Framework.Graphics.Sprites;
using NoitaMap.Game.Compression;

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
