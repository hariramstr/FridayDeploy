using FridayDeploy.Web.Helpers;

namespace FridayDeploy.Tests.Helpers;

public sealed class ApplicationHealthHelperTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    [Theory]
    [InlineData(0, "Online")]
    [InlineData(1, "Online")]
    [InlineData(5, "Warning")]
    [InlineData(9, "Warning")]
    [InlineData(11, "Offline")]
    [InlineData(60, "Offline")]
    public void FromLastSeen_maps_age_in_minutes_to_status(int minutesAgo, string expected)
    {
        var lastSeen = Now.AddMinutes(-minutesAgo);

        Assert.Equal(expected, ApplicationHealthHelper.FromLastSeen(lastSeen, Now));
    }

    [Theory]
    [InlineData("Online", "level-success")]
    [InlineData("Warning", "level-warning")]
    [InlineData("Offline", "level-muted")]
    public void CssClass_maps_status_to_css(string status, string expectedCss)
    {
        Assert.Equal(expectedCss, ApplicationHealthHelper.CssClass(status));
    }
}
