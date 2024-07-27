using Crow.Utilities;
using FluentAssertions;

namespace Crow.Tests.Utilities;

[TestClass]
public class StringComparisonTests
{
    [TestMethod]
    public void IsEqual_ShouldReturnTrue_ForEqualStringsIgnoringCase()
    {
        string str1 = "Hello";
        string str2 = "hello";
        bool result = str1.IsEqual(str2);
        result.Should().BeTrue();
    }

    [TestMethod]
    public void IsEqual_ShouldReturnFalse_ForNonEqualStrings()
    {
        string str1 = "Hello";
        string str2 = "world";
        bool result = str1.IsEqual(str2);
        result.Should().BeFalse();
    }

    [TestMethod]
    public void IsEqual_ShouldReturnTrue_ForExactlyEqualStrings()
    {
        string str1 = "Hello";
        string str2 = "Hello";
        bool result = str1.IsEqual(str2);
        result.Should().BeTrue();
    }
}
