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
        string strConfig =
            $"{Flags.LF} *csproj {Flags.IN} \"C:\\Users\\Mahmud\\RiderProjects\\\" {Flags.RM} \"test 1\" bin obj test2";

        // Act
        var config = Configuration.Parse(strConfig);
        var expectation = new Configuration
        {
            LookFor = "*csproj",
            RemoveCandidates = ["test 1", "bin", "obj", "test2"],
            LookIns = ["C:\\Users\\Mahmud\\RiderProjects\\"]
        };

        // Assert
        config.Should().BeEquivalentTo(expectation);
    }

    [TestMethod]
    public void Rm_Should_Not_Contain_Dot()
    {
        string strConfig =
            $"{Flags.LF} *csproj {Flags.IN} \"C:\\Users\\Mahmud\\RiderProjects\\\" {Flags.RM} \"test 1\" bin . test2";

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
