using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Linq;

namespace WMS_RadiadoresLemos_WPF.src.Models
{
    public class AzureUsageStats
    {
        public DateTime Data { get; set; }
        public int Uploads { get; set; }
        public int Downloads { get; set; }
        public int Deletes { get; set; }
        public long TotalSize { get; set; }
        public string FormattedSize { get; set; }

        private static readonly string StatsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WMS-RadiadoresLemos",
            "azure_stats.json"
        );

        public static void SaveStats(int uploads, int downloads, int deletes, long totalSize)
        {
            try
            {
                var stats = new AzureUsageStats
                {
                    Data = DateTime.Now,
                    Uploads = uploads,
                    Downloads = downloads,
                    Deletes = deletes,
                    TotalSize = totalSize,
                    FormattedSize = FormatFileSize(totalSize)
                };

                var directory = Path.GetDirectoryName(StatsFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var statsList = LoadAllStats();
                statsList.Add(stats);

                // Mantém apenas os últimos 30 dias de estatísticas
                var thirtyDaysAgo = DateTime.Now.AddDays(-30);
                statsList = statsList.Where(s => s.Data >= thirtyDaysAgo).ToList();

                var json = JsonSerializer.Serialize(statsList, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(StatsFilePath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar estatísticas: {ex.Message}");
            }
        }

        public static List<AzureUsageStats> LoadAllStats()
        {
            try
            {
                if (!File.Exists(StatsFilePath))
                {
                    return new List<AzureUsageStats>();
                }

                var json = File.ReadAllText(StatsFilePath);
                return JsonSerializer.Deserialize<List<AzureUsageStats>>(json) ?? new List<AzureUsageStats>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao carregar estatísticas: {ex.Message}");
                return new List<AzureUsageStats>();
            }
        }

        private static string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB", "TB" };
            int order = 0;
            double size = bytes;
            
            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }
    }
} 