using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using LiteDB;
using WMS_RadiadoresLemos_WPF.src.Services;
using System.Windows.Controls;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Views; // Para TextChangedEventArgs e SelectionChangedEventArgs

namespace WMS_RadiadoresLemos_WPF
{
    public partial class MenuTabelasWindow : Window
    {
        private LiteDatabase _database;
        private string _tabelaAtual;

        public MenuTabelasWindow()
        {
            InitializeComponent();
            _database = DatabaseConnect.Database;
            CarregarTabelas();
        }

        // Carrega os nomes das tabelas disponíveis no banco de dados e popula o ComboBox.
        private void CarregarTabelas()
        {
            var tabelas = _database.GetCollectionNames().ToList();
            TabelasComboBox.ItemsSource = tabelas;
        }

        // Evento disparado ao alterar a seleção no ComboBox de tabelas.
        // Atualiza a tabela atual e carrega os dados correspondentes no DataGrid.
        private void TabelasComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabelasComboBox.SelectedItem != null)
            {
                _tabelaAtual = TabelasComboBox.SelectedItem.ToString();

                // Carrega os dados da tabela selecionada
                CarregarDadosTabela(_tabelaAtual);
            }
        }

        // Carrega os dados da tabela selecionada no DataGrid com base no nome da tabela.
        private void CarregarDadosTabela(string tabela)
        {
            // Limpa o DataGrid antes de carregar novos dados
            TabelaDataGrid.ItemsSource = null;

            // Verifica se a tabela é válida
            switch (tabela.ToLower())
            {
                case "usuarios":
                    CarregarDadosGenerico<UsuarioData>("usuarios");
                    break;
                case "produtos":
                    CarregarDadosGenerico<ProdutoData>("produtos");
                    break;
                case "movimentacoes":
                    CarregarDadosGenerico<MovimentacaoData>("movimentacoes");
                    break;
                case "historico":
                    CarregarDadosGenerico<LogData>("historico");
                    break;
                case "alertas":
                    CarregarDadosGenerico<AlertaData>("alertas");
                    break;
                default:
                    MessageBox.Show("Tabela desconhecida. Não foi possível carregar os dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }
        }

        // Método genérico para carregar os dados de qualquer tabela
        private void CarregarDadosGenerico<T>(string tabela) where T : class
        {
            var collection = _database.GetCollection<T>(tabela);
            var dados = collection.FindAll().ToList();
            TabelaDataGrid.ItemsSource = dados;
            ConfigurarColunasGenerico(typeof(T));
        }

        // Método genérico para configurar as colunas do DataGrid
        private void ConfigurarColunasGenerico(Type tipoModelo)
        {
            TabelaDataGrid.Columns.Clear();

            foreach (var propriedade in tipoModelo.GetProperties())
            {
                TabelaDataGrid.Columns.Add(new DataGridTextColumn
                {
                    Header = propriedade.Name,
                    Binding = new System.Windows.Data.Binding(propriedade.Name)
                });
            }
        }

        // Adiciona um novo registro na tabela atual
        private void AdicionarButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_tabelaAtual)) return;

            // Cria um novo BsonDocument vazio
            var novoRegistro = new BsonDocument();

            // Abre a janela de edição com o novo registro
            var janela = new EditarGenericoWindow(_tabelaAtual, novoRegistro);
            if (janela.ShowDialog() == true)
            {
                CarregarDadosTabela(_tabelaAtual);
            }
        }

        // Edita o registro selecionado no DataGrid
        private void EditarButton_Click(object sender, RoutedEventArgs e)
        {
            if (TabelaDataGrid.SelectedItem is not null)
            {
                // Converte o item selecionado para um BsonDocument
                var registroSelecionado = ConvertToBsonDocument(TabelaDataGrid.SelectedItem);

                // Abre a janela de edição com o registro selecionado
                var janela = new EditarGenericoWindow(_tabelaAtual, registroSelecionado);
                if (janela.ShowDialog() == true)
                {
                    CarregarDadosTabela(_tabelaAtual);
                }
            }
            else
            {
                MessageBox.Show("Por favor, selecione um registro para editar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Método auxiliar para converter um objeto para BsonDocument
        private BsonDocument ConvertToBsonDocument(object item)
        {
            var bsonDocument = new BsonDocument();
            var propriedades = item.GetType().GetProperties();

            foreach (var propriedade in propriedades)
            {
                var valor = propriedade.GetValue(item);
                bsonDocument[propriedade.Name] = valor != null ? new BsonValue(valor) : BsonValue.Null;
            }

            return bsonDocument;
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
                // Se a barra de pesquisa estiver vazia, recarrega todos os dados da tabela.
                CarregarDadosTabela(_tabelaAtual);
            }
            else
            {
                var textoPesquisa = SearchBox.Text.ToLower();

                // Obtém a coleção da tabela atual
                var collection = _database.GetCollection<BsonDocument>(_tabelaAtual);

                // Filtra os dados com base no texto de pesquisa
                var dadosFiltrados = collection.FindAll()
                    .Where(doc => doc.RawValue.ToString().ToLower().Contains(textoPesquisa))
                    .ToList();

                // Atualiza o DataGrid com os dados filtrados
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
