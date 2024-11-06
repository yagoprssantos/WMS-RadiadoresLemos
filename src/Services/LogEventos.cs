using Google.Cloud.Firestore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF.src.Services
{
    public static class LogEventos
    {
        private static readonly FirestoreDb? db;

        static LogEventos()
        {
            try
            {
                DatabaseConnect.SetEnvironmentVarible();
                db = DatabaseConnect.Database;
                Console.WriteLine("Conexão com o Firestore estabelecida com sucesso.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao estabelecer conexão com o Firestore: {ex.Message}");
            }
        }

        private const int MaxLogs = 100;

        // Função para registrar um log no Firestore
        public static async Task RegistrarLogAsync(LogData log)
        {
            if (log == null || string.IsNullOrWhiteSpace(log.Detalhes))
            {
                Console.WriteLine("Detalhes do log não podem ser vazios.");
                return;
            }

            if (db == null)
            {
                Console.WriteLine("Conexão com o Firestore não estabelecida.");
                return;
            }

            try
            {
                var logsRef = db.Collection("Eventos");
                await logsRef.AddAsync(log);
                Console.WriteLine("Log registrado com sucesso.");

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

        // Função para obter todos os logs do Firestore
        public static async Task<List<LogData>> ObterLogsAsync()
        {
            if (db == null)
            {
                Console.WriteLine("Conexão com o Firestore não estabelecida.");
                return new List<LogData>();
            }

            try
            {
                var logsRef = db.Collection("Eventos");
                var snapshot = await logsRef.OrderByDescending("Data").GetSnapshotAsync();
                var logs = snapshot.Documents.Select(doc => doc.ConvertTo<LogData>()).ToList();
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
                    Console.WriteLine("Logs antigos removidos com sucesso.");
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
