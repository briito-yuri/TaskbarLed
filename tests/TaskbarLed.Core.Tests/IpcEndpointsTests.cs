using TaskbarLed.Core.Ipc;

namespace TaskbarLed.Core.Tests;

public class IpcEndpointsTests
{
    [Fact]
    public void Packaged_spool_mirrors_the_normal_spool_inside_the_package_local_cache()
    {
        var package = Path.Combine("C:", "Users", "me", "AppData", "Local", "Packages", "Claude_abc");

        var spool = IpcEndpoints.PackagedSpoolDirectory(package);

        Assert.Equal(Path.Combine(package, "LocalCache", "Local", "TaskbarLed", "spool"), spool);
        Assert.EndsWith(Path.Combine("TaskbarLed", "spool"), IpcEndpoints.SpoolDirectory, StringComparison.Ordinal);
    }
}
