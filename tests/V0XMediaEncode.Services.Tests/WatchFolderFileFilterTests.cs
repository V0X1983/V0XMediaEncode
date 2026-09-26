using V0XMediaEncode.Services.WatchFolders;

namespace V0XMediaEncode.Services.Tests;

public class WatchFolderFileFilterTests
{
    [Theory]
    [InlineData(@"C:\watch\clip.mov")]
    [InlineData(@"C:\watch\Interview Final.MP4")]
    [InlineData(@"C:\watch\subdir\audio.wav")]
    public void IsCandidateFile_AcceptsOrdinaryMediaFiles(string path)
    {
        Assert.True(WatchFolderFileFilter.IsCandidateFile(path));
    }

    [Theory]
    [InlineData(@"C:\watch\export.mp4.tmp")]
    [InlineData(@"C:\watch\video.part")]
    [InlineData(@"C:\watch\movie.crdownload")]
    [InlineData(@"C:\watch\.DS_Store")]
    [InlineData(@"C:\watch\~$report.docx")]
    public void IsCandidateFile_RejectsInProgressOrHiddenFiles(string path)
    {
        Assert.False(WatchFolderFileFilter.IsCandidateFile(path));
    }

    [Fact]
    public void IsCandidateFile_RejectsEmptyFileName()
    {
        Assert.False(WatchFolderFileFilter.IsCandidateFile(@"C:\watch\"));
    }
}
