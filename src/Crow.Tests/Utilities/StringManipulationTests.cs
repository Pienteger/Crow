using Crow.Utilities;
using FluentAssertions;

namespace Crow.Tests.Utilities;

[TestClass]
public class StringManipulationTests
{
    [TestMethod]
    public void RemoveWrappingQuotes_ShouldRemoveDoubleQuotes()
    {
        string text = "\"Hello\"";
        string result = text.RemoveWrappingQuotes();
        result.Should().Be("Hello");
    }

    [TestMethod]
    public void RemoveWrappingQuotes_ShouldRemoveSingleQuotes()
    {
        string text = "'Hello'";
        string result = text.RemoveWrappingQuotes();
        result.Should().Be("Hello");
    }

    [TestMethod]
    public void RemoveWrappingQuotes_ShouldReturnOriginalText_WhenNoWrappingQuotes()
    {
        string text = "Hello";
        string result = text.RemoveWrappingQuotes();
        result.Should().Be("Hello");
    }

    [TestMethod]
    public void RemoveWrappingQuotes_ShouldHandleEmptyString()
    {
        string text = "";
        string result = text.RemoveWrappingQuotes();
        result.Should().Be("");
    }

    [TestMethod]
    public void RemoveWrappingQuotes_ShouldHandleSingleCharacterString()
    {
        string text = "\"";
        string result = text.RemoveWrappingQuotes();
        result.Should().Be("\"");
    }
}
