using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Google.Cloud.Firestore;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;
using WMS_RadiadoresLemos_WPF.src.Views;

namespace WMS_RadiadoresLemos_WPF
{
    // Definição parcial da classe RegistroEntradaSaidaUserControl que herda de UserControl
    public partial class RegistroEntradaSaidaUserControl : UserControl
    {
        // Declaração de variáveis privadas para armazenar dados dos produtos
        private List<ProdutoData> produtos = new List<ProdutoData>();
        private ObservableCollection<string> produtosFiltrados = new ObservableCollection<string>();
        private Dictionary<string, string> produtoNomeParaId = new Dictionary<string, string>();
        private ProdutoData? produtoSelecionado;

        // Construtor da classe que inicializa os componentes e carrega os produtos
        public RegistroEntradaSaidaUserControl()
        {
            InitializeComponent();
            ProdutoComboBox.ItemsSource = produtosFiltrados;
            CarregarProdutos();
        }

        // Método para carregar produtos do cache
        private void CarregarProdutos()
        {
            try
            {
                if (DadosCache.Tabelas.TryGetValue("Produtos", out List<object>? produtosCache))
                {
                    produtos = produtosCache.Cast<ProdutoData>().ToList();
                    produtoNomeParaId = produtos.ToDictionary(p => p.Nome, p => p.Id);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar produtos: {ex.Message}");

                // Adicionar alerta
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Erro ao carregar produtos do cache. Possíveis motivos:\n" +
                                            "- Falha na conexão com o banco de dados.\n" +
                                            "- Não foi possível carregar os produtos do cache.",
                                            "- Verifique a conexão com a internet.\n" +
                                            "- Reinicie o aplicativo.");
            }
        }

        // Método que é chamado quando o texto da caixa de pesquisa é alterado
        private async void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = ProdutoComboBox.Text.ToLower();
            var filteredProducts = await Task.Run(() =>
                produtos
                    .Where(p => p.Nome.ToLower().Contains(searchText))
                    .Select(p => p.Nome)
                    .ToList()
            );

            if (!filteredProducts.SequenceEqual(produtosFiltrados))
            {
                produtosFiltrados.Clear();
                foreach (var produto in filteredProducts)
                {
                    produtosFiltrados.Add(produto);
                }
            }

            ProdutoComboBox.IsDropDownOpen = produtosFiltrados.Count > 0;
        }


        // Método para confirmar se o produto selecionado é válido
        private void ProdutoComboBox_LostFocus(object sender, RoutedEventArgs e)
        {
            string inputText = ProdutoComboBox.Text;

            if (ProdutoComboBox.SelectedItem is string selectedProductName)
            {
                inputText = selectedProductName;
            }

            if (!string.IsNullOrEmpty(inputText) && !produtoNomeParaId.ContainsKey(inputText))
            {
                MessageBox.Show("Produto inválido.");
                ProdutoComboBox.Text = string.Empty;
                ProdutoComboBox.SelectedItem = null;
                ProdutoComboBox.Focus();
            }
        }


