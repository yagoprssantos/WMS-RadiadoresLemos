using System;
using System.IO;
using System.Windows;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF.src.Services
{
    public class OrganizarBoleto
    {
        private readonly string numeroNotaFiscal;

        public OrganizarBoleto(string numeroNotaFiscal)
        {
            this.numeroNotaFiscal = numeroNotaFiscal;
        }

        public void Organizar(BoletoData boleto)
        {
            try
            {
                if (string.IsNullOrEmpty(boleto.CaminhoArquivo) || !File.Exists(boleto.CaminhoArquivo))
                {
                    MessageBox.Show("Arquivo do boleto não encontrado.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Obtém a extensão do arquivo original
                string extensao = Path.GetExtension(boleto.CaminhoArquivo);
                
                // Cria o caminho base para os boletos
                string caminhoBase = Path.Combine(
                                                   Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                                                   "WMS-RadiadoresLemos",
                                                   "Boletos"
                                                 );

                // Cria as pastas do ano e mês
                string pastaAno = Path.Combine(caminhoBase, boleto.Vencimento.Year.ToString());
                string nomeMes = $"{boleto.Vencimento.Month} - {boleto.Vencimento.ToString("MMMM", new System.Globalization.CultureInfo("pt-BR"))}";
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
                MessageBox.Show($"Erro ao organizar boleto: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método para retornar o caminho do boleto organizado
        public string ObterCaminhoBoletoOrganizado(BoletoData boleto)
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