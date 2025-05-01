using System;
using System.Collections.Generic;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF.src.Services
{
    public static class DadosIniciais
    {
        public static void InserirDadosIniciais()
        {
            try
            {
                var db = DatabaseConnect.Database;
                if (db == null)
                {
                    Console.WriteLine("Erro: Banco de dados não está conectado");
                    return;
                }

                Console.WriteLine("Iniciando inserção de dados iniciais...");
                Console.WriteLine($"Caminho do banco de dados: {DatabaseConnect.GetDatabasePath()}");

                // Inserir usuário administrador
                var usuariosCollection = db.GetCollection<UsuarioData>("usuarios");
                var admin = new UsuarioData
                {
                    Nome = "Administrador",
                    Email = "admin@radiadoreslemos.com",
                    Matricula = "ADM2401",
                    Senha = CriptografiaService.CriptografarSenha("admin123"),
                    Cargo = "Administrador",
                    Id = "ADM2401"
                };

                try
                {
                    usuariosCollection.Upsert(admin);
                    Console.WriteLine($"Usuário administrador inserido/atualizado com sucesso. ID: {admin.Id}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Erro ao inserir usuário administrador: {ex.Message}");
                    throw;
                }

                // Inserir produtos
                var produtosCollection = db.GetCollection<ProdutoData>("produtos");
                var produtos = new List<ProdutoData>
                {
                    new ProdutoData
                    {
                        Nome = "Radiador Universal 4x4",
                        Tipo = "Radiador",
                        Marca = "Lemos",
                        Codigo = "RAD001",
                        Preco = 450.00,
                        Quantidade = 10,
                        Id = "RAD001"
                    },
                    new ProdutoData
                    {
                        Nome = "Radiador Universal 6x6",
                        Tipo = "Radiador",
                        Marca = "Lemos",
                        Codigo = "RAD002",
                        Preco = 550.00,
                        Quantidade = 15,
                        Id = "RAD002"
                    },
                    new ProdutoData
                    {
                        Nome = "Radiador Universal 8x8",
                        Tipo = "Radiador",
                        Marca = "Lemos",
                        Codigo = "RAD003",
                        Preco = 650.00,
                        Quantidade = 8,
                        Id = "RAD003"
                    }
                };

                foreach (var produto in produtos)
                {
                    try
                    {
                        produtosCollection.Upsert(produto);
                        Console.WriteLine($"Produto inserido/atualizado com sucesso: {produto.Nome} (ID: {produto.Id})");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro ao inserir produto {produto.Nome}: {ex.Message}");
                        throw;
                    }
                }

                // Inserir movimentações
                var movimentacoesCollection = db.GetCollection<MovimentacaoData>("movimentacoes");
                var movimentacoes = new List<MovimentacaoData>
                {
                    new MovimentacaoData
                    {
                        ProdutoId = "RAD001",
                        Tipo = "Entrada",
                        Quantidade = 5,
                        Preco = 450.00,
                        Data = DateTime.Now.AddDays(-7),
                        Id = 1
                    },
                    new MovimentacaoData
                    {
                        ProdutoId = "RAD002",
                        Tipo = "Saída",
                        Quantidade = 3,
                        Preco = 550.00,
                        Data = DateTime.Now.AddDays(-5),
                        Id = 2
                    },
                    new MovimentacaoData
                    {
                        ProdutoId = "RAD003",
                        Tipo = "Entrada",
                        Quantidade = 4,
                        Preco = 650.00,
                        Data = DateTime.Now.AddDays(-3),
                        Id = 3
                    }
                };

                foreach (var movimentacao in movimentacoes)
                {
                    try
                    {
                        movimentacoesCollection.Upsert(movimentacao);
                        Console.WriteLine($"Movimentação inserida/atualizada com sucesso: {movimentacao.Tipo} - {movimentacao.ProdutoId} (ID: {movimentacao.Id})");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro ao inserir movimentação {movimentacao.Id}: {ex.Message}");
                        throw;
                    }
                }

                // Inserir histórico
                var historicoCollection = db.GetCollection<LogData>("historico");
                var logs = new List<LogData>
                {
                    new LogData
                    {
                        Data = DateTime.Now.AddDays(-7),
                        Tipo = "OPERACIONAL",
                        Nivel = "Sistema",
                        Detalhes = "Entrada de 5 unidades do produto RAD001",
                        Usuario = "Sistema"
                    },
                    new LogData
                    {
                        Data = DateTime.Now.AddDays(-5),
                        Tipo = "OPERACIONAL",
                        Nivel = "Sistema",
                        Detalhes = "Saída de 3 unidades do produto RAD002",
                        Usuario = "Sistema"
                    },
                    new LogData
                    {
                        Data = DateTime.Now.AddDays(-3),
                        Tipo = "OPERACIONAL",
                        Nivel = "Sistema",
                        Detalhes = "Entrada de 4 unidades do produto RAD003",
                        Usuario = "Sistema"
                    }
                };

                foreach (var log in logs)
                {
                    try
                    {
                        historicoCollection.Upsert(log);
                        Console.WriteLine($"Log inserido/atualizado com sucesso: {log.Detalhes}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro ao inserir log: {ex.Message}");
                        throw;
                    }
                }

                Console.WriteLine("Dados iniciais inseridos com sucesso!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao inserir dados iniciais: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                Alerta.AdicionarAlerta("Erro",
                    ex.Message.ToString(),
                    "Erro ao inserir dados iniciais. Possíveis motivos:\n" +
                    "- Problemas de conexão com o banco;\n" +
                    "- Dados corrompidos;\n" +
                    "- Falha na operação de inserção.",
                    "- Verifique a conexão com o banco;\n" +
                    "- Tente novamente mais tarde.");
                throw;
            }
        }
    }
} 