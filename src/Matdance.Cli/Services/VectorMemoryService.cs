using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Matdance.Cli.Services;

public sealed class VectorMemoryService
{
    private const string Algorithm = "matdance-local-hash-v1";
    private const int VectorDimensions = 4096;
    private const int MaxVectorTerms = 128;
    private const int MaxLexicalTerms = 96;
    private const int MaxChunkChars = 1400;
    private const int MinChunkChars = 180;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private static readonly object Gate = new();

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "the", "and", "for", "with", "from", "this", "that", "have", "has", "was", "were", "are", "but", "not", "you",
        "your", "about", "into", "when", "then", "than", "they", "them", "will", "can", "could", "should", "would",
        "一个", "这个", "那个", "以及", "但是", "如果", "因为", "所以", "进行", "需要", "可以", "不是", "没有", "已经"
    };

    private readonly PathService _path;

    public VectorMemoryService(PathService path)
    {
        _path = path;
    }

    public void Refresh(string agent)
    {
        lock (Gate)
        {
            Rebuild(agent, CollectSources(agent));
        }
    }

    public VectorMemorySearchResult Search(string agent, string query, int take = 3, int candidateLimit = 80)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new VectorMemorySearchResult { Algorithm = Algorithm };

        VectorMemoryBase index;
        lock (Gate)
        {
            var sources = CollectSources(agent);
            index = Load(agent);
            if (!IsFresh(index, sources))
                index = Rebuild(agent, sources);
        }

        if (index.Entries.Count == 0)
        {
            return new VectorMemorySearchResult
            {
                Algorithm = index.Algorithm,
                EntryCount = 0,
                VisitedNodes = 0,
                CandidateCount = 0
            };
        }

        var queryEmbedding = Embed(query);
        if (queryEmbedding.Vector.Count == 0)
        {
            return new VectorMemorySearchResult
            {
                Algorithm = index.Algorithm,
                EntryCount = index.Entries.Count
            };
        }

        var candidateIndexes = QueryCandidates(index, queryEmbedding.SimHash, candidateLimit);
        if (candidateIndexes.Count == 0 && index.Entries.Count <= 128)
            candidateIndexes.AddRange(Enumerable.Range(0, index.Entries.Count));

        var queryTerms = new HashSet<string>(queryEmbedding.Terms.Select(t => t.Term), StringComparer.OrdinalIgnoreCase);
        var ranked = candidateIndexes
            .Distinct()
            .Select(i => Score(index.Entries[i], queryEmbedding, queryTerms))
            .Where(item => item.Cosine > 0.02 || item.Lexical > 0 || item.HammingSimilarity > 0.72)
            .OrderByDescending(item => item.Score)
            .ThenBy(item => item.HammingDistance)
            .Take(Math.Clamp(take, 1, 20))
            .ToList();

        return new VectorMemorySearchResult
        {
            Algorithm = index.Algorithm,
            EntryCount = index.Entries.Count,
            CandidateCount = candidateIndexes.Distinct().Count(),
            VisitedNodes = index.LastVisitedNodes,
            Items = ranked
        };
    }

    public VectorMemoryAtlas GetAtlas(string agent, int maxNodes = 240)
    {
        VectorMemoryBase index;
        lock (Gate)
        {
            var sources = CollectSources(agent);
            index = Load(agent);
            if (!IsFresh(index, sources))
                index = Rebuild(agent, sources);
        }

        var entries = index.Entries
            .Take(Math.Clamp(maxNodes, 1, 600))
            .ToList();

        var projected = entries.Select(ProjectEntry).ToList();
        var minX = projected.Count == 0 ? 0 : projected.Min(point => point.X);
        var maxX = projected.Count == 0 ? 1 : projected.Max(point => point.X);
        var minY = projected.Count == 0 ? 0 : projected.Min(point => point.Y);
        var maxY = projected.Count == 0 ? 1 : projected.Max(point => point.Y);
        var nodes = new List<VectorMemoryAtlasNode>();

        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var angle = entries.Count <= 1 ? 0 : (Math.PI * 2 * i) / entries.Count;
            var fallbackX = 0.5 + Math.Cos(angle) * 0.34;
            var fallbackY = 0.5 + Math.Sin(angle) * 0.34;
            nodes.Add(new VectorMemoryAtlasNode
            {
                Id = entry.Id,
                SourcePath = entry.SourcePath,
                Kind = entry.Kind,
                Title = entry.Title,
                ChunkIndex = entry.ChunkIndex,
                StartLine = entry.StartLine,
                EndLine = entry.EndLine,
                X = NormalizeAtlasAxis(projected[i].X, minX, maxX, fallbackX),
                Y = NormalizeAtlasAxis(projected[i].Y, minY, maxY, fallbackY),
                Size = Math.Round(Math.Clamp(0.42 + Math.Log(entry.Text.Length + 1) / 12.0, 0.45, 1.25), 3),
                TextPreview = CompactPreview(entry.Text, 260),
                Terms = entry.Terms.Take(8).Select(term => term.Term).ToList()
            });
        }

        return new VectorMemoryAtlas
        {
            Algorithm = index.Algorithm,
            Dimensions = index.Dimensions,
            UpdatedAt = index.UpdatedAt,
            EntryCount = index.Entries.Count,
            NodeCount = nodes.Count,
            Sources = index.Sources,
            Nodes = nodes,
            Links = BuildAtlasLinks(nodes, entries)
        };
    }

    private static (double X, double Y) ProjectEntry(VectorMemoryEntry entry)
    {
        double x = 0;
        double y = 0;
        foreach (var (dimension, value) in entry.Vector)
        {
            x += SignedUnit(StableHash64("atlas:x:" + dimension)) * value;
            y += SignedUnit(StableHash64("atlas:y:" + dimension)) * value;
        }

        if (Math.Abs(x) < 0.000001 && Math.Abs(y) < 0.000001)
        {
            var hash = ParseHex(entry.SimHash);
            x = (((hash & 0xffffffffUL) / (double)uint.MaxValue) * 2.0) - 1.0;
            y = ((((hash >> 32) & 0xffffffffUL) / (double)uint.MaxValue) * 2.0) - 1.0;
        }

        var jitter = StableHash64(entry.Id);
        x += SignedUnit(jitter) * 0.035;
        y += SignedUnit(jitter >> 17) * 0.035;
        return (x, y);
    }

    private static double SignedUnit(ulong hash)
    {
        return ((hash & 0xffffUL) / 32767.5) - 1.0;
    }

    private static double NormalizeAtlasAxis(double value, double min, double max, double fallback)
    {
        if (Math.Abs(max - min) < 0.000001)
            return Math.Round(Math.Clamp(fallback, 0.08, 0.92), 4);

        return Math.Round(0.08 + ((value - min) / (max - min)) * 0.84, 4);
    }

    private static string CompactPreview(string text, int limit)
    {
        var sb = new StringBuilder();
        var pendingSpace = false;
        foreach (var ch in text)
        {
            if (char.IsWhiteSpace(ch))
            {
                pendingSpace = sb.Length > 0;
                continue;
            }

            if (pendingSpace && sb.Length > 0)
            {
                sb.Append(' ');
                pendingSpace = false;
            }

            sb.Append(ch);
            if (sb.Length >= limit)
                break;
        }

        if (sb.Length >= limit && text.Length > limit)
            sb.Append("...");
        return sb.ToString();
    }

    private static List<VectorMemoryAtlasLink> BuildAtlasLinks(List<VectorMemoryAtlasNode> nodes, List<VectorMemoryEntry> entries)
    {
        var links = new List<VectorMemoryAtlasLink>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var maxLinks = Math.Min(420, nodes.Count * 2);
        for (var i = 0; i < entries.Count; i++)
        {
            var sourceHash = ParseHex(entries[i].SimHash);
            var nearest = Enumerable.Range(0, entries.Count)
                .Where(index => index != i)
                .Select(index => new
                {
                    Index = index,
                    Distance = Hamming(sourceHash, ParseHex(entries[index].SimHash))
                })
                .OrderBy(item => item.Distance)
                .ThenBy(item => item.Index)
                .Take(2);

            foreach (var item in nearest)
            {
                var left = Math.Min(i, item.Index);
                var right = Math.Max(i, item.Index);
                var key = left + ":" + right;
                if (!seen.Add(key))
                    continue;

                links.Add(new VectorMemoryAtlasLink
                {
                    SourceId = nodes[left].Id,
                    TargetId = nodes[right].Id,
                    Distance = item.Distance,
                    Strength = Math.Round(1.0 - item.Distance / 64.0, 3)
                });

                if (links.Count >= maxLinks)
                    return links;
            }
        }

        return links;
    }

    private VectorMemoryBase Load(string agent)
    {
        var path = _path.GetVectorMemoryPath(agent);
        if (!File.Exists(path))
            return new VectorMemoryBase();

        try
        {
            return JsonSerializer.Deserialize<VectorMemoryBase>(File.ReadAllText(path), JsonOptions) ?? new VectorMemoryBase();
        }
        catch
        {
            return new VectorMemoryBase();
        }
    }

    private VectorMemoryBase Rebuild(string agent, List<VectorMemorySource> sources)
    {
        var entries = new List<VectorMemoryEntry>();
        foreach (var source in sources)
        {
            if (!File.Exists(source.AbsolutePath))
                continue;

            var content = File.ReadAllText(source.AbsolutePath);
            var chunks = Chunk(content, source);
            entries.AddRange(chunks);
        }

        var index = new VectorMemoryBase
        {
            Version = 1,
            Algorithm = Algorithm,
            Dimensions = VectorDimensions,
            UpdatedAt = UserTimeZoneService.Now(),
            Sources = sources.Select(s => s.ToSnapshot()).ToList(),
            Entries = entries
        };
        index.Tree = BuildTree(entries, Enumerable.Range(0, entries.Count).ToList());

        var path = _path.GetVectorMemoryPath(agent);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        AtomicFile.WriteAllText(path, JsonSerializer.Serialize(index, JsonOptions));
        return index;
    }

    private List<VectorMemorySource> CollectSources(string agent)
    {
        var agentRoot = _path.GetAgentPath(agent);
        var result = new List<VectorMemorySource>();
        AddSource(result, agentRoot, _path.GetHotMemoryPath(agent), "hot", "Hot Memory");
        AddSource(result, agentRoot, _path.GetCoreMemoryPath(agent), "core", "Core Memory");

        var longTermDir = _path.GetLongTermMemoryPath(agent);
        if (Directory.Exists(longTermDir))
        {
            foreach (var file in Directory.GetFiles(longTermDir, "*.md").OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase))
            {
                var date = Path.GetFileNameWithoutExtension(file);
                AddSource(result, agentRoot, file, "long_term", date);
            }
        }

        return result;
    }

    private static void AddSource(List<VectorMemorySource> result, string agentRoot, string path, string kind, string title)
    {
        if (!File.Exists(path))
            return;

        var info = new FileInfo(path);
        var content = File.ReadAllText(path);
        result.Add(new VectorMemorySource
        {
            AbsolutePath = path,
            RelativePath = Path.GetRelativePath(agentRoot, path).Replace('\\', '/'),
            Kind = kind,
            Title = title,
            LastWriteUtc = info.LastWriteTimeUtc,
            Length = info.Length,
            Sha256 = Sha256(content)
        });
    }

    private static bool IsFresh(VectorMemoryBase index, List<VectorMemorySource> sources)
    {
        if (!string.Equals(index.Algorithm, Algorithm, StringComparison.Ordinal) || index.Dimensions != VectorDimensions)
            return false;
        if (index.Sources.Count != sources.Count)
            return false;

        var current = sources.Select(s => s.ToSnapshot()).OrderBy(s => s.Path, StringComparer.Ordinal).ToList();
        var stored = index.Sources.OrderBy(s => s.Path, StringComparer.Ordinal).ToList();
        for (var i = 0; i < current.Count; i++)
        {
            if (!string.Equals(current[i].Path, stored[i].Path, StringComparison.Ordinal)
                || !string.Equals(current[i].Sha256, stored[i].Sha256, StringComparison.Ordinal)
                || current[i].Length != stored[i].Length)
                return false;
        }

        return true;
    }

    private static List<VectorMemoryEntry> Chunk(string content, VectorMemorySource source)
    {
        var result = new List<VectorMemoryEntry>();
        var lines = content.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        var sb = new StringBuilder();
        var startLine = 1;
        var currentTitle = source.Title;
        var chunkIndex = 0;

        void Flush(int endLine)
        {
            var text = sb.ToString().Trim();
            sb.Clear();
            if (string.IsNullOrWhiteSpace(text))
                return;

            var embedding = Embed(text);
            if (embedding.Vector.Count == 0)
                return;

            result.Add(new VectorMemoryEntry
            {
                Id = source.RelativePath + "#" + chunkIndex,
                SourcePath = source.RelativePath,
                Kind = source.Kind,
                Title = currentTitle,
                ChunkIndex = chunkIndex++,
                StartLine = startLine,
                EndLine = Math.Max(startLine, endLine),
                Text = text,
                SimHash = ToHex(embedding.SimHash),
                Vector = embedding.Vector,
                Terms = embedding.Terms
            });
        }

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var trimmed = line.Trim();
            var isHeading = trimmed.StartsWith("#", StringComparison.Ordinal);
            if (isHeading && sb.Length >= MinChunkChars)
            {
                Flush(i);
                startLine = i + 1;
            }

            if (isHeading)
                currentTitle = trimmed.TrimStart('#').Trim();

            if (sb.Length == 0)
                startLine = i + 1;

            sb.AppendLine(line);
            if (sb.Length >= MaxChunkChars && (string.IsNullOrWhiteSpace(trimmed) || sb.Length >= MaxChunkChars * 1.35))
            {
                Flush(i + 1);
                startLine = i + 2;
            }
        }

        Flush(lines.Length);
        return result;
    }

    private static VectorMemoryEmbedding Embed(string text)
    {
        var counts = ExtractFeatures(text);
        var raw = new Dictionary<int, float>();
        var bitWeights = new double[64];
        var lexical = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        foreach (var (feature, count) in counts)
        {
            var weight = (float)(1.0 + Math.Log(count));
            var hash = StableHash64(feature);
            var dim = (int)(hash & (VectorDimensions - 1));
            var sign = ((hash >> 12) & 1UL) == 0 ? 1f : -1f;
            raw[dim] = raw.GetValueOrDefault(dim) + sign * weight;

            for (var bit = 0; bit < 64; bit++)
                bitWeights[bit] += ((hash >> bit) & 1UL) == 1UL ? weight : -weight;

            if (!feature.StartsWith("tri:", StringComparison.Ordinal) && !feature.StartsWith("bi:", StringComparison.Ordinal))
                lexical[feature] = lexical.GetValueOrDefault(feature) + weight;
        }

        var trimmed = raw
            .OrderByDescending(pair => Math.Abs(pair.Value))
            .Take(MaxVectorTerms)
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        var norm = Math.Sqrt(trimmed.Values.Sum(v => v * v));
        if (norm > 0)
        {
            foreach (var key in trimmed.Keys.ToList())
                trimmed[key] = (float)(trimmed[key] / norm);
        }

        ulong simHash = 0;
        for (var bit = 0; bit < 64; bit++)
        {
            if (bitWeights[bit] >= 0)
                simHash |= 1UL << bit;
        }

        var terms = lexical
            .OrderByDescending(pair => pair.Value)
            .Take(MaxLexicalTerms)
            .Select(pair => new VectorMemoryTerm { Term = pair.Key, Weight = pair.Value })
            .ToList();

        return new VectorMemoryEmbedding
        {
            Vector = trimmed,
            Terms = terms,
            SimHash = simHash
        };
    }

    private static Dictionary<string, int> ExtractFeatures(string text)
    {
        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var word = new StringBuilder();
        var cjk = new List<char>();

        void Add(string token, int amount = 1)
        {
            if (token.Length == 0 || StopWords.Contains(token))
                return;
            result[token] = result.GetValueOrDefault(token) + amount;
        }

        void FlushWord()
        {
            if (word.Length == 0)
                return;

            var token = word.ToString().ToLowerInvariant();
            word.Clear();
            if (token.Length <= 1 || StopWords.Contains(token))
                return;

            Add(token, 2);
            if (token.Length >= 5)
            {
                for (var i = 0; i <= token.Length - 3; i++)
                    Add("tri:" + token.Substring(i, 3));
            }
        }

        void FlushCjk()
        {
            if (cjk.Count == 0)
                return;

            for (var i = 0; i < cjk.Count; i++)
                Add(cjk[i].ToString());
            for (var i = 0; i < cjk.Count - 1; i++)
                Add("bi:" + cjk[i] + cjk[i + 1], 2);
            for (var i = 0; i < cjk.Count - 2; i++)
                Add("tri:" + cjk[i] + cjk[i + 1] + cjk[i + 2]);
            cjk.Clear();
        }

        foreach (var ch in text)
        {
            if (IsCjk(ch))
            {
                FlushWord();
                cjk.Add(ch);
                continue;
            }

            FlushCjk();
            if (char.IsLetterOrDigit(ch))
            {
                word.Append(char.ToLowerInvariant(ch));
                continue;
            }

            FlushWord();
        }

        FlushWord();
        FlushCjk();
        return result;
    }

    private static VectorMemoryTreeNode? BuildTree(List<VectorMemoryEntry> entries, List<int> indexes)
    {
        if (indexes.Count == 0)
            return null;

        var vantage = indexes[indexes.Count / 2];
        if (indexes.Count == 1)
            return new VectorMemoryTreeNode { EntryIndex = vantage };

        var vantageHash = ParseHex(entries[vantage].SimHash);
        var rest = indexes.Where(i => i != vantage)
            .Select(i => new { Index = i, Distance = Hamming(vantageHash, ParseHex(entries[i].SimHash)) })
            .OrderBy(item => item.Distance)
            .ThenBy(item => item.Index)
            .ToList();

        var median = rest[rest.Count / 2].Distance;
        var near = rest.Where(item => item.Distance <= median).Select(item => item.Index).ToList();
        var far = rest.Where(item => item.Distance > median).Select(item => item.Index).ToList();
        if (far.Count == 0 && near.Count > 1)
        {
            far = near.Skip(near.Count / 2).ToList();
            near = near.Take(near.Count / 2).ToList();
        }

        return new VectorMemoryTreeNode
        {
            EntryIndex = vantage,
            Radius = median,
            Near = BuildTree(entries, near),
            Far = BuildTree(entries, far)
        };
    }

    private static List<int> QueryCandidates(VectorMemoryBase index, ulong queryHash, int candidateLimit)
    {
        var result = new List<(int Index, int Distance)>();
        var visited = 0;
        var maxVisits = index.Entries.Count <= 128
            ? index.Entries.Count
            : Math.Max(32, (int)Math.Ceiling(Math.Log2(index.Entries.Count + 1) * 18));
        candidateLimit = Math.Clamp(candidateLimit, 8, 512);

        void Add(int entryIndex, int distance)
        {
            result.Add((entryIndex, distance));
            if (result.Count > candidateLimit * 2)
            {
                result = result.OrderBy(item => item.Distance).Take(candidateLimit).ToList();
            }
        }

        void Visit(VectorMemoryTreeNode? node)
        {
            if (node == null || visited >= maxVisits)
                return;

            visited++;
            var distance = Hamming(queryHash, ParseHex(index.Entries[node.EntryIndex].SimHash));
            Add(node.EntryIndex, distance);

            var first = distance <= node.Radius ? node.Near : node.Far;
            var second = distance <= node.Radius ? node.Far : node.Near;
            Visit(first);

            var currentWorst = result.Count < candidateLimit ? 64 : result.OrderBy(item => item.Distance).Take(candidateLimit).Last().Distance;
            if (visited < maxVisits && Math.Abs(distance - node.Radius) <= currentWorst + 2)
                Visit(second);
        }

        Visit(index.Tree);
        index.LastVisitedNodes = visited;
        return result.OrderBy(item => item.Distance).Take(candidateLimit).Select(item => item.Index).ToList();
    }

    private static VectorMemorySearchItem Score(VectorMemoryEntry entry, VectorMemoryEmbedding query, HashSet<string> queryTerms)
    {
        var cosine = Cosine(query.Vector, entry.Vector);
        var entryHash = ParseHex(entry.SimHash);
        var hamming = Hamming(query.SimHash, entryHash);
        var hammingSimilarity = 1.0 - hamming / 64.0;
        var entryTerms = entry.Terms.Select(t => t.Term).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var lexical = queryTerms.Count == 0 ? 0 : queryTerms.Count(entryTerms.Contains) / (double)queryTerms.Count;
        var kindBoost = entry.Kind switch
        {
            "core" => 0.04,
            "hot" => 0.03,
            _ => 0.0
        };
        var score = 0.70 * cosine + 0.22 * lexical + 0.08 * hammingSimilarity + kindBoost;

        return new VectorMemorySearchItem
        {
            Entry = entry,
            Score = score,
            Cosine = cosine,
            Lexical = lexical,
            HammingSimilarity = hammingSimilarity,
            HammingDistance = hamming
        };
    }

    private static double Cosine(Dictionary<int, float> left, Dictionary<int, float> right)
    {
        if (left.Count == 0 || right.Count == 0)
            return 0;

        if (left.Count > right.Count)
            (left, right) = (right, left);

        double sum = 0;
        foreach (var (key, value) in left)
        {
            if (right.TryGetValue(key, out var other))
                sum += value * other;
        }

        return Math.Max(0, sum);
    }

    private static bool IsCjk(char ch)
    {
        return (ch >= 0x4E00 && ch <= 0x9FFF)
            || (ch >= 0x3400 && ch <= 0x4DBF)
            || (ch >= 0x3040 && ch <= 0x30FF)
            || (ch >= 0xAC00 && ch <= 0xD7AF);
    }

    private static string Sha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static ulong StableHash64(string value)
    {
        const ulong offset = 14695981039346656037UL;
        const ulong prime = 1099511628211UL;
        var hash = offset;
        foreach (var b in Encoding.UTF8.GetBytes(value))
        {
            hash ^= b;
            hash *= prime;
        }

        hash ^= hash >> 33;
        hash *= 0xff51afd7ed558ccdUL;
        hash ^= hash >> 33;
        hash *= 0xc4ceb9fe1a85ec53UL;
        hash ^= hash >> 33;
        return hash;
    }

    private static int Hamming(ulong left, ulong right)
    {
        return System.Numerics.BitOperations.PopCount(left ^ right);
    }

    private static string ToHex(ulong value) => value.ToString("x16");

    private static ulong ParseHex(string value)
    {
        return ulong.TryParse(value, System.Globalization.NumberStyles.HexNumber, null, out var parsed) ? parsed : 0UL;
    }
}

