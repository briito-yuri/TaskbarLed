namespace TaskbarLed.Platform.Mac.Tests;

public class AntigravityPresenceTests
{
    [Fact]
    public void A_held_lock_is_detected_and_a_released_lock_is_cleared()
    {
        if (!OperatingSystem.IsMacOS())
        {
            return;
        }

        var path = Path.GetTempFileName();
        try
        {
            using (File.OpenHandle(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            {
                Assert.True(AntigravityPresence.IsAttached(path));
            }

            Assert.True(AntigravityPresence.WasReleased(path));
        }
        finally
        {
            File.Delete(path);
        }
    }
}
