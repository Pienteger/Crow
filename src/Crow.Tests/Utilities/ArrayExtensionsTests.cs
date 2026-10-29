using Crow.Utilities;
using FluentAssertions;

namespace Crow.Tests.Utilities;

[TestClass]
public class ArrayExtensionsTests
{
    [TestMethod]
    public void IndexOf_ShouldReturnCorrectIndex_WhenValueExistsInArray()
    {
        string[] array = ["apple", "banana", "cherry"];
        var value = "Banana";
        var result = array.IndexOf(value);
        result.Should().Be(1);
    }

    [TestMethod]
    public void IndexOf_ShouldReturnNegativeOne_WhenValueDoesNotExistInArray()
    {
        string[] array = ["apple", "banana", "cherry"];
        var value = "grape";
        var result = array.IndexOf(value);
        result.Should().Be(-1);
    }

    [TestMethod]
    public void IndexOf_ShouldReturnNegativeOne_WhenArrayIsNull()
    {
        string[] array = null;
        var value = "banana";
        var result = array.IndexOf(value);
        result.Should().Be(-1);
    }

    [TestMethod]
    public void IndexOf_ShouldReturnNegativeOne_WhenArrayIsEmpty()
    {
        string[] array = [];
        var value = "banana";
        var result = array.IndexOf(value);
        result.Should().Be(-1);
    }
}
