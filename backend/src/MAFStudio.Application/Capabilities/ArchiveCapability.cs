using System.ComponentModel;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace MAFStudio.Application.Capabilities;

/// <summary>
/// 压缩解压能力，提供 ZIP 文件处理功能
/// </summary>
public class ArchiveCapability : ICapability
{
    public string Name => "ArchiveCapability";
    public string Description => "压缩解压能力，支持 ZIP 文件的创建、解压和查看";

    public IEnumerable<MethodInfo> GetTools()
    {
        return GetType().GetMethods()
            .Where(m => m.GetCustomAttribute<ToolAttribute>() != null);
    }

    [Tool("Extract a ZIP archive to a directory.")]
    public string ExtractZip(
        [Description("Absolute path to the ZIP file")] string zipPath,
        [Description("Absolute path to the destination directory")] string destPath,
        [Description("Overwrite existing files. Default false")] bool overwrite = false)
    {
        try
        {
            if (!File.Exists(zipPath))
                return $"ZIP 文件不存在: {zipPath}";

            if (!Directory.Exists(destPath))
                Directory.CreateDirectory(destPath);

            var extractedFiles = new List<string>();
            var extractedDirs = new List<string>();

            using var archive = ZipFile.OpenRead(zipPath);
            var totalEntries = archive.Entries.Count;
            var totalSize = archive.Entries.Sum(e => e.Length);

            foreach (var entry in archive.Entries)
            {
                var fullPath = Path.GetFullPath(Path.Combine(destPath, entry.FullName));

                if (string.IsNullOrEmpty(entry.Name))
                {
                    // 目录
                    if (!Directory.Exists(fullPath))
                    {
                        Directory.CreateDirectory(fullPath);
                        extractedDirs.Add(entry.FullName);
                    }
                }
                else
                {
                    // 文件
                    var dir = Path.GetDirectoryName(fullPath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    if (File.Exists(fullPath) && !overwrite)
                    {
                        continue;
                    }

                    entry.ExtractToFile(fullPath, overwrite);
                    extractedFiles.Add(entry.FullName);
                }
            }

            return JsonSerializer.Serialize(new
            {
                Success = true,
                SourceFile = zipPath,
                DestinationPath = destPath,
                TotalEntries = totalEntries,
                ExtractedFiles = extractedFiles.Count,
                ExtractedDirectories = extractedDirs.Count,
                TotalSize = FormatFileSize(totalSize)
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return $"解压失败: {ex.Message}";
        }
    }

    [Tool("Create a ZIP archive from a file or directory.")]
    public string CreateZip(
        [Description("Absolute path to the file or directory to archive")] string sourcePath,
        [Description("Absolute path for the new ZIP file")] string zipPath,
        [Description("Include the base directory name in the archive. Default false")] bool includeBaseDirectory = false,
        [Description("Compression level: 'Optimal', 'Fastest', or 'NoCompression'. Default 'Optimal'")] string? compressionLevel = "Optimal")
    {
        try
        {
            var level = compressionLevel?.ToLowerInvariant() switch
            {
                "optimal" => CompressionLevel.Optimal,
                "fastest" => CompressionLevel.Fastest,
                "nocompression" or "none" => CompressionLevel.NoCompression,
                _ => CompressionLevel.Optimal
            };

            var fileInfo = new FileInfo(zipPath);
            if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
            {
                fileInfo.Directory.Create();
            }

            long totalSize = 0;
            int fileCount = 0;

            if (Directory.Exists(sourcePath))
            {
                ZipFile.CreateFromDirectory(sourcePath, zipPath, level, includeBaseDirectory);
                
                var dirInfo = new DirectoryInfo(sourcePath);
                var files = dirInfo.GetFiles("*", SearchOption.AllDirectories);
                fileCount = files.Length;
                totalSize = files.Sum(f => f.Length);
            }
            else if (File.Exists(sourcePath))
            {
                using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Create);
                archive.CreateEntryFromFile(sourcePath, Path.GetFileName(sourcePath), level);
                fileCount = 1;
                totalSize = new FileInfo(sourcePath).Length;
            }
            else
            {
                return $"源路径不存在: {sourcePath}";
            }

            var zipInfo = new FileInfo(zipPath);

            return JsonSerializer.Serialize(new
            {
                Success = true,
                SourcePath = sourcePath,
                ZipFile = zipPath,
                SourceSize = FormatFileSize(totalSize),
                ZipSize = FormatFileSize(zipInfo.Length),
                CompressionRatio = totalSize > 0 ? $"{(double)zipInfo.Length / totalSize:P1}" : "0%",
                FileCount = fileCount
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return $"创建 ZIP 失败: {ex.Message}";
        }
    }

    [Tool("List the contents of a ZIP archive.")]
    public string ListArchive(
        [Description("Absolute path to the ZIP file")] string zipPath)
    {
        try
        {
            if (!File.Exists(zipPath))
                return $"ZIP 文件不存在: {zipPath}";

            using var archive = ZipFile.OpenRead(zipPath);
            var entries = new List<object>();
            long totalSize = 0;
            long compressedSize = 0;

            foreach (var entry in archive.Entries)
            {
                totalSize += entry.Length;
                compressedSize += entry.CompressedLength;

                entries.Add(new
                {
                    Name = entry.FullName,
                    Size = FormatFileSize(entry.Length),
                    CompressedSize = FormatFileSize(entry.CompressedLength),
                    CompressionRatio = entry.Length > 0 ? $"{(double)entry.CompressedLength / entry.Length:P1}" : "0%",
                    LastModified = entry.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"),
                    IsDirectory = string.IsNullOrEmpty(entry.Name)
                });
            }

            return JsonSerializer.Serialize(new
            {
                ZipFile = zipPath,
                TotalEntries = archive.Entries.Count,
                TotalSize = FormatFileSize(totalSize),
                TotalCompressedSize = FormatFileSize(compressedSize),
                OverallCompressionRatio = totalSize > 0 ? $"{(double)compressedSize / totalSize:P1}" : "0%",
                Entries = entries
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return $"读取 ZIP 失败: {ex.Message}";
        }
    }

    [Tool("Add a file to an existing ZIP archive.")]
    public string AddToZip(
        [Description("Absolute path to the ZIP file")] string zipPath,
        [Description("Absolute path to the file to add")] string filePath,
        [Description("Name for the entry inside the ZIP. Defaults to the file name")] string? entryName = null)
    {
        try
        {
            if (!File.Exists(filePath))
                return $"文件不存在: {filePath}";

            var fileInfo = new FileInfo(zipPath);
            if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
            {
                fileInfo.Directory.Create();
            }

            var entryPath = entryName ?? Path.GetFileName(filePath);

            using var archive = ZipFile.Open(zipPath, File.Exists(zipPath) ? ZipArchiveMode.Update : ZipArchiveMode.Create);
            
            var existingEntry = archive.GetEntry(entryPath);
            existingEntry?.Delete();

            archive.CreateEntryFromFile(filePath, entryPath);

            return $"成功添加文件 '{filePath}' 到 ZIP '{zipPath}'，条目名: {entryPath}";
        }
        catch (Exception ex)
        {
            return $"添加文件失败: {ex.Message}";
        }
    }

    [Tool("Extract a single file from a ZIP archive.")]
    public string ExtractFileFromZip(
        [Description("Absolute path to the ZIP file")] string zipPath,
        [Description("Name of the entry inside the ZIP to extract")] string entryName,
        [Description("Absolute path where the extracted file will be saved")] string destPath,
        [Description("Overwrite existing file. Default false")] bool overwrite = false)
    {
        try
        {
            if (!File.Exists(zipPath))
                return $"ZIP 文件不存在: {zipPath}";

            using var archive = ZipFile.OpenRead(zipPath);
            var entry = archive.GetEntry(entryName);

            if (entry == null)
                return $"ZIP 中不存在条目: {entryName}";

            if (string.IsNullOrEmpty(entry.Name))
                return $"'{entryName}' 是目录，不是文件";

            var dir = Path.GetDirectoryName(destPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (File.Exists(destPath) && !overwrite)
            {
                return $"目标文件已存在: {destPath}";
            }

            entry.ExtractToFile(destPath, overwrite);

            return JsonSerializer.Serialize(new
            {
                Success = true,
                ZipFile = zipPath,
                EntryName = entryName,
                DestinationPath = destPath,
                Size = FormatFileSize(entry.Length)
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return $"提取文件失败: {ex.Message}";
        }
    }

    [Tool("Remove a file entry from a ZIP archive.")]
    public string RemoveFromZip(
        [Description("Absolute path to the ZIP file")] string zipPath,
        [Description("Name of the entry to remove")] string entryName)
    {
        try
        {
            if (!File.Exists(zipPath))
                return $"ZIP 文件不存在: {zipPath}";

            using var archive = ZipFile.Open(zipPath, ZipArchiveMode.Update);
            var entry = archive.GetEntry(entryName);

            if (entry == null)
                return $"ZIP 中不存在条目: {entryName}";

            entry.Delete();

            return $"成功从 ZIP '{zipPath}' 中删除条目: {entryName}";
        }
        catch (Exception ex)
        {
            return $"删除条目失败: {ex.Message}";
        }
    }

    [Tool("Verify the integrity of a ZIP archive by reading all entries.")]
    public string VerifyZip(
        [Description("Absolute path to the ZIP file")] string zipPath)
    {
        try
        {
            if (!File.Exists(zipPath))
                return $"ZIP 文件不存在: {zipPath}";

            var errors = new List<string>();
            var warnings = new List<string>();
            var fileInfo = new FileInfo(zipPath);

            using var archive = ZipFile.OpenRead(zipPath);
            
            foreach (var entry in archive.Entries)
            {
                try
                {
                    if (!string.IsNullOrEmpty(entry.Name))
                    {
                        using var stream = entry.Open();
                        var buffer = new byte[8192];
                        while (stream.Read(buffer, 0, buffer.Length) > 0)
                        {
                            // 读取整个文件以验证完整性
                        }
                    }
                }
                catch (Exception ex)
                {
                    errors.Add($"条目 '{entry.FullName}' 损坏: {ex.Message}");
                }
            }

            return JsonSerializer.Serialize(new
            {
                ZipFile = zipPath,
                FileSize = FormatFileSize(fileInfo.Length),
                LastModified = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"),
                IsValid = errors.Count == 0,
                TotalEntries = archive.Entries.Count,
                ErrorCount = errors.Count,
                Errors = errors,
                Warnings = warnings
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (InvalidDataException ex)
        {
            return JsonSerializer.Serialize(new
            {
                ZipFile = zipPath,
                IsValid = false,
                Error = $"ZIP 文件格式无效: {ex.Message}"
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return $"验证失败: {ex.Message}";
        }
    }

    [Tool("Get summary information about a ZIP archive.")]
    public string GetZipInfo(
        [Description("Absolute path to the ZIP file")] string zipPath)
    {
        try
        {
            if (!File.Exists(zipPath))
                return $"ZIP 文件不存在: {zipPath}";

            var fileInfo = new FileInfo(zipPath);
            using var archive = ZipFile.OpenRead(zipPath);

            var fileCount = 0;
            var dirCount = 0;
            long totalSize = 0;
            long compressedSize = 0;
            var extensions = new Dictionary<string, int>();

            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name))
                {
                    dirCount++;
                }
                else
                {
                    fileCount++;
                    totalSize += entry.Length;
                    compressedSize += entry.CompressedLength;

                    var ext = Path.GetExtension(entry.Name).ToLowerInvariant();
                    if (!string.IsNullOrEmpty(ext))
                    {
                        if (!extensions.ContainsKey(ext))
                            extensions[ext] = 0;
                        extensions[ext]++;
                    }
                }
            }

            return JsonSerializer.Serialize(new
            {
                ZipFile = zipPath,
                FileSize = FormatFileSize(fileInfo.Length),
                LastModified = fileInfo.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss"),
                TotalEntries = archive.Entries.Count,
                FileCount = fileCount,
                DirectoryCount = dirCount,
                TotalUncompressedSize = FormatFileSize(totalSize),
                TotalCompressedSize = FormatFileSize(compressedSize),
                CompressionRatio = totalSize > 0 ? $"{(double)compressedSize / totalSize:P1}" : "0%",
                FileTypes = extensions.OrderByDescending(kv => kv.Value).Take(10).ToDictionary(kv => kv.Key, kv => kv.Value)
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return $"获取信息失败: {ex.Message}";
        }
    }

    [Tool("Add multiple files to a ZIP archive.")]
    public string AddFilesToZip(
        [Description("Absolute path to the ZIP file")] string zipPath,
        [Description("Semicolon-separated list of absolute file paths to add, e.g. '/path/file1.cs;/path/file2.cs'")] string filePaths,
        [Description("Base path for calculating relative entry names. Optional")] string? basePath = null)
    {
        try
        {
            var fileInfo = new FileInfo(zipPath);
            if (fileInfo.Directory != null && !fileInfo.Directory.Exists)
            {
                fileInfo.Directory.Create();
            }

            var pathList = filePaths.Split(';', ',', StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrEmpty(p))
                .ToList();

            var addedFiles = new List<string>();
            var errors = new List<string>();

            using var archive = ZipFile.Open(zipPath, File.Exists(zipPath) ? ZipArchiveMode.Update : ZipArchiveMode.Create);

            foreach (var filePath in pathList)
            {
                try
                {
                    if (!File.Exists(filePath))
                    {
                        errors.Add($"文件不存在: {filePath}");
                        continue;
                    }

                    string entryName;
                    if (!string.IsNullOrEmpty(basePath))
                    {
                        entryName = Path.GetRelativePath(basePath, filePath);
                    }
                    else
                    {
                        entryName = Path.GetFileName(filePath);
                    }

                    var existingEntry = archive.GetEntry(entryName);
                    existingEntry?.Delete();

                    archive.CreateEntryFromFile(filePath, entryName);
                    addedFiles.Add(entryName);
                }
                catch (Exception ex)
                {
                    errors.Add($"添加 '{filePath}' 失败: {ex.Message}");
                }
            }

            return JsonSerializer.Serialize(new
            {
                Success = errors.Count == 0,
                ZipFile = zipPath,
                AddedFiles = addedFiles.Count,
                AddedFileList = addedFiles,
                ErrorCount = errors.Count,
                Errors = errors
            }, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            return $"批量添加失败: {ex.Message}";
        }
    }

    private static string FormatFileSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double size = bytes;
        while (size >= 1024 && order < sizes.Length - 1)
        {
            order++;
            size /= 1024;
        }
        return $"{size:0.##} {sizes[order]}";
    }
}
