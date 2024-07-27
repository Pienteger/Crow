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
        string value = "Banana";
        int result = array.IndexOf(value);
        result.Should().Be(1);
    }

    [TestMethod]
    public void IndexOf_ShouldReturnNegativeOne_WhenValueDoesNotExistInArray()
    {
        string[] array = ["apple", "banana", "cherry"];
        string value = "grape";
        int result = array.IndexOf(value);
        result.Should().Be(-1);
    }

    [TestMethod]
    public void IndexOf_ShouldReturnNegativeOne_WhenArrayIsNull()
    {
        string[] array = null;
        string value = "banana";
        int result = array.IndexOf(value);
        result.Should().Be(-1);
    }

    [TestMethod]
    public void IndexOf_ShouldReturnNegativeOne_WhenArrayIsEmpty()
    {
        string[] array = [];
        string value = "banana";
        int result = array.IndexOf(value);
        result.Should().Be(-1);
    }
}
