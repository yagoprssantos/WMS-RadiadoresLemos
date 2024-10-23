using Google.Cloud.Firestore;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WMS_RadiadoresLemos_WPF.Classes;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class EditarProdutoWindow : Window
    {
        private ProdutoData produto;
        private bool isModified = false;
        private FirestoreDb firestoreDb = null!; // Inicializado com operador de negação

        // Propriedade pública para acessar o produto editado
        public ProdutoData Produto => produto;

        // Construtor que inicializa a janela com os dados do produto ou vazio
        public EditarProdutoWindow(ProdutoData? produto = null)
        {
            InitializeComponent();
            this.produto = produto ?? new ProdutoData();
            ConectarFirestore();
            PreencherCampos();
        }

        // Conecta ao Firestore
        private void ConectarFirestore()
        {
            try
            {
                firestoreDb = DatabaseConnect.Database ?? throw new InvalidOperationException("Não foi possível conectar ao Firestore.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao conectar ao Firestore: {ex.Message}");
            }
        }

        // Preenche os campos da interface com os dados do produto
        private void PreencherCampos()
        {
            try
            {
                NomeProduto.Text = produto.Nome;
                TipoProduto.Text = produto.Tipo;
                MarcaProduto.Text = produto.Marca;
                CodigoProduto.Text = produto.Codigo;
                QuantidadeInicial.Text = produto.Quantidade.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao preencher campos: {ex.Message}");
            }
        }

        // Evento disparado ao clicar no botão de atualizar produto
        private async void Salvar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ValidarCampos())
                {
                    produto.Nome = NomeProduto.Text;
                    produto.Tipo = TipoProduto.Text;
                    produto.Marca = MarcaProduto.Text;
                    produto.Codigo = CodigoProduto.Text;
                    produto.Quantidade = int.Parse(QuantidadeInicial.Text);

                    await AtualizarProdutoAsync(produto);

                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar produto: {ex.Message}");
            }
        }

        // Evento disparado ao clicar no botão de cancelar
        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isModified)
                {
                    var result = MessageBox.Show("Existem alterações não salvas. Deseja sair sem salvar?", "Confirmação", MessageBoxButton.YesNo);
                    if (result == MessageBoxResult.No)
                    {
                        return;
                    }
                }
                DialogResult = false;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao cancelar: {ex.Message}");
            }
        }

        // Restrições de entrada de texto nos TextBoxes
        // Apenas números para o campo Quantidade Inicial
        private void QuantidadeInicial_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                e.Handled = !IsTextAllowed(e.Text, "[^0-9]+");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao validar entrada: {ex.Message}");
            }
        }

        private void QuantidadeInicial_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            try
            {
                HandlePasting(e, "[^0-9]+");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao colar texto: {ex.Message}");
            }
        }

        // Apenas letras e espaços para o campo Marca do Produto
        private void MarcaProduto_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            try
            {
                e.Handled = !IsTextAllowed(e.Text, "[^a-zA-Z ]+");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao validar entrada: {ex.Message}");
            }
        }

        private void MarcaProduto_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            try
            {
                HandlePasting(e, "[^a-zA-Z ]+");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao colar texto: {ex.Message}");
            }
        }

        // Verifica se o texto é permitido com base no padrão fornecido
        private static bool IsTextAllowed(string text, string pattern)
        {
            try
            {
                Regex regex = new Regex(pattern);
                return !regex.IsMatch(text);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao validar texto: {ex.Message}");
                return false;
            }
        }

        // Lida com a colagem de texto, verificando se o texto colado é permitido
        private static void HandlePasting(DataObjectPastingEventArgs e, string pattern)
        {
            try
            {
                if (e.DataObject.GetDataPresent(typeof(string)))
                {
                    string text = (string)e.DataObject.GetData(typeof(string));
                    if (!IsTextAllowed(text, pattern))
                    {
                        e.CancelCommand();
                    }
                }
                else
                {
                    e.CancelCommand();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao colar texto: {ex.Message}");
                e.CancelCommand();
            }
        }

        // Evento disparado ao mudar a seleção do tipo de produto
        private void TipoProduto_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (TipoProduto.SelectedItem is ComboBoxItem selectedItem && selectedItem.Content != null)
                {
                    produto.Tipo = selectedItem.Content.ToString() ?? string.Empty; // Garante que não será atribuído nulo
                    isModified = true;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao mudar seleção: {ex.Message}");
            }
        }

        // Evento disparado ao modificar qualquer campo de texto
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                isModified = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao modificar texto: {ex.Message}");
            }
        }

        // Evento disparado ao digitar no ComboBox de pesquisa
        private async void SearchComboBox_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            try
            {
                if (e.Key == Key.Enter)
                {
                    await RealizarBuscaAsync();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao buscar produto: {ex.Message}");
            }
        }

        // Evento disparado ao clicar no botão de busca
        private async void Buscar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await RealizarBuscaAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao buscar produto: {ex.Message}");
            }
        }

        // Método para realizar a busca de produtos
        private async Task RealizarBuscaAsync()
        {
            try
            {
                string searchText = SearchComboBox.Text.ToLower();
                var nomesProdutos = await BuscarProdutosAsync(searchText);
                SearchComboBox.ItemsSource = nomesProdutos;
                SearchComboBox.IsDropDownOpen = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao buscar produtos: {ex.Message}");
            }
        }

        // Evento disparado ao selecionar um item no ComboBox de pesquisa
        private async void SearchComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (SearchComboBox.SelectedItem is string selectedProductName)
                {
                    var produtoSelecionado = await BuscarProdutoPorNomeAsync(selectedProductName);
                    if (produtoSelecionado != null)
                    {
                        produto = produtoSelecionado;
                        PreencherCampos();
                        isModified = false; // Resetar o estado de modificação
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao buscar produto: {ex.Message}");
            }
        }

        // Método para buscar produtos no Firestore
        private async Task<List<string>> BuscarProdutosAsync(string searchText)
        {
            try
            {
                ConectarFirestore(); // Garante que a conexão com o Firestore está estabelecida

                // Busca todos os produtos
                Query query = firestoreDb.Collection("Produtos");
                QuerySnapshot querySnapshot = await query.GetSnapshotAsync();
                List<string> nomesProdutos = new List<string>();

                foreach (DocumentSnapshot documentSnapshot in querySnapshot.Documents)
                {
                    ProdutoData produto = documentSnapshot.ConvertTo<ProdutoData>();
                    if (produto.Nome.ToLower().Contains(searchText.ToLower())) // Verifica se o nome do produto contém o texto de pesquisa
                    {
                        nomesProdutos.Add(produto.Nome);
                    }
                }

                return nomesProdutos;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao buscar produtos: {ex.Message}");
                return new List<string>();
            }
        }

        // Método para buscar um produto pelo nome no Firestore
        private async Task<ProdutoData?> BuscarProdutoPorNomeAsync(string nome)
        {
            try
            {
                ConectarFirestore(); // Garante que a conexão com o Firestore está estabelecida

                // Converte o nome do produto para minúsculas
                string nomeLower = nome.ToLower();

                // Busca todos os produtos e filtra em memória
                Query query = firestoreDb.Collection("Produtos");
                QuerySnapshot querySnapshot = await query.GetSnapshotAsync();

                foreach (DocumentSnapshot documentSnapshot in querySnapshot.Documents)
                {
                    ProdutoData produto = documentSnapshot.ConvertTo<ProdutoData>();
                    if (produto.Nome.ToLower() == nomeLower) // Compara os nomes em minúsculas
                    {
                        produto.Id = documentSnapshot.Id; // Atribui o ID do documento ao produto
                        return produto;
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao buscar produto: {ex.Message}");
                return null;
            }
        }

        // Método para atualizar um produto no Firestore
        private async Task AtualizarProdutoAsync(ProdutoData produto)
        {
            try
            {
                ConectarFirestore(); // Garante que a conexão com o Firestore está estabelecida

                DocumentReference docRef = firestoreDb.Collection("Produtos").Document(produto.Id);
                await docRef.SetAsync(produto, SetOptions.Overwrite);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar produto: {ex.Message}");
            }
        }

        // Valida os campos antes de salvar
        private bool ValidarCampos()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(NomeProduto.Text))
                {
                    MessageBox.Show("O campo Nome do Produto deve ser preenchido.");
                    return false;
                }
                if (string.IsNullOrWhiteSpace(TipoProduto.Text))
                {
                    MessageBox.Show("O campo Tipo do Produto deve ser preenchido.");
                    return false;
                }
                if (string.IsNullOrWhiteSpace(MarcaProduto.Text))
                {
                    MessageBox.Show("O campo Marca do Produto deve ser preenchido.");
                    return false;
                }
                if (string.IsNullOrWhiteSpace(CodigoProduto.Text))
                {
                    MessageBox.Show("O campo Código do Produto deve ser preenchido.");
                    return false;
                }
                if (!int.TryParse(QuantidadeInicial.Text, out _))
                {
                    MessageBox.Show("O campo Quantidade Inicial deve ser um número válido.");
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao validar campos: {ex.Message}");
                return false;
            }
        }
    }
}
