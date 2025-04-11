using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using LiteDB;
using WMS_RadiadoresLemos_WPF.src.Services;
using System.Windows.Controls;
using WMS_RadiadoresLemos_WPF.src.Models; // Para TextChangedEventArgs e SelectionChangedEventArgs

namespace WMS_RadiadoresLemos_WPF
{
    public partial class MenuTabelasWindow : Window
    {
        private LiteDatabase _database;
        private string _tabelaAtual;

        public MenuTabelasWindow()
        {
            InitializeComponent();
            _database = DatabaseConnect.Database; // Conexão com o banco de dados
            CarregarTabelas();
        }

        // Carrega os nomes das tabelas disponíveis no banco de dados e popula o ComboBox.
        private void CarregarTabelas()
        {
            var tabelas = _database.GetCollectionNames().ToList();
            TabelasComboBox.ItemsSource = tabelas;

            if (tabelas.Any())
            {
                TabelasComboBox.SelectedIndex = 0; // Seleciona a primeira tabela por padrão
            }
        }

        // Evento disparado ao alterar a seleção no ComboBox de tabelas.
        // Atualiza a tabela atual e carrega os dados correspondentes no DataGrid.
        private void TabelasComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabelasComboBox.SelectedItem != null)
            {
                _tabelaAtual = TabelasComboBox.SelectedItem.ToString();
                CarregarDadosTabela(_tabelaAtual);
            }
        }

        // Carrega os dados da tabela selecionada no DataGrid com base no nome da tabela.
        private void CarregarDadosTabela(string tabela)
        {
            switch (tabela.ToLower())
            {
                case "usuarios":
                    CarregarUsuarios();
                    break;
                case "produtos":
                    CarregarProdutos();
                    break;
                case "movimentacoes":
                    CarregarMovimentacoes();
                    break;
                case "historico":
                    CarregarHistorico();
                    break;
                case "alertas":
                    CarregarAlertas();
                    break;
                default:
                    MessageBox.Show("Tabela desconhecida. Não foi possível carregar os dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }
        }

        // Carrega os dados da tabela "usuarios" e configura as colunas do DataGrid.
        private void CarregarUsuarios()
        {
            var collection = _database.GetCollection<UsuarioData>("usuarios");
            var usuarios = collection.FindAll().ToList();
            TabelaDataGrid.ItemsSource = usuarios;
            ConfigurarColunasUsuarios();
        }

        // Configura as colunas do DataGrid para exibir os dados da tabela "usuarios".
        private void ConfigurarColunasUsuarios()
        {
            TabelaDataGrid.Columns.Clear();
            TabelaDataGrid.Columns.Add(new DataGridTextColumn { Header = "Nome", Binding = new System.Windows.Data.Binding("Nome") });
            TabelaDataGrid.Columns.Add(new DataGridTextColumn { Header = "Email", Binding = new System.Windows.Data.Binding("Email") });
            TabelaDataGrid.Columns.Add(new DataGridTextColumn { Header = "Matrícula", Binding = new System.Windows.Data.Binding("Matricula") });
            TabelaDataGrid.Columns.Add(new DataGridTextColumn { Header = "Cargo", Binding = new System.Windows.Data.Binding("Cargo") });
        }

        // Carrega os dados da tabela "produtos" e configura as colunas do DataGrid.
        private void CarregarProdutos()
        {
            var collection = _database.GetCollection<ProdutoData>("produtos");
            var produtos = collection.FindAll().ToList();
            TabelaDataGrid.ItemsSource = produtos;
            ConfigurarColunasProdutos();
        }

        // Configura as colunas do DataGrid para exibir os dados da tabela "produtos".
        private void ConfigurarColunasProdutos()
        {
            TabelaDataGrid.Columns.Clear();
            TabelaDataGrid.Columns.Add(new DataGridTextColumn { Header = "Nome", Binding = new System.Windows.Data.Binding("Nome") });
            TabelaDataGrid.Columns.Add(new DataGridTextColumn { Header = "Tipo", Binding = new System.Windows.Data.Binding("Tipo") });
            TabelaDataGrid.Columns.Add(new DataGridTextColumn { Header = "Marca", Binding = new System.Windows.Data.Binding("Marca") });
            TabelaDataGrid.Columns.Add(new DataGridTextColumn { Header = "Código", Binding = new System.Windows.Data.Binding("Codigo") });
        }

        // Carrega os dados da tabela "movimentacoes" e configura as colunas do DataGrid.
        private void CarregarMovimentacoes()
        {
            var collection = _database.GetCollection<MovimentacaoData>("movimentacoes");
            var movimentacoes = collection.FindAll().ToList();
            TabelaDataGrid.ItemsSource = movimentacoes;
            ConfigurarColunasMovimentacoes();
        }

        // Configura as colunas do DataGrid para exibir os dados da tabela "movimentacoes".
        private void ConfigurarColunasMovimentacoes()
        {
            TabelaDataGrid.Columns.Clear();
            TabelaDataGrid.Columns.Add(new DataGridTextColumn { Header = "Produto", Binding = new System.Windows.Data.Binding("ProdutoId") });
            TabelaDataGrid.Columns.Add(new DataGridTextColumn { Header = "Tipo", Binding = new System.Windows.Data.Binding("Tipo") });
            TabelaDataGrid.Columns.Add(new DataGridTextColumn { Header = "Data", Binding = new System.Windows.Data.Binding("DataFormatadaComAno") });
            TabelaDataGrid.Columns.Add(new DataGridTextColumn { Header = "Quantidade", Binding = new System.Windows.Data.Binding("Quantidade") });
        }

        // Carrega os dados da tabela "historico" e configura as colunas do DataGrid.
        private void CarregarHistorico()
        {
            var collection = _database.GetCollection<LogData>("historico");
            var historicos = collection.FindAll().ToList();
            TabelaDataGrid.ItemsSource = historicos;
            ConfigurarColunasHistorico();
        }

        // Configura as colunas do DataGrid para exibir os dados da tabela "historico".
        private void ConfigurarColunasHistorico()
        {
            TabelaDataGrid.Columns.Clear();
            TabelaDataGrid.Columns.Add(new DataGridTextColumn { Header = "Data", Binding = new System.Windows.Data.Binding("DataFormatadaComAno") });
            TabelaDataGrid.Columns.Add(new DataGridTextColumn { Header = "Tipo", Binding = new System.Windows.Data.Binding("Tipo") });
            TabelaDataGrid.Columns.Add(new DataGridTextColumn { Header = "Nível", Binding = new System.Windows.Data.Binding("Nivel") });
            TabelaDataGrid.Columns.Add(new DataGridTextColumn { Header = "Detalhes", Binding = new System.Windows.Data.Binding("Detalhes") });
            TabelaDataGrid.Columns.Add(new DataGridTextColumn { Header = "Usuário", Binding = new System.Windows.Data.Binding("Usuario") });
        }

        // Carrega os dados da tabela "alertas" e configura as colunas do DataGrid.
        private void CarregarAlertas()
        {
            var collection = _database.GetCollection<AlertaData>("alertas");
            var alertas = collection.FindAll().ToList();
            TabelaDataGrid.ItemsSource = alertas;
            ConfigurarColunasAlertas();
        }

        // Configura as colunas do DataGrid para exibir os dados da tabela "alertas".
        private void ConfigurarColunasAlertas()
        {
            TabelaDataGrid.Columns.Clear();
            TabelaDataGrid.Columns.Add(new DataGridTextColumn { Header = "Data", Binding = new System.Windows.Data.Binding("Data") });
            TabelaDataGrid.Columns.Add(new DataGridTextColumn { Header = "Tipo", Binding = new System.Windows.Data.Binding("Tipo") });
            TabelaDataGrid.Columns.Add(new DataGridTextColumn { Header = "Sistema", Binding = new System.Windows.Data.Binding("Sistema") });
            TabelaDataGrid.Columns.Add(new DataGridTextColumn { Header = "Detalhes", Binding = new System.Windows.Data.Binding("Detalhes") });
            TabelaDataGrid.Columns.Add(new DataGridTextColumn { Header = "Ações", Binding = new System.Windows.Data.Binding("Acoes") });
        }

        // Adiciona uma nova linha na tabela atual.
        private void AdicionarButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_tabelaAtual)) return;

            var collection = _database.GetCollection(_tabelaAtual);
            var novaLinha = new BsonDocument(); // Cria um documento vazio para ser preenchido
            collection.Insert(novaLinha);
            CarregarDadosTabela(_tabelaAtual);
        }

        // Edita a linha selecionada no DataGrid.
        private void EditarButton_Click(object sender, RoutedEventArgs e)
        {
            if (TabelaDataGrid.SelectedItem is BsonDocument documentoSelecionado)
            {
                var collection = _database.GetCollection(_tabelaAtual);
                collection.Update(documentoSelecionado);
                CarregarDadosTabela(_tabelaAtual);
            }
        }

        // Deleta a linha selecionada no DataGrid.
        private void DeletarButton_Click(object sender, RoutedEventArgs e)
        {
            if (TabelaDataGrid.SelectedItem is BsonDocument documentoSelecionado)
            {
                var collection = _database.GetCollection(_tabelaAtual);
                collection.Delete(documentoSelecionado["_id"]);
                CarregarDadosTabela(_tabelaAtual);
            }
        }

        // Filtra os dados no DataGrid com base no texto digitado na barra de pesquisa.
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(SearchBox.Text))
            {
                CarregarDadosTabela(_tabelaAtual);
            }
            else
            {
                var textoPesquisa = SearchBox.Text.ToLower();
                var collection = _database.GetCollection(_tabelaAtual);
                var dadosFiltrados = collection.FindAll()
                    .Where(doc => doc.Values.Any(valor => valor.ToString().ToLower().Contains(textoPesquisa)))
                    .ToList();
                TabelaDataGrid.ItemsSource = dadosFiltrados;
            }
        }

        // Exibe uma mensagem informando que a funcionalidade de filtro ainda não foi implementada.
        private void FiltrarButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Funcionalidade de filtro ainda não implementada.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
