using WMS_RadiadoresLemos_WPF.src.Models;
using System;
using System.IO;
using System.Threading.Tasks;
using WMS_RadiadoresLemos_WPF.src.Config;

namespace WMS_RadiadoresLemos_WPF.src.Services
{
    public class BackupService
    {
        private readonly AzureStorageService _azureService;
        private readonly string _backupDirectory;

        public BackupService()
        {
            _azureService = new AzureStorageService();
            _backupDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            
            // Cria o diretório de backups se não existir
            if (!Directory.Exists(_backupDirectory))
            {
                Directory.CreateDirectory(_backupDirectory);
            }
        }

        public async Task<string> CriarBackupAsync(string filePath)
        {
            try
            {
                var fileName = Path.GetFileName(filePath);
                await _azureService.UploadBackupAsync(filePath, fileName);
                return fileName;
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao criar backup: {ex.Message}");
            }
        }

        public async Task RestaurarBackupAsync(string fileName, string destinationPath)
        {
            try
            {
                await _azureService.DownloadBackupAsync(fileName, destinationPath);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao restaurar backup: {ex.Message}");
            }
        }

        public async Task<List<string>> ListarBackupsAsync()
        {
            try
            {
                var files = await _azureService.ListBackupFilesAsync();
                return files.Select(f => f.Name).ToList();
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao listar backups: {ex.Message}");
            }
        }

        public async Task DeletarBackupAsync(string backupFileName)
        {
            try
            {
                await _azureService.DeleteBackupAsync(backupFileName);
                
                // Remove também o arquivo local se existir
                string localBackupPath = Path.Combine(_backupDirectory, backupFileName);
                if (File.Exists(localBackupPath))
                {
                    File.Delete(localBackupPath);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao deletar backup: {ex.Message}", ex);
            }
        }
    }
} 