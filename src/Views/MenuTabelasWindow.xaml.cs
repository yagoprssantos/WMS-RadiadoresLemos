using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using LiteDB;
using WMS_RadiadoresLemos_WPF.src.Services;
using System.Windows.Controls;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Views;
using System.Windows.Media;
namespace WMS_RadiadoresLemos_WPF
{
    public partial class MenuTabelasWindow : Window
    {
        private LiteDatabase _database;
        private string _tabelaAtual = string.Empty;
        private Dictionary<string, List<string>> _filtrosPorTabela;
        private Dictionary<string, string> _filtrosAplicados = new();

        public MenuTabelasWindow()
        {
            InitializeComponent();
            _database = DatabaseConnect.Database ?? throw new InvalidOperationException("Database não pode ser nulo.");
            CarregarTabelas();
            ConfigurarFiltros();
        }

        // Carrega os nomes das tabelas disponíveis no banco de dados e popula o ComboBox.
        private void CarregarTabelas()
        {
            var tabelas = _database.GetCollectionNames().ToList();
            TabelasComboBox.ItemsSource = tabelas;
        }

        // Configura os filtros disponíveis para cada tabela.
        private void ConfigurarFiltros()
        {
            _filtrosPorTabela = new Dictionary<string, List<string>>
            {
                { "usuarios", new List<string> { "Cargo" } },
                { "produtos", new List<string> { "Nome", "Tipo", "Marca", "Codigo" } },
                { "movimentacoes", new List<string> { "ProdutoId", "Data" } },
                { "historico", new List<string> { "Tipo", "Nivel", "Usuario" } },
                { "alertas", new List<string> { "Tipo", "Sistema", "Detalhes" } }
            };
        }

