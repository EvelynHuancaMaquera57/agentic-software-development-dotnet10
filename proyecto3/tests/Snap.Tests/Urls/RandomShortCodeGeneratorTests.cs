using Snap.Modules.Urls;

namespace Snap.Tests.Urls;

public sealed class RandomShortCodeGeneratorTests
{
    [Fact]
    public void Generate_ReturnsBase62CodeOfExpectedLength()
    {
        var code = new RandomShortCodeGenerator().Generate();

        Assert.Equal(RandomShortCodeGenerator.Length, code.Length);
        Assert.All(code, c => Assert.Contains(c, RandomShortCodeGenerator.Alphabet));
    }

    [Fact]
    public void Generate_ProducesDifferentCodes()
    {
        var generator = new RandomShortCodeGenerator();

        var codes = Enumerable.Range(0, 1_000).Select(_ => generator.Generate()).ToHashSet();

        Assert.Equal(1_000, codes.Count);
    }
}
