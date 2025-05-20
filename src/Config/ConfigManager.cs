using System;
using System.IO;
using System.Text.Json;

namespace WMS_RadiadoresLemos_WPF.src.Config
{
    public class AzureMetricsConfig
    {
        public string AzureSubscriptionId { get; set; }
        public string AzureResourceId { get; set; }
        public string AzureWorkspaceId { get; set; }
        public string AzureApiKey { get; set; }
        public string AzureTenantId { get; set; }
        public string AzureClientId { get; set; }
    }

    public static class ConfigManager
    {
        private static readonly string ConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "WMS-RadiadoresLemos",
            "azure_config.json"
        );

        public static AzureMetricsConfig LoadConfig()
        {
            try
            {
                if (File.Exists(ConfigPath))
                {
                    var json = File.ReadAllText(ConfigPath);
                    return JsonSerializer.Deserialize<AzureMetricsConfig>(json);
                }
            }
            catch (Exception)
            {
                // Se houver erro ao carregar, retorna configuração padrão
            }

            return new AzureMetricsConfig
            {
                AzureSubscriptionId = string.Empty,
                AzureResourceId = string.Empty,
                AzureWorkspaceId = string.Empty,
                AzureApiKey = string.Empty,
                AzureTenantId = string.Empty,
                AzureClientId = string.Empty
            };
        }

        public static void SaveConfig(AzureMetricsConfig config)
        {
            try
            {
                var directory = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                throw new Exception($"Erro ao salvar configurações: {ex.Message}");
            }
        }
    }
} 