using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF.src.Services
{
    public static class OrganizarBoleto
    {
        // Organiza boletos por data de vencimento
        public static List<BoletoData> OrganizarPorVencimento(List<BoletoData> boletos)
        {
            return boletos.OrderBy(b => b.DataVencimento).ToList(); //  CORRIGIDO: DataVencimento
        }

        // Filtra boletos vencidos
        public static List<BoletoData> FiltrarVencidos(List<BoletoData> boletos)
        {
            return boletos.Where(b => b.DataVencimento < DateTime.Now).ToList(); //  CORRIGIDO: DataVencimento
        }

        // Filtra boletos por status
        public static List<BoletoData> FiltrarPorStatus(List<BoletoData> boletos, StatusBoleto status)
        {
            return boletos.Where(b => b.Status == status).ToList();
        }

        // Filtra boletos por período
        public static List<BoletoData> FiltrarPorPeriodo(List<BoletoData> boletos, DateTime dataInicio, DateTime dataFim)
        {
            return boletos.Where(b => b.DataVencimento >= dataInicio && b.DataVencimento <= dataFim).ToList();
        }

        // Organiza boletos por valor
        public static List<BoletoData> OrganizarPorValor(List<BoletoData> boletos, bool crescente = true)
        {
            return crescente
                ? boletos.OrderBy(b => b.Valor).ToList()
                : boletos.OrderByDescending(b => b.Valor).ToList();
        }

        // Organiza boletos por beneficiário
        public static List<BoletoData> OrganizarPorBeneficiario(List<BoletoData> boletos)
        {
            return boletos.OrderBy(b => b.Beneficiario).ToList();
        }

        // Calcula total de valores por status
        public static Dictionary<StatusBoleto, decimal> CalcularTotalPorStatus(List<BoletoData> boletos)
        {
            return boletos.GroupBy(b => b.Status)
                         .ToDictionary(g => g.Key, g => g.Sum(b => b.Valor));
        }

        // Organiza o arquivo do boleto em pastas por ano/mês
        public static void OrganizarArquivoBoleto(BoletoData boleto, string numeroNotaFiscal)
        {
            try
            {
                // Obtém a extensão do arquivo original
                string extensao = Path.GetExtension(boleto.CaminhoArquivo);
                
                // Cria o caminho base para os boletos
                string caminhoBase = Path.Combine(
                                                 Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                                 "WMS-RadiadoresLemos",
                                                 "Boletos"
                                                );

                // Cria as pastas do ano e mês
                string pastaAno = Path.Combine(caminhoBase, boleto.DataVencimento.Year.ToString());
                string nomeMes = $"{boleto.DataVencimento.Month} - {boleto.DataVencimento.ToString("MMMM", new System.Globalization.CultureInfo("pt-BR"))}";
                string pastaMes = Path.Combine(pastaAno, nomeMes);
                
                // Cria as pastas se não existirem
                Directory.CreateDirectory(pastaMes);
                
                // Usa o número da nota fiscal armazenado
                if (string.IsNullOrEmpty(numeroNotaFiscal))
                {
                    return;
                }

                // Cria o novo nome do arquivo com a nota fiscal e parcela
                string novoNomeArquivo = $"BoletoNF{numeroNotaFiscal}-Parcela{boleto.Parcela}{extensao}";
                
                // Define o caminho de destino
                string caminhoDestino = Path.Combine(pastaMes, novoNomeArquivo);
                
                // Copia o arquivo para o destino
                File.Copy(boleto.CaminhoArquivo, caminhoDestino, true);
                
                // Atualiza o caminho do arquivo no objeto boleto
                boleto.NomeArquivo = novoNomeArquivo;
                boleto.CaminhoArquivo = caminhoDestino;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao organizar boleto: {ex.Message}", "Erro", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public static string ObterCaminhoBoletoOrganizado(BoletoData boleto)
        {
            // Apresenta o caminhoArquivo do boleto
            if (string.IsNullOrEmpty(boleto.CaminhoArquivo) || !File.Exists(boleto.CaminhoArquivo))
            {
                return "Caminho do boleto não encontrado ou inválido.";
            }
            return boleto.CaminhoArquivo;
        }
    }
}