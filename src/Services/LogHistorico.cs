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
    public static class LogHistorico
    {
        private const int MaxLogs = 100;
        private static readonly string CaminhoArquivoLogs = new DatabaseFileManager().ObterCaminhoArquivo("Historico");

        // Função para registrar um log no cache, no Firestore e no arquivo JSON
        public static async Task RegistrarLogAsync(LogData log)
        {
            if (log == null || string.IsNullOrWhiteSpace(log.Detalhes))
            {
                Console.WriteLine("Detalhes do log não podem ser vazios.");
                return;
            }

            try
            {
                var db = DatabaseConnect.Database;
                if (db == null || !DatabaseConnect.IsConnected)
                {
                    AtivarModoOffline();
                    await SalvarLogNoArquivoAsync(log);
                    return;
                }

                var logsRef = db.Collection("Historico");
                await logsRef.AddAsync(log);
                Console.WriteLine("Log registrado com sucesso no Firestore.");

                AdicionarLogAoCache(log);
                await SalvarLogNoArquivoAsync(log);
                await RemoverLogsAntigosAsync(logsRef);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao registrar log: {ex.Message}");
                AtivarModoOffline();
                await SalvarLogNoArquivoAsync(log);
            }
        }

        // Função para obter todos os logs do cache ou do arquivo JSON
        public static List<LogData> ObterLogs()
        {
            if (!DadosCache.Tabelas.ContainsKey("Historico"))
            {
                Console.WriteLine("Nenhum log encontrado no cache.");
                return LerLogsDoArquivo();
            }

            try
            {
                var logs = DadosCache.Tabelas["Historico"].Cast<LogData>().OrderByDescending(log => log.Data).ToList();
                return logs;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao obter logs: {ex.Message}");
                return LerLogsDoArquivo();
            }
        }

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

                    RemoverLogsAntigosDoCache();
                    await RemoverLogsAntigosDoArquivoAsync();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao remover logs antigos: {ex.Message}");
                AtivarModoOffline();
            }
        }

        // Função para salvar um log no arquivo JSON
        private static async Task SalvarLogNoArquivoAsync(LogData log)
        {
            var logs = LerLogsDoArquivo();
            logs.Add(log);
            await SalvarLogsNoArquivoAsync(logs);
        }

        // Função para salvar uma lista de logs no arquivo JSON
        private static async Task SalvarLogsNoArquivoAsync(List<LogData> logs)
        {
            try
            {
                string json = JsonSerializer.Serialize(logs);
                await File.WriteAllTextAsync(CaminhoArquivoLogs, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar logs no arquivo JSON: {ex.Message}");
            }
        }

        // Função para ler os logs do arquivo JSON
        private static List<LogData> LerLogsDoArquivo()
        {
            try
            {
                if (File.Exists(CaminhoArquivoLogs))
                {
                    string json = File.ReadAllText(CaminhoArquivoLogs);
                    return JsonSerializer.Deserialize<List<LogData>>(json) ?? new List<LogData>();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao ler logs do arquivo JSON: {ex.Message}");
            }
            return new List<LogData>();
        }

        // Função para ativar o modo offline
        private static void AtivarModoOffline()
        {
            Console.WriteLine("Modo offline ativado.");
        }

        // Função para adicionar um log ao cache
        private static void AdicionarLogAoCache(LogData log)
        {
            if (!DadosCache.Tabelas.ContainsKey("Historico"))
            {
                DadosCache.Tabelas["Historico"] = new List<object>();
            }
            DadosCache.Tabelas["Historico"].Add(log);
        }

        // Função para remover logs antigos do cache
        private static void RemoverLogsAntigosDoCache()
        {
            if (DadosCache.Tabelas.ContainsKey("Historico"))
            {
                var logs = DadosCache.Tabelas["Historico"].Cast<LogData>().OrderByDescending(log => log.Data).ToList();
                if (logs.Count > MaxLogs)
                {
                    // Captura os logs mais antigos
                    var logsParaRemover = logs.Skip(MaxLogs).ToList();

                    // Remove os logs mais antigos
                    foreach (var log in logsParaRemover)
                    {
                        DadosCache.Tabelas["Historico"].Remove(log);
                    }
                }
            }
        }

        // Função para remover logs antigos do arquivo JSON
        private static async Task RemoverLogsAntigosDoArquivoAsync()
        {
            var logs = LerLogsDoArquivo();
            if (logs.Count > MaxLogs)
            {
                logs = logs.OrderByDescending(log => log.Data).Take(MaxLogs).ToList();
                await SalvarLogsNoArquivoAsync(logs);
            }
        }
    }
}
