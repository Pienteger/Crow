using FluentAssertions;
namespace Crow.Tests;

[TestClass]
public class Utility_Tests
{
    [TestMethod]
    public void GetTimeFromMs_Should_Return_Readable_Time()
    {
        // Arrange
        long ms350 = 350;
        long s2 = 2000;
        long min2 = 1000 * 60 * 2;
        long h3 = (long)TimeSpan.FromHours(3).TotalMilliseconds;

        // Act
        var ms350str = ms350.GetTimeFromMs();
        var s2str = s2.GetTimeFromMs();
        var min2str = min2.GetTimeFromMs();
        var h3str = h3.GetTimeFromMs();

        // Assert
        ms350str.Should().Be("350ms");
        s2str.Should().Be("2s");
        min2str.Should().Be("2min");
        h3str.Should().Be("3h");
    }
}
