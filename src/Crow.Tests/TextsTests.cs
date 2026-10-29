using FluentAssertions;
using Spectre.Console;

namespace Crow.Tests;

[TestClass]
public class TextsTests
{
    // Spectre reads [...] as a style, so a stray bracket breaks the whole message at runtime.
    [TestMethod]
    public void All_Texts_Should_Be_Valid_Markup()
    {
        var texts = typeof(Texts)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Select(f => (f.Name, Value: (string)f.GetValue(null)!));

        foreach (var (name, value) in texts)
        {
            var act = () => new Markup(value);
            act.Should().NotThrow($"{name} must be valid Spectre markup");
        }
    }
}
