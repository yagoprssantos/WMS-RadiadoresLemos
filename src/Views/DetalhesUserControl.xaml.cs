using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.IO;
using System.Diagnostics;
using System.Windows.Input;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class DetalhesUserControl : UserControl
    {
        private CompraData? _compraAtual;
        private VendaData? _vendaAtual;
        private bool _isCompra;
        private List<BoletoData> _boletosList = new List<BoletoData>();

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
            DataContext = compra;
            FornecedorTextBox.Text = compra.FornecedorNome;

            if (FindName("FornecedorLabel") is TextBlock fornecedorLabel)
                fornecedorLabel.Text = "Fornecedor:";
            if (FindName("FornecedorTextBox") is TextBox fornecedorBox)
                fornecedorBox.Text = compra.FornecedorNome;

            CarregarItensProduto(compra);
            CarregarBoletos();
        }

        public DetalhesUserControl(VendaData venda)
        {
            InitializeComponent();
            _vendaAtual = venda;
            _isCompra = false;
            _boletosList = new List<BoletoData>();

            if (FindName("FornecedorLabel") is TextBlock fornecedorLabel)
                fornecedorLabel.Text = "Cliente:";
            if (FindName("FornecedorTextBox") is TextBox fornecedorBox)
                fornecedorBox.Text = venda.ClienteCNPJ;

            // Não mostra campos de boletos
            CampoBoletos.Visibility = Visibility.Collapsed;

            DataContext = venda;
        }


        private void CarregarItensProduto(CompraData compra)
        {
            // Converter os itens da compra para o formato que o DataGrid espera
            var produtosViewModel = compra.Itens.Select(item => new ProdutoCompraViewModel
            {
                Nome = item.ProdutoNome,
                Quantidade = item.Quantidade,
                PrecoUnitario = item.Preco,
                // O subtotal é calculado automaticamente na ViewModel
            }).ToList();

            // Atribuir ao DataGrid
            ProdutosDataGrid.ItemsSource = produtosViewModel;
        }
        private void CarregarBoletos()
        {
            try
            {
                var db = DatabaseConnect.Database;
                if (db != null && _compraAtual != null)
                {
                    var collection = db.GetCollection<BoletoData>("boletos");
                    _boletosList = collection.FindAll().ToList();

                    // Filtra pela Nota Fiscal
                    _boletosList = _boletosList.Where(b => b.NotaFiscal == _compraAtual.NotaFiscal).ToList();

                    // Trata dados para apresentar corretamente
                    var boletosTratados = _boletosList.Select(b => {
                        // Verifica se o arquivo realmente existe
                        bool arquivoExiste = !string.IsNullOrEmpty(b.CaminhoArquivo) && File.Exists(b.CaminhoArquivo);

                        // Calcula status do boleto
                        string status;
                        if (b.Pagamento.HasValue)
                            status = "Pago";
                        else if (b.Vencimento.Date < DateTime.Now.Date)
                            status = "Vencido";
                        else
                            status = "Pendente";

                        // Calcula dias até vencimento ou dias de atraso
                        int diasAteVencimento = (b.Vencimento.Date - DateTime.Now.Date).Days;
                        string situacaoVencimento;

                        if (b.Pagamento.HasValue)
                            situacaoVencimento = "Pago em " + b.Pagamento.Value.ToString("dd/MM/yyyy");
                        else if (diasAteVencimento > 0)
                            situacaoVencimento = $"Vence em {diasAteVencimento} dia(s)";
                        else if (diasAteVencimento < 0)
                            situacaoVencimento = $"Vencido há {Math.Abs(diasAteVencimento)} dia(s)";
                        else
                            situacaoVencimento = "Vence hoje";

                        // Informação de parcelamento
                        string infoParcelamento = _compraAtual.Parcelas > 0
                            ? $"Parcela {b.Parcela}/{_compraAtual.Parcelas}"
                            : $"Parcela {b.Parcela}";

                        // Criar o objeto ViewModel com todas as propriedades
                        return new BoletoViewModel
                        {
                            Original = b,
                            Id = b.Id,
                            Parcela = infoParcelamento,
                            Vencimento = b.Vencimento,
                            VencimentoFormatado = b.Vencimento.ToString("dd/MM/yyyy"),
                            Pagamento = b.Pagamento,
                            PagamentoFormatado = b.Pagamento.HasValue ? b.Pagamento.Value.ToString("dd/MM/yyyy") : "Pendente",
                            CaminhoArquivo = b.CaminhoArquivo,
                            NotaFiscal = b.NotaFiscal,
                            FornecedorId = b.FornecedorId,
                            NomeArquivo = !string.IsNullOrEmpty(b.CaminhoArquivo) ? Path.GetFileName(b.CaminhoArquivo) : "Sem arquivo",
                            ArquivoExiste = arquivoExiste,
                            NomeFornecedor = _compraAtual.FornecedorNome,
                            SituacaoVencimento = situacaoVencimento,
                        };
                    }).ToList();

                    // Apresenta no DataGrid de boletos
                    if (FindName("BoletosDataGrid") is DataGrid boletosDataGrid)
                    {
                        boletosDataGrid.ItemsSource = boletosTratados;
                    }

                    // Atualiza visibilidade da mensagem de "sem boletos"
                    if (FindName("SemBoletosMessage") is TextBlock semBoletosMsg)
                        semBoletosMsg.Visibility = boletosTratados.Any() ? Visibility.Collapsed : Visibility.Visible;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar boletos: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void AbrirPDF_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.CommandParameter is string caminhoArquivo)
            {
                try
                {
                    if (File.Exists(caminhoArquivo))
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "cmd.exe",
                            Arguments = $"/c start \"\" \"{caminhoArquivo}\"",
                            UseShellExecute = false,
                            CreateNoWindow = true
                        };
                        Process.Start(psi);
                    }
                    else
                    {
                        MessageBox.Show("O arquivo PDF não foi encontrado.",
                            "Arquivo não encontrado", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao abrir o arquivo: {ex.Message}",
                        "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private string GetIdentifier(string fornecedorId_clienteCNPJ)
        {
            if (_isCompra && _compraAtual != null)
            {
                return _compraAtual.FornecedorNome;
            }
            else if (!_isCompra && _vendaAtual != null)
            {
                // Se o parâmetro for igual ao ClienteCNPJ, retorna o CNPJ
                if (!string.IsNullOrEmpty(fornecedorId_clienteCNPJ) &&
                    fornecedorId_clienteCNPJ == _vendaAtual.ClienteCNPJ)
                {
                    return _vendaAtual.ClienteCNPJ;
                }
                // Se o parâmetro for igual ao ClienteId, retorna o CNPJ também
                if (!string.IsNullOrEmpty(fornecedorId_clienteCNPJ) &&
                    fornecedorId_clienteCNPJ == _vendaAtual.ClienteId)
                {
                    return _vendaAtual.ClienteCNPJ;
                }
                // Caso não bata, retorna o CNPJ padrão
                return _vendaAtual.ClienteCNPJ;
            }
            return "Desconhecido";
        }

        private void Editar_Click(object sender, RoutedEventArgs e)
        {
        }

        private void VisualizarBoletos_Click(object sender, RoutedEventArgs e)
        {
            if (_boletosList.Count == 0)
            {
                MessageBox.Show("Não há boletos registrados para esta operação",
                    "Boletos", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            string diretorioBoletos = Path.GetDirectoryName(_boletosList.First().CaminhoArquivo);

            if (!Directory.Exists(diretorioBoletos))
            {
                try
                {
                    Directory.CreateDirectory(diretorioBoletos);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Não foi possível criar o diretório de boletos.\n\nDetalhes: {ex.Message}",
                        "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c start \"\" \"{diretorioBoletos}\"",
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                Process.Start(psi);
            }
            catch (Exception ex)
            {
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
            if (true) { return; }

            // Código para testes futuros
            string idOperacao = _isCompra ? _compraAtual?.Id : _vendaAtual?.Id;

            if (string.IsNullOrEmpty(idOperacao))
            {
                MessageBox.Show("Não é possível adicionar boletos sem um ID de operação válido",
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

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

            if (_isCompra && _compraAtual != null)
            {
                _compraAtual.Boletos ??= new List<string>();
                _compraAtual.Boletos.Add(novoBoletoId);

                if (FindName("BoletosDataGrid") is DataGrid boletosDataGrid)
                {
                    boletosDataGrid.ItemsSource = null;
                    boletosDataGrid.ItemsSource = _boletosList;
                }
            }
            else if (!_isCompra && _vendaAtual != null)
            {
                _vendaAtual.Boletos ??= new List<string>();
                _vendaAtual.Boletos.Add(novoBoletoId);

                if (FindName("BoletosDataGrid") is DataGrid boletosDataGrid)
                {
                    boletosDataGrid.ItemsSource = null;
                    boletosDataGrid.ItemsSource = _boletosList;
                }
            }


            MessageBox.Show($"Boleto {novoBoletoId} adicionado com sucesso",
                "Boleto Adicionado", MessageBoxButton.OK, MessageBoxImage.Information);


        }
    }
}