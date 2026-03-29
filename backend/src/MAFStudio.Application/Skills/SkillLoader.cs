using System.Text.Json;
using System.Text.RegularExpressions;
using MAFStudio.Application.Services;

namespace MAFStudio.Application.Skills;

public class SkillLoader
{
    private readonly string _skillsDirectory;
    private readonly Dictionary<string, Skill> _loadedSkills = new();

    public SkillLoader(string? skillsDirectory = null)
    {
        _skillsDirectory = skillsDirectory ?? Path.Combine(Directory.GetCurrentDirectory(), "skills");
        
        if (!Directory.Exists(_skillsDirectory))
        {
            Directory.CreateDirectory(_skillsDirectory);
        }
    }

    public async Task<Skill?> LoadSkillAsync(string skillPath)
    {
        try
        {
            if (!Directory.Exists(skillPath))
            {
                throw new NotFoundException($"Skill目录不存在：{skillPath}");
            }

            var manifestPath = Path.Combine(skillPath, "SKILL.md");
            if (!File.Exists(manifestPath))
            {
                throw new NotFoundException($"SKILL.md文件不存在：{manifestPath}");
            }

            var manifestContent = await File.ReadAllTextAsync(manifestPath);
            var manifest = ParseManifest(manifestContent);

            var skill = new Skill
            {
                Id = Guid.NewGuid().ToString(),
                Name = manifest.Name,
                Description = manifest.Description,
                Version = manifest.Version,
                Author = manifest.Author,
                Path = skillPath,
                Tags = manifest.Tags,
                Dependencies = manifest.Dependencies,
                EntryPoint = manifest.EntryPoint ?? "main.py",
                Runtime = manifest.Runtime ?? "python",
                Parameters = manifest.Parameters,
                LoadedAt = DateTime.UtcNow
            };

            _loadedSkills[skill.Id] = skill;
            return skill;
        }
        catch (Exception ex)
        {
            throw new Exception($"加载Skill失败：{ex.Message}", ex);
        }
    }

    public async Task<List<Skill>> LoadAllSkillsAsync()
    {
        var skills = new List<Skill>();
        
        if (!Directory.Exists(_skillsDirectory))
        {
            return skills;
        }

        var skillDirectories = Directory.GetDirectories(_skillsDirectory);
        
        foreach (var dir in skillDirectories)
        {
            try
            {
                var skill = await LoadSkillAsync(dir);
                if (skill != null)
                {
                    skills.Add(skill);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载Skill失败 {dir}: {ex.Message}");
            }
        }

        return skills;
    }

    public Skill? GetSkill(string skillId)
    {
        return _loadedSkills.TryGetValue(skillId, out var skill) ? skill : null;
    }

    public List<Skill> GetAllLoadedSkills()
    {
        return _loadedSkills.Values.ToList();
    }

    public bool UnloadSkill(string skillId)
    {
        return _loadedSkills.Remove(skillId);
    }

    private SkillManifest ParseManifest(string manifestContent)
    {
        var manifest = new SkillManifest();
        var lines = manifestContent.Split('\n');

        var currentSection = "";
        var currentList = new List<string>();

        foreach (var line in lines)
        {
            var trimmed = line.Trim();

            if (trimmed.StartsWith("# "))
            {
                manifest.Name = trimmed.Substring(2);
            }
            else if (trimmed.StartsWith("**描述**:"))
            {
                manifest.Description = trimmed.Substring("**描述**:".Length).Trim();
            }
            else if (trimmed.StartsWith("**版本**:"))
            {
                manifest.Version = trimmed.Substring("**版本**:".Length).Trim();
            }
            else if (trimmed.StartsWith("**作者**:"))
            {
                manifest.Author = trimmed.Substring("**作者**:".Length).Trim();
            }
            else if (trimmed.StartsWith("**入口**:"))
            {
                manifest.EntryPoint = trimmed.Substring("**入口**:".Length).Trim();
            }
            else if (trimmed.StartsWith("**运行时**:"))
            {
                manifest.Runtime = trimmed.Substring("**运行时**:".Length).Trim();
            }
            else if (trimmed.StartsWith("## 标签"))
            {
                currentSection = "tags";
                currentList = manifest.Tags;
            }
            else if (trimmed.StartsWith("## 依赖"))
            {
                currentSection = "dependencies";
                currentList = manifest.Dependencies;
            }
            else if (trimmed.StartsWith("## 参数"))
            {
                currentSection = "parameters";
            }
            else if (trimmed.StartsWith("- ") && !string.IsNullOrEmpty(currentSection))
            {
                var item = trimmed.Substring(2);
                if (currentSection == "parameters")
                {
                    var match = Regex.Match(item, @"`(\w+)`:\s*(.+)");
                    if (match.Success)
                    {
                        manifest.Parameters[match.Groups[1].Value] = match.Groups[2].Value;
                    }
                }
                else
                {
                    currentList.Add(item);
                }
            }
        }

        return manifest;
    }
}
