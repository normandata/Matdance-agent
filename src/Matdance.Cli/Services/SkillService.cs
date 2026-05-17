using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Matdance.Cli.Models;

namespace Matdance.Cli.Services;

public class SkillService
{
    private readonly PathService _path;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };

    public SkillService(PathService path)
    {
        _path = path;
    }

    public SkillItem Create(string agentName, SkillCreateRequest request)
    {
        var agentPath = _path.GetAgentPath(agentName);
        if (!Directory.Exists(agentPath))
            throw new InvalidOperationException($"Agent '{agentName}' does not exist.");

        var id = GenerateSkillId(request.Name);
        var skillDir = _path.GetSkillPath(agentName, id);
        Directory.CreateDirectory(skillDir);

        var skill = new SkillItem
        {
            Id = id,
            Name = request.Name.Trim(),
            Description = request.Description.Trim(),
            Tags = request.Tags?.Select(t => t.Trim().ToLowerInvariant()).ToList() ?? new List<string>(),
            Content = request.Content,
            CreatedAt = UserTimeZoneService.Now(),
            UpdatedAt = UserTimeZoneService.Now()
        };

        SaveSkill(skillDir, skill);
        return skill;
    }

    public SkillItem Read(string agentName, string skillId)
    {
        var skillDir = _path.GetSkillPath(agentName, skillId);
        if (!Directory.Exists(skillDir))
            throw new InvalidOperationException($"Skill '{skillId}' not found.");

        SkillValidationState.EnsureReportCurrent(skillDir);
        return LoadSkill(skillDir);
    }

    public SkillItem Edit(string agentName, SkillEditRequest request)
    {
        var skillDir = _path.GetSkillPath(agentName, request.Id);
        if (!Directory.Exists(skillDir))
            throw new InvalidOperationException($"Skill '{request.Id}' not found.");

        var skill = LoadSkill(skillDir);

        if (!string.IsNullOrWhiteSpace(request.Name))
            skill.Name = request.Name.Trim();
        if (!string.IsNullOrWhiteSpace(request.Description))
            skill.Description = request.Description.Trim();
        if (request.Tags != null)
            skill.Tags = request.Tags.Select(t => t.Trim().ToLowerInvariant()).ToList();
        if (request.Content != null)
            skill.Content = request.Content;

        skill.UpdatedAt = UserTimeZoneService.Now();
        SaveSkill(skillDir, skill);
        SkillValidationState.DeleteReport(skillDir);
        return skill;
    }

    public void Delete(string agentName, string skillId)
    {
        var skillDir = _path.GetSkillPath(agentName, skillId);
        if (!Directory.Exists(skillDir))
            throw new InvalidOperationException($"Skill '{skillId}' not found.");

        Directory.Delete(skillDir, recursive: true);
    }

    public SkillListResult List(string agentName)
    {
        var skillsPath = _path.GetSkillsPath(agentName);
        var result = new SkillListResult { Skills = new List<SkillSummary>() };

        if (!Directory.Exists(skillsPath))
            return result;

        foreach (var dir in Directory.GetDirectories(skillsPath))
        {
            try
            {
                SkillValidationState.EnsureReportCurrent(dir);
                var skill = LoadSkill(dir);
                result.Skills.Add(new SkillSummary
                {
                    Id = skill.Id,
                    Name = skill.Name,
                    Description = skill.Description,
                    Tags = skill.Tags
                });
            }
            catch { /* skip invalid skill directories */ }
        }

        result.Total = result.Skills.Count;
        return result;
    }

    public List<SkillSummary> Search(string agentName, string query)
    {
        var all = List(agentName);
        var keywords = query.ToLowerInvariant().Split(new[] { ' ', ',', ';' }, StringSplitOptions.RemoveEmptyEntries);

        return all.Skills.Where(skill =>
        {
            var text = $"{skill.Name} {skill.Description} {string.Join(" ", skill.Tags)}".ToLowerInvariant();
            return keywords.Any(kw => text.Contains(kw));
        }).ToList();
    }

    public string GetSkillContent(string agentName, string skillId)
    {
        var skill = Read(agentName, skillId);
        return skill.Content;
    }

    private static string GenerateSkillId(string name)
    {
        // Create a safe directory name from the skill name
        var safe = Regex.Replace(name.ToLowerInvariant(), @"[^a-z0-9\-]", "-");
        safe = Regex.Replace(safe, @"-{2,}", "-").Trim('-');
        if (string.IsNullOrEmpty(safe))
            safe = "skill";
        return $"{safe}_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}";
    }

    private static void SaveSkill(string skillDir, SkillItem skill)
    {
        var metadataPath = Path.Combine(skillDir, "skill.json");
        AtomicFile.WriteAllText(metadataPath, JsonSerializer.Serialize(skill, JsonOptions));

        var mdPath = Path.Combine(skillDir, "skill.md");
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"name: \"{skill.Name}\"");
        sb.AppendLine($"description: \"{skill.Description}\"");
        sb.AppendLine($"tags: [{string.Join(", ", skill.Tags.Select(t => $"\"{t}\""))}]");
        sb.AppendLine($"id: \"{skill.Id}\"");
        sb.AppendLine($"created_at: \"{skill.CreatedAt:O}\"");
        sb.AppendLine($"updated_at: \"{skill.UpdatedAt:O}\"");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine(skill.Content);
        AtomicFile.WriteAllText(mdPath, sb.ToString());
    }

    private static SkillItem LoadSkill(string skillDir)
    {
        var metadataPath = Path.Combine(skillDir, "skill.json");
        if (File.Exists(metadataPath))
        {
            var json = File.ReadAllText(metadataPath);
            return JsonSerializer.Deserialize<SkillItem>(json, JsonOptions) ?? new SkillItem();
        }

        // Fallback: parse from skill.md frontmatter
        var mdPath = Path.Combine(skillDir, "skill.md");
        if (File.Exists(mdPath))
        {
            return ParseSkillMd(mdPath);
        }

        throw new InvalidOperationException("Skill metadata not found.");
    }

    private static SkillItem ParseSkillMd(string mdPath)
    {
        var content = File.ReadAllText(mdPath);
        var skill = new SkillItem();

        // Extract frontmatter
        var frontmatterMatch = Regex.Match(content, @"^---\s*\n(.*?)\n---\s*\n(.*)$", RegexOptions.Singleline);
        if (frontmatterMatch.Success)
        {
            var frontmatter = frontmatterMatch.Groups[1].Value;
            skill.Content = frontmatterMatch.Groups[2].Value.Trim();

            // Parse simple key-value pairs
            foreach (var line in frontmatter.Split('\n'))
            {
                var colonIndex = line.IndexOf(':');
                if (colonIndex < 0) continue;

                var key = line[..colonIndex].Trim();
                var value = line[(colonIndex + 1)..].Trim().Trim('"');

                switch (key)
                {
                    case "name": skill.Name = value; break;
                    case "description": skill.Description = value; break;
                    case "id": skill.Id = value; break;
                    case "created_at": skill.CreatedAt = DateTimeOffset.TryParse(value, out var ca) ? ca : UserTimeZoneService.Now(); break;
                    case "updated_at": skill.UpdatedAt = DateTimeOffset.TryParse(value, out var ua) ? ua : UserTimeZoneService.Now(); break;
                    case "tags":
                        skill.Tags = value.Trim('[', ']').Split(',').Select(t => t.Trim().Trim('"').ToLowerInvariant()).Where(t => !string.IsNullOrEmpty(t)).ToList();
                        break;
                }
            }
        }
        else
        {
            skill.Content = content;
        }

        if (string.IsNullOrEmpty(skill.Id))
            skill.Id = Path.GetFileName(Path.GetDirectoryName(mdPath)) ?? "unknown";

        return skill;
    }
}
