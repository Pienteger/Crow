using Crow.Utilities;
using FluentAssertions;

namespace Crow.Tests.Utilities;

[TestClass]
public class UnitConversionTests
{
    [TestMethod]
    public void ToUnitText_ShouldReturnBytes_WhenSizeIsLessThan1024()
    {
        long size = 500;
        string result = size.ToUnitText();
        result.Should().Be("500 B");
    }

    [TestMethod]
    public void ToUnitText_ShouldReturnKiB_WhenSizeIsLessThan1MiB()
    {
        long size = 2048;
        string result = size.ToUnitText();
        result.Should().Be("2 KiB");
    }

    [TestMethod]
    public void ToUnitText_ShouldReturnMiB_WhenSizeIsLessThan1GiB()
    {
        long size = 2_097_152; // 2 MiB
        string result = size.ToUnitText();
        result.Should().Be("2 MiB");
    }

    [TestMethod]
    public void ToUnitText_ShouldReturnGiB_WhenSizeIsGreaterThanOrEqualTo1GiB()
    {
        long size = 1_073_741_824; // 1 GiB
        string result = size.ToUnitText();
        result.Should().Be("1 GiB");
    }
}
