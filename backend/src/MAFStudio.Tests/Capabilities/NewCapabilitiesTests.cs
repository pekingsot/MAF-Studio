using MAFStudio.Application.Capabilities;
using Xunit;

namespace MAFStudio.Tests.Capabilities;

public class NewCapabilitiesTests
{
    [Fact]
    public void WebCapability_ShouldHaveCorrectTools()
    {
        var capability = new WebCapability();
        
        Assert.Equal("WebCapability", capability.Name);
        Assert.Contains("网络请求能力", capability.Description);
        
        var tools = capability.GetTools().ToList();
        Assert.True(tools.Count >= 6, $"WebCapability 应该有至少 6 个工具，实际: {tools.Count}");
        
        var toolNames = tools.Select(t => t.Name).ToList();
        Assert.Contains("HttpGetAsync", toolNames);
        Assert.Contains("HttpPostAsync", toolNames);
        Assert.Contains("FetchUrlAsync", toolNames);
        Assert.Contains("DownloadFileAsync", toolNames);
        Assert.Contains("CheckUrlAsync", toolNames);
    }

    [Fact]
    public async Task WebCapability_CheckUrl_ShouldWork()
    {
        var capability = new WebCapability();
        
        var result = await capability.CheckUrlAsync("https://www.baidu.com");
        
        Assert.Contains("IsAccessible", result);
    }

    [Fact]
    public void CodeCapability_ShouldHaveCorrectTools()
    {
        var capability = new CodeCapability();
        
        Assert.Equal("CodeCapability", capability.Name);
        Assert.Contains("代码操作能力", capability.Description);
        
        var tools = capability.GetTools().ToList();
        Assert.True(tools.Count >= 5, $"CodeCapability 应该有至少 5 个工具，实际: {tools.Count}");
        
        var toolNames = tools.Select(t => t.Name).ToList();
        Assert.Contains("AnalyzeCode", toolNames);
        Assert.Contains("SearchInCode", toolNames);
        Assert.Contains("GetCodeMetrics", toolNames);
        Assert.Contains("FindDefinitions", toolNames);
        Assert.Contains("ExtractComments", toolNames);
    }

    [Fact]
    public void CodeCapability_AnalyzeCode_ShouldWork()
    {
        var capability = new CodeCapability();
        var testFile = Path.Combine(Path.GetTempPath(), "test_code.cs");
        
        File.WriteAllText(testFile, @"
using System;

namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod() { }
    }
}
");

