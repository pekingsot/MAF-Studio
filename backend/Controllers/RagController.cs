using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using MAFStudio.Backend.Data;
using MAFStudio.Backend.Services;
using MAFStudio.Backend.Abstractions;
using MAFStudio.Backend.Services.DocumentParsing;
using MAFStudio.Backend.Models.Requests;
using System.Security.Claims;

namespace MAFStudio.Backend.Controllers
{
    /// <summary>
    /// RAG控制器
    /// 提供文档上传、分割、向量入库和检索功能
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class RagController : ControllerBase
    {
        private readonly IRagService _ragService;
        private readonly ISystemConfigService _configService;
        private readonly ILLMConfigService _llmConfigService;
        private readonly DocumentParserFactory _parserFactory;

        /// <summary>
        /// 构造函数
        /// </summary>
        public RagController(
            IRagService ragService,
            ISystemConfigService configService,
            ILLMConfigService llmConfigService)
        {
            _ragService = ragService;
            _configService = configService;
            _llmConfigService = llmConfigService;
            _parserFactory = new DocumentParserFactory();
        }

        private string? GetUserId() => User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        /// <summary>
        /// 获取所有文档
        /// </summary>
        [HttpGet("documents")]
        public async Task<ActionResult<List<RagDocument>>> GetAllDocuments()
        {
            var userId = GetUserId();
            var documents = await _ragService.GetAllDocumentsAsync(userId);
            return Ok(documents);
        }

        /// <summary>
        /// 获取文档详情
        /// </summary>
        [HttpGet("documents/{id}")]
        public async Task<ActionResult<RagDocument>> GetDocument(Guid id)
        {
            var userId = GetUserId();
            var document = await _ragService.GetDocumentByIdAsync(id, userId);
            if (document == null)
            {
                return NotFound();
            }
            return Ok(document);
        }

        /// <summary>
        /// 获取文档分块
        /// </summary>
        [HttpGet("documents/{id}/chunks")]
        public async Task<ActionResult<List<RagDocumentChunk>>> GetDocumentChunks(Guid id)
        {
            var chunks = await _ragService.GetDocumentChunksAsync(id);
            return Ok(chunks);
        }

        /// <summary>
        /// 上传文档
        /// </summary>
        [HttpPost("documents")]
        public async Task<ActionResult<RagDocument>> UploadDocument([FromBody] UploadDocumentRequest request)
        {
            var userId = GetUserId();
            var skipExtensions = await _configService.GetConfigByKeyAsync("skip_extensions");
            var defaultSplitMethod = await _configService.GetConfigByKeyAsync("default_split_method");
            var defaultChunkSize = await _configService.GetConfigByKeyAsync("default_chunk_size");
            var defaultChunkOverlap = await _configService.GetConfigByKeyAsync("default_chunk_overlap");

            var document = await _ragService.UploadDocumentAsync(
                request.FileName,
                request.Content,
                request.FileType,
                request.SplitMethod ?? defaultSplitMethod?.Value ?? "recursive",
                request.ChunkSize ?? (defaultChunkSize != null ? int.Parse(defaultChunkSize.Value) : 500),
                request.ChunkOverlap ?? (defaultChunkOverlap != null ? int.Parse(defaultChunkOverlap.Value) : 50),
                skipExtensions?.Value,
                userId
            );
            return Ok(document);
        }

        /// <summary>
        /// 上传文件（支持多种格式）
        /// </summary>
        [HttpPost("documents/upload-file")]
        [RequestSizeLimit(100_000_000)]
        public async Task<ActionResult<RagDocument>> UploadFile(
            IFormFile file,
            [FromForm] string? splitMethod,
            [FromForm] int? chunkSize,
            [FromForm] int? chunkOverlap)
        {
            var userId = GetUserId();
            
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { message = "请选择要上传的文件" });
            }

            var fileName = file.FileName;
            var extension = Path.GetExtension(fileName).TrimStart('.').ToLower();

            if (string.IsNullOrEmpty(extension))
            {
                return BadRequest(new { message = "无法识别文件类型" });
            }