public sealed class VectorMemoryBase
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    [JsonPropertyName("algorithm")]
    public string Algorithm { get; set; } = string.Empty;

    [JsonPropertyName("dimensions")]
    public int Dimensions { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTimeOffset UpdatedAt { get; set; }

    [JsonPropertyName("sources")]
    public List<VectorMemorySourceSnapshot> Sources { get; set; } = new();

    [JsonPropertyName("entries")]
    public List<VectorMemoryEntry> Entries { get; set; } = new();

    [JsonPropertyName("tree")]
    public VectorMemoryTreeNode? Tree { get; set; }

    [JsonIgnore]
    public int LastVisitedNodes { get; set; }
}

public sealed class VectorMemorySource
{
    public string AbsolutePath { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public DateTime LastWriteUtc { get; set; }
    public long Length { get; set; }
    public string Sha256 { get; set; } = string.Empty;

    public VectorMemorySourceSnapshot ToSnapshot() => new()
    {
        Path = RelativePath,
        Kind = Kind,
        Title = Title,
        LastWriteUtc = LastWriteUtc,
        Length = Length,
        Sha256 = Sha256
    };
}

public sealed class VectorMemorySourceSnapshot
{
    [JsonPropertyName("path")]
    public string Path { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("last_write_utc")]
    public DateTime LastWriteUtc { get; set; }

