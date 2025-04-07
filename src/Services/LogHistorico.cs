using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WMS_RadiadoresLemos_WPF.src.Models;
using LiteDB;
using System.Windows;

namespace WMS_RadiadoresLemos_WPF.src.Services
{
    public static class LogHistorico
    {
        private const int MaxLogs = 100;
        private static readonly string CollectionName = "historico";

        public static async Task<List<LogData>> CarregarLogs()
        {
            try
            {
                if (DatabaseConnect.Database == null)
                {
                    MessageBox.Show("Banco de dados não está conectado", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return new List<LogData>();
                }

                var collection = DatabaseConnect.Database.GetCollection<LogData>(CollectionName);
                return collection.FindAll().ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar logs: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<LogData>();
            }
        }

        public static async Task SalvarLog(LogData log)
        {
            try
            {
                if (DatabaseConnect.Database == null)
                {
                    MessageBox.Show("Banco de dados não está conectado", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var collection = DatabaseConnect.Database.GetCollection<LogData>(CollectionName);
                collection.Insert(log);

                // Remove logs antigos se houver mais de MaxLogs
                var logs = collection.FindAll().OrderByDescending(l => l.Data).ToList();
                if (logs.Count > MaxLogs)
                {
                    var logsParaRemover = logs.Skip(MaxLogs);
                    foreach (var logAntigo in logsParaRemover)
                    {
                        collection.Delete(logAntigo.Id);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar log: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
