using System.Windows.Controls;
using System.Windows;
using LiteDB;
using WMS_RadiadoresLemos_WPF.src.Services;
using WMS_RadiadoresLemos_WPF.src.Models;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class CadastroUserControl : UserControl
    {
        private LiteDatabase _database;
        private string _tabelaAtual = string.Empty;
        private string _tituloAtual = string.Empty;

        public CadastroUserControl()
        {
            InitializeComponent();
            _database = DatabaseConnect.Database ?? throw new InvalidOperationException("Database não pode ser nulo."); // Evita CS8601 e CS8618  
            CarregarTabelas();
        }

        private void CarregarTabelas()
        {
            // Adiciona as tabelas disponíveis ao ComboBox (Produtos, Clientes, Fornecedores e Usuários)
            TabelasComboBox.Items.Add("Produtos");
            TabelasComboBox.Items.Add("Clientes");
            TabelasComboBox.Items.Add("Fornecedores");
            TabelasComboBox.Items.Add("Usuários");
        }

        private void TabelasComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabelasComboBox.SelectedItem != null)
            {
                _tabelaAtual = TabelasComboBox.SelectedItem?.ToString() ?? string.Empty;

                // Carrega os dados da tabela selecionada
                CarregarDadosTabela(_tabelaAtual);

                // Altera o título
                AtualizarTitulo(_tabelaAtual);
            }
        }

        private void CarregarDadosTabela(string tabela)
        {
            if (string.IsNullOrEmpty(tabela))
            {
                MessageBox.Show("Tabela inválida.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            switch (tabela.ToLower())
            {
                case "produtos":
                    CarregarDadosGenerico<ProdutoData>("produtos");
                    break;

                case "clientes":
                    // TODO: Implementar carregamento de dados de clientes

                    // CarregarDadosGenerico<ClienteData>("clientes");

                    MessageBox.Show("Funcionalidade de clientes não implementada.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);

                    break;

                case "fornecedores":
                    // TODO: Implementar carregamento de dados de fornecedores
                    // CarregarDadosGenerico<FornecedorData>("fornecedores");

                    MessageBox.Show("Funcionalidade de fornecedores não implementada.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);

                    break;

                case "usuários":
                    CarregarDadosGenerico<UsuarioData>("usuarios");
                    break;

                default:
                    MessageBox.Show("Tabela desconhecida. Não foi possível carregar os dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }
        }

        private void CarregarDadosGenerico<T>(string tabela) where T : class
        {
            // Limpa o DataGrid antes de carregar novos dados
            CadastroDataGrid.ItemsSource = null;
            CadastroDataGrid.Columns.Clear();
            CadastroDataGrid.Items.Clear();

            var collection = _database.GetCollection<T>(tabela);
            var dados = collection.FindAll().ToList();
            CadastroDataGrid.ItemsSource = dados;
            ConfigurarColunasGenerico(typeof(T));
        }

        private void ConfigurarColunasGenerico(Type tipoModelo)
        {
            CadastroDataGrid.Columns.Clear();

            foreach (var propriedade in tipoModelo.GetProperties())
            {
                CadastroDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = propriedade.Name,
                    Binding = new System.Windows.Data.Binding(propriedade.Name)
                });
            }
        }

        private void AtualizarTitulo(string tabela)
        {
            _tituloAtual = tabela switch
            {
                "Produtos" => "Cadastro de Produtos",
                "Clientes" => "Cadastro de Clientes",
                "Fornecedores" => "Cadastro de Fornecedores",
                "Usuários" => "Cadastro de Usuários",
                _ => "Selecione uma tabela"
            };
            Titulo.Text = _tituloAtual;
        }

        private void FiltrarButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_tabelaAtual))
            {
                MessageBox.Show("Selecione uma tabela antes de aplicar o filtro.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Exibe o popup de filtro correspondente à tabela selecionada
            switch (_tabelaAtual.ToLower())
            {
                case "produtos":
                    ProdutosPopup.IsOpen = true;
                    PreencherFiltrosProdutos();
                    break;

                case "clientes":
                    // TODO: Implementar funcionalidade de filtro para clientes
                    ClientesPopup.IsOpen = true;
                    // PreencherFiltrosClientes();

                    MessageBox.Show("Funcionalidade de filtro para clientes não implementada.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;

                case "fornecedores":
                    // TODO: Implementar funcionalidade de filtro para fornecedores
                    FornecedoresPopup.IsOpen = true;
                    // PreencherFiltrosFornecedores();

                    MessageBox.Show("Funcionalidade de filtro para fornecedores não implementada.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
                    break;

                case "usuários":
                    UsuariosPopup.IsOpen = true;
                    PreencherFiltrosUsuarios();
                    break;

                default:
                    MessageBox.Show("Tabela desconhecida. Não foi possível aplicar o filtro.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }
        }

        private void PreencherFiltrosProdutos()
        {
            try
            {
                var collection = _database.GetCollection<ProdutoData>("produtos");
                var produtos = collection.FindAll().ToList();

                var marcas = produtos.Select(p => p.Marca).Where(m => !string.IsNullOrEmpty(m)).Distinct().ToList();
                var tipos = produtos.Select(p => p.Tipo).Where(t => !string.IsNullOrEmpty(t)).Distinct().ToList();
                var codigos = produtos.Select(p => p.Codigo).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
                var nomes = produtos.Select(p => p.Nome).Where(n => !string.IsNullOrEmpty(n)).Distinct().ToList();

                MarcaComboBox.ItemsSource = marcas;
                TipoComboBox.ItemsSource = tipos;
                CodigoComboBox.ItemsSource = codigos;
                ProdutoComboBox.ItemsSource = nomes;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao preencher filtros de produtos: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PreencherFiltrosUsuarios()
        {
            try
            {
                var collection = _database.GetCollection<UsuarioData>("usuarios");
                var usuarios = collection.FindAll().ToList();

                var cargos = usuarios.Select(u => u.Cargo).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
                CargoComboBox.ItemsSource = cargos;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao preencher filtros de usuários: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AplicarFiltroButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_tabelaAtual))
            {
                MessageBox.Show("Selecione uma tabela antes de aplicar o filtro.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            switch (_tabelaAtual.ToLower())
            {
                case "produtos":
                    AplicarFiltroProdutos();
                    ProdutosPopup.IsOpen = false;
                    break;

                case "clientes":
                    // TODO: Implementar funcionalidade de filtro para clientes
                    // AplicarFiltroClientes();
                    ClientesPopup.IsOpen = false;
                    break;

                case "fornecedores":
                    // TODO: Implementar funcionalidade de filtro para fornecedores
                    // AplicarFiltroFornecedores();
                    FornecedoresPopup.IsOpen = false;
                    break;

                case "usuários":
                    AplicarFiltroUsuarios();
                    UsuariosPopup.IsOpen = false;
                    break;

                default:
                    MessageBox.Show("Tabela desconhecida. Não foi possível aplicar o filtro.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }
        }

        private void AplicarFiltroProdutos()
        {
            try
            {
                var produto = ProdutoComboBox.SelectedItem?.ToString();
                var tipo = TipoComboBox.SelectedItem?.ToString();
                var marca = MarcaComboBox.SelectedItem?.ToString();
                var codigo = CodigoComboBox.SelectedItem?.ToString();
                var emEstoque = EmEstoqueCheckBox.IsChecked == true;

                var collection = _database.GetCollection<ProdutoData>("produtos");
                var produtos = collection.FindAll().ToList();

                var produtosFiltrados = produtos.Where(p =>
                    (string.IsNullOrEmpty(produto) || p.Nome.Contains(produto, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrEmpty(tipo) || p.Tipo == tipo) &&
                    (string.IsNullOrEmpty(marca) || p.Marca == marca) &&
                    (string.IsNullOrEmpty(codigo) || p.Codigo.Contains(codigo, StringComparison.OrdinalIgnoreCase)) &&
                    (!emEstoque || p.Quantidade > 0)).ToList();

                CadastroDataGrid.ItemsSource = produtosFiltrados;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao aplicar filtro de produtos: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AplicarFiltroUsuarios()
        {
            try
            {
                var cargo = CargoComboBox.SelectedItem?.ToString();

                var collection = _database.GetCollection<UsuarioData>("usuarios");
                var usuarios = collection.FindAll().ToList();

                var usuariosFiltrados = usuarios.Where(u =>
                    string.IsNullOrEmpty(cargo) || u.Cargo.Equals(cargo, StringComparison.OrdinalIgnoreCase)).ToList();

                CadastroDataGrid.ItemsSource = usuariosFiltrados;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao aplicar filtro de usuários: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LimparFiltroButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_tabelaAtual))
            {
                MessageBox.Show("Selecione uma tabela antes de limpar o filtro.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            switch (_tabelaAtual.ToLower())
            {
                case "produtos":
                    ProdutoComboBox.SelectedItem = null;
                    TipoComboBox.SelectedItem = null;
                    MarcaComboBox.SelectedItem = null;
                    CodigoComboBox.SelectedItem = null;
                    EmEstoqueCheckBox.IsChecked = false;
                    CarregarDadosTabela(_tabelaAtual);
                    ProdutosPopup.IsOpen = false;
                    break;

                case "clientes":
                    // TODO: Implementar funcionalidade de limpar filtro para clientes
                    ClienteComboBox.SelectedItem = null;
                    EstadoComboBox.SelectedItem = null;

                    CarregarDadosTabela(_tabelaAtual);
                    ClientesPopup.IsOpen = false;
                    break;

                case "fornecedores":
                    // TODO: Implementar funcionalidade de limpar filtro para fornecedores
                    FornecedorComboBox.SelectedItem = null;
                    EstadoComboBox.SelectedItem = null;

                    CarregarDadosTabela(_tabelaAtual);
                    FornecedoresPopup.IsOpen = false;
                    break;

                case "usuários":
                    CargoComboBox.SelectedItem = null;
                    CarregarDadosTabela(_tabelaAtual);
                    UsuariosPopup.IsOpen = false;
                    break;

                default:
                    MessageBox.Show("Tabela desconhecida. Não foi possível limpar o filtro.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }
        }


        private void CadastrarButton_Click(object sender, RoutedEventArgs e)
        {
            // Verifica qual tabela está selecionada
            if (string.IsNullOrEmpty(_tabelaAtual))
            {
                MessageBox.Show("Selecione uma tabela antes de cadastrar.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            switch (_tabelaAtual.ToLower())
            {
                case "produtos":
                    // Abre a janela de cadastro de produtos
                    var cadastroProduto = new EditarProdutoWindow(null);
                    if (cadastroProduto.ShowDialog() == true)
                    {
                        // Se o cadastro foi bem-sucedido, atualiza o DataGrid
                        CarregarDadosTabela(_tabelaAtual);
                    }
                    break;

                case "clientes":
                    // TODO: Implementar janela de cadastro de clientes

                    // var cadastroCliente = new EditarClienteWindow(null);
                    // if (cadastroCliente.ShowDialog() == true)
                    // {
                    //     // Se o cadastro foi bem-sucedido, atualiza o DataGrid
                    //     CarregarDadosTabela(_tabelaAtual);
                    // }

                    MessageBox.Show("Funcionalidade de cadastro de clientes não implementada.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);

                    break;

                case "fornecedores":
                    // TODO: Implementar janela de cadastro de fornecedores

                    // var cadastroFornecedor = new EditarFornecedorWindow(null);
                    // if (cadastroFornecedor.ShowDialog() == true)
                    // {
                    //     // Se o cadastro foi bem-sucedido, atualiza o DataGrid
                    //     CarregarDadosTabela(_tabelaAtual);
                    // }

                    MessageBox.Show("Funcionalidade de cadastro de fornecedores não implementada.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);

                    break;


                case "usuários":
                    // Abre a janela de cadastro de usuários
                    var cadastroUsuario = new EditarUsuarioWindow(null);
                    if (cadastroUsuario.ShowDialog() == true)
                    {
                        // Se o cadastro foi bem-sucedido, atualiza o DataGrid
                        CarregarDadosTabela(_tabelaAtual);
                    }
                    break;

                default:
                    MessageBox.Show("Tabela desconhecida. Não foi possível cadastrar.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }

        }


        private void EditarButton_Click(object sender, RoutedEventArgs e)
        {
            // Verifica se há um item selecionado no DataGrid
            if (CadastroDataGrid.SelectedItem is null)
            {
                MessageBox.Show("Por favor, selecione um registro para editar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Verifica qual tabela está selecionada
            if (string.IsNullOrEmpty(_tabelaAtual))
            {
                MessageBox.Show("Selecione uma tabela antes de editar.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            switch (_tabelaAtual.ToLower())
            {
                case "produtos":
                    if (CadastroDataGrid.SelectedItem is ProdutoData produtoSelecionado)
                    {
                        var editarProduto = new EditarProdutoWindow(produtoSelecionado);
                        if (editarProduto.ShowDialog() == true)
                        {
                            // Se a edição foi bem-sucedida, atualiza o DataGrid
                            CarregarDadosTabela(_tabelaAtual);
                        }
                    }
                    break;

                case "clientes":
                    // TODO: Implementar janela de edição de clientes

                    // if (CadastroDataGrid.SelectedItem is ClienteData clienteSelecionado)
                    // {
                    //     var editarCliente = new EditarClienteWindow(clienteSelecionado);
                    //     if (editarCliente.ShowDialog() == true)
                    //     {
                    //         // Se a edição foi bem-sucedida, atualiza o DataGrid
                    //         CarregarDadosTabela(_tabelaAtual);
                    //     }
                    // }

                    MessageBox.Show("Funcionalidade de edição de clientes não implementada.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);

                    break;

                case "fornecedores":
                    // TODO: Implementar janela de edição de fornecedores

                    // if (CadastroDataGrid.SelectedItem is FornecedorData fornecedorSelecionado)
                    // {
                    //     var editarFornecedor = new EditarFornecedorWindow(fornecedorSelecionado);
                    //     if (editarFornecedor.ShowDialog() == true)
                    //     {
                    //         // Se a edição foi bem-sucedida, atualiza o DataGrid
                    //         CarregarDadosTabela(_tabelaAtual);
                    //     }
                    // }

                    MessageBox.Show("Funcionalidade de edição de fornecedores não implementada.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);

                    break;

                case "usuários":
                    if (CadastroDataGrid.SelectedItem is UsuarioData usuarioSelecionado)
                    {
                        var editarUsuario = new EditarUsuarioWindow(usuarioSelecionado);
                        if (editarUsuario.ShowDialog() == true)
                        {
                            // Se a edição foi bem-sucedida, atualiza o DataGrid
                            CarregarDadosTabela(_tabelaAtual);
                        }
                    }
                    break;

                default:
                    MessageBox.Show("Tabela desconhecida. Não foi possível editar.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }
        }


        private void DeletarButton_Click(object sender, RoutedEventArgs e)
        {
            // Verifica se há um item selecionado no DataGrid
            if (CadastroDataGrid.SelectedItem is null)
            {
                MessageBox.Show("Por favor, selecione um registro para deletar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Verifica qual tabela está selecionada
            if (string.IsNullOrEmpty(_tabelaAtual))
            {
                MessageBox.Show("Selecione uma tabela antes de deletar.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Confirmação do usuário
            var resultado = MessageBox.Show("Tem certeza que deseja deletar o registro selecionado?", "Confirmação", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (resultado != MessageBoxResult.Yes)
            {
                return;
            }

            // Realiza a exclusão com base na tabela atual
            switch (_tabelaAtual.ToLower())
            {
                case "produtos":
                    if (CadastroDataGrid.SelectedItem is ProdutoData produtoSelecionado)
                    {
                        var collectionProdutos = _database.GetCollection<ProdutoData>("produtos");
                        collectionProdutos.Delete(produtoSelecionado.Id); // Assume que o modelo possui uma propriedade "Id"
                        MessageBox.Show("Produto deletado com sucesso.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    break;

                case "clientes":
                    // TODO: Implementar exclusão de clientes

                    //if (CadastroDataGrid.SelectedItem is ClienteData clienteSelecionado)
                    //{
                    //    var collectionClientes = _database.GetCollection<ClienteData>("clientes");
                    //    collectionClientes.Delete(clienteSelecionado.Id); // Assume que o modelo possui uma propriedade "Id"
                    //    MessageBox.Show("Cliente deletado com sucesso.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
                    //}

                    MessageBox.Show("Funcionalidade de exclusão de clientes não implementada.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);

                    break;

                case "usuários":
                    if (CadastroDataGrid.SelectedItem is UsuarioData usuarioSelecionado)
                    {
                        var collectionUsuarios = _database.GetCollection<UsuarioData>("usuarios");
                        collectionUsuarios.Delete(usuarioSelecionado.Id); // Assume que o modelo possui uma propriedade "Id"
                        MessageBox.Show("Usuário deletado com sucesso.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    break;

                default:
                    MessageBox.Show("Tabela desconhecida. Não foi possível deletar o registro.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
            }

            // Atualiza o DataGrid após a exclusão
            CarregarDadosTabela(_tabelaAtual);
        }
    }
}