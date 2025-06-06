using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class EditarDetalhesWindow : Window
    {
        private CompraData _compraOriginal;
        private CompraData _compraEditada;
        private ObservableCollection<MovimentacaoData> _itensCompra;
        private ObservableCollection<BoletoData> _boletos;
        private bool _apenasGerenciarBoletos;
        private string _diretorioBoletos;

        public EditarDetalhesWindow()
        {
            InitializeComponent();
        }

        public EditarDetalhesWindow(CompraData compra, bool apenasGerenciarBoletos = false)
        {
            InitializeComponent();

            _compraOriginal = compra;
            _compraEditada = CloneCompra(compra);
            _apenasGerenciarBoletos = apenasGerenciarBoletos;
            _itensCompra = new ObservableCollection<MovimentacaoData>(_compraEditada.Itens);

            // Configurar diretório para boletos
            ConfigurarDiretorioBoletos();

            // Carregar dados na interface
            CarregarDadosCompra();
            CarregarBoletos();

            // Configurar o comportamento da janela baseado na flag
            if (_apenasGerenciarBoletos)
            {
                ConfigurarModoGerenciarBoletos();
            }

            // Atualizar o valor total inicial
            CalcularValorTotal();
        }

        private void ConfigurarModoGerenciarBoletos()
        {
            Title = "Gerenciar Boletos";

            // Oculta seções não relacionadas a boletos
            ExpandirInfoCompraButton_Click(null, null);
            ExpandirPagamentoButton_Click(null, null);
            ExpandirItensButton_Click(null, null);

            // Desabilita edição de outros campos
            NotaFiscalTextBox.IsEnabled = false;
            FornecedorComboBox.IsEnabled = false;
            DataCompraDatePicker.IsEnabled = false;
            DetalhesTextBox.IsEnabled = false;
            TipoPagamentoComboBox.IsEnabled = false;
            ParcelasTextBox.IsEnabled = false;

            // Expande a seção de boletos
            ExpandirBoletosButton_Click(null, null);
        }

        private CompraData CloneCompra(CompraData original)
        {
            // Cria uma cópia profunda da compra para não modificar a original enquanto edita
            var clone = new CompraData
            {
                Id = original.Id,
                FornecedorId = original.FornecedorId,
                FornecedorNome = original.FornecedorNome,
                DataCompra = original.DataCompra,
                TipoPagamento = original.TipoPagamento,
                Parcelas = original.Parcelas,
                NotaFiscal = original.NotaFiscal,
                ValorTotal = original.ValorTotal,
                Detalhes = original.Detalhes,
                Boletos = original.Boletos != null ? new List<string>(original.Boletos) : new List<string>(),
                Itens = original.Itens.Select(item => new MovimentacaoData
                {
                    ProdutoId = item.ProdutoId,
                    ProdutoNome = item.ProdutoNome,
                    Tipo = item.Tipo,
                    Preco = item.Preco,
                    Quantidade = item.Quantidade,
                    Data = item.Data,
                    Detalhes = item.Detalhes,
                    CompraId = item.CompraId,
                    VendaId = item.VendaId
                }).ToList()
            };
            return clone;
        }

        private void ConfigurarDiretorioBoletos()
        {
            // Cria o diretório base para boletos se não existir
            string baseDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "RadiadoresLemos", "Boletos");

            if (!Directory.Exists(baseDir))
            {
                Directory.CreateDirectory(baseDir);
            }

            // Cria um diretório específico para esta compra baseado na nota fiscal
            string notaFiscal = _compraEditada.NotaFiscal ?? "SemNF";
            string fornecedor = _compraEditada.FornecedorNome?.Replace(" ", "_") ?? "SemFornecedor";
            _diretorioBoletos = Path.Combine(baseDir, $"{fornecedor}_{notaFiscal}");

            if (!Directory.Exists(_diretorioBoletos))
            {
                Directory.CreateDirectory(_diretorioBoletos);
            }

            // Atualiza o campo de texto na interface
            DiretorioBoletosTextBox.Text = _diretorioBoletos;
        }

        private void CarregarDadosCompra()
        {
            // Preenche os campos da tela com os dados da compra
            NotaFiscalTextBox.Text = _compraEditada.NotaFiscal;
            FornecedorComboBox.Text = _compraEditada.FornecedorNome;
            DataCompraDatePicker.SelectedDate = _compraEditada.DataCompra;
            ValorTotalTextBlock.Text = $"R$ {_compraEditada.ValorTotal:N2}";
            DetalhesTextBox.Text = _compraEditada.Detalhes;

            // Configura o tipo de pagamento
            if (_compraEditada.TipoPagamento == "Parcelado")
            {
                TipoPagamentoComboBox.SelectedIndex = 1;
                ParcelasTextBox.Text = _compraEditada.Parcelas.ToString();
                ParcelasTextBox.IsEnabled = true;
            }
            else
            {
                TipoPagamentoComboBox.SelectedIndex = 0;
                ParcelasTextBox.Text = "0";
                ParcelasTextBox.IsEnabled = false;
            }

            // Carrega os itens no DataGrid
            ItensDataGrid.ItemsSource = _itensCompra;

            // Carrega fornecedores para o ComboBox
            CarregarFornecedores();
        }

        private void CarregarFornecedores()
        {
            try
            {
                var db = DatabaseConnect.Database;
                if (db != null)
                {
                    var collection = db.GetCollection<FornecedorData>("fornecedores");
                    var fornecedores = collection.FindAll().Select(f => f.Nome).ToList();

                    FornecedorComboBox.ItemsSource = fornecedores;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar fornecedores: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CarregarBoletos()
        {
            try
            {
                _boletos = new ObservableCollection<BoletoData>();

                var db = DatabaseConnect.Database;
                if (db != null)
                {
                    var collection = db.GetCollection<BoletoData>("boletos");
                    var boletosList = collection.FindAll()
                        .Where(b => b.NotaFiscal == _compraEditada.NotaFiscal)
                        .ToList();

                    foreach (var boleto in boletosList)
                    {
                        _boletos.Add(boleto);
                    }
                }

                BoletosItemsControl.ItemsSource = _boletos;
                SemBoletosMessage.Visibility = _boletos.Any() ? Visibility.Collapsed : Visibility.Visible;

                AtualizarResumoBoletos();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar boletos: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AtualizarResumoBoletos()
        {
            // Atualiza as estatísticas dos boletos
            TotalBoletosTextBlock.Text = _boletos.Count.ToString();

            // Verifica o próximo vencimento
            var boletosNaoPagos = _boletos.Where(b => !b.Pagamento.HasValue).ToList();
            if (boletosNaoPagos.Any())
            {
                var proximoVencimento = boletosNaoPagos.OrderBy(b => b.Vencimento).First();
                ProximoVencimentoTextBlock.Text = proximoVencimento.Vencimento.ToString("dd/MM/yyyy");

                if (proximoVencimento.Vencimento.Date < DateTime.Now.Date)
                {
                    StatusBoletosTextBlock.Text = "Vencido";
                    StatusBoletosTextBlock.Foreground = new SolidColorBrush(Colors.Red);
                }
                else
                {
                    StatusBoletosTextBlock.Text = "Pendente";
                    StatusBoletosTextBlock.Foreground = new SolidColorBrush(Colors.Orange);
                }
            }
            else if (_boletos.Any())
            {
                ProximoVencimentoTextBlock.Text = "Todos pagos";
                StatusBoletosTextBlock.Text = "Quitado";
                StatusBoletosTextBlock.Foreground = new SolidColorBrush(Colors.Green);
            }
            else
            {
                ProximoVencimentoTextBlock.Text = "N/A";
                StatusBoletosTextBlock.Text = "Sem boletos";
                StatusBoletosTextBlock.Foreground = new SolidColorBrush(Colors.Gray);
            }
        }

        private void CalcularValorTotal()
        {
            decimal total = 0;

            foreach (var item in _itensCompra)
            {
                total += (decimal)(item.Preco * item.Quantidade);
            }

            _compraEditada.ValorTotal = total;
            ValorTotalTextBlock.Text = $"R$ {total:N2}";
        }

        // Eventos de botões de expansão
        private void ExpandirInfoCompraButton_Click(object sender, RoutedEventArgs e)
        {
            if (InfoCompraConteudo.Visibility == Visibility.Visible)
            {
                InfoCompraConteudo.Visibility = Visibility.Collapsed;
                ExpandirInfoCompraButton.Content = "▲";
            }
            else
            {
                InfoCompraConteudo.Visibility = Visibility.Visible;
                ExpandirInfoCompraButton.Content = "▼";
            }
        }

        private void ExpandirPagamentoButton_Click(object sender, RoutedEventArgs e)
        {
            if (PagamentoConteudo.Visibility == Visibility.Visible)
            {
                PagamentoConteudo.Visibility = Visibility.Collapsed;
                ExpandirPagamentoButton.Content = "▲";
            }
            else
            {
                PagamentoConteudo.Visibility = Visibility.Visible;
                ExpandirPagamentoButton.Content = "▼";
            }
        }

        private void ExpandirItensButton_Click(object sender, RoutedEventArgs e)
        {
            if (ItensConteudo.Visibility == Visibility.Visible)
            {
                ItensConteudo.Visibility = Visibility.Collapsed;
                ExpandirItensButton.Content = "▲";
            }
            else
            {
                ItensConteudo.Visibility = Visibility.Visible;
                ExpandirItensButton.Content = "▼";
            }
        }

        private void ExpandirBoletosButton_Click(object sender, RoutedEventArgs e)
        {
            if (BoletosConteudo.Visibility == Visibility.Visible)
            {
                BoletosConteudo.Visibility = Visibility.Collapsed;
                ExpandirBoletosButton.Content = "▲";
            }
            else
            {
                BoletosConteudo.Visibility = Visibility.Visible;
                ExpandirBoletosButton.Content = "▼";
            }
        }

        // Eventos de manipulação de fornecedores
        private void FornecedorComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filtro = FornecedorComboBox.Text.ToLower();

            if (FornecedorComboBox.ItemsSource != null)
            {
                CollectionView view = (CollectionView)CollectionViewSource.GetDefaultView(FornecedorComboBox.ItemsSource);
                view.Filter = item => ((string)item).ToLower().Contains(filtro);

                // Abre o dropdown se estiver digitando
                if (!string.IsNullOrEmpty(filtro) && !FornecedorComboBox.IsDropDownOpen)
                {
                    FornecedorComboBox.IsDropDownOpen = true;
                }
            }
        }

        private void FornecedorComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            _compraEditada.FornecedorNome = FornecedorComboBox.Text;
            ConfigurarDiretorioBoletos(); // Atualiza o diretório de boletos quando muda o fornecedor
        }

        // Eventos de manipulação de pagamento
        private void TipoPagamentoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TipoPagamentoComboBox.SelectedItem == null) return;

            var selectedItem = (ComboBoxItem)TipoPagamentoComboBox.SelectedItem;
            string tipoPagamento = selectedItem.Content.ToString();

            _compraEditada.TipoPagamento = tipoPagamento;

            if (tipoPagamento == "Parcelado")
            {
                ParcelasTextBox.IsEnabled = true;

                // Se não houver parcelas definidas, inicia com 1
                if (_compraEditada.Parcelas <= 0)
                {
                    _compraEditada.Parcelas = 1;
                    ParcelasTextBox.Text = "1";
                }
            }
            else
            {
                ParcelasTextBox.IsEnabled = false;
                ParcelasTextBox.Text = "0";
                _compraEditada.Parcelas = 0;
            }
        }

        private void ParcelasTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Permite apenas dígitos
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void ParcelasTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (int.TryParse(ParcelasTextBox.Text, out int parcelas))
            {
                _compraEditada.Parcelas = parcelas;
            }
        }

        // Eventos de manipulação de itens
        private void AdicionarItem_Click(object sender, RoutedEventArgs e)
        {
            // Aqui você adicionaria a lógica para abrir uma janela de seleção de produto
            // Por enquanto, vamos adicionar um item de exemplo
            MessageBox.Show("Funcionalidade de adicionar itens será implementada posteriormente.",
                "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void EditarItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is MovimentacaoData item)
            {
                // Aqui você adicionaria a lógica para editar o item selecionado
                MessageBox.Show($"Editar item: {item.ProdutoNome}",
                    "Editar Item", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Eventos de manipulação de boletos
        private void AdicionarBoleto_Click(object sender, RoutedEventArgs e)
        {
            // Verifica se é pagamento parcelado
            if (_compraEditada.TipoPagamento != "Parcelado")
            {
                var resultado = MessageBox.Show(
                    "Esta compra não está configurada como parcelada. Deseja alterá-la para parcelada?",
                    "Confirmação",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    _compraEditada.TipoPagamento = "Parcelado";
                    _compraEditada.Parcelas = 1;

                    TipoPagamentoComboBox.SelectedIndex = 1;
                    ParcelasTextBox.Text = "1";
                    ParcelasTextBox.IsEnabled = true;
                }
                else
                {
                    return;
                }
            }

            // Determina a próxima parcela
            int proximaParcela = 1;
            if (_boletos.Any())
            {
                proximaParcela = _boletos.Max(b => b.Parcela) + 1;
            }

            // Valida se não excede o número de parcelas
            if (proximaParcela > _compraEditada.Parcelas)
            {
                // Pergunta se deseja aumentar o número de parcelas
                var resultado = MessageBox.Show(
                    $"O número de boletos excederá o número de parcelas definido ({_compraEditada.Parcelas}). " +
                    $"Deseja aumentar para {proximaParcela} parcelas?",
                    "Confirmação",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (resultado == MessageBoxResult.Yes)
                {
                    _compraEditada.Parcelas = proximaParcela;
                    ParcelasTextBox.Text = proximaParcela.ToString();
                }
                else
                {
                    return;
                }
            }

            // Cria um novo boleto
            var novoBoleto = new BoletoData
            {
                Id = Guid.NewGuid().ToString(),
                NotaFiscal = _compraEditada.NotaFiscal,
                FornecedorId = _compraEditada.FornecedorId,
                Vencimento = DateTime.Now.AddMonths(proximaParcela - 1),
                Parcela = proximaParcela,
                CaminhoArquivo = ""
            };

            _boletos.Add(novoBoleto);

            // Atualiza a interface
            SemBoletosMessage.Visibility = Visibility.Collapsed;
            AtualizarResumoBoletos();
        }

        private void RemoverBoleto_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int parcela)
            {
                var boleto = _boletos.FirstOrDefault(b => b.Parcela == parcela);
                if (boleto != null)
                {
                    var resultado = MessageBox.Show(
                        $"Tem certeza que deseja remover o boleto da parcela {parcela}?",
                        "Confirmação",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (resultado == MessageBoxResult.Yes)
                    {
                        _boletos.Remove(boleto);

                        // Atualiza a interface
                        SemBoletosMessage.Visibility = _boletos.Any() ? Visibility.Collapsed : Visibility.Visible;
                        AtualizarResumoBoletos();
                    }
                }
            }
        }

        private void SelecionarBoleto_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is int parcela)
            {
                var boleto = _boletos.FirstOrDefault(b => b.Parcela == parcela);
                if (boleto != null)
                {
                    var openFileDialog = new OpenFileDialog
                    {
                        Filter = "Arquivos PDF|*.pdf|Todos os arquivos|*.*",
                        Title = $"Selecione o boleto para a parcela {parcela}"
                    };

                    if (openFileDialog.ShowDialog() == true)
                    {
                        try
                        {
                            string arquivoOriginal = openFileDialog.FileName;
                            string nomeArquivo = Path.GetFileName(arquivoOriginal);

                            // Gera um nome de arquivo baseado na nota fiscal e parcela
                            string nomeArquivoDestino = $"{_compraEditada.NotaFiscal}_Parcela{parcela}_{Path.GetExtension(arquivoOriginal)}";
                            string caminhoCompleto = Path.Combine(_diretorioBoletos, nomeArquivoDestino);

                            // Copia o arquivo para o diretório de boletos
                            File.Copy(arquivoOriginal, caminhoCompleto, true);

                            // Atualiza o boleto
                            boleto.CaminhoArquivo = caminhoCompleto;
                            boleto.NomeArquivo = nomeArquivoDestino;

                            // Força atualização da interface
                            int index = _boletos.IndexOf(boleto);
                            _boletos.Remove(boleto);
                            _boletos.Insert(index, boleto);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Erro ao selecionar o arquivo: {ex.Message}",
                                "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
            }
        }

        private void ImportarXML_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Funcionalidade de importação de XML será implementada posteriormente.",
                "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void AbrirDiretorio_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (Directory.Exists(_diretorioBoletos))
                {
                    System.Diagnostics.Process.Start("explorer.exe", _diretorioBoletos);
                }
                else
                {
                    MessageBox.Show("O diretório de boletos ainda não existe. Será criado quando você salvar as alterações.",
                        "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao abrir o diretório: {ex.Message}",
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Eventos de ação da janela
        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            var resultado = MessageBox.Show("Tem certeza que deseja cancelar? Todas as alterações serão perdidas.",
                "Confirmação", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (resultado == MessageBoxResult.Yes)
            {
                DialogResult = false;
                Close();
            }
        }

        private void Salvar_Click(object sender, RoutedEventArgs e)
        {
            // Valida se há informações essenciais
            if (string.IsNullOrWhiteSpace(_compraEditada.NotaFiscal))
            {
                MessageBox.Show("É necessário informar o número da Nota Fiscal.",
                    "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                NotaFiscalTextBox.Focus();
                return;
            }

            if (string.IsNullOrWhiteSpace(_compraEditada.FornecedorNome))
            {
                MessageBox.Show("É necessário informar o Fornecedor.",
                    "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                FornecedorComboBox.Focus();
                return;
            }

            try
            {
                // Atualiza os dados da compra a partir dos campos da interface
                _compraEditada.NotaFiscal = NotaFiscalTextBox.Text;
                _compraEditada.FornecedorNome = FornecedorComboBox.Text;
                _compraEditada.DataCompra = DataCompraDatePicker.SelectedDate ?? DateTime.Now;
                _compraEditada.Detalhes = DetalhesTextBox.Text;

                // Atualiza a lista de itens da compra
                _compraEditada.Itens = _itensCompra.ToList();

                // Persiste os boletos no banco de dados
                SalvarBoletos();

                // Atualiza a compra no banco de dados
                SalvarCompra();

                // Fecha a janela com sucesso
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar as alterações: {ex.Message}",
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SalvarBoletos()
        {
            var db = DatabaseConnect.Database;
            if (db != null)
            {
                var collection = db.GetCollection<BoletoData>("boletos");

                // Remove todos os boletos existentes desta compra
                var boletosExistentes = collection.FindAll()
                    .Where(b => b.NotaFiscal == _compraEditada.NotaFiscal)
                    .ToList();

                foreach (var boleto in boletosExistentes)
                {
                    collection.Delete(boleto.Id);
                }

                // Insere os boletos atualizados
                foreach (var boleto in _boletos)
                {
                    collection.Upsert(boleto);
                }
            }
        }

        private void SalvarCompra()
        {
            var db = DatabaseConnect.Database;
            if (db != null)
            {
                var collection = db.GetCollection<CompraData>("compras");

                // Atualiza a compra no banco de dados
                collection.Upsert(_compraEditada);

                // Atualiza a compra original para refletir as mudanças na tela de detalhes
                _compraOriginal.FornecedorId = _compraEditada.FornecedorId;
                _compraOriginal.FornecedorNome = _compraEditada.FornecedorNome;
                _compraOriginal.DataCompra = _compraEditada.DataCompra;
                _compraOriginal.TipoPagamento = _compraEditada.TipoPagamento;
                _compraOriginal.Parcelas = _compraEditada.Parcelas;
                _compraOriginal.NotaFiscal = _compraEditada.NotaFiscal;
                _compraOriginal.ValorTotal = _compraEditada.ValorTotal;
                _compraOriginal.Detalhes = _compraEditada.Detalhes;
                _compraOriginal.Boletos = _compraEditada.Boletos;
                _compraOriginal.Itens = _compraEditada.Itens;
            }
        }
    }
}