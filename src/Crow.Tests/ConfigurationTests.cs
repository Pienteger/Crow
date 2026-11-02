using Crow.Models;
using FluentAssertions;

namespace Crow.Tests;

[TestClass]
public class ConfigurationTests
{
    [TestMethod]
    public void Parse_Should_Return_Valid_Config()
    {
        // Arrange
        const string strConfig =
            $"{Flags.LF} *csproj {Flags.IN} \"C:\\Users\\Mahmud\\RiderProjects\\\" {Flags.RM} \"test 1\" bin obj test2";

        // Act
        var config = ScanDeleteConfiguration.Parse(strConfig);
        var expectation = new ScanDeleteConfiguration
        {
            LookFor = "*csproj",
            RemoveCandidates = ["test 1", "bin", "obj", "test2"],
            LookIns = [@"C:\Users\Mahmud\RiderProjects\"],
            Ignores = []
        };

        // Assert
        config.Should().BeEquivalentTo(expectation);
    }

    [TestMethod]
    public void Rm_Should_Not_Contain_Dot()
    {
        const string strConfig =
            $"{Flags.LF} *csproj {Flags.IN} \"C:\\Users\\Mahmud\\RiderProjects\\\" {Flags.RM} \"test 1\" bin . test2";

        // Act
        try
        {
            _ = ScanDeleteConfiguration.Parse(strConfig);
            throw new Exception();
        }
        catch (Exception e)
        {
            // Assert
            e.Should().BeOfType<ArgumentException>();
        }
    }

    [TestMethod]
    public void Parse_Should_Keep_Spaces_In_Path_Args()
    {
        string[] args = [Flags.LF, "*.csproj", Flags.IN, @"C:\My Projects", Flags.RM, "bin", Flags.IG, @"C:\My Projects\Old Stuff"];

        var config = ScanDeleteConfiguration.Parse(args);

        config.LookIns.Should().Equal(@"C:\My Projects");
        config.Ignores.Should().Equal(@"C:\My Projects\Old Stuff");
    }

    [TestMethod]
    public void Dev_Parse_Should_Keep_Spaces_In_Path_Args()
    {
        string[] args = ["dev", Flags.IN, @"C:\My Projects", Flags.FORCE];

        var config = DevScanConfiguration.Parse(args);

        config.LookIns.Should().Equal(@"C:\My Projects");
        config.IsForceEnabled.Should().BeTrue();
    }
}