    [JsonPropertyName("length")]
    public long Length { get; set; }

    [JsonPropertyName("sha256")]
    public string Sha256 { get; set; } = string.Empty;
}

public sealed class VectorMemoryEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("source_path")]
    public string SourcePath { get; set; } = string.Empty;

    [JsonPropertyName("kind")]
    public string Kind { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("chunk_index")]
    public int ChunkIndex { get; set; }

    [JsonPropertyName("start_line")]
    public int StartLine { get; set; }

    [JsonPropertyName("end_line")]
    public int EndLine { get; set; }

    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;

    [JsonPropertyName("simhash")]
    public string SimHash { get; set; } = string.Empty;

    [JsonPropertyName("vector")]
    public Dictionary<int, float> Vector { get; set; } = new();

    [JsonPropertyName("terms")]
    public List<VectorMemoryTerm> Terms { get; set; } = new();
}

public sealed class VectorMemoryTerm
{
    [JsonPropertyName("term")]
    public string Term { get; set; } = string.Empty;

    [JsonPropertyName("weight")]
    public float Weight { get; set; }
}

public sealed class VectorMemoryTreeNode
{
    [JsonPropertyName("entry_index")]
    public int EntryIndex { get; set; }

    [JsonPropertyName("radius")]
    public int Radius { get; set; }

