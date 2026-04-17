using MAFStudio.Application.Services.Rag;
using MAFStudio.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace MAFStudio.Tests.Rag;

public class TextSplitterServiceTests
{
    private readonly Mock<MAFStudio.Core.Interfaces.Repositories.ISystemConfigRepository> _configRepo;
    private readonly TextSplitterService _service;

    public TextSplitterServiceTests()
    {
        _configRepo = new Mock<MAFStudio.Core.Interfaces.Repositories.ISystemConfigRepository>();
        _service = new TextSplitterService(_configRepo.Object);
    }

    [Fact]
    public void Split_RecursiveMethod_SplitsLongText()
    {
        var text = string.Join("\n\n", Enumerable.Repeat("这是一段测试文本，用于验证递归分割功能是否正常工作。", 50));

        var result = _service.Split(text, "recursive", 200, 20);

        Assert.NotEmpty(result);
        Assert.True(result.Count > 1);
        Assert.All(result, chunk => Assert.False(string.IsNullOrWhiteSpace(chunk.Content)));
    }

    [Fact]
    public void Split_RecursiveMethod_PreservesOrder()
    {
        var text = "第一段内容。第二段内容。第三段内容。第四段内容。第五段内容。";

        var result = _service.Split(text, "recursive", 10, 2);

        for (int i = 0; i < result.Count; i++)
        {
            Assert.Equal(i, result[i].Index);
        }
    }

    [Fact]
    public void Split_CharacterMethod_SplitsByCharCount()
    {
        var text = new string('A', 1000);

        var result = _service.Split(text, "character", 200, 50);

        Assert.NotEmpty(result);
        Assert.True(result.Count >= 4);
    }

    [Fact]
    public void Split_SeparatorMethod_SplitsByParagraph()
    {
        var text = "第一段\n\n第二段\n\n第三段";

        var result = _service.Split(text, "separator");

        Assert.Equal(3, result.Count);
        Assert.Equal("第一段", result[0].Content);
        Assert.Equal("第二段", result[1].Content);
        Assert.Equal("第三段", result[2].Content);
    }

    [Fact]
    public void Split_ShortText_ReturnsSingleChunk()
    {
        var text = "短文本";

        var result = _service.Split(text, "recursive", 500, 50);

        Assert.Single(result);
        Assert.Equal(text, result[0].Content);
    }

    [Fact]
    public void Split_EmptyText_ReturnsEmptyList()
    {
        var result = _service.Split("", "recursive", 500, 50);

        Assert.Empty(result);
    }

    [Fact]
    public void Split_DefaultMethod_UsesRecursive()
    {
        var text = string.Join("\n\n", Enumerable.Repeat("测试默认方法。", 20));

        var result = _service.Split(text, null, 100, 10);

        Assert.NotEmpty(result);
    }

    [Fact]
    public void Split_UnknownMethod_FallsBackToRecursive()
    {
        var text = string.Join("\n\n", Enumerable.Repeat("测试未知方法。", 20));

        var result = _service.Split(text, "unknown", 100, 10);

        Assert.NotEmpty(result);
    }

    [Fact]
    public void Split_ChineseText_HandlesCorrectly()
    {
        var text = "人工智能是计算机科学的一个分支，它企图了解智能的实质，并生产出一种新的能以人类智能相似的方式做出反应的智能机器。研究领域包括机器人、语言识别、图像识别、自然语言处理和专家系统等。";

        var result = _service.Split(text, "recursive", 50, 10);

        Assert.NotEmpty(result);
        Assert.All(result, chunk => Assert.False(string.IsNullOrWhiteSpace(chunk.Content)));
    }
}

public class RagServiceTests
{
    private readonly Mock<MAFStudio.Core.Interfaces.Repositories.IRagDocumentRepository> _docRepo;
    private readonly Mock<MAFStudio.Core.Interfaces.Repositories.IRagDocumentChunkRepository> _chunkRepo;
    private readonly Mock<MAFStudio.Core.Interfaces.Repositories.ISystemConfigRepository> _configRepo;
    private readonly Mock<IEmbeddingService> _embeddingService;
    private readonly Mock<IRerankService> _rerankService;
    private readonly Mock<IVectorStoreService> _vectorStoreService;
    private readonly Mock<ITextSplitterService> _textSplitterService;
    private readonly Mock<MAFStudio.Core.Interfaces.Services.IChatService> _chatService;
    private readonly Mock<MAFStudio.Core.Interfaces.Services.ILlmConfigService> _llmConfigService;
    private readonly Mock<ILogger<RagService>> _logger;
    private readonly RagService _ragService;

