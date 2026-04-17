using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MAFStudio.Application.Services.Rag;
using MAFStudio.Core.Entities;
using MAFStudio.Core.Interfaces.Repositories;

namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RagController : ControllerBase
{
    private readonly RagService _ragService;
    private readonly IRagDocumentRepository _documentRepo;
    private readonly IRagDocumentChunkRepository _chunkRepo;
    private readonly ILogger<RagController> _logger;

    public RagController(
        RagService ragService,
        IRagDocumentRepository documentRepo,
        IRagDocumentChunkRepository chunkRepo,
        ILogger<RagController> logger)
    {
        _ragService = ragService;
        _documentRepo = documentRepo;
        _chunkRepo = chunkRepo;
        _logger = logger;
    }

    [HttpGet("documents")]
    public async Task<ActionResult> GetDocuments()
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? "1";
            var docs = await _ragService.GetDocumentsAsync(long.Parse(userId));
            return Ok(docs);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取文档列表失败");
            return BadRequest(new { message = $"获取文档列表失败: {ex.Message}" });
        }
    }

    [HttpGet("documents/{id}")]
    public async Task<ActionResult> GetDocument(long id)
    {
        try
        {
            var doc = await _ragService.GetDocumentAsync(id);
            if (doc == null) return NotFound(new { message = "文档不存在" });
            return Ok(doc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取文档失败: Id={Id}", id);
            return BadRequest(new { message = $"获取文档失败: {ex.Message}" });
        }
    }

    [HttpPost("documents/upload")]
    public async Task<ActionResult> UploadDocument(
        [FromForm] IFormFile file,
        [FromForm] string? splitMethod = null,
        [FromForm] int? chunkSize = null,
        [FromForm] int? chunkOverlap = null)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { message = "请选择文件" });

            var userId = User.FindFirst("sub")?.Value ?? "1";
            var workspaceBase = "/home/pekingost/workspace";
            var uploadDir = Path.Combine(workspaceBase, "rag-uploads");
            Directory.CreateDirectory(uploadDir);

            var fileName = $"{Guid.NewGuid()}_{file.FileName}";
            var filePath = Path.Combine(uploadDir, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var doc = await _ragService.UploadDocumentAsync(
                file.FileName,
                filePath,
                Path.GetExtension(file.FileName).TrimStart('.'),
                file.Length,
                long.Parse(userId),
                splitMethod,
                chunkSize,
                chunkOverlap);

            return Ok(doc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "上传文档失败");
            return BadRequest(new { message = $"上传文档失败: {ex.Message}" });
        }
    }

    [HttpDelete("documents/{id}")]
    public async Task<ActionResult> DeleteDocument(long id)
    {
        try
        {
            var deleted = await _ragService.DeleteDocumentAsync(id);
            if (!deleted) return NotFound(new { message = "文档不存在" });
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除文档失败: Id={Id}", id);
            return BadRequest(new { message = $"删除文档失败: {ex.Message}" });
        }
    }

    [HttpPost("documents/{id}/vectorize")]
    public async Task<ActionResult> VectorizeDocument(long id)
    {
        try
        {
            var result = await _ragService.VectorizeAsync(id);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "向量入库失败: DocId={DocId}", id);
            return BadRequest(new { message = $"向量入库失败: {ex.Message}" });
        }
    }

    [HttpGet("documents/{id}/chunks")]
    public async Task<ActionResult> GetDocumentChunks(long id)
    {
        try
        {
            var chunks = await _ragService.GetChunksAsync(id);
            return Ok(chunks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取文档分块失败: DocId={DocId}", id);
            return BadRequest(new { message = $"获取分块失败: {ex.Message}" });
        }
    }

    [HttpPost("query")]
    public async Task<ActionResult> Query([FromBody] RagQueryDto dto)
    {
        try
        {
            var result = await _ragService.QueryAsync(
                dto.Query,
                dto.TopK ?? 5,
                dto.ScoreThreshold ?? 0.5,
                dto.LlmConfigId,
                dto.SystemPrompt);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RAG查询失败");
            return BadRequest(new { message = $"RAG查询失败: {ex.Message}" });
        }
    }

    [HttpPost("test-split")]
    public async Task<ActionResult> TestSplit([FromBody] RagTestSplitDto dto)
    {
        try
        {
            var chunks = await _ragService.TestSplitAsync(dto.Content, dto.SplitMethod, dto.ChunkSize, dto.ChunkOverlap);
            return Ok(new
            {
                chunkCount = chunks.Count,
                chunks = chunks.Select(c => new { content = c.Content, chunkIndex = c.Index }),
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "测试分割失败");
            return BadRequest(new { message = $"测试分割失败: {ex.Message}" });
        }
    }

    [HttpGet("vector-docs")]
    public async Task<ActionResult> GetVectorDocs([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? keyword = null)
    {
        try
        {
            var result = await _ragService.GetVectorDocsAsync(page, pageSize, keyword);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取向量文档失败");
            return BadRequest(new { message = $"获取向量文档失败: {ex.Message}" });
        }
    }

    [HttpDelete("vector-docs/{id}")]
    public async Task<ActionResult> DeleteVectorDoc(string id)
    {
        try
        {
            var deleted = await _ragService.DeleteVectorDocAsync(id);
            if (!deleted) return NotFound(new { message = "向量文档不存在" });
            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "删除向量文档失败: Id={Id}", id);
            return BadRequest(new { message = $"删除向量文档失败: {ex.Message}" });
        }
    }

    [HttpGet("split-methods")]
    public ActionResult GetSplitMethods()
    {
        var methods = _ragService.GetSplitMethods();
        return Ok(methods.Select(m => new { value = m.Key, label = m.Name, description = m.Description }));
    }

    [HttpGet("file-types")]
    public ActionResult GetFileTypes()
    {
        var types = _ragService.GetFileTypes();
        return Ok(types.Select(t => new { ext = t.Extension, name = t.Name, category = t.Category }));
    }

    [HttpGet("supported-types")]
    public ActionResult GetSupportedTypes()
    {
        var types = _ragService.GetFileTypes();
        return Ok(types.Select(t => new { extension = t.Extension, name = t.Name, category = t.Category }));
    }

    [HttpPost("documents")]
    public async Task<ActionResult> CreateDocument([FromBody] RagCreateDocumentDto dto)
    {
        try
        {
            var userId = User.FindFirst("sub")?.Value ?? "1";
            var workspaceBase = "/home/pekingost/workspace";
            var uploadDir = Path.Combine(workspaceBase, "rag-uploads");
            Directory.CreateDirectory(uploadDir);

            var fileName = dto.FileName ?? "untitled.txt";
            var filePath = Path.Combine(uploadDir, $"{Guid.NewGuid()}_{fileName}");
            await System.IO.File.WriteAllTextAsync(filePath, dto.Content ?? "");

            var doc = await _ragService.UploadDocumentAsync(
                fileName,
                filePath,
                dto.FileType ?? Path.GetExtension(fileName).TrimStart('.'),
                System.Text.Encoding.UTF8.GetByteCount(dto.Content ?? ""),
                long.Parse(userId),
                dto.SplitMethod,
                dto.ChunkSize,
                dto.ChunkOverlap);

            return Ok(doc);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "创建文档失败");
            return BadRequest(new { message = $"创建文档失败: {ex.Message}" });
        }
    }

    [HttpPost("documents/upload-file")]
    public async Task<ActionResult> UploadFile(
        [FromForm] IFormFile file,
        [FromForm] string? splitMethod = null,
        [FromForm] int? chunkSize = null,
        [FromForm] int? chunkOverlap = null)
    {
        return await UploadDocument(file, splitMethod, chunkSize, chunkOverlap);
    }

    [HttpGet("vector-documents")]
    public async Task<ActionResult> GetVectorDocuments([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? keyword = null)
    {
        try
        {
            var result = await _ragService.GetVectorDocsAsync(page, pageSize, keyword);
            return Ok(new { items = result.Documents, total = result.Total });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "获取向量文档失败");
            return BadRequest(new { message = $"获取向量文档失败: {ex.Message}" });
        }
    }

    [HttpDelete("vector-documents/{id}")]
    public async Task<ActionResult> DeleteVectorDocument(string id)
    {
        return await DeleteVectorDoc(id);
    }
}

public class RagQueryDto
{
    public string Query { get; set; } = string.Empty;
    public int? TopK { get; set; }
    public double? ScoreThreshold { get; set; }
    public long? LlmConfigId { get; set; }
    public string? SystemPrompt { get; set; }
}

public class RagTestSplitDto
{
    public string Content { get; set; } = string.Empty;
    public string? SplitMethod { get; set; }
    public int? ChunkSize { get; set; }
    public int? ChunkOverlap { get; set; }
}

public class RagCreateDocumentDto
{
    public string? FileName { get; set; }
    public string? Content { get; set; }
    public string? FileType { get; set; }
    public string? SplitMethod { get; set; }
    public int? ChunkSize { get; set; }
    public int? ChunkOverlap { get; set; }
}