    [JsonPropertyName("near")]
    public VectorMemoryTreeNode? Near { get; set; }

    [JsonPropertyName("far")]
    public VectorMemoryTreeNode? Far { get; set; }
}

public sealed class VectorMemoryAtlas
{
    public string Algorithm { get; set; } = string.Empty;
    public int Dimensions { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public int EntryCount { get; set; }
    public int NodeCount { get; set; }
    public List<VectorMemorySourceSnapshot> Sources { get; set; } = new();
    public List<VectorMemoryAtlasNode> Nodes { get; set; } = new();
    public List<VectorMemoryAtlasLink> Links { get; set; } = new();
}

public sealed class VectorMemoryAtlasNode
{
    public string Id { get; set; } = string.Empty;
    public string SourcePath { get; set; } = string.Empty;
    public string Kind { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public int ChunkIndex { get; set; }
    public int StartLine { get; set; }
    public int EndLine { get; set; }
    public double X { get; set; }
    public double Y { get; set; }
    public double Size { get; set; }
    public string TextPreview { get; set; } = string.Empty;
    public List<string> Terms { get; set; } = new();
}

public sealed class VectorMemoryAtlasLink
{
    public string SourceId { get; set; } = string.Empty;
    public string TargetId { get; set; } = string.Empty;
    public int Distance { get; set; }
    public double Strength { get; set; }
}

public sealed class VectorMemoryEmbedding
{
    public Dictionary<int, float> Vector { get; set; } = new();
    public List<VectorMemoryTerm> Terms { get; set; } = new();
    public ulong SimHash { get; set; }
}

public sealed class VectorMemorySearchResult
{
    public string Algorithm { get; set; } = string.Empty;
    public int EntryCount { get; set; }
    public int CandidateCount { get; set; }
    public int VisitedNodes { get; set; }
    public List<VectorMemorySearchItem> Items { get; set; } = new();
}

public sealed class VectorMemorySearchItem
{
    public VectorMemoryEntry Entry { get; set; } = new();
    public double Score { get; set; }
    public double Cosine { get; set; }
    public double Lexical { get; set; }
    public double HammingSimilarity { get; set; }
    public int HammingDistance { get; set; }
}
