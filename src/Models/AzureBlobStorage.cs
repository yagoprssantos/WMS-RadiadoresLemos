using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace WMS_RadiadoresLemos_WPF.src.Models
{
    public class AzureBlobStorage
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;

        public AzureBlobStorage(string connectionString, string containerName)
        {
            _blobServiceClient = new BlobServiceClient(connectionString);
            _containerName = containerName;
            
            // Cria o container se não existir
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            containerClient.CreateIfNotExists();
        }

        public async Task UploadBackupAsync(string localFilePath, string blobName)
        {
            try
            {
                // Obtém o container
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                await containerClient.CreateIfNotExistsAsync();

                // Obtém o blob client
                var blobClient = containerClient.GetBlobClient(blobName);

                // Faz o upload do arquivo
                using (var fileStream = File.OpenRead(localFilePath))
                {
                    await blobClient.UploadAsync(fileStream, true);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao fazer upload do backup: {ex.Message}", ex);
            }
        }

        public async Task DownloadBackupAsync(string blobName, string localFilePath)
        {
            try
            {
                // Obtém o container
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                
                // Obtém o blob client
                var blobClient = containerClient.GetBlobClient(blobName);

                // Faz o download do arquivo
                await blobClient.DownloadToAsync(localFilePath);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao fazer download do backup: {ex.Message}", ex);
            }
        }

        public async Task DeleteBackupAsync(string blobName)
        {
            try
            {
                // Obtém o container
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
                
                // Obtém o blob client
                var blobClient = containerClient.GetBlobClient(blobName);

                // Deleta o blob
                await blobClient.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao deletar o backup: {ex.Message}", ex);
            }
        }

        public async Task<List<string>> ListBackupsAsync()
        {
            try
            {
                var backups = new List<string>();
                var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

                // Garante que o container existe
                await containerClient.CreateIfNotExistsAsync();

                await foreach (var blob in containerClient.GetBlobsAsync())
                {
                    backups.Add(blob.Name);
                }

                return backups;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao listar backups: {ex.Message}", ex);
            }
        }
    }
} 