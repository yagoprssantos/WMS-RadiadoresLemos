using System;
using System.IO;
using System.Linq;

namespace WMS_RadiadoresLemos_WPF.src.Services
{
    public static class DatabaseBackup
    {
        private static string? backupDirectory;

        private static string GetBackupDirectory(string dbPath)
        {
            if (backupDirectory == null)
            {
                var directory = Path.GetDirectoryName(dbPath);
                backupDirectory = Path.Combine(directory, "local");
                
                // Cria o diretório de backup se não existir
                if (!Directory.Exists(backupDirectory))
                {
                    Directory.CreateDirectory(backupDirectory);
                    Console.WriteLine($"Diretório de backup criado: {backupDirectory}");
                }
            }
            
            return backupDirectory;
        }

        private static string GetBackupPath(int version, string timestamp, string dbPath)
        {
            return Path.Combine(GetBackupDirectory(dbPath), $"Database_v{version}_{timestamp}.db");
        }

        private static string CalculateFileHash(string filePath)
        {
            using (var md5 = System.Security.Cryptography.MD5.Create())
            using (var stream = File.OpenRead(filePath))
            {
                var hash = md5.ComputeHash(stream);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        private static bool DatabaseWasModified(string dbPath)
        {
            try
            {
                var backups = Directory.GetFiles(GetBackupDirectory(dbPath), "Database_v*_*.db")
                    .OrderByDescending(f => File.GetLastWriteTime(f))
                    .ToList();

                if (!backups.Any())
                    return true; // Se não tem backup, considera que foi modificado

                var lastBackup = backups.First();
                var currentHash = CalculateFileHash(dbPath);
                var lastBackupHash = CalculateFileHash(lastBackup);

                return currentHash != lastBackupHash;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERRO ao verificar modificações:");
                Console.WriteLine($"Mensagem: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                return true; // Em caso de erro, considera que foi modificado
            }
        }

        public static void CreateBackup(string dbPath)
        {
            try
            {
                // Se o banco atual existe, faz backup
                if (File.Exists(dbPath))
                {
                    // Verifica se o banco foi modificado
                    if (!DatabaseWasModified(dbPath))
                    {
                        Console.WriteLine("Banco de dados não foi modificado desde o último backup. Pulando criação de backup.");
                        return;
                    }
                    
                    // Lista todos os backups existentes para determinar a próxima versão
                    var backups = Directory.GetFiles(GetBackupDirectory(dbPath), "Database_v*_*.db")
                        .Select(f => {
                            var partes = Path.GetFileName(f).Split('_');
                            var versao = int.Parse(partes[1].Substring(1));
                            var timestamp = partes[2].Replace(".db", "");
                            return new
                            {
                                Path = f,
                                Version = versao,
                                Timestamp = timestamp
                            };
                        })
                        .OrderByDescending(x => x.Version)
                        .ToList();

                    int proximaVersao;
                    string timestamp;
                    if (backups.Any())
                    {
                        var ultimaVersao = backups.First().Version;
                        if (ultimaVersao < 20)
                        {
                            proximaVersao = ultimaVersao + 1;
                            timestamp = DateTime.Now.ToString("yyyy-MM-dd");
                        }
                        else
                        {
                            var backupV1 = backups.FirstOrDefault(b => b.Version == 1);
                            if (backupV1 != null)
                            {
                                try
                                {
                                    File.Delete(backupV1.Path);
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine($"ERRO ao remover backup v1:");
                                    Console.WriteLine($"Mensagem: {ex.Message}");
                                    Console.WriteLine($"StackTrace: {ex.StackTrace}");
                                }
                            }

                            for (int i = 2; i <= 20; i++)
                            {
                                var backupAtual = backups.FirstOrDefault(b => b.Version == i);
                                if (backupAtual != null)
                                {
                                    var novoCaminho = GetBackupPath(i - 1, backupAtual.Timestamp, dbPath);
                                    try
                                    {
                                        File.Move(backupAtual.Path, novoCaminho, true);
                                    }
                                    catch (Exception ex)
                                    {
                                        Console.WriteLine($"ERRO ao rotacionar backup v{i}:");
                                        Console.WriteLine($"Mensagem: {ex.Message}");
                                        Console.WriteLine($"StackTrace: {ex.StackTrace}");
                                    }
                                }
                            }
                            proximaVersao = 20;
                            timestamp = DateTime.Now.ToString("yyyy-MM-dd");
                        }
                    }
                    else
                    {
                        proximaVersao = 1;
                        timestamp = DateTime.Now.ToString("yyyy-MM-dd");
                    }

                    string novoBackup = GetBackupPath(proximaVersao, timestamp, dbPath);
                    try
                    {
                        File.Copy(dbPath, novoBackup, true);
                        Console.WriteLine($"Backup criado com sucesso: {novoBackup}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"ERRO ao criar novo backup:");
                        Console.WriteLine($"Mensagem: {ex.Message}");
                        Console.WriteLine($"StackTrace: {ex.StackTrace}");
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao criar backup: {ex.Message}");
            }
        }
    }
} 