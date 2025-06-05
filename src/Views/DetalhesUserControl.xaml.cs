using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Diagnostics;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class DetalhesUserControl : UserControl
    {
        private CompraData? _compraAtual;
        private VendaData? _vendaAtual;
        private bool _isCompra;
        private List<BoletoData> _boletosList;

        public DetalhesUserControl()
        {
            InitializeComponent();
            _boletosList = new List<BoletoData>();
        }

        public DetalhesUserControl(CompraData compra)
        {
            InitializeComponent();
            _compraAtual = compra;
            _isCompra = true;
            _boletosList = new List<BoletoData>();

            // Carregar dados de boletos relacionados (implementação real carregaria do banco)
            CarregarBoletos();

            // Calcular próximo vencimento
            CalcularProximoVencimento();

            // Configura a exibição para compras
            if (FindName("FornecedorLabel") is TextBlock fornecedorLabel)
                fornecedorLabel.Text = "Fornecedor:";
            if (FindName("FornecedorTextBox") is TextBox fornecedorBox)
                fornecedorBox.Text = compra.FornecedorNome;

            DataContext = compra;
        }

        public DetalhesUserControl(VendaData venda)
        {
            InitializeComponent();
            _vendaAtual = venda;
            _isCompra = false;
            _boletosList = new List<BoletoData>();

            // Carregar dados de boletos relacionados (implementação real carregaria do banco)
            CarregarBoletos();

            // Calcular próximo vencimento
            CalcularProximoVencimento();

            // Ajusta labels específicos para venda
            if (FindName("FornecedorLabel") is TextBlock fornecedorLabel)
                fornecedorLabel.Text = "Cliente:";
            if (FindName("FornecedorTextBox") is TextBox fornecedorBox)
                fornecedorBox.Text = venda.ClienteCNPJ;

            DataContext = venda;
        }

        private void CarregarBoletos()
        {
            // Implementação fictícia para simular carregamento de boletos
            // Em um cenário real, você buscaria esses dados do banco de dados
            if (_compraAtual != null && _compraAtual.Boletos != null)
            {
                foreach (var boletoId in _compraAtual.Boletos)
                {
                    _boletosList.Add(new BoletoData
                    {
                        Id = boletoId,
                        FornecedorId = _compraAtual.FornecedorId,
                        NotaFiscal = _compraAtual.NotaFiscal,
                        Vencimento = DateTime.Now.AddDays(30),
                        Parcela = _boletosList.Count + 1,
                        CaminhoArquivo = $"boletos/{boletoId}.pdf"
                    });
                }
            }
            else if (_vendaAtual != null && _vendaAtual.Boletos != null)
            {
                foreach (var boletoId in _vendaAtual.Boletos)
                {
                    _boletosList.Add(new BoletoData
                    {
                        Id = boletoId,
                        FornecedorId = _vendaAtual.ClienteId, // No caso de venda, usamos o clienteId
                        NotaFiscal = _vendaAtual.NotaFiscal,
                        Vencimento = DateTime.Now.AddDays(30),
                        Parcela = _boletosList.Count + 1,
                        CaminhoArquivo = $"boletos/{boletoId}.pdf"
                    });
                }
            }
        }

        private void CalcularProximoVencimento()
        {
            if (_boletosList.Count > 0)
            {
                var boletosNaoPagos = _boletosList.Where(b => !b.Pagamento.HasValue)
                    .OrderBy(b => b.Vencimento).ToList();

                if (boletosNaoPagos.Any())
                {
                    if (_compraAtual != null)
                        _compraAtual.ProximoVencimento = boletosNaoPagos.First().Vencimento;
                }
            }
        }

        private void ImprimirPDF_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Funcionalidade de impressão em desenvolvimento", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Editar_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Funcionalidade de edição em desenvolvimento", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void GerarNotaFiscal_Click(object sender, RoutedEventArgs e)
        {
            string tipo = _isCompra ? "compra" : "venda";
            string notaFiscal = _isCompra ? _compraAtual?.NotaFiscal : _vendaAtual?.NotaFiscal;

            if (string.IsNullOrEmpty(notaFiscal))
            {
                MessageBox.Show($"Nota fiscal ainda não gerada para esta {tipo}. Deseja gerar uma nova?",
                    "Nota Fiscal", MessageBoxButton.YesNo, MessageBoxImage.Question);
            }
            else
            {
                MessageBox.Show($"Visualizando nota fiscal: {notaFiscal}",
                    "Nota Fiscal", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void VisualizarBoletos_Click(object sender, RoutedEventArgs e)
        {
            if (_boletosList.Count == 0)
            {
                MessageBox.Show("Não há boletos registrados para esta operação",
                    "Boletos", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Obter o diretório de boletos (usando o caminho do primeiro boleto como referência)
            string diretorioBoletos = Path.GetDirectoryName(_boletosList.First().CaminhoArquivo);

            // Verificar se o diretório existe
            if (!Directory.Exists(diretorioBoletos))
            {
                try
                {
                    // Criar o diretório se não existir
                    Directory.CreateDirectory(diretorioBoletos);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Não foi possível criar o diretório de boletos.\n\nDetalhes: {ex.Message}",
                        "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // Abrir o diretório usando o explorador de arquivos
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = diretorioBoletos,
                    UseShellExecute = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                // Mostrar os boletos disponíveis caso não consiga abrir o diretório
                string mensagem = "Não foi possível abrir a pasta de boletos.\n\nBoletos disponíveis:\n\n";

                foreach (var boleto in _boletosList)
                {
                    string status = boleto.Pagamento.HasValue ? "Pago" : "Pendente";
                    mensagem += $"• Parcela {boleto.Parcela} - Vencimento: {boleto.Vencimento:dd/MM/yyyy} - Status: {status}\n";
                }

                MessageBox.Show(mensagem, "Boletos", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void AdicionarBoleto_Click(object sender, RoutedEventArgs e)
        {
            // Implementação básica para adicionar um novo boleto
            // Em um cenário real, você abriria um diálogo para o usuário inserir informações do boleto

            string idOperacao = _isCompra ? _compraAtual?.Id : _vendaAtual?.Id;

            if (string.IsNullOrEmpty(idOperacao))
            {
                MessageBox.Show("Não é possível adicionar boletos sem um ID de operação válido",
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Criar um novo boleto com dados fictícios
            string novoBoletoId = $"BOL-{DateTime.Now.Ticks.ToString().Substring(10)}";

            var novoBoleto = new BoletoData
            {
                Id = novoBoletoId,
                FornecedorId = _isCompra ? _compraAtual?.FornecedorId ?? "" : _vendaAtual?.ClienteId ?? "",
                NotaFiscal = _isCompra ? _compraAtual?.NotaFiscal : _vendaAtual?.NotaFiscal,
                Vencimento = DateTime.Now.AddMonths(1),
                Parcela = _boletosList.Count + 1,
                CaminhoArquivo = $"boletos/{novoBoletoId}.pdf"
            };

            _boletosList.Add(novoBoleto);

            // Adicionar o boleto à lista da operação
            if (_isCompra && _compraAtual != null)
            {
                _compraAtual.Boletos ??= new List<string>();
                _compraAtual.Boletos.Add(novoBoletoId);

                // Atualizar a lista visual
                BoletosListView.ItemsSource = null;
                BoletosListView.ItemsSource = _compraAtual.Boletos;
            }
            else if (!_isCompra && _vendaAtual != null)
            {
                _vendaAtual.Boletos ??= new List<string>();
                _vendaAtual.Boletos.Add(novoBoletoId);

                // Atualizar a lista visual
                BoletosListView.ItemsSource = null;
                BoletosListView.ItemsSource = _vendaAtual.Boletos;
            }

            // Recalcular próximo vencimento
            CalcularProximoVencimento();

            MessageBox.Show($"Boleto {novoBoletoId} adicionado com sucesso",
                "Boleto Adicionado", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}