using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Google.Cloud.Firestore;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class ControleEstoqueUserControl : UserControl
    {
        // Lista para armazenar os produtos carregados do banco de dados
        private List<ProdutoData> produtos = [];
        // Flag para verificar se os produtos já foram carregados
        private bool produtosCarregados = false;
        // Flag para verificar se a tabela de estoque precisa ser atualizada
        private bool precisaAtualizarEstoque = true;

        public ControleEstoqueUserControl()
        {
            InitializeComponent();
            CarregarDadosIniciais();
            PreencherFiltros();
        }

        // Método para carregar os dados iniciais
        private void CarregarDadosIniciais()
        {
            if (DadosCache.Tabelas.TryGetValue("Produtos", out List<object>? value))
            {
                produtos = value.Cast<ProdutoData>().ToList();
                EstoqueDataGrid.ItemsSource = produtos;
            }
        }

        // Método para preencher os filtros de marca e tipo de produto
        private void PreencherFiltros()
        {
            try
            {
                var marcas = produtos.Select(p => p.Marca).Distinct().ToList();
                var tipos = produtos.Select(p => p.Tipo).Distinct().ToList();

                // Adiciona uma opção vazia no início das listas
                marcas.Insert(0, string.Empty);
                tipos.Insert(0, string.Empty);

                if (marcas != null && marcas.Any())
                {
                    MarcaProdutoComboBox.ItemsSource = marcas;
                }

                if (tipos != null && tipos.Any())
                {
                    TipoProdutoComboBox.ItemsSource = tipos;
                }
            }
            catch (InvalidOperationException ex)
            {
                MessageBox.Show($"Erro ao preencher filtros: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                // Adiciona alerta
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Erro ao preencher filtros de marca e tipo de produto no Controle de Estoque. Possíveis Motivos\n: " +
                                            "- Não foi possível carregar os produtos;\n" +
                                            "- Filtro de marca ou tipo não encontrado.",
                                            "- Verifique se os produtos foram carregados corretamente;\n" +
                                            "- Verifique se os filtros de marca e tipo existem;\n" +
                                            "- Tente atualizar a tabela de estoque novamente.");
            }
        }


        // Método para atualizar a tabela de estoque com os produtos do cache
        private void AtualizarTabelaEstoqueCache()
        {
            if (DadosCache.Tabelas.TryGetValue("Produtos", out List<object>? value))
            {
                produtos = value.Cast<ProdutoData>().ToList();
                EstoqueDataGrid.ItemsSource = produtos;
                produtosCarregados = true;
                precisaAtualizarEstoque = false;
            }
            else
            {
                precisaAtualizarEstoque = true;
            }
        }

        // Método para atualizar a tabela de estoque com os produtos do banco de dados
        private async Task AtualizarTabelaEstoqueBanco()
        {
            try
            {
                var db = DatabaseConnect.Database ?? throw new InvalidOperationException("Conexão com o banco de dados não estabelecida.");
                var produtosSnapshot = await db.Collection("Produtos").GetSnapshotAsync();
                produtos = produtosSnapshot.Documents.Select(doc =>
                {
                    var produto = doc.ConvertTo<ProdutoData>();
                    produto.Id = doc.Id;
                    return produto;
                }).ToList();

                DadosCache.Tabelas["Produtos"] = produtos.Cast<object>().ToList();
                EstoqueDataGrid.ItemsSource = produtos;
                produtosCarregados = true;
                precisaAtualizarEstoque = false;
            }
            catch (Exception ex)
            {
                precisaAtualizarEstoque = true;
                MessageBox.Show($"Erro ao carregar produtos do banco de dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                // Adiciona alerta
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Erro ao carregar produtos do banco de dados no Controle de Estoque. Possíveis Motivos:\n " +
                                            "- Falha na conexão com o banco de dados;\n" +
                                            "- Falha ao carregar os produtos do banco de dados.",
                                            "- Verifique a conexão com o banco de dados;\n" +
                                            "- Verifique se os produtos foram carregados corretamente;\n" +
                                            "- Tente atualizar a tabela de estoque novamente.");
            }
        }

        // Método chamado quando um TextBox ganha foco
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && IsPlaceholderText(textBox.Text))
            {
                textBox.Text = string.Empty;
            }
        }

        // Método chamado quando um TextBox perde foco
        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && string.IsNullOrEmpty(textBox.Text))
            {
                textBox.Text = GetPlaceholderText(textBox.Name);
            }
        }

        // Verifica se o texto é um texto de placeholder
        private static bool IsPlaceholderText(string text) =>
            text is "Nome do Produto" or "Tipo do Produto" or "Marca do Produto" or "Preço do Produto" or "Quantidade";

        // Retorna o texto de placeholder baseado no nome do TextBox
        private static string GetPlaceholderText(string textBoxName) => textBoxName switch
        {
            "NomeProduto" => "Nome do Produto",
            "TipoProduto" => "Tipo do Produto",
            "MarcaProduto" => "Marca do Produto",
            "PrecoProduto" => "Preço do Produto",
            "QuantidadeInicial" => "Quantidade",
            _ => string.Empty
        };

        // Método para validar a entrada de texto no TextBox de quantidade inicial
        private void QuantidadeInicial_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
            e.Handled = !IsTextAllowed(e.Text, "[^0-9]+");

        // Método para validar a colagem de texto no TextBox de quantidade inicial
        private void QuantidadeInicial_Pasting(object sender, DataObjectPastingEventArgs e) =>
            HandlePasting(e, "[^0-9]+");

        // Método para formatar o texto da caixa de quantidade ao perder o foco (1.000)
        private void QuantidadeTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
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

        // Método para validar a entrada de texto no TextBox de marca do produto
        private void MarcaProduto_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
            e.Handled = !IsTextAllowed(e.Text, "[^a-zA-Z ]+");

        // Método para validar a colagem de texto no TextBox de marca do produto
        private void MarcaProduto_Pasting(object sender, DataObjectPastingEventArgs e) =>
            HandlePasting(e, "[^a-zA-Z ]+");

        // Método para validar a entrada de texto no TextBox de preço do produto (incluindo decimais e uma única vírgula)
        private void PrecoProduto_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
            e.Handled = !IsTextAllowed(e.Text, "[^0-9]+");

        // Método para validar a colagem de texto no TextBox de preço do produto (incluindo decimais e uma única vírgula)
        private void PrecoProduto_Pasting(object sender, DataObjectPastingEventArgs e) =>
            HandlePasting(e, "[^0-9]+");

        // Método para formatar o texto da caixa de preço ao perder o foco (1.000,00)
        private void PrecoTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox)
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

        // Verifica se o texto é permitido baseado no padrão regex
        private static bool IsTextAllowed(string text, string pattern) =>
            !new Regex(pattern).IsMatch(text);

        // Método para lidar com a colagem de texto e validar se é permitido
        private static void HandlePasting(DataObjectPastingEventArgs e, string pattern)
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

        // Método para obter os dados do produto a partir dos TextBoxes
        private ProdutoData DadosDoProduto() => new()
        {
            Nome = NomeProduto.Text.Trim(),
            Tipo = TipoProduto.Text.Trim(),
            Marca = MarcaProduto.Text.Trim(),
            Codigo = CodigoProduto.Text.Trim(),

            // Remove a formatação do preço (1.000,00 -> 1000.00)
            Preco = double.Parse(PrecoProduto.Text.Trim().Replace(".", "").Replace(",", ".")),

            // Remove a formatação da quantidade (1.000 -> 1000)
            Quantidade = int.Parse(QuantidadeInicial.Text.Trim().Replace(".", ""))
        };

        // Método para cadastrar um novo produto no banco de dados
        private async void CadastrarProdutoNoBanco()
        {
            if (DatabaseConnect.Database == null)
            {
                MessageBox.Show("Conexão com o banco de dados não estabelecida.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                // Adiciona alerta
                AlertaCache.AdicionarAlerta("Erro",
                                            "Conexão com o banco de dados não estabelecida.",
                                            "Erro ao cadastrar produto no banco de dados no Controle de Estoque. Possíveis Motivos\n: " +
                                            "- Falha na conexão com o banco de dados.",
                                            "- Verifique a conexão com o banco de dados.");

                return;
            }

            var db = DatabaseConnect.Database;
            var data = DadosDoProduto();
            var docRef = db.Collection("Produtos").Document(data.Codigo);
            await docRef.SetAsync(data);

            // Atualiza o cache local
            if (!DadosCache.Tabelas.TryGetValue("Produtos", out List<object>? value))
            {
                value = [];
                DadosCache.Tabelas["Produtos"] = value;
            }

            value.Add(data);
            produtos.Add(data);
            EstoqueDataGrid.ItemsSource = null;
            EstoqueDataGrid.ItemsSource = produtos;

            // Adiciona log
            var log = new LogData
            {
                Data = DateTime.UtcNow,
                Tipo = "OPERACIONAL",
                Nivel = "Usuário",
                Detalhes = $"Produto cadastrado: {data.Nome}, Código: {data.Codigo}",
                Usuario = "NomeDoUsuario" // Substitua pelo nome do usuário real
            };
            await LogHistorico.RegistrarLogAsync(log);
        }

        // Método chamado ao clicar no botão de cadastrar produto
        private async void CadastrarProduto_Click(object sender, RoutedEventArgs e)
        {
            if (CamposPreenchidos())
            {
                if (!precisaAtualizarEstoque)
                {
                    // Se a tabela de estoque não precisa ser atualizada, cadastra o produto
                    CadastrarProdutoNoBanco();
                    MessageBox.Show("Produto cadastrado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    LimparCamposCadastro();
                }
                else
                {
                    // Se a tabela de estoque precisa ser atualizada, atualiza a tabela e cadastra o produto
                    await AtualizarTabelaEstoqueBanco();

                    CadastrarProdutoNoBanco();
                    MessageBox.Show("Produto cadastrado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    LimparCamposCadastro();
                }
            }
            else
            {
                MessageBox.Show("Preencha todos os campos para cadastrar o produto.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Verifica se todos os campos necessários estão preenchidos
        private bool CamposPreenchidos() =>
            !string.IsNullOrEmpty(NomeProduto.Text) &&
            !string.IsNullOrEmpty(TipoProduto.Text) &&
            !string.IsNullOrEmpty(MarcaProduto.Text) &&
            !string.IsNullOrEmpty(CodigoProduto.Text) &&
            !string.IsNullOrEmpty(PrecoProduto.Text) &&
            !string.IsNullOrEmpty(QuantidadeInicial.Text);

        // Método para limpar os campos de cadastro
        private void LimparCamposCadastro()
        {
            NomeProduto.Text = string.Empty;
            TipoProduto.Text = string.Empty;
            MarcaProduto.Text = string.Empty;
            CodigoProduto.Text = string.Empty;
            PrecoProduto.Text = string.Empty;
            QuantidadeInicial.Text = string.Empty;
        }

        // Método chamado ao carregar a aba de estoque
        private void AbaEstoque_Loaded(object sender, RoutedEventArgs e)
        {
            AtualizarTabelaEstoqueCache();
        }

        // Método chamado ao alterar o texto da caixa de busca
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!produtosCarregados)
            {
                AtualizarTabelaEstoqueCache();
            }

            // Aplica filtros
            AplicarFiltros();
        }

        // Método chamado ao clicar no botão de filtrar
        private void AbrirFiltroPopup_Click(object sender, RoutedEventArgs e)
        {
            // Verifica se o PainelFiltrosPopup está aberto
            if (PainelFiltrosPopup.IsOpen)
            {
                // Se estiver aberto, fecha o popup
                PainelFiltrosPopup.IsOpen = false;
            }
            else
            {
                // Se estiver fechado, abre o popup
                PainelFiltrosPopup.IsOpen = true;
            }
        }

        // Método chamado ao clicar no botão de editar produto
        private async void EditarProduto_Click(object sender, RoutedEventArgs e)
        {
            if (EstoqueDataGrid.SelectedItem is ProdutoData produtoSelecionado)
            {
                EditarProdutoWindow editarProdutoWindow = new(produtoSelecionado);
                if (editarProdutoWindow.ShowDialog() == true)
                {
                    // Atualiza o produto na lista local
                    var produtoEditado = editarProdutoWindow.Produto;
                    var index = produtos.FindIndex(p => p.Id == produtoEditado.Id);
                    if (index >= 0)
                    {
                        produtos[index] = produtoEditado;
                    }

                    // Atualiza o cache local
                    DadosCache.Tabelas["Produtos"] = produtos.Cast<object>().ToList();

                    // Atualiza o banco de dados
                    await AtualizarProdutoNoBanco(produtoEditado);

                    // Adiciona log
                    var log = new LogData
                    {
                        Data = DateTime.UtcNow,
                        Tipo = "OPERACIONAL",
                        Nivel = "Usuário",
                        Detalhes = $"Produto editado: {produtoEditado.Nome}, Código: {produtoEditado.Codigo}",
                        Usuario = "NomeDoUsuario" // Substitua pelo nome do usuário real
                    };
                    await LogHistorico.RegistrarLogAsync(log);

                    // Atualiza a fonte de dados do DataGrid
                    EstoqueDataGrid.ItemsSource = null;
                    EstoqueDataGrid.ItemsSource = produtos;

                    // Avisa o usuário que o produto foi editado
                    MessageBox.Show("Produto editado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        // Método para atualizar um produto no banco de dados
        private static async Task AtualizarProdutoNoBanco(ProdutoData produto)
        {
            try
            {
                var db = DatabaseConnect.Database ?? throw new InvalidOperationException("Conexão com o banco de dados não estabelecida.");

                DocumentReference docRef = db.Collection("Produtos").Document(produto.Id);
                await docRef.SetAsync(produto, SetOptions.Overwrite);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar produto no banco de dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                // Adiciona alerta
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Erro ao atualizar produto no banco de dados no Controle de Estoque. Possíveis Motivos\n: " +
                                            "- Falha na conexão com o banco de dados;\n" +
                                            "- Falha ao atualizar o produto no banco de dados.",
                                            "- Verifique a conexão com o banco de dados;\n" +
                                            "- Verifique se o produto foi atualizado corretamente.");
            }
        }

        // Método chamado ao clicar no botão de filtros
        private void Filtro_Changed(object sender, RoutedEventArgs e)
        {
            AplicarFiltros();
        }

        // Método para aplicar os filtros na tabela de estoque
        private void AplicarFiltros()
        {
            var view = CollectionViewSource.GetDefaultView(produtos);
            if (view != null)
            {
                view.Filter = item =>
                {
                    var produto = item as ProdutoData;
                    if (produto == null) return false;

                    bool emEstoque = EmEstoqueCheckBox.IsChecked == true ? produto.Quantidade > 0 : true;
                    bool marcaCorreta = MarcaProdutoComboBox.SelectedItem == null || MarcaProdutoComboBox.SelectedItem.ToString() == string.Empty || produto.Marca == MarcaProdutoComboBox.SelectedItem.ToString();
                    bool tipoCorreto = TipoProdutoComboBox.SelectedItem == null || TipoProdutoComboBox.SelectedItem.ToString() == string.Empty || produto.Tipo == TipoProdutoComboBox.SelectedItem.ToString();

                    // Faz a pesquisa por texto em todos os campos do produto com o filtro (se houver)
                    string searchText = SearchBox.Text.ToLower();
                    bool pesquisaCorreta = string.IsNullOrEmpty(searchText) ||
                                           produto.Nome.ToLower().Contains(searchText) ||
                                           produto.Tipo.ToLower().Contains(searchText) ||
                                           produto.Marca.ToLower().Contains(searchText) ||
                                           produto.Codigo.ToLower().Contains(searchText);

                    return emEstoque && marcaCorreta && tipoCorreto && pesquisaCorreta;
                };
                view.Refresh();
            }
        }

        // Método chamado ao clicar no botão de atualizar tabela de estoque
        private async void AtualizarDataGrid_Click(object sender, RoutedEventArgs e)
        {
            await AtualizarTabelaEstoqueBanco();
            LimparCamposCadastro();
            MessageBox.Show("Tabela de estoque atualizada.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Método chamado ao clicar no botão de alterar quantidade
        private async void AlterarQuantidade_Click(object sender, RoutedEventArgs e)
        {
            if (EstoqueDataGrid.SelectedItem is ProdutoData produtoSelecionado)
            {
                AlterarQuantidadeWindow alterarQuantidadeWindow = new(produtoSelecionado);
                if (alterarQuantidadeWindow.ShowDialog() == true)
                {
                    produtoSelecionado.Quantidade = alterarQuantidadeWindow.Quantidade;
                    await AtualizarProdutoNoBanco(produtoSelecionado);

                    // Atualiza o cache local
                    DadosCache.Tabelas["Produtos"] = produtos.Cast<object>().ToList();

                    // Adiciona log
                    var log = new LogData
                    {
                        Data = DateTime.UtcNow,
                        Tipo = "OPERACIONAL",
                        Nivel = "Usuário",
                        Detalhes = $"Quantidade alterada do produto '{produtoSelecionado.Nome}', Código: {produtoSelecionado.Codigo}; Nova Quantidade: {produtoSelecionado.Quantidade}",
                        Usuario = "NomeDoUsuario" // Substitua pelo nome do usuário real
                    };
                    await LogHistorico.RegistrarLogAsync(log);

                    // Atualiza a fonte de dados do DataGrid
                    EstoqueDataGrid.ItemsSource = null;
                    EstoqueDataGrid.ItemsSource = produtos;

                    // Avisa o usuário que a quantidade foi alterada
                    MessageBox.Show("Quantidade alterada com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        // Método chamado ao clicar no botão de deletar produto
        private async void DeletarProduto_Click(object sender, RoutedEventArgs e)
        {
            if (EstoqueDataGrid.SelectedItem is ProdutoData produtoSelecionado)
            {
                // Exibe confirmação
                var result = MessageBox.Show("Tem certeza que deseja deletar este produto?", "Confirmação", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    // Atualiza a lista e Cache local
                    produtos.Remove(produtoSelecionado);
                    DadosCache.Tabelas["Produtos"] = produtos.Cast<object>().ToList();

                    // Deleta o produto do banco de dados
                    await DeletarProdutoNoBanco(produtoSelecionado);

                    // Adiciona log
                    var log = new LogData
                    {
                        Data = DateTime.UtcNow,
                        Tipo = "CRÍTICO",
                        Nivel = "Usuário",
                        Detalhes = $"Produto deletado: {produtoSelecionado.Nome}, Código: {produtoSelecionado.Codigo}",
                        Usuario = "NomeDoUsuario" // Substitua pelo nome do usuário real
                    };
                    await LogHistorico.RegistrarLogAsync(log);

                    // Atualiza a fonte de dados do DataGrid
                    EstoqueDataGrid.ItemsSource = null;
                    EstoqueDataGrid.ItemsSource = produtos;

                    MessageBox.Show("Produto deletado com sucesso", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Selecione um produto para deletar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Método para deletar um produto no banco de dados
        private async Task DeletarProdutoNoBanco(ProdutoData produto)
        {
            try
            {
                var db = DatabaseConnect.Database ?? throw new InvalidOperationException("Conexão com o banco de dados não estabelecida.");
                DocumentReference docRef = db.Collection("Produtos").Document(produto.Id);
                await docRef.DeleteAsync();

                // Adiciona log
                var log = new LogData
                {
                    Data = DateTime.UtcNow,
                    Tipo = "OPERACIONAL",
                    Nivel = "Usuário",
                    Detalhes = $"Produto deletado: {produto.Nome}, Código: {produto.Codigo}",
                    Usuario = "NomeDoUsuario" // Substitua pelo nome do usuário real
                };
                await LogHistorico.RegistrarLogAsync(log);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao deletar produto no banco de dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                // Adiciona alerta
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Erro ao deletar produto no banco de dados no Controle de Estoque. Possíveis Motivos\n: " +
                                            "- Falha na conexão com o banco de dados;\n" +
                                            "- Falha ao deletar o produto no banco de dados.",
                                            "- Verifique a conexão com o banco de dados;\n" +
                                            "- Verifique se o produto foi deletado corretamente.");
            }
        }

        // Método para abrir edição de produto ao dar duplo clique na linha da tabela
        private async void EstoqueDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (EstoqueDataGrid.SelectedItem is ProdutoData produtoSelecionado)
            {
                EditarProdutoWindow editarProdutoWindow = new(produtoSelecionado);
                if (editarProdutoWindow.ShowDialog() == true)
                {
                    // Atualiza o produto na lista local
                    var produtoEditado = editarProdutoWindow.Produto;
                    var index = produtos.FindIndex(p => p.Id == produtoEditado.Id);
                    if (index >= 0)
                    {
                        produtos[index] = produtoEditado;
                    }

                    // Atualiza o cache local
                    DadosCache.Tabelas["Produtos"] = produtos.Cast<object>().ToList();

                    // Atualiza o banco de dados
                    await AtualizarProdutoNoBanco(produtoEditado);

                    // Adiciona log
                    var log = new LogData
                    {
                        Data = DateTime.UtcNow,
                        Tipo = "OPERACIONAL",
                        Nivel = "Usuário",
                        Detalhes = $"Produto editado: {produtoEditado.Nome}, Código: {produtoEditado.Codigo}",
                        Usuario = "NomeDoUsuario" // Substitua pelo nome do usuário real
                    };
                    await LogHistorico.RegistrarLogAsync(log);

                    // Atualiza a fonte de dados do DataGrid
                    EstoqueDataGrid.ItemsSource = null;
                    EstoqueDataGrid.ItemsSource = produtos;

                    // Avisa o usuário que o produto foi editado
                    MessageBox.Show("Produto editado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
    }
}