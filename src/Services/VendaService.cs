using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF.src.Services
{
    public static class VendaService
    {
        private static string _dataPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
        private static string _vendasFilePath = Path.Combine(_dataPath, "vendas.json");
        private static List<VendaData> _vendas = new List<VendaData>();

        static VendaService()
        {
            // Garantir que o diretório de dados exista
            if (!Directory.Exists(_dataPath))
            {
                Directory.CreateDirectory(_dataPath);
            }

            // Carregar dados existentes se o arquivo existir
            if (File.Exists(_vendasFilePath))
            {
                try
                {
                    var json = File.ReadAllText(_vendasFilePath);
                    _vendas = JsonSerializer.Deserialize<List<VendaData>>(json) ?? new List<VendaData>();
                }
                catch
                {
                    _vendas = new List<VendaData>();
                }
            }
        }

        public static async Task<bool> SalvarVenda(VendaData venda)
        {
            try
            {
                // Adicionar a nova venda à lista
                _vendas.Add(venda);

                // Salvar todas as vendas no arquivo JSON
                var json = JsonSerializer.Serialize(_vendas, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_vendasFilePath, json);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static List<VendaData> ObterVendas()
        {
            // Retornar a lista de vendas ordenada por data de cadastro (mais recentes primeiro)
            return _vendas.OrderByDescending(v => v.DataCadastro).ToList();
        }

        public static VendaData ObterVendaPorId(string id)
        {
            return _vendas.FirstOrDefault(v => v.Id == id);
        }
    }
}