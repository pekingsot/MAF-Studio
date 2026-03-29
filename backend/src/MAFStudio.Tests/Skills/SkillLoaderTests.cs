using MAFStudio.Application.Skills;
using MAFStudio.Application.Services;
using Xunit;

namespace MAFStudio.Tests.Skills;

public class SkillLoaderTests : IDisposable
{
    private readonly string _testSkillsDir;
    private readonly SkillLoader _loader;

    public SkillLoaderTests()
    {
        _testSkillsDir = Path.Combine(Path.GetTempPath(), "test_skills_" + Guid.NewGuid());
        Directory.CreateDirectory(_testSkillsDir);
        _loader = new SkillLoader(_testSkillsDir);
    }

    [Fact]
    public async Task LoadSkillAsync_NonExistentPath_ThrowsException()
    {
        var nonExistentPath = Path.Combine(_testSkillsDir, "nonexistent");
        
        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _loader.LoadSkillAsync(nonExistentPath));
        
        Assert.Contains("Skill目录不存在", exception.Message);
    }

    [Fact]
    public async Task LoadSkillAsync_MissingSkillMd_ThrowsException()
    {
        var skillDir = Path.Combine(_testSkillsDir, "test_skill");
        Directory.CreateDirectory(skillDir);

        var exception = await Assert.ThrowsAsync<Exception>(() => 
            _loader.LoadSkillAsync(skillDir));
        
        Assert.Contains("SKILL.md文件不存在", exception.Message);
    }

    [Fact]
    public async Task LoadSkillAsync_ValidSkill_ReturnsSkill()
    {
        var skillDir = Path.Combine(_testSkillsDir, "test_skill");
        Directory.CreateDirectory(skillDir);

        var skillMd = Path.Combine(skillDir, "SKILL.md");
        var content = @"# 测试技能

**描述**: 这是一个测试技能
**版本**: 1.0.0
**作者**: Test Author

## 标签
- 测试
- 示例

## 依赖
- python >= 3.8
";
        await File.WriteAllTextAsync(skillMd, content);

        var skill = await _loader.LoadSkillAsync(skillDir);

        Assert.NotNull(skill);
        Assert.Equal("测试技能", skill.Name);
        Assert.Equal("这是一个测试技能", skill.Description);
        Assert.Equal("1.0.0", skill.Version);
        Assert.Equal("Test Author", skill.Author);
        Assert.Contains("测试", skill.Tags);
        Assert.Contains("示例", skill.Tags);
    }

    [Fact]
    public async Task LoadAllSkillsAsync_ReturnsAllValidSkills()
    {
        var skill1Dir = Path.Combine(_testSkillsDir, "skill1");
        var skill2Dir = Path.Combine(_testSkillsDir, "skill2");
        
        Directory.CreateDirectory(skill1Dir);
        Directory.CreateDirectory(skill2Dir);

        await File.WriteAllTextAsync(Path.Combine(skill1Dir, "SKILL.md"), "# 技能1\n**描述**: 描述1");
        await File.WriteAllTextAsync(Path.Combine(skill2Dir, "SKILL.md"), "# 技能2\n**描述**: 描述2");

        var skills = await _loader.LoadAllSkillsAsync();

        Assert.Equal(2, skills.Count);
        Assert.Contains(skills, s => s.Name == "技能1");
        Assert.Contains(skills, s => s.Name == "技能2");
    }

    [Fact]
    public void GetSkill_AfterLoad_ReturnsSkill()
    {
        var skill = new Skill { Id = "test-id", Name = "Test" };
        _loader.GetType()
            .GetField("_loadedSkills", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(_loader, new Dictionary<string, Skill> { { "test-id", skill } });

        var result = _loader.GetSkill("test-id");

        Assert.NotNull(result);
        Assert.Equal("Test", result.Name);
    }

    [Fact]
    public void UnloadSkill_RemovesSkill()
    {
        var skill = new Skill { Id = "test-id", Name = "Test" };
        _loader.GetType()
            .GetField("_loadedSkills", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.SetValue(_loader, new Dictionary<string, Skill> { { "test-id", skill } });

        var result = _loader.UnloadSkill("test-id");

        Assert.True(result);
        Assert.Null(_loader.GetSkill("test-id"));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testSkillsDir))
        {
            Directory.Delete(_testSkillsDir, true);
        }
    }
}
