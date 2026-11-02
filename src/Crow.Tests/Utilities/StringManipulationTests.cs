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

    [TestMethod]
    public void RepairQuotedArgs_ShouldKeepSpacesInsideAnArg()
    {
        string[] args = ["dev", "--in", @"C:\My Projects"];
        args.RepairQuotedArgs().Should().Equal("dev", "--in", @"C:\My Projects");
    }

    [TestMethod]
    public void RepairQuotedArgs_ShouldDropQuoteLeftByTrailingBackslash()
    {
        // What Windows passes for: crow dev --in "C:\My Projects\"
        string[] args = ["dev", "--in", @"C:\My Projects\"""];
        args.RepairQuotedArgs().Should().Equal("dev", "--in", @"C:\My Projects\");
    }

    [TestMethod]
    public void RepairQuotedArgs_ShouldSplitFlagsSwallowedByTrailingBackslash()
    {
        // What Windows passes for: crow dev --in "C:\My Projects\" --ig foo --force
        string[] args = ["dev", "--in", @"C:\My Projects\"" --ig foo --force"];
        args.RepairQuotedArgs().Should().Equal("dev", "--in", @"C:\My Projects\", "--ig", "foo", "--force");
    }

    [TestMethod]
    public void RepairQuotedArgs_ShouldDropEmptyArgs()
    {
        string[] args = ["dev", "--in", "", " "];
        args.RepairQuotedArgs().Should().Equal("dev", "--in");
    }
}
