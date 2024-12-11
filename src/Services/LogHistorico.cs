using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF.src.Services
{
    public static class LogHistorico
    {
        private const int MaxLogs = 100;

        // TODO: UTILIZAR JSON QUANDO BANCO DE DADOS NÃO ESTIVER DISPONÍVEL
        // Função para registrar um log no cache e no Firestore
        public static async Task RegistrarLogAsync(LogData log)
        {
            if (log == null || string.IsNullOrWhiteSpace(log.Detalhes))
            {
                Console.WriteLine("Detalhes do log não podem ser vazios.");
                return;
            }

            try
            {
                // Adiciona o log ao Firestore
                var db = DatabaseConnect.Database;
                if (db == null)
                {
                    Console.WriteLine("Conexão com o Firestore não estabelecida.");
                    return;
                }

                var logsRef = db.Collection("Historico");
                await logsRef.AddAsync(log);
                Console.WriteLine("Log registrado com sucesso no Firestore.");

                // Adiciona o log ao cache
                if (!DadosCache.Tabelas.ContainsKey("Historico"))
                {
                    DadosCache.Tabelas["Historico"] = new List<object>();
                }
                DadosCache.Tabelas["Historico"].Add(log);
                Console.WriteLine("Log registrado com sucesso no cache.");

                await RemoverLogsAntigosAsync(logsRef);
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Erro ao registrar log (ArgumentException): {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao registrar log: {ex.Message}");
            }
        }

        // TODO: UTILIZAR JSON QUANDO BANCO DE DADOS NÃO ESTIVER DISPONÍVEL
        // Função para obter todos os logs do cache
        public static List<LogData> ObterLogs()
        {
            if (!DadosCache.Tabelas.ContainsKey("Historico"))
            {
                Console.WriteLine("Nenhum log encontrado no cache.");
                return new List<LogData>();
            }

            try
            {
                var logs = DadosCache.Tabelas["Historico"].Cast<LogData>().OrderByDescending(log => log.Data).ToList();
                return logs;
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Erro ao obter logs (ArgumentException): {ex.Message}");
                return new List<LogData>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao obter logs: {ex.Message}");
                return new List<LogData>();
            }
        }

        // TODO: UTILIZAR JSON QUANDO BANCO DE DADOS NÃO ESTIVER DISPONÍVEL
        // Função para remover logs antigos se houver mais de MaxLogs
        private static async Task RemoverLogsAntigosAsync(CollectionReference logsRef)
        {
            try
            {
                var snapshot = await logsRef.OrderBy("Data").GetSnapshotAsync();
                if (snapshot.Count > MaxLogs)
                {
                    var logsParaRemover = snapshot.Documents.Take(snapshot.Count - MaxLogs);
                    foreach (var log in logsParaRemover)
                    {
                        await logsRef.Document(log.Id).DeleteAsync();
                    }
                    Console.WriteLine("Logs antigos removidos com sucesso do Firestore.");

                    // Remove os logs antigos do cache
                    var logsCache = DadosCache.Tabelas["Historico"].Cast<LogData>().OrderBy(log => log.Data).ToList();
                    var logsParaRemoverCache = logsCache.Take(logsCache.Count - MaxLogs).ToList();
                    foreach (var log in logsParaRemoverCache)
                    {
                        DadosCache.Tabelas["Historico"].Remove(log);
                    }
                    Console.WriteLine("Logs antigos removidos com sucesso do cache.");
                }
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Erro ao remover logs antigos (ArgumentException): {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao remover logs antigos: {ex.Message}");
            }
        }
    }
}
