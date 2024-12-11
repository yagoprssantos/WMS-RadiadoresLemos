using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF.src.Services
{
    public static class MovimentacoesCache
    {
        private const int MaxMovimentacoes = 1000;

        // Função para registrar uma movimentação no cache e no Firestore
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
                if (db == null)
                {
                    Console.WriteLine("Erro ao conectar ao Firestore.");
                    return;
                }

                var movimentacoesRef = db.Collection("Movimentacoes");
                await movimentacoesRef.AddAsync(movimentacao);
                Console.WriteLine("Movimentação registrada com sucesso no Firestore.");

                // Adiciona a movimentação ao cache
                if (!DadosCache.Tabelas.ContainsKey("Movimentacoes"))
                {
                    DadosCache.Tabelas["Movimentacoes"] = new List<object>();
                }
                DadosCache.Tabelas["Movimentacoes"].Add(movimentacao);
                Console.WriteLine("Movimentação registrada com sucesso no cache.");

                await RemoverMovimentacoesAntigasAsync(movimentacoesRef);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Erro ao registrar movimentação (ArgumentException): {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao registrar movimentação: {ex.Message}");
            }
        }

        // Função para obter todas as movimentações do cache
        public static List<MovimentacaoData> ObterMovimentacoes()
        {
            if (!DadosCache.Tabelas.ContainsKey("Movimentacoes"))
            {
                Console.WriteLine("Nenhuma movimentação encontrada no cache.");
                return new List<MovimentacaoData>();
            }

            try
            {
                var movimentacoes = DadosCache.Tabelas["Movimentacoes"].Cast<MovimentacaoData>().OrderByDescending(mov => mov.DataHora).ToList();
                return movimentacoes;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Erro ao obter movimentações (ArgumentException): {ex.Message}");
                return new List<MovimentacaoData>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao obter movimentações: {ex.Message}");
                return new List<MovimentacaoData>();
            }
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
                    var movimentacoesCache = DadosCache.Tabelas["Movimentacoes"].Cast<MovimentacaoData>().OrderBy(mov => mov.DataHora).ToList();
                    var movimentacoesParaRemoverCache = movimentacoesCache.Take(movimentacoesCache.Count - MaxMovimentacoes).ToList();
                    foreach (var movimentacao in movimentacoesParaRemoverCache)
                    {
                        DadosCache.Tabelas["Movimentacoes"].Remove(movimentacao);
                    }
                    Console.WriteLine("Movimentações antigas removidas com sucesso do cache.");
                }
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Erro ao remover movimentações antigas (ArgumentException): {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao remover movimentações antigas: {ex.Message}");
            }
        }
    }
}