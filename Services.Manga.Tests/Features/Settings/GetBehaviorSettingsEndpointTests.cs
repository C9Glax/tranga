using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Services.Manga.Features.Settings;

namespace Services.Manga.Tests.Features.Settings;

public class GetBehaviorSettingsEndpointTests : TrangaTest
{
    [Fact]
    public void GetBehaviorSettings_ReflectsCurrentSettings()
    {
        bool original = Common.Settings.Settings.AllowNSFW;
        try
        {
            if (!Common.Settings.Settings.AllowNSFWLocked)
                Common.Settings.Settings.AllowNSFW = true;

            Ok<GetBehaviorSettingsEndpoint.BehaviorSettings> result = GetBehaviorSettingsEndpoint.Handle();

            Assert.Equal(Common.Settings.Settings.AllowNSFW, result.Value!.AllowNsfw);
            Assert.Equal(Common.Settings.Settings.AllowNSFWLocked, result.Value.AllowNsfwLocked);
        }
        finally
        {
            if (!Common.Settings.Settings.AllowNSFWLocked)
                Common.Settings.Settings.AllowNSFW = original;
        }
    }
}
