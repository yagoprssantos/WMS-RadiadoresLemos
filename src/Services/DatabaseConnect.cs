using LiteDB;
using System;
using System.IO;
using System.Linq;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF.src.Services
{
    public static class DatabaseConnect
    {
        private static string dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "WMS-RadiadoresLemos",
            "Database.db"
        );
        public static LiteDatabase? Database { get; private set; }

        private static string GetBackupPath(int version, string timestamp)
        {
            var directory = Path.GetDirectoryName(dbPath);
            return Path.Combine(directory, $"Database_v{version}_{timestamp}.db");
        }

        private static void CreateBackup()
        {
            try
            {
                // Se o banco atual existe, faz backup
                if (File.Exists(dbPath))
                {
                    var directory = Path.GetDirectoryName(dbPath);
                    
                    // Lista todos os backups existentes para determinar a próxima versão
                    var backups = Directory.GetFiles(directory, "Database_v*_*.db")
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
                        if (ultimaVersao < 3)
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

                            for (int i = 2; i <= 3; i++)
                            {
                                var backupAtual = backups.FirstOrDefault(b => b.Version == i);
                                if (backupAtual != null)
                                {
                                    var novoCaminho = GetBackupPath(i - 1, backupAtual.Timestamp);
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
                            proximaVersao = 3;
                            timestamp = DateTime.Now.ToString("yyyy-MM-dd");
                        }
                    }
                    else
                    {
                        proximaVersao = 1;
                        timestamp = DateTime.Now.ToString("yyyy-MM-dd");
                    }

                    string novoBackup = GetBackupPath(proximaVersao, timestamp);
                    try
                    {
                        File.Copy(dbPath, novoBackup, true);
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

        public static string GetDatabasePath()
        {
            return dbPath;
        }

        public static bool DatabaseExists()
        {
            return File.Exists(dbPath);
        }

        public static void SetEnvironmentVariable()
        {
            try
            {
                // Cria o diretório se não existir
                var directory = Path.GetDirectoryName(dbPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Console.WriteLine($"Diretório criado: {directory}");
                }

                // Cria backup antes de abrir o banco
                CreateBackup();

                // Cria ou abre o banco de dados
                Database = new LiteDatabase(dbPath);
                Console.WriteLine($"Banco de dados conectado: {dbPath}");

                // Cria as coleções se não existirem
                var usuarios = Database.GetCollection<UsuarioData>("usuarios");
                var produtos = Database.GetCollection<ProdutoData>("produtos");
                var movimentacoes = Database.GetCollection<MovimentacaoData>("movimentacoes");
                var historico = Database.GetCollection<LogData>("historico");

                // Cria índices para melhor performance
                usuarios.EnsureIndex(x => x.Matricula, unique: true);
                produtos.EnsureIndex(x => x.Codigo, unique: true);
                movimentacoes.EnsureIndex(x => x.Data);
                historico.EnsureIndex(x => x.Data);

                Console.WriteLine("Coleções e índices criados com sucesso");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao criar banco de dados: {ex.Message}");
                Database = null;
                throw;
            }
        }

        public static void Disconnect()
        {
            try
            {
                if (Database != null)
                {
                    Database.Dispose();
                    Database = null;
                    Console.WriteLine("Banco de dados desconectado");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao desconectar banco de dados: {ex.Message}");
            }
        }
    }
}
