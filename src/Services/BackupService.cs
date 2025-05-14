using WMS_RadiadoresLemos_WPF.src.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace WMS_RadiadoresLemos_WPF.src.Services
{
    public class BackupService
    {
        private readonly AzureBlobStorage _azureStorage;
        private readonly string _backupDirectory;

        public BackupService()
        {
            _azureStorage = new AzureBlobStorage(AzureConfig.ConnectionString, AzureConfig.ContainerName);
            _backupDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Backups");
            
            // Cria o diretório de backups se não existir
            if (!Directory.Exists(_backupDirectory))
            {
                Directory.CreateDirectory(_backupDirectory);
            }
        }

        public async Task<string> CriarBackupAsync(string databasePath)
        {
            try
            {
                // Verifica se o arquivo de banco de dados existe
                if (!File.Exists(databasePath))
                {
                    throw new FileNotFoundException($"Arquivo de banco de dados não encontrado: {databasePath}");
                }

                Console.WriteLine($"📂 Arquivo de banco encontrado: {databasePath}");

                // Gera um nome único para o backup baseado na data e hora
                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string backupFileName = $"backup_{timestamp}.db";
                string localBackupPath = Path.Combine(_backupDirectory, backupFileName);

                Console.WriteLine($"📝 Criando backup local: {localBackupPath}");

                // Copia o arquivo do banco de dados para o diretório de backups
                File.Copy(databasePath, localBackupPath, true);
                Console.WriteLine("✅ Backup local criado com sucesso");

                // Faz upload do backup para o Azure
                Console.WriteLine("☁️ Iniciando upload para Azure...");
                await _azureStorage.UploadBackupAsync(localBackupPath, backupFileName);
                Console.WriteLine("✅ Upload para Azure concluído");

                // Verifica se o upload foi bem-sucedido
                Console.WriteLine("🔍 Verificando backup no Azure...");
                var backups = await _azureStorage.ListBackupsAsync();
                if (!backups.Contains(backupFileName))
                {
                    throw new Exception("O backup foi criado localmente, mas falhou ao ser enviado para o Azure");
                }

                Console.WriteLine("✅ Backup confirmado no Azure");
                return backupFileName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Erro ao criar backup: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Detalhes do erro: {ex.InnerException.Message}");
                }
                throw new Exception($"Erro ao criar backup: {ex.Message}", ex);
            }
        }

        public async Task RestaurarBackupAsync(string backupFileName, string targetDatabasePath)
        {
            try
            {
                string localBackupPath = Path.Combine(_backupDirectory, backupFileName);

                // Faz download do backup do Azure
                await _azureStorage.DownloadBackupAsync(backupFileName, localBackupPath);

                // Copia o arquivo de backup para o local do banco de dados
                File.Copy(localBackupPath, targetDatabasePath, true);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao restaurar backup: {ex.Message}", ex);
            }
        }

        public async Task<List<string>> ListarBackupsAsync()
        {
            try
            {
                return await _azureStorage.ListBackupsAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao listar backups: {ex.Message}", ex);
            }
        }

        public async Task DeletarBackupAsync(string backupFileName)
        {
            try
            {
                await _azureStorage.DeleteBackupAsync(backupFileName);
                
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