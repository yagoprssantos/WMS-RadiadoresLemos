using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WMS_RadiadoresLemos_WPF.Classes;
using Google.Cloud.Firestore;

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
                if (Cache.Tabelas.TryGetValue("Produtos", out List<object>? produtosCache))
                {
                    produtos = produtosCache.Cast<ProdutoData>().ToList();
                    produtoNomeParaId = produtos.ToDictionary(p => p.Nome, p => p.Id);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar produtos: {ex.Message}");
            }
        }

        // Método que é chamado quando o texto da caixa de pesquisa é alterado
        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchText = ProdutoComboBox.Text.ToLower();
            var filteredProducts = produtos
                .Where(p => p.Nome.ToLower().Contains(searchText))
                .Select(p => p.Nome)
                .ToList();

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

        // Método que é chamado quando a seleção do ComboBox é alterada
        private void ProdutoComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ProdutoComboBox.SelectedItem is string selectedProductName)
            {
                produtoSelecionado = produtos.FirstOrDefault(p => p.Nome == selectedProductName);
                if (produtoSelecionado != null)
                {
                    MessageBox.Show($"Produto selecionado:\n" +
                                    $"Nome: {produtoSelecionado.Nome}\n" +
                                    $"Tipo: {produtoSelecionado.Tipo}\n" +
                                    $"Marca: {produtoSelecionado.Marca}\n" +
                                    $"Código: {produtoSelecionado.Codigo}\n" +
                                    $"Quantidade: {produtoSelecionado.Quantidade}");
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

                    // Diálogo de confirmação
                    var resultado = MessageBox.Show(
                        $"Produto: {produtoSelecionado.Nome}\n" +
                        $"Quantidade Atual: {produtoSelecionado.Quantidade}\n" +
                        $"Quantidade {(isEntrada ? "Após Entrada" : "Após Saída")}: {quantidadeFinal}\n\n" +
                        "Deseja confirmar a operação?",
                        "Confirmação",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (resultado == MessageBoxResult.Yes)
                    {
                        produtoSelecionado.Quantidade = quantidadeFinal;
                        await AtualizarProdutoNoBanco(produtoSelecionado);
                        MessageBox.Show($"{(isEntrada ? "Entrada" : "Saída")} registrada: Produto - {produtoSelecionado.Nome}, Quantidade - {quantidade}");

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

                Cache.Tabelas["Produtos"] = produtos.Cast<object>().ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar produto no banco de dados: {ex.Message}");
            }
        }

        // Método para limpar os campos de entrada
        private void LimparCampos()
        {
            QuantidadeTextBox.Clear();
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
    }
}
