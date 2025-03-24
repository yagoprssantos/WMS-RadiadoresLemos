using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using Google.Cloud.Firestore;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;
using WMS_RadiadoresLemos_WPF.src.Views;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class ControleEstoqueUserControl : UserControl
    {
        private List<ProdutoData> produtos = [];
        private bool produtosCarregados = false;
        private bool precisaAtualizarEstoque = true;

        public ControleEstoqueUserControl()
        {
            InitializeComponent();
            CarregarDadosIniciais();

            CarregarProdutos();
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

        // Método para carregar produtos no ComboBox
        private void CarregarProdutos()
        {
            try
            {
                var movimentacoes = MovimentacoesCache.ObterMovimentacoes();
                produtos = movimentacoes.Select(m => new ProdutoData
                {
                    Nome = m.ProdutoId,
                    Tipo = "", // Inicialize com um valor padrão
                    Marca = "", // Inicialize com um valor padrão
                    Codigo = "", // Inicialize com um valor padrão
                    Preço = 0.0, // Inicialize com um valor padrão
                    Quantidade = 0 // Inicialize com um valor padrão
                }).Distinct().ToList();
                ProdutoComboBox.ItemsSource = produtos.Select(p => p.Nome).Distinct().ToList();
                MarcaComboBox.ItemsSource = produtos.Select(p => p.Marca).Distinct().ToList();
                TipoComboBox.ItemsSource = produtos.Select(p => p.Tipo).Distinct().ToList();
                CodigoComboBox.ItemsSource = produtos.Select(p => p.Codigo).Distinct().ToList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar produtos: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Método para preencher os filtros de marca e tipo de produto
        private void PreencherFiltros()
        {
            try
            {
                var marcas = produtos.Select(p => p.Marca).Distinct().ToList();
                var tipos = produtos.Select(p => p.Tipo).Distinct().ToList();

                MarcaComboBox.ItemsSource = marcas;
                TipoComboBox.ItemsSource = tipos;
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

        // Método chamado ao clicar no botão de cadastrar produto
        private async void CadastrarProduto_Click(object sender, RoutedEventArgs e)
        {
            var cadastrarProdutoWindow = new CadastrarProdutoWindow();
            if (cadastrarProdutoWindow.ShowDialog() == true)
            {
                // Obtém o produto cadastrado
                var produtoCadastrado = cadastrarProdutoWindow.Produto;

                // Atualiza o banco de dados
                await AtualizarProduto(produtoCadastrado);

                // Atualiza a fonte de dados do DataGrid
                EstoqueDataGrid.ItemsSource = null;
                EstoqueDataGrid.ItemsSource = produtos;

                // Avisa o usuário que o produto foi cadastrado
                MessageBox.Show("Produto cadastrado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Cadastro de produto cancelado.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        // Método para atualizar a tabela de estoque com os produtos
        private async Task AtualizarTabelaEstoque()
        {
            try
            {
                var db = DatabaseConnect.Database;

                if (db == null || !DatabaseConnect.IsConnected)
                {
                    // Utiliza o arquivo JSON
                    var caminhoArquivoProdutos = new DatabaseFileManager().ObterCaminhoArquivo("Produtos");

                    if (File.Exists(caminhoArquivoProdutos))
                    {
                        produtos = await DatabaseFileManager.LerDoArquivoAsync<ProdutoData>(caminhoArquivoProdutos);
                    }
                }
                else
                {
                    // Utiliza o banco de dados normalmente
                    var produtosSnapshot = await db.Collection("Produtos").GetSnapshotAsync();
                    produtos = produtosSnapshot.Documents.Select(doc =>
                    {
                        var produto = doc.ConvertTo<ProdutoData>();
                        produto.Id = doc.Id;
                        return produto;
                    }).ToList();
                }

                // Atualiza o cache local e a fonte de dados do DataGrid
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


        // Método chamado ao alterar o texto da caixa de busca
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!produtosCarregados)
            {
                // Garante que produtos estejam sempre carregados
                AtualizarTabelaEstoqueCache();
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

        // Método chamado ao clicar no botão de filtrar
        private void FiltrarButton_Click(object sender, RoutedEventArgs e)
        {
            FiltroPopup.IsOpen = true;
        }

        // Método chamado ao clicar no botão de aplicar filtro
        private void AplicarFiltroButton_Click(object sender, RoutedEventArgs e)
        {
            string produto = ProdutoComboBox.SelectedItem?.ToString();
            string tipo = TipoComboBox.SelectedItem?.ToString();
            string marca = MarcaComboBox.SelectedItem?.ToString();
            string codigo = CodigoComboBox.SelectedItem?.ToString();
            bool emEstoque = EmEstoqueCheckBox.IsChecked == true;

            AplicarFiltro(produto, tipo, marca, codigo, emEstoque);
            FiltroPopup.IsOpen = false;
        }

        // Método para aplicar os filtros na tabela de estoque
        private void AplicarFiltro(string produto, string tipo, string marca, string codigo, bool emEstoque)
        {
            try
            {
                var produtosFiltrados = produtos.Where(p =>
                    (string.IsNullOrEmpty(produto) || p.Nome.Contains(produto, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrEmpty(tipo) || p.Tipo == tipo) &&
                    (string.IsNullOrEmpty(marca) || p.Marca == marca) &&
                    (string.IsNullOrEmpty(codigo) || p.Codigo.Contains(codigo, StringComparison.OrdinalIgnoreCase)) &&
                    (!emEstoque || p.Quantidade > 0)).ToList();

                EstoqueDataGrid.ItemsSource = produtosFiltrados;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao aplicar filtro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Evento para limpar os filtros
        private void LimparFiltroButton_Click(object sender, RoutedEventArgs e)
        {
            ProdutoComboBox.SelectedItem = null;
            TipoComboBox.SelectedItem = null;
            MarcaComboBox.SelectedItem = null;
            CodigoComboBox.SelectedItem = null;
            EmEstoqueCheckBox.IsChecked = false;

            // Recarregar todos os produtos
            CarregarProdutos();
            FiltroPopup.IsOpen = false;
        }


        // Método chamado ao clicar no botão de editar produto
        private async void EditarProduto_Click(object sender, RoutedEventArgs e)
        {
            if (EstoqueDataGrid.SelectedItem is ProdutoData produtoSelecionado)
            {
                EditarProdutoWindow editarProdutoWindow = new(produtoSelecionado);
                if (editarProdutoWindow.ShowDialog() == true)
                {
                    // Obtém o produto editado
                    var produtoEditado = editarProdutoWindow.Produto;

                    // Atualiza o banco de dados
                    await AtualizarProduto(produtoEditado);

                    // Atualiza a fonte de dados do DataGrid
                    EstoqueDataGrid.ItemsSource = null;
                    EstoqueDataGrid.ItemsSource = produtos;

                    // Avisa o usuário que o produto foi editado
                    MessageBox.Show("Produto editado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        // Método chamado ao clicar no botão de alterar quantidade
        private async void AlterarQuantidade_Click(object sender, RoutedEventArgs e)
        {
            if (EstoqueDataGrid.SelectedItem is ProdutoData produtoSelecionado)
            {
                AlterarQuantidadeWindow alterarQuantidadeWindow = new(produtoSelecionado);
                if (alterarQuantidadeWindow.ShowDialog() == true)
                {
                    // Obtém nova quantidade do produto
                    produtoSelecionado.Quantidade = alterarQuantidadeWindow.Quantidade;

                    // Atualiza o produto no banco de dados
                    await AtualizarProduto(produtoSelecionado);

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
                var confirmarSenhaWindow = new ConfirmarSenhaWindow();
                confirmarSenhaWindow.ShowDialog();

                if (confirmarSenhaWindow.IsConfirmed)
                {
                    // Exibe confirmação
                    var result = MessageBox.Show("Tem certeza que deseja deletar este produto?", "Confirmação", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        // Deleta o produto do banco de dados
                        await DeletarProduto(produtoSelecionado);

                        // Atualiza a fonte de dados do DataGrid
                        EstoqueDataGrid.ItemsSource = null;
                        EstoqueDataGrid.ItemsSource = produtos;

                        MessageBox.Show("Produto deletado com sucesso", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Ação cancelada. Senha não confirmada.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("Selecione um produto para deletar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }


        // Método para atualizar um produto
        private static async Task AtualizarProduto(ProdutoData produto)
        {
            var db = DatabaseConnect.Database;

            try
            {
                // Se não estiver conectado ao banco
                if (db == null || !DatabaseConnect.IsConnected)
                {
                    // Ativa modo offline caso não esteja ativo
                    if (MainWindow.isAppOffline == false)
                    {
                        MainWindow._instance?.ativarModoOffline();
                    }
                }
                else
                {
                    // Atualiza o produto no banco de dados Firestore
                    DocumentReference docRef = db.Collection("Produtos").Document(produto.Id);
                    await docRef.SetAsync(produto, SetOptions.Overwrite);
                }

                // Atualiza o cache local
                if (DadosCache.Tabelas.TryGetValue("Produtos", out List<object>? value))
                {
                    var posicao = value.FindIndex(p => ((ProdutoData)p).Id == produto.Id);
                    if (posicao >= 0)
                    {
                        value[posicao] = produto;
                    }
                }

                // Atualiza o produto no arquivo JSON
                var caminhoArquivoProdutos = new DatabaseFileManager().ObterCaminhoArquivo("Produtos");
                var produtos = await DatabaseFileManager.LerDoArquivoAsync<ProdutoData>(caminhoArquivoProdutos);
                var index = produtos.FindIndex(p => p.Id == produto.Id);
                if (index >= 0)
                {
                    produtos[index] = produto;
                    await DatabaseFileManager.SalvarNoArquivoAsync(caminhoArquivoProdutos, produtos);
                }
                else
                {
                    throw new Exception("Produto não encontrado no arquivo JSON.");
                }

                // Adiciona log
                var log = new LogData
                {
                    Data = DateTime.UtcNow,
                    Tipo = "OPERACIONAL",
                    Nivel = "Usuário",
                    Detalhes = $"Produto atualizado: {produto.Nome}, Código: {produto.Codigo}, Quantidade: {produto.Quantidade}, Preço: {produto.Preço}, Tipo: {produto.Tipo}, Marca: {produto.Marca}",
                    Usuario = MainWindow.UsuarioLogado.Nome
                };
                await LogHistorico.RegistrarLogAsync(log);
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

        // Método para deletar um produto
        private async Task DeletarProduto(ProdutoData produto)
        {
            var db = DatabaseConnect.Database;

            try
            {
                // Se não estiver conectado ao banco
                if (db == null || !DatabaseConnect.IsConnected)
                {
                    // Ativa modo offline caso não esteja ativo
                    if (MainWindow.isAppOffline == false)
                    {
                        MainWindow._instance?.ativarModoOffline();
                    }
                }
                else
                {
                    // Deleta o produto do banco de dados Firestore
                    DocumentReference docRef = db.Collection("Produtos").Document(produto.Id);
                    await docRef.DeleteAsync();
                }

                // Atualiza o cache local
                if (DadosCache.Tabelas.TryGetValue("Produtos", out List<object>? value))
                {
                    var produtoParaRemover1 = value.FirstOrDefault(p => ((ProdutoData)p).Id == produto.Id);
                    if (produtoParaRemover1 != null)
                    {
                        value.Remove(produtoParaRemover1);
                    }
                }

                // Atualiza o arquivo JSON
                var caminhoArquivoProdutos = new DatabaseFileManager().ObterCaminhoArquivo("Produtos");
                var produtos = await DatabaseFileManager.LerDoArquivoAsync<ProdutoData>(caminhoArquivoProdutos);
                var produtoParaRemover2 = produtos.FirstOrDefault(p => p.Id == produto.Id);
                if (produtoParaRemover2 != null)
                {
                    produtos.Remove(produtoParaRemover2);
                    await DatabaseFileManager.SalvarNoArquivoAsync(caminhoArquivoProdutos, produtos);
                }

                // Adiciona log
                var log = new LogData
                {
                    Data = DateTime.UtcNow,
                    Tipo = "OPERACIONAL",
                    Nivel = "Usuário",
                    Detalhes = $"Produto deletado: {produto.Nome}, Código: {produto.Codigo}, Quantidade: {produto.Quantidade}, Preço: {produto.Preço}, Tipo: {produto.Tipo}, Marca: {produto.Marca}",
                    Usuario = MainWindow.UsuarioLogado.Nome
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
            // Exatamente igual ao método EditarProduto_Click
            if (EstoqueDataGrid.SelectedItem is ProdutoData produtoSelecionado)
            {
                EditarProdutoWindow editarProdutoWindow = new(produtoSelecionado);
                if (editarProdutoWindow.ShowDialog() == true)
                {
                    var produtoEditado = editarProdutoWindow.Produto;
                    await AtualizarProduto(produtoEditado);
                    EstoqueDataGrid.ItemsSource = null;
                    EstoqueDataGrid.ItemsSource = produtos;
                    MessageBox.Show("Produto editado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }



        // Tratamento de entradas

        // Quantidade
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

        // Marca
        // Método para validar a entrada de texto no TextBox de marca do produto
        private void MarcaProduto_PreviewTextInput(object sender, TextCompositionEventArgs e) =>
            e.Handled = !IsTextAllowed(e.Text, "[^a-zA-Z ]+");

        // Método para validar a colagem de texto no TextBox de marca do produto
        private void MarcaProduto_Pasting(object sender, DataObjectPastingEventArgs e) =>
            HandlePasting(e, "[^a-zA-Z ]+");

        // Preço
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
                if (double.TryParse(textBox.Text.Trim().Replace(".", "").Replace(",", "."), out double preco))
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
    }
}