        // Método que é chamado quando a seleção do ComboBox é alterada
        private void ProdutoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProdutoComboBox.SelectedItem is string selectedProductName)
            {
                produtoSelecionado = produtos.FirstOrDefault(p => p.Nome == selectedProductName);
                if (produtoSelecionado != null)
                {
                    var detalhesProdutoWindow = new DetalhesProdutoWindow(produtoSelecionado);
                    detalhesProdutoWindow.ShowDialog();
                }
                else
                {
                    MessageBox.Show("Produto não encontrado no cache.");
                }
            }
        }


        // Método assíncrono para registrar a entrada de produtos
        private async void RegistrarEntrada_Click(object sender, RoutedEventArgs e)
        {
            await RegistrarMovimentacaoAsync(true);
        }

        // Método assíncrono para registrar a saída de produtos
        private async void RegistrarSaida_Click(object sender, RoutedEventArgs e)
        {
            await RegistrarMovimentacaoAsync(false);
        }

        // Método assíncrono para registrar a movimentação de produtos
        private async Task RegistrarMovimentacaoAsync(bool isEntrada)
        {
            try
            {
                if (produtoSelecionado != null)
                {
                    if (!int.TryParse(QuantidadeTextBox.Text, out int quantidade))
                    {
                        MessageBox.Show("Quantidade inválida.");
                        return;
                    }

                    if (!isEntrada && produtoSelecionado.Quantidade < quantidade)
                    {
                        MessageBox.Show("Quantidade insuficiente em estoque.");
                        return;
                    }

                    int quantidadeFinal = produtoSelecionado.Quantidade + (isEntrada ? quantidade : -quantidade);

                    // Garantir que a quantidade final nunca seja menor do que zero
                    if (quantidadeFinal < 0)
                    {
                        quantidadeFinal = 0;
                    }

                    double preco = produtoSelecionado.Preco;

                    // Garantir que o preço do produto não seja negativo
                    if (preco < 0)
                    {
                        preco = 0;
                    }

                    var confirmarRegistroWindow = new ConfirmarRegistroWindow(produtoSelecionado, quantidadeFinal, isEntrada);
                    if (confirmarRegistroWindow.ShowDialog() == true)
                    {
                        // Atualiza a quantidade do produto
                        produtoSelecionado.Quantidade = quantidadeFinal;

                        // Atualiza o produto no banco de dados
                        await AtualizarProdutoNoBanco(produtoSelecionado);
                        MessageBox.Show($"{(isEntrada ? "Entrada" : "Saída")} registrada: Produto - {produtoSelecionado.Nome}, Quantidade - {quantidade}");

                        // Adiciona log
                        var log = new LogData
                        {
                            Data = DateTime.UtcNow,
                            Tipo = "OPERACIONAL",
                            Nivel = "Usuário",
                            Detalhes = $"{(isEntrada ? "Entrada" : "Saída")} registrada: Produto - {produtoSelecionado.Nome}; Quantidade adicionada - {quantidade};  Quantidade atual - {quantidadeFinal}",
                            Usuario = "NomeDoUsuario" // Substitua pelo nome do usuário real
                        };
                        await LogHistorico.RegistrarLogAsync(log);

                        // Limpar campos após o registro, mantendo o produto selecionado
                        LimparCampos();
                    }

                }
                else
                {
                    MessageBox.Show("Selecione um produto.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao registrar movimentação: {ex.Message}");

                // Adicionar alerta
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Erro ao registrar movimentação de produtos. Possíveis motivos:\n" +
                                            "- Produto inválido ou não selecionado.\n" +
                                            "- Quantidade inválida ou insuficiente.\n" +
                                            "- Falha na conexão com o banco de dados.\n" +
                                            "- Não foi possível atualizar o produto no banco de dados.",
                                            "- Verifique se o produto foi selecionado corretamente.\n" +
                                            "- Verifique se a quantidade um valor é válido.\n" +
                                            "- Verifique a conexão com a internet.\n" +
                                            "- Reinicie o aplicativo.");
            }
        }

        // Método assíncrono para atualizar o produto no banco de dados
        private async Task AtualizarProdutoNoBanco(ProdutoData produto)
        {
            try
            {
                var db = DatabaseConnect.Database ?? throw new InvalidOperationException("Conexão com o banco de dados não estabelecida.");
                DocumentReference docRef = db.Collection("Produtos").Document(produto.Id);
                await docRef.SetAsync(produto, SetOptions.Overwrite);

                DadosCache.Tabelas["Produtos"] = produtos.Cast<object>().ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar produto no banco de dados: {ex.Message}");

                // Adicionar alerta
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Erro ao atualizar produto no banco de dados. Possíveis motivos:\n" +
                                            "- Falha na conexão com o banco de dados.\n" +
                                            "- Não foi possível atualizar o produto no banco de dados.",
                                            "- Verifique a conexão com a internet.\n" +
                                            "- Reinicie o aplicativo.");
            }
        }

        // Método para limpar os campos de entrada
        private void LimparCampos()
        {
            QuantidadeTextBox.Clear();
            PrecoTextBox.Clear();
            ProdutoComboBox.SelectedItem = null;
            ProdutoComboBox.Text = string.Empty;
        }

        // Método para validar a entrada de texto na caixa de quantidade
        private void QuantidadeTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }

        // Método para validar a colagem de texto na caixa de quantidade
        private void QuantidadeTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)) && !int.TryParse((string)e.DataObject.GetData(typeof(string)), out _))
            {
                e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }
        private void QuantidadeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && !string.IsNullOrEmpty(textBox.Text))
            {
                if (int.TryParse(textBox.Text, out int quantidade))
                {
                    textBox.Text = quantidade.ToString("N0", new System.Globalization.CultureInfo("pt-BR"));
                }
                else
                {
                    MessageBox.Show("Quantidade inválida.");
                    textBox.Clear();
                }
            }
        }

        // Método para validar a entrada de texto na caixa de preço (incluindo decimais e uma única vírgula)
        private void PrecoTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == ",")
            {
                if (((TextBox)sender).Text.Contains(","))
                {
                    e.Handled = true;
                }
                return;
            }

            e.Handled = !double.TryParse(e.Text, out _);
        }

        // Método para validar a colagem de texto na caixa de preço
        private void PrecoTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(string)) && !double.TryParse((string)e.DataObject.GetData(typeof(string)), out _))
            {
                e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        // Método para formatar o texto da caixa de preço ao perder o foco (1.000,00)
        private void PrecoTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && !string.IsNullOrEmpty(textBox.Text))
            {
                if (double.TryParse(textBox.Text, out double preco))
                {
                    textBox.Text = preco.ToString("N2", new System.Globalization.CultureInfo("pt-BR"));
                }
                else
                {
                    MessageBox.Show("Preço inválido.");
                    textBox.Clear();
                }
            }
        }
    }
}
