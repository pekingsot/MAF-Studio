using MAFStudio.Application.Capabilities;
using Xunit;

namespace MAFStudio.Tests.Capabilities;

public class FileCapabilityTests
{
    private readonly FileCapability _capability;

    public FileCapabilityTests()
    {
        _capability = new FileCapability();
    }

    [Fact]
    public void TestWriteAndReadFile()
    {
        var testFile = Path.Combine(Path.GetTempPath(), "test_file.txt");
        var testContent = "Hello, MAF Studio!";

        var writeResult = _capability.WriteFile(testFile, testContent);
        Assert.Contains("成功写入文件", writeResult);

        var readResult = _capability.ReadFile(testFile);
        Assert.Equal(testContent, readResult);

        File.Delete(testFile);
    }

    [Fact]
    public void TestCreateAndDeleteDirectory()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "test_dir_" + Guid.NewGuid());

        var createResult = _capability.CreateDirectory(testDir);
        Assert.Contains("成功创建目录", createResult);
        Assert.True(Directory.Exists(testDir));

        var deleteResult = _capability.DeleteDirectory(testDir);
        Assert.Contains("成功删除目录", deleteResult);
        Assert.False(Directory.Exists(testDir));
    }

    [Fact]
    public void TestListFiles()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "test_list_" + Guid.NewGuid());
        Directory.CreateDirectory(testDir);

        File.WriteAllText(Path.Combine(testDir, "file1.txt"), "content1");
        File.WriteAllText(Path.Combine(testDir, "file2.txt"), "content2");

        var listResult = _capability.ListFiles(testDir);
        Assert.Contains("file1.txt", listResult);
        Assert.Contains("file2.txt", listResult);

        Directory.Delete(testDir, true);
    }

    [Fact]
    public void TestCopyFile()
    {
        var sourceFile = Path.Combine(Path.GetTempPath(), "source.txt");
        var destFile = Path.Combine(Path.GetTempPath(), "dest.txt");

        File.WriteAllText(sourceFile, "test content");

        var copyResult = _capability.CopyFile(sourceFile, destFile);
        Assert.Contains("成功复制文件", copyResult);
        Assert.True(File.Exists(destFile));

        File.Delete(sourceFile);
        File.Delete(destFile);
    }

    [Fact]
    public void TestMoveFile()
    {
        var sourceFile = Path.Combine(Path.GetTempPath(), "source_move.txt");
        var destFile = Path.Combine(Path.GetTempPath(), "dest_move.txt");

        File.WriteAllText(sourceFile, "test content");

        var moveResult = _capability.MoveFile(sourceFile, destFile);
        Assert.Contains("成功移动文件", moveResult);
        Assert.False(File.Exists(sourceFile));
        Assert.True(File.Exists(destFile));

        File.Delete(destFile);
    }
}
