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
            "src",
            "Database",
            "Database.db"
        );
        public static LiteDatabase? Database { get; private set; }

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
                Console.WriteLine($"Pasta Documentos: {Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}");
                Console.WriteLine($"Caminho do banco: {dbPath}");

                // Cria o diretório se não existir
                var directory = Path.GetDirectoryName(dbPath);
                Console.WriteLine($"Diretório do banco: {directory}");

                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                    Console.WriteLine($"Diretório criado: {directory}");
                }

                // Verifica permissões de escrita
                try
                {
                    var testFile = Path.Combine(directory, "test.tmp");
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);
                    Console.WriteLine("Permissões de escrita verificadas com sucesso");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ERRO: Sem permissão de escrita no diretório: {ex.Message}");
                    throw;
                }

                // Verifica se o banco existe e tenta repará-lo se necessário
                if (File.Exists(dbPath))
                {
                    try
                    {
                        // Tenta abrir o banco para verificar integridade
                        using (var testDb = new LiteDatabase(dbPath))
                        {
                            // Se chegou aqui, o banco está íntegro
                            testDb.Dispose();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Banco de dados corrompido detectado: {ex.Message}");
                        // Faz backup do banco corrompido
                        string dataHoraFormatada = DateTime.Now.ToString("ddMMyyyy_HHmmss");
                        string backupPath = Path.Combine(
                            directory,
                            $"Database_corrupted_{dataHoraFormatada}.db"
                        );
                        File.Copy(dbPath, backupPath, true);
                        // Remove o banco corrompido
                        File.Delete(dbPath);
                        Console.WriteLine($"Backup do banco corrompido criado em: {backupPath}");
                    }
                }

                // Cria backup antes de abrir o banco
                DatabaseBackup.CreateBackup(dbPath);

                // Cria ou abre o banco de dados
                Console.WriteLine("Tentando abrir o banco de dados...");
                Database = new LiteDatabase(dbPath);
                Console.WriteLine($"Banco de dados conectado: {dbPath}");

                // Cria as coleções se não existirem
                Console.WriteLine("Criando coleções...");
                var usuarios = Database.GetCollection<UsuarioData>("usuarios");
                var produtos = Database.GetCollection<ProdutoData>("produtos");
                var movimentacoes = Database.GetCollection<MovimentacaoData>("movimentacoes");
                var historico = Database.GetCollection<LogData>("historico");
                var clientes = Database.GetCollection<ClienteData>("clientes");
                var fornecedores = Database.GetCollection<FornecedorData>("fornecedores");
                var compras = Database.GetCollection<CompraData>("compras");
                var vendas = Database.GetCollection<VendaData>("vendas");

                // Cria índices para melhor performance
                Console.WriteLine("Criando índices...");
                usuarios.EnsureIndex(x => x.Matricula, unique: true);
                produtos.EnsureIndex(x => x.Codigo, unique: true);
                movimentacoes.EnsureIndex(x => x.Data);
                historico.EnsureIndex(x => x.Data);


                Console.WriteLine("Coleções e índices criados com sucesso");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERRO ao criar banco de dados:");
                Console.WriteLine($"Mensagem: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
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
