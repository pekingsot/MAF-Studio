using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Application.DTOs.Rag;
using MAFStudio.Core.Interfaces.Services;
using MAFStudio.Api.Extensions;

namespace MAFStudio.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class RagController : ControllerBase
{
    private readonly IRagService _ragService;
    private readonly ILogger<RagController> _logger;

    public RagController(
        IRagService ragService,
        ILogger<RagController> logger)
    {
        _ragService = ragService;
        _logger = logger;
    }

    [HttpGet("documents")]
    public async Task<ActionResult> GetDocuments()
    {
        var userId = User.GetUserId();
        var documents = await _ragService.GetDocumentsAsync(userId);
        return Ok(documents);
    }

    [HttpGet("documents/{id}")]
    public async Task<ActionResult> GetDocument(long id)
    {
        var document = await _ragService.GetDocumentAsync(id);
        if (document == null)
        {
            return NotFound(new { message = "文档不存在" });
        }
        return Ok(document);
    }

    [HttpPost("documents")]
    public async Task<ActionResult> CreateDocument([FromBody] RagCreateDocumentDto dto)
    {
        var userId = User.GetUserId();
        var document = await _ragService.UploadDocumentAsync(
            dto.FileName ?? "untitled.txt",
            null,
            dto.FileType,
            dto.Content?.Length ?? 0,
            userId,
            dto.SplitMethod,
            dto.ChunkSize,
            dto.ChunkOverlap);

        if (!string.IsNullOrEmpty(dto.Content))
        {
            document = await _ragService.UploadDocumentAsync(
                dto.FileName ?? "untitled.txt",
                null,
                dto.FileType,
                dto.Content.Length,
                userId,
                dto.SplitMethod,
                dto.ChunkSize,
                dto.ChunkOverlap);
        }

        return Ok(document);
    }

    [HttpPost("documents/upload-file")]
    public async Task<ActionResult> UploadFile(IFormFile file, string? splitMethod = null, int? chunkSize = null, int? chunkOverlap = null)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new { message = "请选择文件" });
        }

        var userId = User.GetUserId();
        var tempPath = Path.Combine(Path.GetTempPath(), $"rag_upload_{Guid.NewGuid()}");
        
        try
        {
            using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var extension = Path.GetExtension(file.FileName)?.TrimStart('.').ToLower() ?? "txt";
            var document = await _ragService.UploadDocumentAsync(
                file.FileName,
                tempPath,
                extension,
                file.Length,
                userId,
                splitMethod,
                chunkSize,
                chunkOverlap);

            return Ok(document);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "文件上传失败");
            return StatusCode(500, new { message = $"文件上传失败: {ex.Message}" });
        }
    }

    [HttpDelete("documents/{id}")]
    public async Task<ActionResult> DeleteDocument(long id)
    {
        var result = await _ragService.DeleteDocumentAsync(id);
        if (!result)
        {
            return NotFound(new { message = "文档不存在" });
        }
        return Ok(new { message = "删除成功" });
    }

    [HttpGet("documents/{id}/chunks")]
    public async Task<ActionResult> GetChunks(long id)
    {
        var chunks = await _ragService.GetChunksAsync(id);
        return Ok(chunks);
    }

    [HttpPost("documents/{id}/vectorize")]
    public async Task<ActionResult> Vectorize(long id)
    {
        try
        {
            var result = await _ragService.VectorizeAsync(id);
            return Ok(new { successCount = result.SuccessCount, errorMessage = result.ErrorMessage });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "向量入库失败");
            return StatusCode(500, new { message = $"向量入库失败: {ex.Message}" });
        }
    }

    [HttpPost("test-split")]
    public async Task<ActionResult> TestSplit([FromBody] RagTestSplitDto dto)
    {
        var result = await _ragService.TestSplitAsync(dto.Content, dto.SplitMethod, dto.ChunkSize, dto.ChunkOverlap);
        return Ok(new
        {
            chunkCount = result.Count,
            chunks = result.Select(c => new { chunkIndex = c.Index, content = c.Content })
        });
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
                dto.LlmModelConfigId,
                dto.SystemPrompt);

            return Ok(new
            {
                answer = result.Answer,
                chunkCount = result.Sources.Count,
                sources = result.Sources.Select(s => new
                {
                    content = s.Content,
                    score = s.Score,
                    documentId = s.DocumentId,
                    chunkIndex = s.ChunkIndex
                })
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "RAG查询失败");
            return StatusCode(500, new { message = $"RAG查询失败: {ex.Message}" });
        }
    }

    [HttpGet("split-methods")]
    public ActionResult GetSplitMethods()
    {
        var methods = _ragService.GetSplitMethods();
        return Ok(methods.Select(m => new
        {
            value = m.Key,
            label = m.Name,
            description = m.Description
        }));
    }

    [HttpGet("file-types")]
    public ActionResult GetFileTypes()
    {
        var types = _ragService.GetFileTypes();
        return Ok(types.Select(t => new
        {
            value = t.Extension,
            label = t.Name,
            extension = t.Extension,
            name = t.Name,
            needSplit = t.Category != "image"
        }));
    }

    [HttpGet("supported-types")]
    public ActionResult GetSupportedTypes()
    {
        var types = _ragService.GetFileTypes();
        return Ok(types.Select(t => new
        {
            value = t.Extension,
            label = t.Name,
            extension = $".{t.Extension}",
            name = t.Name,
            needSplit = t.Category != "image"
        }));
    }

    [HttpGet("vector-documents")]
    public async Task<ActionResult> GetVectorDocs([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? keyword = null)
    {
        var result = await _ragService.GetVectorDocsAsync(page, pageSize, keyword);
        return Ok(new
        {
            items = result.Documents.Select(d => new
            {
                id = d.Id,
                content = d.Content,
                score = d.Score,
                documentId = d.DocumentId,
                chunkIndex = d.ChunkIndex
            }),
            total = result.Total
        });
    }

    [HttpDelete("vector-documents/{id}")]
    public async Task<ActionResult> DeleteVectorDoc(string id)
    {
        var result = await _ragService.DeleteVectorDocAsync(id);
        if (!result)
        {
            return NotFound(new { message = "向量文档不存在" });
        }
        return Ok(new { message = "删除成功" });
    }
}
