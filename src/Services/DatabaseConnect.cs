using LiteDB;
using System;
using System.IO;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF.src.Services
{
    internal static class DatabaseConnect
    {
        private static string dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "WMS-RadiadoresLemos",
            "Database.db"
        );
        public static LiteDatabase? Database { get; private set; }

        public static string GetDatabasePath()
        {
            return dbPath;
        }

        public static void SetEnvironmentVarible()
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
                throw; // Propaga a exceção para que a aplicação saiba que houve um erro
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
