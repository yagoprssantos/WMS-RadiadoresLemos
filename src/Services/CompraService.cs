using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF.src.Services
{
    public static class CompraService
    {
        private static string _dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
        private static string _comprasFilePath = Path.Combine(_dataPath, "compras.json");
        private static List<CompraData> _compras = new List<CompraData>();

        static CompraService()
        {
            // Garantir que o diretório de dados exista
            if (!Directory.Exists(_dataPath))
            {
                Directory.CreateDirectory(_dataPath);
            }

            // Carregar dados existentes se o arquivo existir
            if (File.Exists(_comprasFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_comprasFilePath);
                    _compras = JsonSerializer.Deserialize<List<CompraData>>(json) ?? new List<CompraData>();
                }
                catch
                {
                    _compras = new List<CompraData>();
                }
            }
        }

        public static async Task<bool> SalvarCompra(CompraData compra)
        {
            try
            {
                // Adicionar a nova compra à lista
                _compras.Add(compra);

                // Salvar todas as compras no arquivo JSON
                var json = JsonSerializer.Serialize(_compras, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_comprasFilePath, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static List<CompraData> ObterCompras()
        {
            // Retornar a lista de compras ordenada por data de cadastro (mais recentes primeiro)
            return _compras.OrderByDescending(c => c.DataCadastro).ToList();
        }

        public static CompraData? ObterCompraPorId(string id)
        {
            return _compras.FirstOrDefault(c => c.Id != null && c.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }
    }
}