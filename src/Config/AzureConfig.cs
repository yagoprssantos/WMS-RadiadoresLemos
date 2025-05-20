using System;

namespace WMS_RadiadoresLemos_WPF.src.Config
{
    public static class AzureConfig
    {
        public static string ConnectionString { get; set; } = string.Empty;
        public static string ContainerName { get; set; } = "wms-backups";

        public static void Initialize(string connectionString)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new ArgumentException("A string de conexão não pode ser vazia.", nameof(connectionString));
            }

            ConnectionString = connectionString;
        }
    }
} 