using Crow.Utilities;
using FluentAssertions;

namespace Crow.Tests.Utilities;

[TestClass]
public class TimeConversionTests
{
    [TestMethod]
    public void ToTimeText_ShouldReturnMilliseconds_WhenTimeIsLessThan1000()
    {
        long ms = 500;
        string result = ms.ToTimeText();
        result.Should().Be("500ms");
    }

    [TestMethod]
    public void ToTimeText_ShouldReturnSeconds_WhenTimeIsLessThan1Minute()
    {
        long ms = 30_000;
        string result = ms.ToTimeText();
        result.Should().Be("30s");
    }

    [TestMethod]
    public void ToTimeText_ShouldReturnMinutes_WhenTimeIsLessThan1Hour()
    {
        long ms = 1_800_000; // 30 minutes
        string result = ms.ToTimeText();
        result.Should().Be("30min");
    }

    [TestMethod]
    public void ToTimeText_ShouldReturnHours_WhenTimeIsGreaterThanOrEqualTo1Hour()
    {
        long ms = 3_600_000; // 1 hour
        string result = ms.ToTimeText();
        result.Should().Be("1h");
    }
}
