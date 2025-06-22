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
        private VendaData _vendaOriginal;
        private VendaData _vendaEditada;
        private bool _isCompra;
        private ObservableCollection<ItemEdicaoViewModel> _itensCompra;
        private ObservableCollection<BoletoData> _boletos = new ObservableCollection<BoletoData>();
        private List<FornecedorData> _fornecedores = new List<FornecedorData>();
        private string _fornecedorSelecionado;
        private bool _apenasGerenciarBoletos;
        private string _diretorioBoletos;

        public EditarDetalhesWindow(CompraData compra, bool apenasGerenciarBoletos = false)
        {
            InitializeComponent();
            _isCompra = true;
            _compraOriginal = compra;
            _compraEditada = CloneCompra(compra);
            _apenasGerenciarBoletos = apenasGerenciarBoletos;

            // Converte os itens para usar o ViewModel
            _itensCompra = new ObservableCollection<ItemEdicaoViewModel>(
                _compraEditada.Itens.Select(item => new ItemEdicaoViewModel(item))
            );

            // Configurar diretório de boletos
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            _diretorioBoletos = Path.Combine(appDataPath, "RadiadoresLemos", "Boletos");

            // Carrega dados e configura a interface
            CarregarFornecedores();
            CarregarDadosCompra();
            CarregarBoletos();
            CalcularValorTotal();

            // Configurar o comportamento da janela baseado na flag
            if (_apenasGerenciarBoletos)
            {
                ConfigurarModoGerenciarBoletos();
            }
        }

        public EditarDetalhesWindow(VendaData venda)
        {
            InitializeComponent();
            _isCompra = false;
            _vendaOriginal = venda;
            _vendaEditada = CloneVenda(venda);

            // Converte os itens para usar o ViewModel
            _itensCompra = new ObservableCollection<ItemEdicaoViewModel>(
                _vendaEditada.Itens.Select(item => new ItemEdicaoViewModel(item))
            );

            // Carrega dados e configura a interface
            CarregarDadosVenda();
            CalcularValorTotal();

            // Esconde a seção de boletos para vendas
            BoletosSection.Visibility = Visibility.Collapsed;
        }

        private void ConfigurarModoGerenciarBoletos()
        {

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

        private VendaData CloneVenda(VendaData original)
        {
            // Cria uma cópia profunda da venda para não modificar a original enquanto edita
            var clone = new VendaData
            {
                Id = original.Id,
                ClienteCNPJ = original.ClienteCNPJ,
                DataCompra = original.DataCompra,
                TipoPagamento = original.TipoPagamento,
                Parcelas = original.Parcelas,
                NotaFiscal = original.NotaFiscal,
                ValorTotal = original.ValorTotal,
                Detalhes = original.Detalhes,
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

        private async void CarregarFornecedores()
        {
            try
            {
                var db = DatabaseConnect.Database;
                if (db != null)
                {
                    var collection = db.GetCollection<FornecedorData>("fornecedores");
                    _fornecedores = collection.FindAll().ToList();

                    // Adiciona os fornecedores ao ComboBox
                    FornecedorComboBox.ItemsSource = _fornecedores.Select(p => p.Nome).ToList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar fornecedores: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CarregarDadosCompra()
        {
            try
            {
                // Preenche os campos da tela com os dados da compra
                NotaFiscalTextBox.Text = _compraEditada.NotaFiscal;
                FornecedorComboBox.Text = _compraEditada.FornecedorNome;

                DataCompraDatePicker.SelectedDate = _compraEditada.DataCompra;
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
                    ParcelasTextBox.Text = "1";
                    ParcelasTextBox.IsEnabled = false;
                }

                // Carrega os itens no DataGrid usando o ViewModel
                ItensDataGrid.ItemsSource = _itensCompra;

                // Define o DataContext para habilitar os bindings
                DataContext = _compraEditada;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar dados da compra: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CarregarBoletos()
        {
            try
            {
                _boletos.Clear();

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
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar boletos: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CarregarDadosVenda()
        {
            // Configura os campos da interface com os dados da venda
            NotaFiscalTextBox.Text = _vendaEditada.NotaFiscal;
            FornecedorComboBox.Text = _vendaEditada.ClienteCNPJ;
            DataCompraDatePicker.SelectedDate = _vendaEditada.DataCompra;
            DetalhesTextBox.Text = _vendaEditada.Detalhes;
            TipoPagamentoComboBox.Text = _vendaEditada.TipoPagamento;
            ParcelasTextBox.Text = _vendaEditada.Parcelas.ToString();

            // Configura o DataGrid de itens
            ItensDataGrid.ItemsSource = _itensCompra;

            // Atualiza o título da janela
            Title = "Editar Detalhes da Venda";
        }

        private void CalcularValorTotal()
        {
            // Calcula o valor total com base nos itens da compra/venda usando o Subtotal do ViewModel
            decimal valorTotal = 0;
            foreach (var item in _itensCompra)
            {
                valorTotal += (decimal)item.Subtotal;
            }

            if (_isCompra && _compraEditada != null)
            {
                _compraEditada.ValorTotal = valorTotal;
                DataContext = null;
                DataContext = _compraEditada;
            }
            else if (!_isCompra && _vendaEditada != null)
            {
                _vendaEditada.ValorTotal = valorTotal;
                DataContext = null;
                DataContext = _vendaEditada;
            }
        }

        private void ItensDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction == DataGridEditAction.Commit)
            {
                // Atualiza o DataGrid para mostrar o subtotal recalculado
                ItensDataGrid.Items.Refresh();

                // Recalcula o valor total após a edição
                CalcularValorTotal();
            }
        }

        private void ExpandirInfoCompraButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePanel(InfoCompraConteudo, ExpandirInfoCompraButton);
        }

        private void ExpandirPagamentoButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePanel(PagamentoConteudo, ExpandirPagamentoButton);
        }

        private void ExpandirItensButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePanel(ItensConteudo, ExpandirItensButton);
        }

        private void ExpandirBoletosButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePanel(BoletosConteudo, ExpandirBoletosButton);
        }

        private void TogglePanel(Panel panel, Button button)
        {
            if (panel.Visibility == Visibility.Visible)
            {
                panel.Visibility = Visibility.Collapsed;
                button.Content = "▲";
            }
            else
            {
                panel.Visibility = Visibility.Visible;
                button.Content = "▼";
            }
        }

        private void FornecedorComboBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.Template.FindName("PART_EditableTextBox", comboBox) is TextBox textBox)
            {
                string searchText = textBox.Text;

                // Filtrar os fornecedores com base no texto digitado (case-insensitive)
                var filteredFornecedores = _fornecedores
                    .Where(f => f.Nome.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                    .Select(f => f.Nome)
                    .ToList();

                // Evita manipular Items quando ItemsSource está em uso
                comboBox.ItemsSource = null;
                comboBox.Items.Clear();

                // Apresenta apenas os fornecedores filtrados
                foreach (var nome in filteredFornecedores)
                {
                    comboBox.Items.Add(nome);
                }

                // Atualiza o texto da caixa de pesquisa (mantém o texto original digitado)
                textBox.Text = searchText;
                textBox.CaretIndex = textBox.Text.Length;
            }
        }

        private void FornecedorComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            string inputText = FornecedorComboBox.Text;

            if (FornecedorComboBox.SelectedItem is string selected)
                inputText = selected;

            var fornecedor = _fornecedores.FirstOrDefault(f => f.Nome == inputText);
            if (!string.IsNullOrEmpty(inputText) && fornecedor != null)
            {
                _fornecedorSelecionado = fornecedor.Nome;
                _compraEditada.FornecedorId = fornecedor.Id;
                _compraEditada.FornecedorNome = fornecedor.Nome;
            }
            else
            {
                FornecedorComboBox.Text = string.Empty;
                FornecedorComboBox.SelectedItem = null;
                _fornecedorSelecionado = null;
            }
        }

        private bool IsValidDecimalInput(string text)
        {
            // Remove símbolos de moeda para validação
            text = text.Replace("R$", "").Replace(".", "").Trim();

            // Permite apenas dígitos e no máximo uma vírgula
            if (string.IsNullOrEmpty(text)) return true;
            int commaCount = text.Count(c => c == ',');
            if (commaCount > 1) return false;
            return text.All(c => char.IsDigit(c) || c == ',');
        }

        private void TipoPagamentoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TipoPagamentoComboBox.SelectedItem == null) return;

            var selectedItem = (ComboBoxItem)TipoPagamentoComboBox.SelectedItem;
            string tipoPagamento = selectedItem.Content.ToString();

            _compraEditada.TipoPagamento = tipoPagamento;

            if (tipoPagamento == "À vista")
            {
                ParcelasTextBox.IsEnabled = false;
                _compraEditada.Parcelas = 1;
                ParcelasTextBox.Text = "1";
            }
            else
            {
                ParcelasTextBox.Text = "";
                ParcelasTextBox.IsEnabled = true;
            }
        }

        private void ParcelasTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Permite apenas dígitos e impede valor maior que 8
            if (!e.Text.All(char.IsDigit))
            {
                e.Handled = true;
                return;
            }

            var textBox = sender as TextBox;
            string novoTexto = textBox != null
                ? textBox.Text.Insert(textBox.SelectionStart, e.Text)
                : e.Text;

            if (int.TryParse(novoTexto, out int valor))
            {
                e.Handled = valor > 8 || valor < 1;
            }
            else
            {
                e.Handled = true;
            }
        }

        private void ParcelasTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox textBox)
                return;

            // Remove handlers para evitar recursão infinita ao alterar o texto
            textBox.TextChanged -= ParcelasTextBox_TextChanged;

            string textoOriginal = textBox.Text;
            if (!string.IsNullOrEmpty(textoOriginal))
            {
                // Remove formatação e espaços
                string textoLimpo = new string(textoOriginal.Where(char.IsDigit).ToArray());

                if (int.TryParse(textoLimpo, out int parcelas))
                {
                    // Limita o valor entre 1 e 8
                    if (parcelas < 1)
                        parcelas = 1;
                    else if (parcelas > 8)
                        parcelas = 8;

                    textBox.Text = parcelas.ToString("N0", new System.Globalization.CultureInfo("pt-BR"));
                    textBox.CaretIndex = textBox.Text.Length;

                    // Atualiza o objeto correto baseado no tipo de operação
                    if (_isCompra)
                    {
                        _compraEditada.Parcelas = parcelas;
                    }
                    else
                    {
                        _vendaEditada.Parcelas = parcelas;
                    }
                }
                else
                {
                    MessageBox.Show("Parcelas inválidas.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    textBox.Clear();
                    if (_isCompra)
                    {
                        _compraEditada.Parcelas = 0;
                    }
                    else
                    {
                        _vendaEditada.Parcelas = 0;
                    }
                }

                // Verifica a forma de pagamento para alterar o texto
                if (TipoPagamentoComboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    string tipoPagamentoSelecionado = selectedItem.Content?.ToString();

                    if (tipoPagamentoSelecionado == "À vista")
                    {
                        textBox.Text = "1";
                        textBox.IsEnabled = false;
                        if (_isCompra)
                        {
                            _compraEditada.Parcelas = 1;
                        }
                        else
                        {
                            _vendaEditada.Parcelas = 1;
                        }
                    }
                    else if (tipoPagamentoSelecionado == "Parcelado")
                    {
                        // Se for parcelado, impede parcelas iguais a 1
                        if (textBox.Text == "1")
                        {
                            textBox.Text = "";
                            if (_isCompra)
                            {
                                _compraEditada.Parcelas = 0;
                            }
                            else
                            {
                                _vendaEditada.Parcelas = 0;
                            }
                        }
                    }

                    if (!textBox.IsEnabled && tipoPagamentoSelecionado == "Parcelado")
                    {
                        textBox.IsEnabled = true;
                    }
                }
            }

            // Reanexa o handler
            textBox.TextChanged += ParcelasTextBox_TextChanged;
        }

        private void AdicionarItem_Click(object sender, RoutedEventArgs e)
        {
            // Implementação básica para abrir uma janela de seleção de produto
            MessageBox.Show("Funcionalidade de adicionar itens será implementada posteriormente.",
                "Informação", MessageBoxButton.OK, MessageBoxImage.Information);

            // Exemplo de como seria a implementação
            /*
            var novoItem = new MovimentacaoData
            {
                ProdutoId = "produto123",
                ProdutoNome = "Novo Produto",
                Quantidade = 1,
                Preco = 100.0,
                Tipo = "Entrada",
                Data = DateTime.Now
            };

            // Criar o ViewModel para o novo item
            var novoItemViewModel = new ItemEdicaoViewModel(novoItem);

            // Adicionar à lista
            _itensCompra.Add(novoItemViewModel);

            // Recalcular total
            CalcularValorTotal();
            */
        }

        private void EditarItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is ItemEdicaoViewModel item)
            {
                // Cria uma cópia do item para edição
                var itemEditado = new ItemEdicaoViewModel(new MovimentacaoData
                {
                    ProdutoId = item.ObterItem().ProdutoId,
                    ProdutoNome = item.ProdutoNome,
                    Quantidade = item.Quantidade,
                    Preco = item.Preco,
                    Tipo = _isCompra ? "Entrada" : "Saída",
                    Data = DateTime.Now,
                    Detalhes = item.ObterItem().Detalhes
                });

                // Abre a janela de edição
                var editarItemWindow = new EditarItemWindow(itemEditado);
                if (editarItemWindow.ShowDialog() == true)
                {
                    // Atualiza o item na lista com os dados editados
                    var index = _itensCompra.IndexOf(item);
                    _itensCompra[index] = itemEditado;

                    // Recalcula o valor total
                    CalcularValorTotal();

                    // Atualiza o DataGrid
                    ItensDataGrid.Items.Refresh();
                }
            }
        }

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
                DataVencimento = DateTime.Now.AddMonths(proximaParcela - 1),
                Parcela = proximaParcela,
                CaminhoArquivo = "",
                NomeArquivo = $"Boleto NF{_compraEditada.NotaFiscal} - Parcela {proximaParcela}"
            };

            _boletos.Add(novoBoleto);

            // Atualiza a interface
            SemBoletosMessage.Visibility = Visibility.Collapsed;
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
                            string extensao = Path.GetExtension(arquivoOriginal);

                            // Gera um nome de arquivo padronizado
                            string nomeArquivo = $"BoletoNF{_compraEditada.NotaFiscal}-Parcela{parcela}{extensao}";
                            string caminhoCompleto = Path.Combine(_diretorioBoletos, nomeArquivo);

                            // Verifica se o diretório existe, se não, cria
                            if (!Directory.Exists(_diretorioBoletos))
                            {
                                Directory.CreateDirectory(_diretorioBoletos);
                            }

                            // Copia o arquivo para o diretório de boletos
                            File.Copy(arquivoOriginal, caminhoCompleto, true);

                            // Atualiza o boleto
                            boleto.CaminhoArquivo = caminhoCompleto;
                            boleto.NomeArquivo = nomeArquivo;

                            // Força atualização da interface
                            var index = _boletos.IndexOf(boleto);
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
            if (true) { return; }

            var dialog = new OpenFileDialog
            {
                Title = "Selecione o arquivo XML da nota fiscal",
                Filter = "Arquivos XML (*.xml)|*.xml",
                InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                RestoreDirectory = true
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {
                    // Lógica para processar o XML (implementação futura)
                    MessageBox.Show("Funcionalidade de importação de XML será implementada completamente em uma versão futura.",
                        "Informação", MessageBoxButton.OK, MessageBoxImage.Information);

                    // Atualiza o campo da nota fiscal com o nome do arquivo
                    NotaFiscalTextBox.Text = Path.GetFileNameWithoutExtension(dialog.FileName);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao importar arquivo XML: {ex.Message}",
                        "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

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
            try
            {
                // Solicita a senha do usuário para confirmar a alteração
                var confirmarSenhaWindow = new ConfirmarSenhaWindow();
                confirmarSenhaWindow.ShowDialog();

                if (!confirmarSenhaWindow.IsConfirmed)
                {
                    return; // Cancela a operação se a senha não for confirmada
                }

                if (_isCompra)
                {
                    // Valida se há informações essenciais para compra
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

                    // Atualiza os dados da compra a partir dos campos da interface
                    _compraEditada.NotaFiscal = NotaFiscalTextBox.Text;
                    _compraEditada.FornecedorNome = FornecedorComboBox.Text;
                    _compraEditada.DataCompra = DataCompraDatePicker.SelectedDate ?? DateTime.Now;
                    _compraEditada.Detalhes = DetalhesTextBox.Text;

                    // Atualiza a lista de itens da compra, convertendo de volta para MovimentacaoData
                    _compraEditada.Itens = _itensCompra.Select(vm => vm.ObterItem()).ToList();

                    // Recalcula o valor total antes de salvar
                    CalcularValorTotal();

                    // Cria uma lista de nomes de boletos
                    _compraEditada.Boletos = _boletos.Select(b => b.NomeArquivo).ToList();

                    // Executa o processo de salvamento completo
                    RealizarSalvamentoCompleto();
                }
                else
                {
                    // Valida se há informações essenciais para venda
                    if (string.IsNullOrWhiteSpace(_vendaEditada.NotaFiscal))
                    {
                        MessageBox.Show("É necessário informar o número da Nota Fiscal.",
                            "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                        NotaFiscalTextBox.Focus();
                        return;
                    }

                    if (string.IsNullOrWhiteSpace(_vendaEditada.ClienteCNPJ))
                    {
                        MessageBox.Show("É necessário informar o Cliente.",
                            "Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                        FornecedorComboBox.Focus();
                        return;
                    }

                    // Atualiza os dados da venda a partir dos campos da interface
                    _vendaEditada.NotaFiscal = NotaFiscalTextBox.Text;
                    _vendaEditada.ClienteCNPJ = FornecedorComboBox.Text;
                    _vendaEditada.DataCompra = DataCompraDatePicker.SelectedDate ?? DateTime.Now;
                    _vendaEditada.Detalhes = DetalhesTextBox.Text;
                    _vendaEditada.TipoPagamento = TipoPagamentoComboBox.Text;
                    _vendaEditada.Parcelas = int.Parse(ParcelasTextBox.Text);

                    // Atualiza a lista de itens da venda
                    _vendaEditada.Itens = _itensCompra.Select(vm => vm.ObterItem()).ToList();

                    // Recalcula o valor total antes de salvar
                    CalcularValorTotal();

                    // Salva a venda no banco de dados
                    var db = DatabaseConnect.Database;
                    if (db != null)
                    {
                        var collection = db.GetCollection<VendaData>("vendas");
                        collection.Update(_vendaEditada);
                    }
                }

                // Fecha a janela com sucesso
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar alterações: {ex.Message}",
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RealizarSalvamentoCompleto()
        {
            var db = DatabaseConnect.Database;
            if (db == null) return;

            try
            {
                // 1. Salvar os boletos
                SalvarBoletos();

                // 2. Salvar a compra e suas movimentações
                SalvarCompra();

                // Se chegou até aqui sem erros, a transação foi concluída com sucesso
                MessageBox.Show("Alterações salvas com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                // Em caso de erro, exibe a mensagem e não fecha a janela
                MessageBox.Show($"Erro ao salvar as alterações: {ex.Message}\n\nAs mudanças não foram aplicadas.",
                    "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                // Indica que houve falha
                DialogResult = false;
                throw; // Propaga o erro para que a janela não seja fechada
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
                    .Where(b => b.NotaFiscal == _compraOriginal.NotaFiscal)
                    .ToList();

                foreach (var boleto in boletosExistentes)
                {
                    collection.Delete(boleto.Id);
                }

                // Insere os boletos atualizados
                foreach (var boleto in _boletos)
                {
                    // Verifica se o CNPJ do pagador é exatamente "38.046.801/0001-60"
                    if (!string.IsNullOrWhiteSpace(boleto.CnpjPagador))
                    {
                        string cnpjLimpo = boleto.CnpjPagador.Replace(".", "").Replace("/", "").Replace("-", "");
                        string cnpjEsperado = "38046801000160";
                        
                        if (cnpjLimpo != cnpjEsperado)
                        {
                            throw new InvalidOperationException(
                                $"CNPJ do pagador inválido no boleto da parcela {boleto.Parcela}!\n\n" +
                                $"CNPJ encontrado: {boleto.CnpjPagador}\n" +
                                $"CNPJ esperado: 38.046.801/0001-60\n\n" +
                                $"Por favor, verifique se o boleto é realmente da empresa Radiadores Lemos.");
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"CNPJ do pagador não foi informado no boleto da parcela {boleto.Parcela}!\n\n" +
                            $"Por favor, preencha o CNPJ do pagador para continuar.");
                    }

                    // Garante que a nota fiscal está atualizada
                    boleto.NotaFiscal = _compraEditada.NotaFiscal;
                    boleto.FornecedorId = _compraEditada.FornecedorId;

                    // Gera ID com base no nome do arquivo
                    if (string.IsNullOrEmpty(boleto.Id))
                    {
                        boleto.Id = Guid.NewGuid().ToString();
                    }

                    collection.Upsert(boleto);
                }
            }
        }

        private void SalvarCompra()
        {
            var db = DatabaseConnect.Database;
            if (db == null) return;

            var comprasCollection = db.GetCollection<CompraData>("compras");
            var movimentacoesCollection = db.GetCollection<MovimentacaoData>("movimentacoes");

            // Independentemente de a nota fiscal ter mudado, tratamos como se fosse uma nova compra
            // 1. Remover a compra original e suas referências
            comprasCollection.Delete(_compraOriginal.Id);

            // 2. Remover ou atualizar movimentações relacionadas à compra original
            var movimentacoesOriginais = movimentacoesCollection.FindAll()
                .Where(m => m.CompraId != null && m.CompraId.ToString() == _compraOriginal.Id)
                .ToList();

            foreach (var movimentacao in movimentacoesOriginais)
            {
                movimentacoesCollection.Delete(movimentacao.Id);
            }

            // 3. Criar nova ID para a compra editada
            _compraEditada.SetIdFromNotaFiscal();

            // 4. Atualizar o CompraId em todos os itens
            foreach (var item in _compraEditada.Itens)
            {
                if (Guid.TryParse(_compraEditada.Id, out Guid compraId))
                {
                    item.CompraId = compraId;
                }
            }

            // 5. Inserir a compra editada como nova
            comprasCollection.Insert(_compraEditada);

            // 6. Inserir as movimentações como novas com o ID da nova compra
            foreach (var item in _compraEditada.Itens)
            {
                movimentacoesCollection.Insert(item);
            }

            // 7. Atualizar o relacionamento com o fornecedor
            AtualizarRelacionamentoFornecedor();

            // 8. Atualizar a compra original para refletir as mudanças na tela de detalhes
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
            _compraOriginal.Id = _compraEditada.Id;
        }

        private void AtualizarRelacionamentoFornecedor()
        {
            var db = DatabaseConnect.Database;
            if (db != null)
            {
                var fornecedoresCollection = db.GetCollection<FornecedorData>("fornecedores");

                // Se o fornecedor mudou, atualiza os relacionamentos
                if (_compraOriginal.FornecedorId != _compraEditada.FornecedorId)
                {
                    // Remove a relação do fornecedor antigo
                    var fornecedorAntigo = fornecedoresCollection.FindById(_compraOriginal.FornecedorId);
                    if (fornecedorAntigo != null)
                    {
                        fornecedorAntigo.ComprasRelacionadas.Remove(_compraOriginal.Id);
                        fornecedoresCollection.Update(fornecedorAntigo);
                    }

                    // Adiciona a relação ao novo fornecedor
                    var fornecedorNovo = fornecedoresCollection.FindById(_compraEditada.FornecedorId);
                    if (fornecedorNovo != null)
                    {
                        if (!fornecedorNovo.ComprasRelacionadas.Contains(_compraEditada.Id))
                        {
                            fornecedorNovo.ComprasRelacionadas.Add(_compraEditada.Id);
                            fornecedoresCollection.Update(fornecedorNovo);
                        }
                    }
                }
                // Se apenas a nota fiscal mudou (e consequentemente o ID da compra)
                else if (_compraOriginal.Id != _compraEditada.Id)
                {
                    var fornecedor = fornecedoresCollection.FindById(_compraEditada.FornecedorId);
                    if (fornecedor != null)
                    {
                        // Remove referência antiga
                        fornecedor.ComprasRelacionadas.Remove(_compraOriginal.Id);

                        // Adiciona nova referência
                        if (!fornecedor.ComprasRelacionadas.Contains(_compraEditada.Id))
                        {
                            fornecedor.ComprasRelacionadas.Add(_compraEditada.Id);
                        }

                        fornecedoresCollection.Update(fornecedor);
                    }
                }
            }
        }
    }
}