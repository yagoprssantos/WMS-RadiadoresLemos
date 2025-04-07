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

        private static string GetBackupPath(int version)
        {
            var directory = Path.GetDirectoryName(dbPath);
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            return Path.Combine(directory, $"Database_backup_{timestamp}.db");
        }

        private static void CreateBackup()
        {
            try
            {
                // Se o banco atual existe, faz backup
                if (File.Exists(dbPath))
                {
                    var directory = Path.GetDirectoryName(dbPath);
                    
                    // Cria backup com timestamp
                    string novoBackup = GetBackupPath(1);
                    File.Copy(dbPath, novoBackup, true);

                    // Lista todos os backups existentes
                    var backups = Directory.GetFiles(directory, "Database_backup_*.db")
                        .OrderByDescending(f => f)
                        .ToList();

                    // Se houver mais de 20 backups, remove os mais antigos
                    if (backups.Count > 20)
                    {
                        foreach (var backup in backups.Skip(20))
                        {
                            try
                            {
                                File.Delete(backup);
                                Console.WriteLine($"Backup antigo removido: {backup}");
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"Erro ao remover backup antigo {backup}: {ex.Message}");
                            }
                        }
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