    public RagServiceTests()
    {
        _docRepo = new Mock<MAFStudio.Core.Interfaces.Repositories.IRagDocumentRepository>();
        _chunkRepo = new Mock<MAFStudio.Core.Interfaces.Repositories.IRagDocumentChunkRepository>();
        _configRepo = new Mock<MAFStudio.Core.Interfaces.Repositories.ISystemConfigRepository>();
        _embeddingService = new Mock<IEmbeddingService>();
        _rerankService = new Mock<IRerankService>();
        _vectorStoreService = new Mock<IVectorStoreService>();
        _textSplitterService = new Mock<ITextSplitterService>();
        _chatService = new Mock<MAFStudio.Core.Interfaces.Services.IChatService>();
        _llmConfigService = new Mock<MAFStudio.Core.Interfaces.Services.ILlmConfigService>();
        _logger = new Mock<ILogger<RagService>>();

        _configRepo.Setup(x => x.GetByKeyAsync("vector_db_collection"))
            .ReturnsAsync(new MAFStudio.Core.Entities.SystemConfig { Value = "test_collection" });
        _configRepo.Setup(x => x.GetByKeyAsync("default_split_method"))
            .ReturnsAsync(new MAFStudio.Core.Entities.SystemConfig { Value = "recursive" });
        _configRepo.Setup(x => x.GetByKeyAsync("default_chunk_size"))
            .ReturnsAsync(new MAFStudio.Core.Entities.SystemConfig { Value = "500" });
        _configRepo.Setup(x => x.GetByKeyAsync("default_chunk_overlap"))
            .ReturnsAsync(new MAFStudio.Core.Entities.SystemConfig { Value = "50" });
        _configRepo.Setup(x => x.GetByKeyAsync("skip_extensions"))
            .ReturnsAsync(new MAFStudio.Core.Entities.SystemConfig { Value = ".xml,.json" });

        _ragService = new RagService(
            _docRepo.Object,
            _chunkRepo.Object,
            _configRepo.Object,
            _embeddingService.Object,
            _rerankService.Object,
            _vectorStoreService.Object,
            _textSplitterService.Object,
            _chatService.Object,
            _llmConfigService.Object,
            _logger.Object);
    }

    [Fact]
    public async Task UploadDocumentAsync_CreatesDocument()
    {
        _docRepo.Setup(x => x.CreateAsync(It.IsAny<MAFStudio.Core.Entities.RagDocument>()))
            .ReturnsAsync((MAFStudio.Core.Entities.RagDocument doc) => doc);

        var result = await _ragService.UploadDocumentAsync("test.txt", "/path/test.txt", "txt", 1024, 1);

        Assert.Equal("test.txt", result.FileName);
        Assert.Equal(MAFStudio.Core.Enums.RagDocumentStatus.Pending, result.Status);
        _docRepo.Verify(x => x.CreateAsync(It.IsAny<MAFStudio.Core.Entities.RagDocument>()), Times.Once);
    }

    [Fact]
    public async Task GetDocumentsAsync_ReturnsUserDocs()
    {
        var docs = new List<MAFStudio.Core.Entities.RagDocument>
        {
            new() { Id = 1, FileName = "a.txt", UserId = 1 },
            new() { Id = 2, FileName = "b.txt", UserId = 1 },
        };
        _docRepo.Setup(x => x.GetByUserIdAsync("1")).ReturnsAsync(docs);

        var result = await _ragService.GetDocumentsAsync(1);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task DeleteDocumentAsync_DeletesDocAndChunks()
    {
        var doc = new MAFStudio.Core.Entities.RagDocument { Id = 1, FileName = "test.txt" };
        _docRepo.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(doc);
        _chunkRepo.Setup(x => x.DeleteByDocumentIdAsync(1)).ReturnsAsync(true);
        _docRepo.Setup(x => x.DeleteAsync(1)).ReturnsAsync(true);

        var result = await _ragService.DeleteDocumentAsync(1);

        Assert.True(result);
        _chunkRepo.Verify(x => x.DeleteByDocumentIdAsync(1), Times.Once);
        _docRepo.Verify(x => x.DeleteAsync(1), Times.Once);
    }

    [Fact]
    public async Task DeleteDocumentAsync_NotFound_ReturnsFalse()
    {
        _docRepo.Setup(x => x.GetByIdAsync(999)).ReturnsAsync((MAFStudio.Core.Entities.RagDocument?)null);

        var result = await _ragService.DeleteDocumentAsync(999);

        Assert.False(result);
    }

    [Fact]
    public async Task TestSplitAsync_DelegatesToTextSplitter()
    {
        var chunks = new List<TextChunk>
        {
            new() { Index = 0, Content = "chunk1" },
            new() { Index = 1, Content = "chunk2" },
        };
        _textSplitterService.Setup(x => x.Split("test", "recursive", 500, 50)).Returns(chunks);

        var result = await _ragService.TestSplitAsync("test", "recursive", 500, 50);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void GetSplitMethods_ReturnsAllMethods()
    {
        var methods = _ragService.GetSplitMethods();

        Assert.Equal(3, methods.Count);
        Assert.Contains(methods, m => m.Key == "recursive");
        Assert.Contains(methods, m => m.Key == "character");
        Assert.Contains(methods, m => m.Key == "separator");
    }

    [Fact]
    public void GetFileTypes_ReturnsSupportedTypes()
    {
        var types = _ragService.GetFileTypes();

        Assert.NotEmpty(types);
        Assert.Contains(types, t => t.Extension == ".pdf");
        Assert.Contains(types, t => t.Extension == ".md");
    }

    [Fact]
    public async Task QueryAsync_WithoutLlm_ReturnsSourcesOnly()
    {
        var queryVector = new float[128];
        _embeddingService.Setup(x => x.EmbedAsync("test query")).ReturnsAsync(queryVector);

        var searchResults = new List<VectorSearchResult>
        {
            new() { Id = "1_0", Content = "相关内容", Score = 0.9, Metadata = new Dictionary<string, string> { ["file_name"] = "doc.txt", ["document_id"] = "1", ["chunk_index"] = "0" } },
        };
        _vectorStoreService.Setup(x => x.SearchAsync("test_collection", queryVector, 5, 0.5f))
            .ReturnsAsync(searchResults);

        _rerankService.Setup(x => x.RerankAsync("test query", It.IsAny<List<string>>(), 5))
            .ReturnsAsync(new List<RerankResult>
            {
                new() { Index = 0, Text = "相关内容", RelevanceScore = 0.95 },
            });

        var result = await _ragService.QueryAsync("test query");

        Assert.Null(result.Answer);
        Assert.Single(result.Sources);
        Assert.Equal(0.95, result.Sources[0].Score);
    }
}
