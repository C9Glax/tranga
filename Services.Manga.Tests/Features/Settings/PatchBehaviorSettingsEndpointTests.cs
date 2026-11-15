using Common.Tests;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Services.Manga.Database;
using Services.Manga.Features.Settings;
using Services.Manga.Tests.Helpers;

namespace Services.Manga.Tests.Features.Settings;

// Settings holds static mutable state shared across the whole test process, so each test restores the value it changed.
public class PatchBehaviorSettingsEndpointTests : TrangaTest
{
    [Fact]
    public async Task PatchBehaviorSettings_PersistsAndAppliesValue()
    {
        Assert.SkipWhen(Common.Settings.Settings.AllowNSFWLocked, "AllowNSFW is locked by the environment variable.");
        bool original = Common.Settings.Settings.AllowNSFW;
        try
        {
            await using MangaContext context = MangaContextFactory.Create();

            Results<Ok, Conflict<string>> result = await PatchBehaviorSettingsEndpoint.Handle(
                context, new PatchBehaviorSettingsEndpoint.PatchBehaviorSettingsRequest(AllowNsfw: true), ct);

            Assert.IsType<Ok>(result.Result);
            Assert.True(Common.Settings.Settings.AllowNSFW);
            Assert.Equal("True", (await context.Settings.SingleAsync(s => s.Key == DbSetting.AllowNsfwKey, ct)).Value);

            result = await PatchBehaviorSettingsEndpoint.Handle(
                context, new PatchBehaviorSettingsEndpoint.PatchBehaviorSettingsRequest(AllowNsfw: false), ct);

            Assert.IsType<Ok>(result.Result);
            Assert.False(Common.Settings.Settings.AllowNSFW);
            Assert.Equal("False", (await context.Settings.SingleAsync(s => s.Key == DbSetting.AllowNsfwKey, ct)).Value);
        }
        finally
        {
            Common.Settings.Settings.AllowNSFW = original;
        }
    }

    [Fact]
    public async Task PatchBehaviorSettings_WithoutValue_ChangesNothing()
    {
        await using MangaContext context = MangaContextFactory.Create();
        bool original = Common.Settings.Settings.AllowNSFW;

        Results<Ok, Conflict<string>> result = await PatchBehaviorSettingsEndpoint.Handle(
            context, new PatchBehaviorSettingsEndpoint.PatchBehaviorSettingsRequest(AllowNsfw: null), ct);

        Assert.IsType<Ok>(result.Result);
        Assert.Equal(original, Common.Settings.Settings.AllowNSFW);
        Assert.Empty(await context.Settings.ToListAsync(ct));
    }

    [Fact]
    public async Task PatchBehaviorSettings_Returns409WhenLockedByEnvironmentVariable()
    {
        Assert.SkipUnless(Common.Settings.Settings.AllowNSFWLocked, "AllowNSFW is not set through the environment variable.");
        await using MangaContext context = MangaContextFactory.Create();
        bool original = Common.Settings.Settings.AllowNSFW;

        Results<Ok, Conflict<string>> result = await PatchBehaviorSettingsEndpoint.Handle(
            context, new PatchBehaviorSettingsEndpoint.PatchBehaviorSettingsRequest(AllowNsfw: !original), ct);

        Assert.IsType<Conflict<string>>(result.Result);
        Assert.Equal(original, Common.Settings.Settings.AllowNSFW);
        Assert.Empty(await context.Settings.ToListAsync(ct));
    }
}
