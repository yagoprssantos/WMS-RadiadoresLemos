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
                        .OrderByDescending(f => f)
                        .ToList();

                    // Determina a próxima versão
                    int proximaVersao = 1;
                    if (backups.Any())
                    {
                        // Extrai a versão do backup mais recente
                        var ultimoBackup = backups.First();
                        var nomeArquivo = Path.GetFileName(ultimoBackup);
                        var versaoStr = nomeArquivo.Split('_')[1].Substring(1); // Remove o 'v' do início
                        if (int.TryParse(versaoStr, out int ultimaVersao))
                        {
                            proximaVersao = ultimaVersao + 1;
                        }
                    }
                    
                    // Cria backup com timestamp e nova versão
                    string novoBackup = GetBackupPath(proximaVersao);
                    File.Copy(dbPath, novoBackup, true);

                    // Se houver mais de 20 backups, remove os mais antigos
                    if (backups.Count >= 20)
                    {
                        foreach (var backup in backups.Skip(19)) // Mantém os 19 mais recentes + o novo
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
