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
        private string _tabelaAtual = string.Empty;

        public MenuTabelasWindow()
        {
            InitializeComponent();
            _database = DatabaseConnect.Database ?? throw new InvalidOperationException("Database não pode ser nulo."); // Evita CS8601 e CS8618  
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
                _tabelaAtual = TabelasComboBox.SelectedItem?.ToString() ?? string.Empty;

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
            // Limpa o DataGrid antes de carregar novos dados
            TabelaDataGrid.ItemsSource = null;
            TabelaDataGrid.Columns.Clear();
            TabelaDataGrid.Items.Clear();

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

            // Cria um novo BsonDocument com valores padrão
            var novoRegistro = new BsonDocument();
            var modelo = ObterModelo(_tabelaAtual);

            foreach (var propriedade in modelo.GetProperties())
            {
                object? valorPadrao = propriedade.PropertyType.IsValueType ? Activator.CreateInstance(propriedade.PropertyType) : null;
                novoRegistro[propriedade.Name] = new BsonValue(valorPadrao);
            }

            // Abre a janela de edição com o novo registro
            var janela = new EditarGenericoWindow(_tabelaAtual, novoRegistro);
            if (janela.ShowDialog() == true)
            {
                CarregarDadosTabela(_tabelaAtual);
            }
        }

        // Método auxiliar para obter o modelo da tabela
        private Type ObterModelo(string tabela)
        {
            return tabela.ToLower() switch
            {
                "usuarios" => typeof(UsuarioData),
                "produtos" => typeof(ProdutoData),
                "historico" => typeof(LogData),
                "movimentacoes" => typeof(MovimentacaoData),
                "alertas" => typeof(AlertaData),
                _ => throw new InvalidOperationException($"Modelo para a tabela '{tabela}' não encontrado.")
            };
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
            if (TabelaDataGrid.SelectedItem is not null)
            {
                // Obtém o item selecionado e o ID
                var registroSelecionado = TabelaDataGrid.SelectedItem;
                var idPropriedade = registroSelecionado.GetType().GetProperty("Id");

                if (idPropriedade != null)
                {
                    var idValor = idPropriedade.GetValue(registroSelecionado);

                    // Deleta o registro com base no ID
                    var collection = _database.GetCollection(_tabelaAtual);
                    if (collection.Delete(new BsonValue(idValor)))
                    {
                        MessageBox.Show("Registro deletado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                        CarregarDadosTabela(_tabelaAtual);
                    }
                    else
                    {
                        MessageBox.Show("Não foi possível deletar o registro. ID não encontrado.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    MessageBox.Show("O registro selecionado não possui um ID válido.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            else
            {
                MessageBox.Show("Por favor, selecione um registro para deletar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Filtra os dados no DataGrid com base no texto digitado na barra de pesquisa.
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(_tabelaAtual) || TabelaDataGrid.ItemsSource == null)
                return;

            // Obtém o texto digitado na barra de pesquisa
            string textoPesquisa = SearchBox.Text.ToLower();

            // Recupera os dados originais da tabela atual
            var collection = _database.GetCollection(_tabelaAtual);
            var dadosOriginais = collection.FindAll().Cast<BsonDocument>().ToList();

            // Filtra os dados com base no texto de pesquisa
            var dadosFiltrados = dadosOriginais.Where(dado =>
            {
                foreach (var propriedade in dado.Keys)
                {
                    var valor = dado[propriedade]?.ToString()?.ToLower();
                    if (!string.IsNullOrEmpty(valor) && valor.Contains(textoPesquisa))
                    {
                        return true;
                    }
                }
                return false;
            }).ToList();

            // Converte os dados filtrados para o tipo correto
            switch (_tabelaAtual.ToLower())
            {
                case "usuarios":
                    TabelaDataGrid.ItemsSource = dadosFiltrados.Select(d => BsonMapper.Global.ToObject<UsuarioData>(d)).ToList();
                    ConfigurarColunasGenerico(typeof(UsuarioData));
                    break;
                case "produtos":
                    TabelaDataGrid.ItemsSource = dadosFiltrados.Select(d => BsonMapper.Global.ToObject<ProdutoData>(d)).ToList();
                    ConfigurarColunasGenerico(typeof(ProdutoData));
                    break;
                case "movimentacoes":
                    TabelaDataGrid.ItemsSource = dadosFiltrados.Select(d => BsonMapper.Global.ToObject<MovimentacaoData>(d)).ToList();
                    ConfigurarColunasGenerico(typeof(MovimentacaoData));
                    break;
                case "historico":
                    TabelaDataGrid.ItemsSource = dadosFiltrados.Select(d => BsonMapper.Global.ToObject<LogData>(d)).ToList();
                    ConfigurarColunasGenerico(typeof(LogData));
                    break;
                case "alertas":
                    TabelaDataGrid.ItemsSource = dadosFiltrados.Select(d => BsonMapper.Global.ToObject<AlertaData>(d)).ToList();
                    ConfigurarColunasGenerico(typeof(AlertaData));
                    break;
                default:
                    MessageBox.Show("Tabela desconhecida. Não foi possível aplicar o filtro.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    break;
            }
        }


        // Exibe uma mensagem informando que a funcionalidade de filtro ainda não foi implementada.
        private void FiltrarButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Funcionalidade de filtro ainda não implementada.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
