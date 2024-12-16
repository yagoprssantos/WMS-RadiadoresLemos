using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF.src.Services
{
    public static class MovimentacoesCache
    {
        private const int MaxMovimentacoes = 1000;
        private static readonly string CaminhoArquivoMovimentacoes = new DatabaseFileManager().ObterCaminhoArquivo("Movimentacoes");


        // Função para registrar uma movimentação no cache, no Firestore e no arquivo JSON
        public static async Task RegistrarMovimentacaoAsync(MovimentacaoData movimentacao)
        {
            if (movimentacao == null)
            {
                Console.WriteLine("Movimentação não pode ser nula.");
                return;
            }

            try
            {
                // Adiciona a movimentação ao Firestore
                var db = DatabaseConnect.Database;
                if (db == null || !DatabaseConnect.IsConnected)
                {
                    Console.WriteLine("Não foi possível conectar ao Firestore. Registrando movimentação em modo offline.");

                    // Adiciona a movimentação ao cache
                    AdicionarMovimentacaoAoCache(movimentacao);

                    // Adiciona a movimentação nos arquivos JSON
                    await AdicionarMovimentacaoNoArquivoAsync(movimentacao);

                    // Deixa offline
                    new MainWindow().ativarModoOffline();
                    return;
                }

                // Adiciona no banco de dados
                var movimentacoesRef = db.Collection("Movimentacoes");
                await movimentacoesRef.AddAsync(movimentacao);
                Console.WriteLine("Movimentação registrada com sucesso no Firestore.");

                // Adiciona a movimentação ao cache
                AdicionarMovimentacaoAoCache(movimentacao);

                // Adiciona a movimentação nos arquivos JSON
                await AdicionarMovimentacaoNoArquivoAsync(movimentacao);

                // Remove movimentações antigas se houver mais de MaxMovimentacoes
                await RemoverMovimentacoesAntigasAsync(movimentacoesRef);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao registrar movimentação: {ex.Message}");
                return;
            }
        }


        // Função para obter todas as movimentações do cache ou do arquivo JSON
        public static List<MovimentacaoData> ObterMovimentacoes()
        {
            if (!DadosCache.Tabelas.ContainsKey("Movimentacoes"))
            {
                Console.WriteLine("Nenhuma movimentação encontrada no cache.");
                return LerMovimentacoesDoArquivo();
            }

            try
            {
                var movimentacoes = DadosCache.Tabelas["Movimentacoes"].Cast<MovimentacaoData>().OrderByDescending(mov => mov.Data).ToList();
                return movimentacoes;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Erro ao obter movimentações (ArgumentException): {ex.Message}");
                return LerMovimentacoesDoArquivo();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao obter movimentações: {ex.Message}");
                return LerMovimentacoesDoArquivo();
            }
        }


        // Função para adicionar uma movimentação ao cache
        private static void AdicionarMovimentacaoAoCache(MovimentacaoData movimentacao)
        {
            if (!DadosCache.Tabelas.ContainsKey("Movimentacoes"))
            {
                DadosCache.Tabelas["Movimentacoes"] = new List<object>();
            }
            DadosCache.Tabelas["Movimentacoes"].Add(movimentacao);
            Console.WriteLine("Movimentação registrada com sucesso no cache.");
        }

        // Função para salvar uma movimentação no arquivo JSON
        private static async Task AdicionarMovimentacaoNoArquivoAsync(MovimentacaoData movimentacao)
        {
            try
            {
                var movimentacoes = LerMovimentacoesDoArquivo();
                movimentacoes.Add(movimentacao);
                await SalvarMovimentacoesNoArquivoAsync(movimentacoes);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar movimentação no arquivo JSON: {ex.Message}");
            }
        }

        // Função para salvar uma lista de movimentações no arquivo JSON
        private static async Task SalvarMovimentacoesNoArquivoAsync(List<MovimentacaoData> movimentacoes)
        {
            try
            {
                string json = JsonSerializer.Serialize(movimentacoes);
                await File.WriteAllTextAsync(CaminhoArquivoMovimentacoes, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar movimentações no arquivo JSON: {ex.Message}");
            }
        }


        // Função para ler as movimentações do arquivo JSON
        private static List<MovimentacaoData> LerMovimentacoesDoArquivo()
        {
            try
            {
                if (File.Exists(CaminhoArquivoMovimentacoes))
                {
                    string json = File.ReadAllText(CaminhoArquivoMovimentacoes);
                    return JsonSerializer.Deserialize<List<MovimentacaoData>>(json) ?? new List<MovimentacaoData>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao ler movimentações do arquivo JSON: {ex.Message}");
            }
            return new List<MovimentacaoData>();
        }


        // Função para remover movimentações antigas se houver mais de MaxMovimentacoes
        private static async Task RemoverMovimentacoesAntigasAsync(CollectionReference movimentacoesRef)
        {
            try
            {
                var snapshot = await movimentacoesRef.OrderBy("DataHora").GetSnapshotAsync();
                if (snapshot.Count > MaxMovimentacoes)
                {
                    var movimentacoesParaRemover = snapshot.Documents.Take(snapshot.Count - MaxMovimentacoes);
                    foreach (var movimentacao in movimentacoesParaRemover)
                    {
                        await movimentacoesRef.Document(movimentacao.Id).DeleteAsync();
                    }
                    Console.WriteLine("Movimentações antigas removidas com sucesso do Firestore.");

                    // Remove as movimentações antigas do cache
                    RemoverMovimentacoesAntigasDoCache();

                    // Remove as movimentações antigas do arquivo JSON
                    await RemoverMovimentacoesAntigasDoArquivoAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao remover movimentações antigas: {ex.Message}");
            }
        }

        // Função para remover movimentações antigas do cache
        private static void RemoverMovimentacoesAntigasDoCache()
        {
            var movimentacoesCache = DadosCache.Tabelas["Movimentacoes"].Cast<MovimentacaoData>().OrderBy(mov => mov.Data).ToList();
            var movimentacoesParaRemoverCache = movimentacoesCache.Take(movimentacoesCache.Count - MaxMovimentacoes).ToList();
            foreach (var movimentacao in movimentacoesParaRemoverCache)
            {
                DadosCache.Tabelas["Movimentacoes"].Remove(movimentacao);
            }
            Console.WriteLine("Movimentações antigas removidas com sucesso do cache.");
        }

        // Função para remover movimentações antigas do arquivo JSON
        private static async Task RemoverMovimentacoesAntigasDoArquivoAsync()
        {
            var movimentacoesArquivo = LerMovimentacoesDoArquivo();
            var movimentacoesParaRemoverArquivo = movimentacoesArquivo.Take(movimentacoesArquivo.Count - MaxMovimentacoes).ToList();
            await SalvarMovimentacoesNoArquivoAsync(movimentacoesArquivo.Except(movimentacoesParaRemoverArquivo).ToList());
            Console.WriteLine("Movimentações antigas removidas com sucesso do arquivo JSON.");
        }
    }
}
