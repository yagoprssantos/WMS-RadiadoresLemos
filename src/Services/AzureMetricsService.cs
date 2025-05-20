using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using System;
using System.Threading.Tasks;
using WMS_RadiadoresLemos_WPF.src.Config;

namespace WMS_RadiadoresLemos.Services
{
    public class AzureMetricsService
    {
        private readonly string _subscriptionId;
        private readonly string _resourceId;
        private readonly string _workspaceId;
        private readonly LogsQueryClient _logsClient;

        public AzureMetricsService(string subscriptionId, string resourceId, string workspaceId, string clientSecret)
        {
            _subscriptionId = subscriptionId;
            _resourceId = resourceId;
            _workspaceId = workspaceId;

            var config = ConfigManager.LoadConfig();
            
            // Criar credencial usando ClientSecretCredential
            var credential = new ClientSecretCredential(
                config.AzureTenantId,
                config.AzureClientId,
                clientSecret
            );

            _logsClient = new LogsQueryClient(credential);
        }

        public async Task<string> GetDatabaseMetricsAsync()
        {
            try
            {
                var query = @"
                    AzureDiagnostics
                    | where ResourceType == 'STORAGEACCOUNTS'
                    | where OperationName in ('GetBlob', 'PutBlob', 'DeleteBlob')
                    | summarize count() by OperationName, bin(TimeGenerated, 1h)
                    | order by TimeGenerated desc";

                var response = await _logsClient.QueryWorkspaceAsync(
                    _workspaceId,
                    query,
                    new QueryTimeRange(TimeSpan.FromDays(1)));

                var result = "Métricas do Banco de Dados:\n\n";
                
                if (response.Value != null && response.Value.Table != null)
                {
                    foreach (var row in response.Value.Table.Rows)
                    {
                        result += $"Operação: {row[1]}, Hora: {row[2]}, Total: {row[3]}\n";
                    }
                }
                else
                {
                    result += "Nenhuma métrica encontrada para o período especificado.";
                }

                return result;
            }
            catch (Exception ex)
            {
                return $"Erro ao obter métricas: {ex.Message}";
            }
        }
    }
} 