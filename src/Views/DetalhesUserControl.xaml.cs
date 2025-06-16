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

            // Exibe o botão de excluir venda
            if (FindName("ExcluirVendaButton") is Button excluirBtn)
                excluirBtn.Visibility = Visibility.Visible;

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
                        else if (b.DataVencimento.Date < DateTime.Now.Date)
                            status = "Vencido";
                        else
                            status = "Pendente";

                        // Calcula dias até vencimento ou dias de atraso
                        int diasAteVencimento = (b.DataVencimento.Date - DateTime.Now.Date).Days;
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
                            Vencimento = b.DataVencimento,
                            VencimentoFormatado = b.DataVencimento.ToString("dd/MM/yyyy"),
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

                    // Atualiza visibilidade da mensagem de "sem boletos" se necessário
                    if (boletosTratados.Count == 0)
                    {
                        if (FindName("SemBoletosMessage") is TextBlock semBoletosMsg)
                        {
                            semBoletosMsg.Visibility = Visibility.Visible;
                            BoletosDataGrid.Visibility = Visibility.Collapsed;
                            semBoletosMsg.Text = "Nenhum boleto registrado para esta compra.";
                        }
                    }
                    else
                    {
                        if (FindName("SemBoletosMessage") is TextBlock semBoletosLabel)
                        {
                            semBoletosLabel.Visibility = Visibility.Collapsed;
                            BoletosDataGrid.Visibility = Visibility.Visible;
                        }
                    }

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

        private void Editar_Click(object sender, RoutedEventArgs e)
        {
            if (_isCompra && _compraAtual != null)
            {
                var editarJanela = new EditarDetalhesWindow(_compraAtual);
                bool? resultado = editarJanela.ShowDialog();

                if (resultado == true)
                {
                    // Recarrega a compra do banco de dados pelo ID atualizado
                    var db = DatabaseConnect.Database;
                    if (db != null)
                    {
                        var collection = db.GetCollection<CompraData>("compras");
                        var compraAtualizada = collection.FindById(_compraAtual.Id);
                        if (compraAtualizada != null)
                        {
                            _compraAtual = compraAtualizada;
                            DataContext = _compraAtual;
                            CarregarItensProduto(_compraAtual);
                            CarregarBoletos();
                        }
                    }
                }
            }
            else if (!_isCompra && _vendaAtual != null)
            {
                var editarJanela = new EditarDetalhesWindow(_vendaAtual);
                bool? resultado = editarJanela.ShowDialog();

                if (resultado == true)
                {
                    // Recarrega a venda do banco de dados pelo ID atualizado
                    var db = DatabaseConnect.Database;
                    if (db != null)
                    {
                        var collection = db.GetCollection<VendaData>("vendas");
                        var vendaAtualizada = collection.FindById(_vendaAtual.Id);
                        if (vendaAtualizada != null)
                        {
                            _vendaAtual = vendaAtualizada;
                            DataContext = _vendaAtual;
                            CarregarItensProduto(_vendaAtual);
                        }
                    }
                }
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
                    mensagem += $"• Parcela {boleto.Parcela} - Vencimento: {boleto.DataVencimento:dd/MM/yyyy} - Status: {status}\n";
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

        private void ExcluirVenda_Click(object sender, RoutedEventArgs e)
        {
            if (_vendaAtual == null) return;

            var confirm = MessageBox.Show($"Tem certeza que deseja excluir a venda da nota fiscal '{_vendaAtual.NotaFiscal}'? Essa ação não pode ser desfeita.",
                "Confirmar Exclusão", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm != MessageBoxResult.Yes) return;

            var db = DatabaseConnect.Database;
            if (db != null)
            {
                var collection = db.GetCollection<VendaData>("vendas");
                collection.Delete(_vendaAtual.Id);
                MessageBox.Show("Venda excluída com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);

                // Opcional: você pode navegar para outra tela ou atualizar a interface
                // Aqui, por padrão, pode-se ocultar os detalhes
                this.Visibility = Visibility.Collapsed;
            }
        }
    }
}