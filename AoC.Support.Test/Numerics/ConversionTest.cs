using AoC.Support.Numerics;
using FluentAssertions;
using JetBrains.Annotations;

namespace AoC.Support.Test.Numerics;

[TestFixture]
[TestSubject(typeof(Conversion))]
[TestOf(typeof(Conversion))]
public class ConversionTest {
    
    [Test]
    public void TestConversion(
        [Random(int.MaxValue / 10, int.MaxValue, 20)] long a,
        [Random(int.MaxValue / 100, int.MaxValue / 10, 20)] long b
    ) {
        var concatenated = a.Concatenate(b, 10);
        var concatSlow = long.Parse($"{a}{b}");
        concatenated.Should().Be(concatSlow);
    }
    
    [Test]
    public void TestConversionStack(
        [Random(int.MaxValue / 100, int.MaxValue / 10, 20)] long a,
        [Random(int.MaxValue / 100, int.MaxValue / 10, 20)] long b
    ) {
        var concatenated = a.ConcatenateDecimalChars(b);
        var concatSlow = long.Parse($"{a}{b}");
        concatenated.Should().Be(concatSlow);
    }
    
    [Test]
    public void TestConversionStackBytes(
        [Random(0, int.MaxValue / 10, 20)] long a,
        [Random(0, int.MaxValue / 10, 20)] long b
    ) {
        var concatenated = a.ConcatenateDecimalMagnitude(b);
        var concatSlow = long.Parse($"{a}{b}");
        concatenated.Should().Be(concatSlow);
    }
}