        try
        {
            var result = capability.AnalyzeCode(testFile);
            
            Assert.Contains("C#", result);
            Assert.Contains("类数量", result);
        }
        finally
        {
            File.Delete(testFile);
        }
    }

    [Fact]
    public void CodeCapability_GetCodeMetrics_ShouldWork()
    {
        var capability = new CodeCapability();
        var testDir = Path.Combine(Path.GetTempPath(), "test_metrics_" + Guid.NewGuid().ToString("N")[..8]);
        
        Directory.CreateDirectory(testDir);
        File.WriteAllText(Path.Combine(testDir, "test1.cs"), "using System;\nclass Test1 { }");
        File.WriteAllText(Path.Combine(testDir, "test2.py"), "import os\nclass Test2:\n    pass");

        try
        {
            var result = capability.GetCodeMetrics(testDir);
            
            Assert.Contains("代码统计信息", result);
            Assert.Contains("C#", result);
            Assert.Contains("Python", result);
        }
        finally
        {
            Directory.Delete(testDir, true);
        }
    }

    [Fact]
    public void SearchCapability_ShouldHaveCorrectTools()
    {
        var capability = new SearchCapability();
        
        Assert.Equal("SearchCapability", capability.Name);
        Assert.Contains("搜索能力", capability.Description);
        
        var tools = capability.GetTools().ToList();
        Assert.True(tools.Count >= 6, $"SearchCapability 应该有至少 6 个工具，实际: {tools.Count}");
        
        var toolNames = tools.Select(t => t.Name).ToList();
        Assert.Contains("SearchInFiles", toolNames);
        Assert.Contains("Grep", toolNames);
        Assert.Contains("FindFiles", toolNames);
        Assert.Contains("FindDirectories", toolNames);
        Assert.Contains("FindRecentlyModified", toolNames);
        Assert.Contains("FindLargeFiles", toolNames);
    }

    [Fact]
    public void SearchCapability_SearchInFiles_ShouldWork()
    {
        var capability = new SearchCapability();
        var testDir = Path.Combine(Path.GetTempPath(), "test_search_" + Guid.NewGuid().ToString("N")[..8]);
        
        Directory.CreateDirectory(testDir);
        File.WriteAllText(Path.Combine(testDir, "test.txt"), "Hello World\nThis is a test\nHello Again");

        try
        {
            var result = capability.SearchInFiles("Hello", testDir);
            
            Assert.Contains("匹配结果", result);
            Assert.Contains("Hello", result);
        }
        finally
        {
            Directory.Delete(testDir, true);
        }
    }

    [Fact]
    public void SearchCapability_FindFiles_ShouldWork()
    {
        var capability = new SearchCapability();
        var testDir = Path.Combine(Path.GetTempPath(), "test_find_" + Guid.NewGuid().ToString("N")[..8]);
        
        Directory.CreateDirectory(testDir);
        File.WriteAllText(Path.Combine(testDir, "file1.txt"), "content1");
        File.WriteAllText(Path.Combine(testDir, "file2.txt"), "content2");
        File.WriteAllText(Path.Combine(testDir, "file3.md"), "content3");

        try
        {
            var result = capability.FindFiles("*.txt", testDir);
            
            Assert.Contains("找到文件", result);
            Assert.Contains("file1.txt", result);
            Assert.Contains("file2.txt", result);
        }
        finally
        {
            Directory.Delete(testDir, true);
        }
    }

    [Fact]
    public void ArchiveCapability_ShouldHaveCorrectTools()
    {
        var capability = new ArchiveCapability();
        
        Assert.Equal("ArchiveCapability", capability.Name);
        Assert.Contains("压缩解压能力", capability.Description);
        
        var tools = capability.GetTools().ToList();
        Assert.True(tools.Count >= 6, $"ArchiveCapability 应该有至少 6 个工具，实际: {tools.Count}");
        
        var toolNames = tools.Select(t => t.Name).ToList();
        Assert.Contains("ExtractZip", toolNames);
        Assert.Contains("CreateZip", toolNames);
        Assert.Contains("ListArchive", toolNames);
        Assert.Contains("AddToZip", toolNames);
        Assert.Contains("VerifyZip", toolNames);
        Assert.Contains("GetZipInfo", toolNames);
    }

    [Fact]
    public void ArchiveCapability_CreateAndListZip_ShouldWork()
    {
        var capability = new ArchiveCapability();
        var testDir = Path.Combine(Path.GetTempPath(), "test_archive_" + Guid.NewGuid().ToString("N")[..8]);
        var testFile = Path.Combine(testDir, "test.txt");
        var zipFile = Path.Combine(Path.GetTempPath(), "test_" + Guid.NewGuid().ToString("N")[..8] + ".zip");
        
        Directory.CreateDirectory(testDir);
        File.WriteAllText(testFile, "Test content for zip file");

        try
        {
            var createResult = capability.CreateZip(testDir, zipFile);
            Assert.Contains("Success", createResult);
            Assert.True(File.Exists(zipFile));
            
            var listResult = capability.ListArchive(zipFile);
            Assert.Contains("TotalEntries", listResult);
            Assert.Contains("test.txt", listResult);
            
            var infoResult = capability.GetZipInfo(zipFile);
            Assert.Contains("FileCount", infoResult);
        }
        finally
        {
            if (File.Exists(zipFile)) File.Delete(zipFile);
            if (Directory.Exists(testDir)) Directory.Delete(testDir, true);
        }
    }

    [Fact]
    public void ArchiveCapability_ExtractZip_ShouldWork()
    {
        var capability = new ArchiveCapability();
        var testDir = Path.Combine(Path.GetTempPath(), "test_extract_" + Guid.NewGuid().ToString("N")[..8]);
        var extractDir = Path.Combine(Path.GetTempPath(), "test_extracted_" + Guid.NewGuid().ToString("N")[..8]);
        var testFile = Path.Combine(testDir, "test.txt");
        var zipFile = Path.Combine(Path.GetTempPath(), "test_extract_" + Guid.NewGuid().ToString("N")[..8] + ".zip");
        
        Directory.CreateDirectory(testDir);
        File.WriteAllText(testFile, "Test content for extraction");

        try
        {
            capability.CreateZip(testDir, zipFile);
            
            var extractResult = capability.ExtractZip(zipFile, extractDir);
            Assert.Contains("Success", extractResult);
            Assert.True(Directory.Exists(extractDir));
            Assert.True(File.Exists(Path.Combine(extractDir, "test.txt")));
        }
        finally
        {
            if (File.Exists(zipFile)) File.Delete(zipFile);
            if (Directory.Exists(testDir)) Directory.Delete(testDir, true);
            if (Directory.Exists(extractDir)) Directory.Delete(extractDir, true);
        }
    }

    [Fact]
    public void CapabilityManager_ShouldRegisterAllNewCapabilities()
    {
        var manager = new CapabilityManager();
        
        var capabilities = manager.GetAllCapabilities().ToList();
        
        Assert.Contains(capabilities, c => c.Name == "WebCapability");
        Assert.Contains(capabilities, c => c.Name == "CodeCapability");
        Assert.Contains(capabilities, c => c.Name == "SearchCapability");
        Assert.Contains(capabilities, c => c.Name == "ArchiveCapability");
    }

    [Fact]
    public void CapabilityManager_ShouldHaveMoreToolsAfterAddingNewCapabilities()
    {
        var manager = new CapabilityManager();
        
        var tools = manager.GetAllTools().ToList();
        
        Assert.True(tools.Count >= 50, $"应该有至少 50 个工具，实际: {tools.Count}");
        
        var toolNames = tools.Select(t => t.Name).ToList();
        
        Assert.Contains("HttpGetAsync", toolNames);
        Assert.Contains("AnalyzeCode", toolNames);
        Assert.Contains("SearchInFiles", toolNames);
        Assert.Contains("CreateZip", toolNames);
    }
}