        private void FiltrarButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_tabelaAtual))
            {
                MessageBox.Show("Por favor, selecione uma tabela antes de aplicar filtros.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Limpa os filtros existentes
            FiltroContainer.Children.Clear();

            // Configura os filtros com base na tabela atual
            switch (_tabelaAtual.ToLower())
            {
                case "produtos":
                    CriarFiltrosProdutos();
                    break;
                case "usuarios":
                    CriarFiltrosUsuarios();
                    break;
                case "movimentacoes":
                    CriarFiltrosMovimentacoes();
                    break;
                case "historico":
                    CriarFiltrosHistorico();
                    break;
                case "alertas":
                    CriarFiltrosAlertas();
                    break;
                default:
                    MessageBox.Show("Tabela desconhecida. Não foi possível configurar os filtros.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
            }

            // Abre o Popup
            FiltroPopup.IsOpen = true;
        }

        // Método para criar filtros para a tabela "Produtos"
        private void CriarFiltrosProdutos()
        {
            var estoqueCheckBox = new CheckBox
            {
                Content = "Ver apenas itens em estoque",
                Foreground = (Brush)FindResource("TextBrush"),
                FontSize = 16,
                Margin = new Thickness(10, 10, 0, 5)
            };
            FiltroContainer.Children.Add(estoqueCheckBox);

            AdicionarFiltroComboBox("Produto", "ProdutoComboBox", ObterDadosColuna("Nome"));
            AdicionarFiltroComboBox("Tipo", "TipoComboBox", ObterDadosColuna("Tipo"));
            AdicionarFiltroComboBox("Marca", "MarcaComboBox", ObterDadosColuna("Marca"));
            AdicionarFiltroComboBox("Código", "CodigoComboBox", ObterDadosColuna("Codigo"));
        }

        // Método para criar filtros para a tabela "Usuários"
        private void CriarFiltrosUsuarios()
        {
            AdicionarFiltroComboBox("Cargo", "CargoComboBox", ObterDadosColuna("Cargo"));
        }

        // Método para criar filtros para a tabela "Movimentações"
        private void CriarFiltrosMovimentacoes()
        {
            AdicionarFiltroComboBox("Produto", "ProdutoComboBox", ObterDadosColuna("ProdutoId"));
            AdicionarFiltroPeriodo("Data");
        }

        // Método para criar filtros para a tabela "Histórico"
        private void CriarFiltrosHistorico()
        {
            AdicionarFiltroComboBox("Tipo", "TipoComboBox", new[] { "Operacional", "Erro" });
            AdicionarFiltroComboBox("Nível", "NivelComboBox", new[] { "Usuário", "Sistema" });
            AdicionarFiltroComboBox("Usuário", "UsuarioComboBox", ObterDadosColuna("Usuario"));
            AdicionarFiltroPeriodo("Período");
        }

        // Método para criar filtros para a tabela "Alertas"
        private void CriarFiltrosAlertas()
        {
            AdicionarFiltroComboBox("Tipo", "TipoComboBox", ObterDadosColuna("Tipo"));
            AdicionarFiltroComboBox("Sistema", "SistemaComboBox", ObterDadosColuna("Sistema"));
            AdicionarFiltroComboBox("Detalhes", "DetalhesComboBox", ObterDadosColuna("Detalhes"));
        }

        // Método auxiliar para obter os dados de uma coluna específica da tabela atual
        private IEnumerable<string> ObterDadosColuna(string coluna)
        {
            if (string.IsNullOrEmpty(_tabelaAtual))
                return Enumerable.Empty<string>();

            var collection = _database.GetCollection(_tabelaAtual);
            var dados = collection.FindAll().Cast<BsonDocument>().ToList();

            return dados
                .Where(d => d.ContainsKey(coluna))
                .Select(d => d[coluna]?.ToString()?.Replace("\"", "").Trim()) // Remove aspas duplas e espaços extras
                .Where(valor => !string.IsNullOrEmpty(valor))
                .Distinct()
                .OrderBy(valor => valor)
                .ToList();
        }


        // Método auxiliar para adicionar filtros do tipo ComboBox
        private void AdicionarFiltroComboBox(string label, string comboBoxName, IEnumerable<string>? items = null)
        {
            var textBlock = new TextBlock
            {
                Text = $"Filtrar por {label}",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10, 10, 0, 5),
                Foreground = (Brush)FindResource("TextBrush")
            };
            FiltroContainer.Children.Add(textBlock);

            var comboBox = new ComboBox
            {
                Name = comboBoxName,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Margin = new Thickness(15, 0, 15, 0),
                Style = (Style)FindResource("ComboBoxSearchStyle"),
                Background = (Brush)FindResource("PanelBackgroundBrush")
            };

            if (items != null)
            {
                foreach (var item in items)
                {
                    comboBox.Items.Add(new ComboBoxItem { Content = item.Replace("\"", "").Trim() }); // Remove aspas duplas e espaços extras
                }
            }

            // Restaura o valor selecionado, se existir
            if (_filtrosAplicados.TryGetValue(comboBoxName.Replace("ComboBox", ""), out var valorSelecionado))
            {
                comboBox.SelectedItem = comboBox.Items.Cast<ComboBoxItem>().FirstOrDefault(i => i.Content.ToString()?.ToLower() == valorSelecionado);
            }

            FiltroContainer.Children.Add(comboBox);
        }

        // Método auxiliar para adicionar filtros do tipo Período (Data)
        private void AdicionarFiltroPeriodo(string label)
        {
            var textBlock = new TextBlock
            {
                Text = $"Filtrar por {label}",
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(10, 10, 0, 5),
                Foreground = (Brush)FindResource("TextBrush")
            };
            FiltroContainer.Children.Add(textBlock);

            var stackPanel = new StackPanel { Orientation = Orientation.Horizontal };

            var dataInicioPicker = new DatePicker
            {
                Name = "DataInicioPicker",
                Width = 125,
                Style = (Style)FindResource("DatePickerStyle"),
                Margin = new Thickness(15, 0, 0, 0)
            };
            stackPanel.Children.Add(dataInicioPicker);

            var dataFimPicker = new DatePicker
            {
                Name = "DataFimPicker",
                Width = 125,
                Style = (Style)FindResource("DatePickerStyle"),
                Margin = new Thickness(15, 0, 15, 0)
            };
            stackPanel.Children.Add(dataFimPicker);

            FiltroContainer.Children.Add(stackPanel);
        }

        // Aplica o filtro selecionado na tabela atual
        private void AplicarFiltroButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_tabelaAtual) || TabelaDataGrid.ItemsSource == null)
                return;

            // Variáveis para filtros de data
            DateTime? dataInicio = null;
            DateTime? dataFim = null;

            // Atualiza os filtros aplicados
            foreach (var child in FiltroContainer.Children)
            {
                if (child is ComboBox comboBox && comboBox.SelectedItem is ComboBoxItem selectedItem)
                {
                    var coluna = comboBox.Name.Replace("ComboBox", "");
                    _filtrosAplicados[coluna] = selectedItem.Content.ToString()?.Replace("\"", "").Trim().ToLower() ?? string.Empty;
                }
                else if (child is CheckBox checkBox)
                {
                    var coluna = checkBox.Content.ToString()?.ToLower();
                    if (!string.IsNullOrEmpty(coluna))
                        _filtrosAplicados[coluna] = checkBox.IsChecked == true ? "true" : "false";
                }
                else if (child is StackPanel panel)
                {
                    foreach (var element in panel.Children)
                    {
                        if (element is DatePicker datePicker)
                        {
                            if (datePicker.Name == "DataInicioPicker")
                                dataInicio = datePicker.SelectedDate;
                            else if (datePicker.Name == "DataFimPicker")
                                dataFim = datePicker.SelectedDate;
                        }
                    }
                }
            }

            // Aplica os filtros
            var collection = _database.GetCollection(_tabelaAtual);
            var dados = collection.FindAll().Cast<BsonDocument>().ToList();

            var dadosFiltrados = dados.Where(dado =>
            {
                // Verifica os filtros de texto
                bool atendeFiltrosTexto = _filtrosAplicados.All(filtro =>
                {
                    var valor = dado[filtro.Key]?.ToString()?.Replace("\"", "").Trim().ToLower();
                    return valor != null && valor.Contains(filtro.Value);
                });

                // Verifica os filtros de data
                if (dataInicio.HasValue || dataFim.HasValue)
                {
                    var data = dado["Data"]?.AsDateTime;
                    if (dataInicio.HasValue && data < dataInicio.Value)
                        return false;
                    if (dataFim.HasValue && data > dataFim.Value)
                        return false;
                }

                return atendeFiltrosTexto;
            }).ToList();

            // Atualiza o DataGrid
            TabelaDataGrid.Columns.Clear();
            ConfigurarColunasGenerico(ObterModelo(_tabelaAtual));
            TabelaDataGrid.ItemsSource = dadosFiltrados.Select(d => BsonMapper.Global.ToObject(ObterModelo(_tabelaAtual), d)).ToList();
        }

        // Limpa os filtros e recarrega os dados originais da tabela
        private void LimparFiltroButton_Click(object sender, RoutedEventArgs e)
        {
            _filtrosAplicados.Clear();
            FiltroContainer.Children.Clear();
            CarregarDadosTabela(_tabelaAtual);

            FiltroPopup.IsOpen = false;
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
    }
}
