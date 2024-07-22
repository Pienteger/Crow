using Crow.Models;
using FluentAssertions;

namespace Crow.Tests;

[TestClass]
public class Configuration_Tests
{
    [TestMethod]
    public void Parse_Should_Return_Valid_Config()
    {
        // Arrange
        string[] strConfig =
        [
            Flags.LF,
            "*csproj",
            Flags.IN,
            "C:\\Users\\Mahmud\\RiderProjects\\",
            Flags.RM,
            "bin",
            "obj",
            "test 1",
            "test2"
        ];

        // Act
        var config = Configuration.Parse(strConfig);
        var expectation = new Configuration
        {
            LookFor = "*csproj",
            RemoveCandidates = ["bin", "obj", "test 1", "test2"],
            LookIns = ["C:\\Users\\Mahmud\\RiderProjects\\"]
        };

        // Assert
        config.Should().BeEquivalentTo(expectation);
    }

    [TestMethod]
    public void Rm_Should_Not_Contain_Dot()
    {
        // Arrange
        string[] strConfig =
        [
            Flags.LF,
            "*csproj",
            Flags.IN,
            "C:\\Users\\Mahmud\\RiderProjects\\",
            Flags.RM,
            "bin",
            ".",
            "ola bonita"
        ];

        // Act
        try
        {
            _ = Configuration.Parse(strConfig);
            throw new Exception();
        }
        catch (Exception e)
        {
            // Assert
            e.Should().BeOfType<ArgumentException>();
        }
    }
}
