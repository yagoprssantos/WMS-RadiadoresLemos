using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Config;
using WMS_RadiadoresLemos_WPF.src.Views;

namespace WMS_RadiadoresLemos_WPF.src.Services
{
    public class AzureStorageService
    {
        private readonly BlobContainerClient _containerClient;

        public AzureStorageService()
        {
            try
            {
                if (string.IsNullOrEmpty(AzureConfig.ConnectionString))
                {
                    throw new Exception("String de conexão do Azure Storage não configurada. Use AzureConfig.Initialize() para configurar a string de conexão.");
                }

                // Cria o cliente do container usando a URL SAS
                _containerClient = new BlobContainerClient(new Uri(AzureConfig.ConnectionString));
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao inicializar o Azure Storage Service: {ex.Message}");
            }
        }

        public async Task<List<BackupFile>> ListBackupFilesAsync()
        {
            try
            {
                var files = new List<BackupFile>();

                await foreach (var blob in _containerClient.GetBlobsAsync())
                {
                    files.Add(new BackupFile
                    {
                        Name = blob.Name,
                        Size = FormatFileSize(blob.Properties.ContentLength ?? 0),
                        LastModified = blob.Properties.LastModified?.DateTime ?? DateTime.MinValue
                    });
                }

                return files;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao listar arquivos no Azure: {ex.Message}");
            }
        }

        public async Task UploadBackupAsync(string filePath, string blobName)
        {
            try
            {
                var blobClient = _containerClient.GetBlobClient(blobName);
                using (var fileStream = File.OpenRead(filePath))
                {
                    await blobClient.UploadAsync(fileStream, true);
                }
                BancoDadosUserControl.UploadCount++;
                
                // Salva as estatísticas após o upload
                var stats = await GetContainerStatsAsync();
                AzureUsageStats.SaveStats(
                    BancoDadosUserControl.UploadCount,
                    BancoDadosUserControl.DownloadCount,
                    BancoDadosUserControl.DeleteCount,
                    stats.TotalSize
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao fazer upload do arquivo: {ex.Message}");
            }
        }

        public async Task DownloadBackupAsync(string blobName, string destinationPath)
        {
            try
            {
                var blobClient = _containerClient.GetBlobClient(blobName);
                await blobClient.DownloadToAsync(destinationPath);
                BancoDadosUserControl.DownloadCount++;
                
                // Salva as estatísticas após o download
                var stats = await GetContainerStatsAsync();
                AzureUsageStats.SaveStats(
                    BancoDadosUserControl.UploadCount,
                    BancoDadosUserControl.DownloadCount,
                    BancoDadosUserControl.DeleteCount,
                    stats.TotalSize
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao baixar arquivo: {ex.Message}");
            }
        }

        public async Task DeleteBackupAsync(string blobName)
        {
            try
            {
                var blobClient = _containerClient.GetBlobClient(blobName);
                await blobClient.DeleteIfExistsAsync();
                BancoDadosUserControl.DeleteCount++;
                
                // Salva as estatísticas após a exclusão
                var stats = await GetContainerStatsAsync();
                AzureUsageStats.SaveStats(
                    BancoDadosUserControl.UploadCount,
                    BancoDadosUserControl.DownloadCount,
                    BancoDadosUserControl.DeleteCount,
                    stats.TotalSize
                );
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao deletar arquivo: {ex.Message}");
            }
        }

        private string FormatFileSize(long bytes)
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

        public async Task<AzureContainerStats> GetContainerStatsAsync()
        {
            try
            {
                var stats = new AzureContainerStats();
                var blobs = new List<BlobItem>();
                
                await foreach (var blob in _containerClient.GetBlobsAsync())
                {
                    blobs.Add(blob);
                    stats.TotalSize += blob.Properties.ContentLength ?? 0;
                    stats.TotalFiles++;
                    
                    if (blob.Properties.LastModified.HasValue)
                    {
                        var lastModified = blob.Properties.LastModified.Value.DateTime;
                        if (lastModified > stats.LastModified)
                        {
                            stats.LastModified = lastModified;
                        }
                    }
                }

                stats.FormattedSize = FormatFileSize(stats.TotalSize);
                stats.LastModifiedFormatted = stats.LastModified.ToString("dd/MM/yyyy HH:mm:ss");
                
                return stats;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao obter estatísticas do container: {ex.Message}");
            }
        }
    }

    public class AzureContainerStats
    {
        public long TotalSize { get; set; }
        public string FormattedSize { get; set; }
        public int TotalFiles { get; set; }
        public DateTime LastModified { get; set; }
        public string LastModifiedFormatted { get; set; }
    }
} 