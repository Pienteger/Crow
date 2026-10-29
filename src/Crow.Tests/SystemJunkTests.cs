using Crow.Helpers;
using Crow.Models;
using FluentAssertions;

namespace Crow.Tests;

[TestClass]
public class SystemJunkTests
{
    private string root = null!;

    [TestInitialize]
    public void Setup()
    {
        root = Path.Combine(Path.GetTempPath(), "crow-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
    }

    [TestCleanup]
    public void Teardown()
    {
        if (Directory.Exists(root))
            Directory.Delete(root, true);
    }

    [TestMethod]
    public void Parse_Should_Read_Flags()
    {
        var config = SystemJunkConfiguration.Parse(
            ["clean", Flags.DRY_RUN, Flags.ALL, Flags.OLDER_THAN, "48"]);

        config.IsDryRun.Should().BeTrue();
        config.IncludeAll.Should().BeTrue();
        config.IsForceEnabled.Should().BeFalse();
        config.OlderThan.Should().Be(TimeSpan.FromHours(48));
    }

    [TestMethod]
    public void Parse_Should_Reject_Invalid_Age()
    {
        var act = () => SystemJunkConfiguration.Parse(["clean", Flags.OLDER_THAN, "soon"]);

        act.Should().Throw<ArgumentException>();
    }

    [TestMethod]
    public void Clean_Should_Remove_Only_Old_Files_And_Keep_Root()
    {
        var oldFolder = Directory.CreateDirectory(Path.Combine(root, "old"));
        var oldFile = WriteFile(Path.Combine(oldFolder.FullName, "old.tmp"), 100, TimeSpan.FromDays(3));
        var newFile = WriteFile(Path.Combine(root, "new.tmp"), 50, TimeSpan.Zero);
        oldFolder.CreationTimeUtc = DateTime.UtcNow.AddDays(-3);

        var location = new SystemJunkLocation("Test", "Test", [root], TimeSpan.FromDays(1), true);

        var scan = location.Measure(location.MinAge);
        scan.FileCount.Should().Be(1);
        scan.Size.Should().Be(100);

        var result = scan.Clean();

        result.FreedBytes.Should().Be(100);
        result.DeletedCount.Should().Be(1);
        File.Exists(oldFile).Should().BeFalse();
        Directory.Exists(oldFolder.FullName).Should().BeFalse();
        File.Exists(newFile).Should().BeTrue();
        Directory.Exists(root).Should().BeTrue();
    }

    [TestMethod]
    public void Clean_Should_Remove_ReadOnly_Files()
    {
        var file = WriteFile(Path.Combine(root, "locked.tmp"), 10, TimeSpan.FromDays(2));
        File.SetAttributes(file, FileAttributes.ReadOnly);

        var location = new SystemJunkLocation("Test", "Test", [root], TimeSpan.Zero, true);
        var result = location.Measure(TimeSpan.Zero).Clean();

        result.DeletedCount.Should().Be(1);
        File.Exists(file).Should().BeFalse();
    }

    [TestMethod]
    public void Dev_Scan_Should_Find_Nested_Projects_And_Skip_Removal_Targets()
    {
        // A .NET project nested inside a Node.js monorepo.
        File.WriteAllText(Path.Combine(root, "package.json"), "{}");
        var nodeModules = Directory.CreateDirectory(Path.Combine(root, "node_modules"));
        WriteFile(Path.Combine(nodeModules.FullName, "lib.js"), 30, TimeSpan.Zero);
        // A dependency with its own marker must not be reported separately.
        var dependency = Directory.CreateDirectory(Path.Combine(nodeModules.FullName, "dep"));
        File.WriteAllText(Path.Combine(dependency.FullName, "package.json"), "{}");
        Directory.CreateDirectory(Path.Combine(dependency.FullName, "node_modules"));

        var api = Directory.CreateDirectory(Path.Combine(root, "services", "api"));
        File.WriteAllText(Path.Combine(api.FullName, "Api.csproj"), "<Project />");
        var bin = Directory.CreateDirectory(Path.Combine(api.FullName, "bin"));
        WriteFile(Path.Combine(bin.FullName, "Api.dll"), 70, TimeSpan.Zero);
        Directory.CreateDirectory(Path.Combine(api.FullName, "obj"));
        Directory.CreateDirectory(Path.Combine(api.FullName, "src"));

        var config = DevScanConfiguration.Parse($"dev {Flags.IN} {root}");
        var junkYards = config.ScanJunkYards().ToList();

        junkYards.Select(j => j.Path).Should().BeEquivalentTo(
            [nodeModules.FullName, bin.FullName, Path.Combine(api.FullName, "obj")]);
        junkYards.Single(j => j.Path == bin.FullName).Size.Should().Be(70);
        junkYards.Single(j => j.Path == bin.FullName).Label.Should().Be(".NET");
    }

    [TestMethod]
    public void Dev_Parse_Should_Not_Treat_Command_As_Ignore()
    {
        var config = DevScanConfiguration.Parse($"dev {Flags.IN} {root}");

        config.Ignores.Should().BeEmpty();
        config.LookIns.Should().BeEquivalentTo([root]);
    }

    [TestMethod]
    public void Dev_Scan_Should_Respect_Ignores()
    {
        var ignored = Directory.CreateDirectory(Path.Combine(root, "ignored"));
        File.WriteAllText(Path.Combine(ignored.FullName, "Cargo.toml"), "");
        Directory.CreateDirectory(Path.Combine(ignored.FullName, "target"));

        var config = DevScanConfiguration.Parse($"dev {Flags.IN} {root} {Flags.IG} {ignored.FullName}\\");

        config.ScanJunkYards().Should().BeEmpty();
    }

    private static string WriteFile(string path, int bytes, TimeSpan age)
    {
        File.WriteAllBytes(path, new byte[bytes]);
        File.SetLastWriteTimeUtc(path, DateTime.UtcNow - age);
        return path;
    }
}