            byte[] fileContent;
            using (var memoryStream = new MemoryStream())
            {
                await file.CopyToAsync(memoryStream);
                fileContent = memoryStream.ToArray();
            }

            string content;
            try
            {
                var parser = _parserFactory.GetParser(extension);
                content = await parser.ParseAsync(fileContent, fileName);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = $"文件解析失败: {ex.Message}" });
            }

            var skipExtensions = await _configService.GetConfigByKeyAsync("skip_extensions");
            var defaultSplitMethod = await _configService.GetConfigByKeyAsync("default_split_method");
            var defaultChunkSize = await _configService.GetConfigByKeyAsync("default_chunk_size");
            var defaultChunkOverlap = await _configService.GetConfigByKeyAsync("default_chunk_overlap");

            var document = await _ragService.UploadDocumentAsync(
                fileName,
                content,
                $".{extension}",
                splitMethod ?? defaultSplitMethod?.Value ?? "recursive",
                chunkSize ?? (defaultChunkSize != null ? int.Parse(defaultChunkSize.Value) : 500),
                chunkOverlap ?? (defaultChunkOverlap != null ? int.Parse(defaultChunkOverlap.Value) : 50),
                skipExtensions?.Value,
                userId
            );

            return Ok(document);
        }

        /// <summary>
        /// 获取支持的文件类型
        /// </summary>
        [HttpGet("supported-types")]
        [AllowAnonymous]
        public ActionResult GetSupportedFileTypes()
        {
            var extensions = _parserFactory.GetAllSupportedExtensions();
            return Ok(extensions.Select(e => new { extension = $".{e}", name = GetFileTypeDescription(e) }));
        }

        /// <summary>
        /// 获取文件类型描述
        /// </summary>
        private string GetFileTypeDescription(string extension)
        {
            return extension.ToLower() switch
            {
                "txt" => "文本文件",
                "md" or "markdown" => "Markdown文档",
                "log" => "日志文件",
                "rst" => "reStructuredText",
                "org" => "Org模式文档",
                "py" => "Python代码",
                "js" => "JavaScript代码",
                "ts" => "TypeScript代码",
                "jsx" or "tsx" => "React组件",
                "java" => "Java代码",
                "c" => "C代码",
                "cpp" or "cc" or "cxx" => "C++代码",
                "h" or "hpp" => "C/C++头文件",
                "cs" => "C#代码",
                "go" => "Go代码",
                "rs" => "Rust代码",
                "rb" => "Ruby代码",
                "php" => "PHP代码",
                "swift" => "Swift代码",
                "kt" => "Kotlin代码",
                "scala" => "Scala代码",
                "lua" => "Lua代码",
                "perl" or "pl" => "Perl代码",
                "sh" or "bash" or "zsh" => "Shell脚本",
                "bat" or "cmd" => "批处理脚本",
                "ps1" => "PowerShell脚本",
                "sql" => "SQL脚本",
                "html" or "htm" => "HTML网页",
                "css" => "CSS样式",
                "scss" or "sass" => "Sass样式",
                "less" => "Less样式",
                "xml" or "xaml" => "XML文档",
                "json" => "JSON数据",
                "yaml" or "yml" => "YAML配置",
                "toml" => "TOML配置",
                "ini" => "INI配置",
                "env" => "环境变量",
                "dockerfile" => "Docker配置",
                "makefile" => "Makefile",
                "csv" => "CSV表格",
                "tsv" => "TSV表格",
                "r" => "R代码",
                "m" => "MATLAB/Objective-C",
                _ => extension.ToUpper()
            };
        }

        /// <summary>
        /// 删除文档
        /// </summary>
        [HttpDelete("documents/{id}")]
        public async Task<ActionResult> DeleteDocument(Guid id)
        {
            var userId = GetUserId();
            var result = await _ragService.DeleteDocumentAsync(id, userId);
            if (!result)
            {
                return NotFound(new { message = "文档不存在或无权限删除" });
            }
            return NoContent();
        }

        /// <summary>
        /// 向量入库
        /// </summary>
        [HttpPost("documents/{id}/vectorize")]
        public async Task<ActionResult> VectorizeDocument(Guid id)
        {
            var userId = GetUserId();
            var vectorDbEndpoint = await _configService.GetConfigByKeyAsync("vector_db_endpoint");
            var vectorizationEndpoint = await _configService.GetConfigByKeyAsync("vectorization_endpoint");
            var collection = await _configService.GetConfigByKeyAsync("vector_db_collection");

            if (string.IsNullOrEmpty(vectorDbEndpoint?.Value))
            {
                return BadRequest(new { message = "未配置向量库接口地址，请先在RAG配置中设置" });
            }

            if (string.IsNullOrEmpty(vectorizationEndpoint?.Value))
            {
                return BadRequest(new { message = "未配置向量化接口地址，请先在RAG配置中设置" });
            }

            var result = await _ragService.VectorizeDocumentAsync(
                id,
                vectorizationEndpoint.Value,
                vectorDbEndpoint.Value,
                collection?.Value ?? "rag_documents",
                userId
            );

            return Ok(new { successCount = result });
        }

        /// <summary>
        /// RAG检索
        /// </summary>
        [HttpPost("query")]
        public async Task<ActionResult> RagQuery([FromBody] RagQueryRequest request)
        {
            var userId = GetUserId();
            var vectorDbEndpoint = await _configService.GetConfigByKeyAsync("vector_db_endpoint");
            var vectorizationEndpoint = await _configService.GetConfigByKeyAsync("vectorization_endpoint");
            var collection = await _configService.GetConfigByKeyAsync("vector_db_collection");

            if (string.IsNullOrEmpty(vectorDbEndpoint?.Value))
            {
                return BadRequest(new { message = "未配置向量库接口地址" });
            }

            if (string.IsNullOrEmpty(vectorizationEndpoint?.Value))
            {
                return BadRequest(new { message = "未配置向量化接口地址" });
            }

            var llmConfig = await _llmConfigService.GetConfigByIdAsync(request.LlmConfigId, userId);
            if (llmConfig == null)
            {
                return BadRequest(new { message = "未找到指定的大模型配置" });
            }

            LLMModelConfig? modelConfig = null;
            if (request.LlmModelConfigId.HasValue)
            {
                modelConfig = llmConfig.Models?.FirstOrDefault(m => m.Id == request.LlmModelConfigId.Value);
            }
            modelConfig ??= llmConfig.Models?.FirstOrDefault(m => m.IsDefault) ?? llmConfig.Models?.FirstOrDefault();

            var result = await _ragService.RagQueryAsync(
                request.Query,
                vectorizationEndpoint.Value,
                vectorDbEndpoint.Value,
                collection?.Value ?? "rag_documents",
                llmConfig,
                modelConfig,
                request.TopK,
                request.ScoreThreshold,
                request.SystemPrompt
            );

            return Ok(result);
        }

        /// <summary>
        /// 测试文本分割
        /// </summary>
        [HttpPost("test-split")]
        public async Task<ActionResult> TestSplit([FromBody] TestSplitRequest request)
        {
            var result = await _ragService.TestSplitAsync(
                request.Content,
                request.SplitMethod ?? "recursive",
                request.ChunkSize ?? 500,
                request.ChunkOverlap ?? 50
            );
            return Ok(result);
        }

        /// <summary>
        /// 获取支持的分割方式
        /// </summary>
        [HttpGet("split-methods")]
        [AllowAnonymous]
        public ActionResult GetSplitMethods()
        {
            var methods = new[]
            {
                new { value = "character", label = "按字符分割", description = "按固定字符数分割文本" },
                new { value = "recursive", label = "递归分割", description = "按段落、句子、词递归分割（推荐）" },
                new { value = "separator", label = "按分隔符分割", description = "按自定义分隔符分割" }
            };
            return Ok(methods);
        }

        /// <summary>
        /// 获取文件类型列表（用于文本粘贴模式）
        /// </summary>
        [HttpGet("file-types")]
        [AllowAnonymous]
        public async Task<ActionResult> GetFileTypes()
        {
            var skipExtensions = await _configService.GetConfigByKeyAsync("skip_extensions");
            var skipExtList = (skipExtensions?.Value ?? ".xml,.json,.yml,.yaml,.dockerfile,.sh")
                .Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(e => e.Trim().ToLower())
                .ToHashSet();

            var types = new[]
            {
                new { ext = ".txt", name = "文本文件", needSplit = true },
                new { ext = ".md", name = "Markdown", needSplit = true },
                new { ext = ".pdf", name = "PDF文档", needSplit = true },
                new { ext = ".doc", name = "Word文档", needSplit = true },
                new { ext = ".docx", name = "Word文档", needSplit = true },
                new { ext = ".xls", name = "Excel表格", needSplit = true },
                new { ext = ".xlsx", name = "Excel表格", needSplit = true },
                new { ext = ".csv", name = "CSV文件", needSplit = true },
                new { ext = ".html", name = "HTML网页", needSplit = true },
                new { ext = ".json", name = "JSON文件", needSplit = !skipExtList.Contains(".json") },
                new { ext = ".xml", name = "XML文件", needSplit = !skipExtList.Contains(".xml") },
                new { ext = ".yml", name = "YAML文件", needSplit = !skipExtList.Contains(".yml") },
                new { ext = ".yaml", name = "YAML文件", needSplit = !skipExtList.Contains(".yaml") },
                new { ext = ".toml", name = "TOML文件", needSplit = !skipExtList.Contains(".toml") },
                new { ext = ".ini", name = "INI配置", needSplit = !skipExtList.Contains(".ini") },
                new { ext = ".env", name = "环境变量", needSplit = !skipExtList.Contains(".env") },
                new { ext = ".sh", name = "Shell脚本", needSplit = !skipExtList.Contains(".sh") },
                new { ext = ".bat", name = "批处理", needSplit = !skipExtList.Contains(".bat") },
                new { ext = ".ps1", name = "PowerShell", needSplit = !skipExtList.Contains(".ps1") },
                new { ext = ".dockerfile", name = "Dockerfile", needSplit = !skipExtList.Contains(".dockerfile") },
                new { ext = ".py", name = "Python代码", needSplit = true },
                new { ext = ".js", name = "JavaScript代码", needSplit = true },
                new { ext = ".ts", name = "TypeScript代码", needSplit = true },
                new { ext = ".java", name = "Java代码", needSplit = true },
                new { ext = ".cs", name = "C#代码", needSplit = true },
                new { ext = ".go", name = "Go代码", needSplit = true },
                new { ext = ".rs", name = "Rust代码", needSplit = true }
            };
            return Ok(types);
        }

        /// <summary>
        /// 查询向量文档列表
        /// </summary>
        [HttpGet("vector-documents")]
        public async Task<ActionResult> GetVectorDocuments([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? keyword = null)
        {
            var vectorDbEndpoint = await _configService.GetConfigByKeyAsync("vector_db_endpoint");
            var collection = await _configService.GetConfigByKeyAsync("vector_db_collection");

            if (string.IsNullOrEmpty(vectorDbEndpoint?.Value))
            {
                return Ok(new { items = new List<object>(), total = 0, message = "未配置向量库接口地址" });
            }

            var result = await _ragService.GetVectorDocumentsAsync(
                vectorDbEndpoint.Value,
                collection?.Value ?? "rag_documents",
                page,
                pageSize,
                keyword
            );

            return Ok(result);
        }

        /// <summary>
        /// 删除向量文档
        /// </summary>
        [HttpDelete("vector-documents/{id}")]
        public async Task<ActionResult> DeleteVectorDocument(string id)
        {
            var vectorDbEndpoint = await _configService.GetConfigByKeyAsync("vector_db_endpoint");
            var collection = await _configService.GetConfigByKeyAsync("vector_db_collection");

            if (string.IsNullOrEmpty(vectorDbEndpoint?.Value))
            {
                return BadRequest(new { message = "未配置向量库接口地址" });
            }

            var result = await _ragService.DeleteVectorDocumentAsync(
                vectorDbEndpoint.Value,
                collection?.Value ?? "rag_documents",
                id
            );

            if (result)
            {
                return Ok(new { message = "删除成功" });
            }
            return BadRequest(new { message = "删除失败" });
        }
    }
}
