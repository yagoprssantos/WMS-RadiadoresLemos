using LiteDB;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

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

        public CadastroUserControl(string tipoTabela) : this()
        {
            // Seleciona automaticamente a tabela especificada
            if (!string.IsNullOrEmpty(tipoTabela))
            {
                foreach (var item in TabelasComboBox.Items)
                {
                    if (item.ToString().Equals(tipoTabela, StringComparison.OrdinalIgnoreCase))
                    {
                        TabelasComboBox.SelectedItem = item;
                        break;
                    }
                }
            }
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
                    CarregarDadosGenerico<ClienteData>("clientes");
                    break;

                case "fornecedores":
                    CarregarDadosGenerico<FornecedorData>("fornecedores");
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
                // Ignorar colunas
                if (propriedade.Name.Equals("Id", StringComparison.OrdinalIgnoreCase) ||
                    propriedade.Name.Equals("Senha", StringComparison.OrdinalIgnoreCase))
                    continue;

                // Verifica se a propriedade é uma lista de strings (VendasRelacionadas ou ComprasRelacionadas)
                if ((propriedade.Name.Equals("VendasRelacionadas", StringComparison.OrdinalIgnoreCase) ||
                     propriedade.Name.Equals("ComprasRelacionadas", StringComparison.OrdinalIgnoreCase)) &&
                    propriedade.PropertyType == typeof(List<string>))
                {
                    // Cria uma coluna para exibir a lista como texto separado por vírgulas
                    var column = new DataGridTextColumn
                    {
                        Header = propriedade.Name,
                        Binding = new System.Windows.Data.Binding(propriedade.Name)
                        {
                            Converter = new ListToStringConverter()
                        }
                    };
                    CadastroDataGrid.Columns.Add(column);
                }
                else
                {
                    // Para outras propriedades, usa a abordagem padrão
                    CadastroDataGrid.Columns.Add(new DataGridTextColumn
                    {
                        Header = propriedade.Name,
                        Binding = new System.Windows.Data.Binding(propriedade.Name)
                    });
                }
            }
        }
        // Conversor para transformar List<string> em string separada por vírgulas
        public class ListToStringConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                if (value is List<string> lista && lista.Count > 0)
                {
                    return string.Join(", ", lista);
                }
                return string.Empty;
            }

            public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
            {
                throw new NotImplementedException();
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
                    ClientesPopup.IsOpen = true;
                    PreencherFiltrosClientes();

                    break;

                case "fornecedores":
                    FornecedoresPopup.IsOpen = true;
                    PreencherFiltrosFornecedores();
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

        private void PreencherFiltrosClientes()
        {
            try
            {
                var collection = _database.GetCollection<ClienteData>("clientes");
                var clientes = collection.FindAll().ToList();

                var estados = clientes.Select(c => c.Estado).Where(e => !string.IsNullOrEmpty(e)).Distinct().ToList();
                var cnpj = clientes.Select(c => c.CNPJ).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
                var vendasNF = clientes.Select(c => c.VendasRelacionadas).Where(v => v != null && v.Count > 0).ToList();

                EstadoComboBox.ItemsSource = estados;
                ClienteComboBox.ItemsSource = cnpj;
                // VendasNFComboBox.ItemsSource = vendasNF; // Se necessário, implementar lógica para preencher vendas relacionadas
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao preencher filtros de clientes: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PreencherFiltrosFornecedores()
        {
            try
            {
                var collection = _database.GetCollection<FornecedorData>("fornecedores");
                var fornecedores = collection.FindAll().ToList();

                var estados = fornecedores.Select(f => f.Estado).Where(e => !string.IsNullOrEmpty(e)).Distinct().ToList();
                var cnpj = fornecedores.Select(f => f.CNPJ).Where(c => !string.IsNullOrEmpty(c)).Distinct().ToList();
                var comprasNF = fornecedores.Select(f => f.ComprasRelacionadas).Where(c => c != null && c.Count > 0).ToList();

                EstadoComboBox.ItemsSource = estados;
                FornecedorComboBox.ItemsSource = cnpj;
                // ComprasNFComboBox.ItemsSource = comprasNF; // Se necessário, implementar lógica para preencher compras relacionadas
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao preencher filtros de fornecedores: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    AplicarFiltroClientes();
                    ClientesPopup.IsOpen = false;
                    break;

                case "fornecedores":
                    AplicarFiltroFornecedores();
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

        private void AplicarFiltroClientes()
        {
            try
            {
                var cliente = ClienteComboBox.SelectedItem?.ToString();
                var estado = EstadoComboBox.SelectedItem?.ToString();

                var collection = _database.GetCollection<ClienteData>("clientes");
                var clientes = collection.FindAll().ToList();

                var clientesFiltrados = clientes.Where(c =>
                    (string.IsNullOrEmpty(cliente) || c.CNPJ.Contains(cliente, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrEmpty(estado) || c.Estado == estado)).ToList();
                CadastroDataGrid.ItemsSource = clientesFiltrados;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao aplicar filtro de clientes: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AplicarFiltroFornecedores()
        {
            try
            {
                var fornecedor = FornecedorComboBox.SelectedItem?.ToString();
                var estado = EstadoComboBox.SelectedItem?.ToString();

                var collection = _database.GetCollection<FornecedorData>("fornecedores");
                var fornecedores = collection.FindAll().ToList();

                var fornecedoresFiltrados = fornecedores.Where(f =>
                    (string.IsNullOrEmpty(fornecedor) || f.CNPJ.Contains(fornecedor, StringComparison.OrdinalIgnoreCase)) &&
                    (string.IsNullOrEmpty(estado) || f.Estado == estado)).ToList();
                CadastroDataGrid.ItemsSource = fornecedoresFiltrados;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao aplicar filtro de fornecedores: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
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
                    ClienteComboBox.SelectedItem = null;
                    EstadoComboBox.SelectedItem = null;
                    CarregarDadosTabela(_tabelaAtual);
                    ClientesPopup.IsOpen = false;
                    break;

                case "fornecedores":
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
                    var cadastroCliente = new EditarClienteWindow(null);
                    if (cadastroCliente.ShowDialog() == true)
                    {
                        // Se o cadastro foi bem-sucedido, atualiza o DataGrid
                        CarregarDadosTabela(_tabelaAtual);
                    }
                    break;

                case "fornecedores":
                    var cadastroFornecedor = new EditarFornecedorWindow(null);
                    if (cadastroFornecedor.ShowDialog() == true)
                    {
                        // Se o cadastro foi bem-sucedido, atualiza o DataGrid
                        CarregarDadosTabela(_tabelaAtual);
                    }
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
                    if (CadastroDataGrid.SelectedItem is ClienteData clienteSelecionado)
                    {
                        var editarCliente = new EditarClienteWindow(clienteSelecionado);
                        if (editarCliente.ShowDialog() == true)
                        {
                            // Se a edição foi bem-sucedida, atualiza o DataGrid
                            CarregarDadosTabela(_tabelaAtual);
                        }
                    }
                    break;

                case "fornecedores":
                    if (CadastroDataGrid.SelectedItem is FornecedorData fornecedorSelecionado)
                    {
                        var editarFornecedor = new EditarFornecedorWindow(fornecedorSelecionado);
                        if (editarFornecedor.ShowDialog() == true)
                        {
                            // Se a edição foi bem-sucedida, atualiza o DataGrid
                            CarregarDadosTabela(_tabelaAtual);
                        }
                    }
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
            var confirmacao = MessageBox.Show("Tem certeza que deseja deletar o registro selecionado?", "Confirmação", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (confirmacao != MessageBoxResult.Yes)
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
                    if (CadastroDataGrid.SelectedItem is ClienteData clienteSelecionado)
                    {
                        var collectionClientes = _database.GetCollection<ClienteData>("clientes");
                        var collectionVendas = _database.GetCollection<VendaData>("vendas");
                        
                        // Verifica se existem vendas relacionadas
                        var vendasRelacionadas = collectionVendas.Find(v => v.ClienteId == clienteSelecionado.Id).ToList();
                        
                        if (vendasRelacionadas.Count > 0)
                        {
                            var confirmacaoCliente = MessageBox.Show(
                                $"Existem {vendasRelacionadas.Count} vendas relacionadas a este cliente. " +
                                "As vendas serão mantidas mas perderão a referência ao cliente. " +
                                "Deseja continuar com a exclusão?",
                                "Aviso",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Warning);

                            if (confirmacaoCliente == MessageBoxResult.Yes)
                            {
                                // Atualiza as vendas para remover a referência ao cliente
                                foreach (var venda in vendasRelacionadas)
                                {
                                    venda.ClienteId = string.Empty;
                                    venda.ClienteCNPJ = "Cliente Removido";
                                    collectionVendas.Update(venda);
                                }

                                // Deleta o cliente
                                collectionClientes.Delete(clienteSelecionado.Id);
                                MessageBox.Show("Cliente deletado com sucesso.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                        else
                        {
                            collectionClientes.Delete(clienteSelecionado.Id);
                            MessageBox.Show("Cliente deletado com sucesso.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    break;

                case "fornecedores":
                    if (CadastroDataGrid.SelectedItem is FornecedorData fornecedorSelecionado)
                    {
                        var collectionFornecedores = _database.GetCollection<FornecedorData>("fornecedores");
                        var collectionCompras = _database.GetCollection<CompraData>("compras");
                        
                        // Verifica se existem compras relacionadas
                        var comprasRelacionadas = collectionCompras.Find(c => c.FornecedorId == fornecedorSelecionado.Id).ToList();
                        
                        if (comprasRelacionadas.Count > 0)
                        {
                            var confirmacaoFornecedor = MessageBox.Show(
                                $"Existem {comprasRelacionadas.Count} compras relacionadas a este fornecedor. " +
                                "As compras serão mantidas mas perderão a referência ao fornecedor. " +
                                "Deseja continuar com a exclusão?",
                                "Aviso",
                                MessageBoxButton.YesNo,
                                MessageBoxImage.Warning);

                            if (confirmacaoFornecedor == MessageBoxResult.Yes)
                            {
                                // Atualiza as compras para remover a referência ao fornecedor
                                foreach (var compra in comprasRelacionadas)
                                {
                                    compra.FornecedorId = string.Empty;
                                    compra.FornecedorNome = "Fornecedor Removido";
                                    collectionCompras.Update(compra);
                                }

                                // Deleta o fornecedor
                                collectionFornecedores.Delete(fornecedorSelecionado.Id);
                                MessageBox.Show("Fornecedor deletado com sucesso.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
                            }
                        }
                        else
                        {
                            collectionFornecedores.Delete(fornecedorSelecionado.Id);
                            MessageBox.Show("Fornecedor deletado com sucesso.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
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

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(_tabelaAtual) || CadastroDataGrid.ItemsSource == null)
            {
                return;
            }

            string searchText = SearchBox.Text.ToLower();

            // Se a caixa de pesquisa estiver vazia, recarregar todos os dados
            if (string.IsNullOrEmpty(searchText))
            {
                CarregarDadosTabela(_tabelaAtual);
                return;
            }

            switch (_tabelaAtual.ToLower())
            {
                case "produtos":
                    FiltrarProdutosPorTexto(searchText);
                    break;

                case "clientes":
                    FiltrarClientesPorTexto(searchText);
                    break;

                case "fornecedores":
                    FiltrarFornecedoresPorTexto(searchText);
                    break;

                case "usuários":
                    FiltrarUsuariosPorTexto(searchText);
                    break;
            }
        }

        private void FiltrarProdutosPorTexto(string searchText)
        {
            var collection = _database.GetCollection<ProdutoData>("produtos");
            var produtos = collection.FindAll().ToList();

            // Filtra produtos que contenham o texto de busca em qualquer campo relevante
            var produtosFiltrados = produtos.Where(p =>
                (p.Nome?.ToLower().Contains(searchText) ?? false) ||
                (p.Tipo?.ToLower().Contains(searchText) ?? false) ||
                (p.Marca?.ToLower().Contains(searchText) ?? false) ||
                (p.Codigo?.ToLower().Contains(searchText) ?? false)).ToList();

            // Reordena para mostrar primeiro os itens que começam com o texto de busca
            produtosFiltrados = produtosFiltrados.OrderBy(p =>
                (p.Nome?.ToLower().StartsWith(searchText) ?? false) ? 0 :
                (p.Tipo?.ToLower().StartsWith(searchText) ?? false) ? 1 :
                (p.Marca?.ToLower().StartsWith(searchText) ?? false) ? 2 :
                (p.Codigo?.ToLower().StartsWith(searchText) ?? false) ? 3 : 4).ToList();

            CadastroDataGrid.ItemsSource = produtosFiltrados;
        }

        private void FiltrarClientesPorTexto(string searchText)
        {
            var collection = _database.GetCollection<ClienteData>("clientes");
            var clientes = collection.FindAll().ToList();

            // Filtra clientes que contenham o texto de busca em qualquer campo relevante
            var clientesFiltrados = clientes.Where(c =>
                (c.CNPJ?.ToLower().Contains(searchText) ?? false) ||
                (c.Email?.ToLower().Contains(searchText) ?? false) ||
                (c.Estado?.ToLower().Contains(searchText) ?? false));

            // Reordena para mostrar primeiro os itens que começam com o texto de busca
            clientesFiltrados = clientesFiltrados.OrderBy(c =>
                (c.CNPJ?.ToLower().StartsWith(searchText) ?? false) ? 0 :
                (c.Email?.ToLower().StartsWith(searchText) ?? false) ? 1 :
                (c.Estado?.ToLower().StartsWith(searchText) ?? false) ? 2 : 3).ToList();

            CadastroDataGrid.ItemsSource = clientesFiltrados;
        }

        private void FiltrarFornecedoresPorTexto(string searchText)
        {
            var collection = _database.GetCollection<FornecedorData>("fornecedores");
            var fornecedores = collection.FindAll().ToList();

            // Filtra fornecedores que contenham o texto de busca em qualquer campo relevante
            var fornecedoresFiltrados = fornecedores.Where(f =>
                (f.Nome?.ToLower().Contains(searchText) ?? false) ||
                (f.CNPJ?.ToLower().Contains(searchText) ?? false) ||
                (f.Estado?.ToLower().Contains(searchText) ?? false));

            // Reordena para mostrar primeiro os itens que começam com o texto de busca
            fornecedoresFiltrados = fornecedoresFiltrados.OrderBy(f =>
                (f.Nome?.ToLower().StartsWith(searchText) ?? false) ? 0 :
                (f.CNPJ?.ToLower().StartsWith(searchText) ?? false) ? 1 :
                (f.Estado?.ToLower().StartsWith(searchText) ?? false) ? 2 : 3).ToList();

            CadastroDataGrid.ItemsSource = fornecedoresFiltrados;
        }

        private void FiltrarUsuariosPorTexto(string searchText)
        {
            var collection = _database.GetCollection<UsuarioData>("usuarios");
            var usuarios = collection.FindAll().ToList();

            // Filtra usuários que contenham o texto de busca em qualquer campo relevante
            var usuariosFiltrados = usuarios.Where(u =>
                (u.Nome?.ToLower().Contains(searchText) ?? false) ||
                (u.Email?.ToLower().Contains(searchText) ?? false) ||
                (u.Cargo?.ToLower().Contains(searchText) ?? false)).ToList();

            // Reordena para mostrar primeiro os itens que começam com o texto de busca
            usuariosFiltrados = usuariosFiltrados.OrderBy(u =>
                (u.Nome?.ToLower().StartsWith(searchText) ?? false) ? 0 :
                (u.Email?.ToLower().StartsWith(searchText) ?? false) ? 1 :
                (u.Cargo?.ToLower().StartsWith(searchText) ?? false) ? 2 : 3).ToList();

            CadastroDataGrid.ItemsSource = usuariosFiltrados;
        }
    }
}