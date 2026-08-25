using Common.Tests;

namespace Common.Services.Tests;

public class GitInfoResponseTests : TrangaTest
{
    [Fact]
    public void CurrentGitInfo_ReturnsWellFormedResponse()
    {
        GitInfoResponse gitInfo = Service.CurrentGitInfo();

        Assert.False(string.IsNullOrWhiteSpace(gitInfo.Sha));
        Assert.False(string.IsNullOrWhiteSpace(gitInfo.Commit));
        Assert.False(string.IsNullOrWhiteSpace(gitInfo.Branch));
        Assert.False(string.IsNullOrWhiteSpace(gitInfo.Version));
        Assert.False(string.IsNullOrWhiteSpace(gitInfo.CommitDate));
        Assert.StartsWith(gitInfo.Commit, gitInfo.Sha);
    }
}
