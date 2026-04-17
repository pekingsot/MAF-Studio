using MAFStudio.Application.Interfaces;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Application.Services.Rag;

public class TextSplitterService : ITextSplitterService
{
    private readonly ISystemConfigRepository _configRepo;

    public TextSplitterService(ISystemConfigRepository configRepo)
    {
        _configRepo = configRepo;
    }

    public List<TextChunk> Split(string text, string? method = null, int? chunkSize = null, int? chunkOverlap = null)
    {
        var splitMethod = method ?? "recursive";
        var size = chunkSize ?? 500;
        var overlap = chunkOverlap ?? 50;

        return splitMethod switch
        {
            "recursive" => RecursiveSplit(text, size, overlap),
            "character" => CharacterSplit(text, size, overlap),
            "separator" => SeparatorSplit(text),
            _ => RecursiveSplit(text, size, overlap),
        };
    }

    private List<TextChunk> RecursiveSplit(string text, int chunkSize, int overlap)
    {
        var chunks = new List<TextChunk>();
        var separators = new[] { "\n\n", "\n", "。", ".", "！", "!", "？", "?", "；", ";", " ", "" };

        var pieces = SplitBySeparators(text, separators, chunkSize);
        var currentChunk = "";
        var index = 0;

        foreach (var piece in pieces)
        {
            if (currentChunk.Length + piece.Length > chunkSize && currentChunk.Length > 0)
            {
                chunks.Add(new TextChunk { Index = index++, Content = currentChunk.Trim() });
                if (overlap > 0 && currentChunk.Length > overlap)
                {
                    currentChunk = currentChunk[^overlap..] + piece;
                }
                else
                {
                    currentChunk = piece;
                }
            }
            else
            {
                currentChunk += piece;
            }
        }

        if (!string.IsNullOrWhiteSpace(currentChunk))
        {
            chunks.Add(new TextChunk { Index = index, Content = currentChunk.Trim() });
        }

        return chunks;
    }

    private List<string> SplitBySeparators(string text, string[] separators, int chunkSize)
    {
        if (separators.Length == 0 || string.IsNullOrEmpty(text))
            return new List<string> { text };

        var separator = separators[0];
        var remainingSeparators = separators[1..];

        if (string.IsNullOrEmpty(separator))
            return SplitBySize(text, chunkSize);

        var parts = text.Split(separator, StringSplitOptions.None);
        var result = new List<string>();

        foreach (var part in parts)
        {
            if (part.Length <= chunkSize)
            {
                if (!string.IsNullOrEmpty(part))
                    result.Add(part + separator);
            }
            else
            {
                var subParts = SplitBySeparators(part, remainingSeparators, chunkSize);
                result.AddRange(subParts);
            }
        }

        return result;
    }

    private List<string> SplitBySize(string text, int chunkSize)
    {
        var result = new List<string>();
        for (int i = 0; i < text.Length; i += chunkSize)
        {
            var length = Math.Min(chunkSize, text.Length - i);
            result.Add(text.Substring(i, length));
        }
        return result;
    }

    private List<TextChunk> CharacterSplit(string text, int chunkSize, int overlap)
    {
        var chunks = new List<TextChunk>();
        var index = 0;

        for (int i = 0; i < text.Length; i += chunkSize - overlap)
        {
            var length = Math.Min(chunkSize, text.Length - i);
            var content = text.Substring(i, length).Trim();
            if (!string.IsNullOrWhiteSpace(content))
            {
                chunks.Add(new TextChunk { Index = index++, Content = content });
            }
        }

        return chunks;
    }

    private List<TextChunk> SeparatorSplit(string text)
    {
        var separators = new[] { "\n\n", "\n" };
        var parts = new List<string>();

        foreach (var sep in separators)
        {
            if (text.Contains(sep))
            {
                parts = text.Split(sep, StringSplitOptions.RemoveEmptyEntries)
                    .Select(p => p.Trim())
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .ToList();
                break;
            }
        }

        if (parts.Count == 0)
            parts = new List<string> { text.Trim() };

        return parts.Select((p, i) => new TextChunk { Index = i, Content = p }).ToList();
    }
}
