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

            if (FindName("FCLabel") is TextBlock FCLabel)
                FCLabel.Text = "Fornecedor:";
            if (FindName("FCTextBox") is TextBox FCTextBox)
                FCTextBox.Text = compra.FornecedorNome;
            if (FindName("ProdutoLabel") is TextBlock ProdutoLabel)
                ProdutoLabel.Text = "Produtos comprados:";

            // Mostra campos de boletos
            CampoBoletos.Visibility = Visibility.Visible;

            DataContext = compra;

            CarregarItensProduto(compra);
            CarregarBoletos();
        }

        public DetalhesUserControl(VendaData venda)
        {
            InitializeComponent();
            _vendaAtual = venda;
            _isCompra = false;

            // Altera textos para Venda
            if (FindName("FCLabel") is TextBlock FCLabel)
                FCLabel.Text = "Cliente:";
            if (FindName("FCTextBox") is TextBox FCTextBox)
                FCTextBox.Text = venda.ClienteCNPJ;
            if (FindName("ProdutoLabel") is TextBlock ProdutoLabel)
                ProdutoLabel.Text = "Produtos vendidos:";

            // Não mostra campos de boletos
            CampoBoletos.Visibility = Visibility.Collapsed;

            DataContext = venda;

            CarregarItensProduto(venda);
        }


        private void CarregarItensProduto(CompraData compra)
        {
            // Converter os itens da compra para o formato que o DataGrid espera
            var produtosViewModel = compra.Itens.Select(item => new ProdutoViewModel
            {
                Nome = item.ProdutoNome,
                Quantidade = item.Quantidade,
                PrecoUnitario = item.Preco,
            }).ToList();

            // Atribuir ao DataGrid
            ProdutosDataGrid.ItemsSource = produtosViewModel;
        }
        private void CarregarItensProduto(VendaData venda)
        {
            // Converter os itens da venda para o formato que o DataGrid espera
            var produtosViewModel = venda.Itens.Select(item => new ProdutoViewModel
            {
                Nome = item.ProdutoNome,
                Quantidade = item.Quantidade,
                PrecoUnitario = item.Preco,
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
                    var boletosTratados = _boletosList.Select(b =>
                    {
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
            // Abre a janela de edição passando a compra atual
            var editarJanela = new EditarDetalhesWindow(_compraAtual);
            bool? resultado = editarJanela.ShowDialog();

            // Se a edição foi confirmada, recarrega os dados
            if (resultado == true)
            {
                // Atualiza o datacontext para refletir as mudanças
                DataContext = null;
                DataContext = _compraAtual;

                // Recarrega os boletos
                CarregarBoletos();

                // Atualiza a interface
                AtualizarCampos();
            }
        }

        private void AtualizarCampos()
        {
            // Atualiza os campos específicos que não são automaticamente atualizados pelo binding
            FCTextBox.Text = _compraAtual.FornecedorNome;
            ProdutosDataGrid.ItemsSource = null;
            ProdutosDataGrid.ItemsSource = _compraAtual.Itens;
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
        private void AdicionarBoletos_Click(object sender, RoutedEventArgs e)
        {
            // Abre a janela apenas para adicionar boletos
            var editarJanela = new EditarDetalhesWindow(_compraAtual, true);
            bool? resultado = editarJanela.ShowDialog();

            // Se a adição foi confirmada, recarrega os boletos
            if (resultado == true)
            {
                CarregarBoletos();
            }
        }
    }
}