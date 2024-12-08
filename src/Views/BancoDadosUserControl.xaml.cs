using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using WMS_RadiadoresLemos_WPF.src.Models; // Certifique-se de que o namespace correto está sendo usado
using Microsoft.Win32; // Para o diálogo de salvar arquivo
using ClosedXML.Excel; // Adicione esta linha ao topo do arquivo
using System.IO;
using System.Windows;
using Google.Cloud.Firestore;
using WMS_RadiadoresLemos_WPF.src.Services;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using DocumentFormat.OpenXml.Packaging;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class BancoDadosUserControl : UserControl
    {
        private List<object> dadosFiltrados = new List<object>();
        private bool dadosCarregados = false;
        private List<string> tabelasSelecionadas = new List<string>();

        public BancoDadosUserControl()
        {
            InitializeComponent();
            DataContext = this;
            CarregarTabelas();
        }

        // Método para carregar as tabelas no ComboBox
        private void CarregarTabelas()
        {
            try
            {
                TabelaComboBox.Items.Clear();
                foreach (var tabela in DadosCache.Tabelas.Keys)
                {
                    TabelaComboBox.Items.Add(tabela);
                }
            }
            catch (Exception ex)
            {
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            $"Erro ao carregar tabelas. Possíveis motivos:\n" +
                                            "- Não foi possível acessar o cache de dados;\n" +
                                            "- Problemas de conexão com a internet;\n" +
                                            "- Serviço do banco de dados indisponível.",
                                            "- Recarregue a tela de Banco de Dados.");

                Console.WriteLine($"Erro ao carregar tabelas: {ex.Message}");
            }
        }

        // Evento disparado quando uma tabela é selecionada no ComboBox
        private void TabelaComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (TabelaComboBox.SelectedItem != null)
                {
                    string? tabelaSelecionada = TabelaComboBox.SelectedItem?.ToString();
                    if (tabelaSelecionada != null)
                    {
                        AtualizarTabelaDadosCache(tabelaSelecionada);
                    }
                }
            }
            catch (Exception ex)
            {
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            $"Erro ao selecionar tabela. Possíveis motivos:\n" +
                                            "- Tabela não encontrada;\n" +
                                            "- Tabela com dados corrompidos;\n" +
                                            "- Serviço do banco de dados indisponível.",
                                            "- Verifique se a tabela selecionada está disponível\n" +
                                            "- Recarregue a tela de Banco de Dados.");

                Console.WriteLine($"Erro ao selecionar tabela: {ex.Message}");
            }
        }

        // Método para atualizar a tabela de dados com os dados do cache
        private void AtualizarTabelaDadosCache(string tabela)
        {
            try
            {
                if (DadosCache.Tabelas.TryGetValue(tabela, out List<object>? value))
                {
                    dadosFiltrados = value;
                    DadosDataGrid.ItemsSource = dadosFiltrados;
                    dadosCarregados = true;
                    RemoverUltimaColuna();
                }
            }
            catch (Exception ex)
            {
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            $"Erro ao atualizar tabela de dados. Possíveis motivos:\n" +
                                            "- Tabela corrompida;\n" +
                                            "- Dados não encontrados;\n" +
                                            "- Serviço do banco de dados indisponível.",
                                            "- Verifique se a tabela selecionada está disponível\n" +
                                            "- Recarregue a tela de Banco de Dados.");

                Console.WriteLine($"Erro ao atualizar tabela de dados: {ex.Message}");
            }
        }

        // Método para remover a última coluna do DataGrid
        private void RemoverUltimaColuna()
        {
            try
            {
                if (DadosDataGrid.Columns.Count > 0)
                {
                    DadosDataGrid.Columns.RemoveAt(DadosDataGrid.Columns.Count - 1);
                }
            }
            catch (Exception ex)
            {
                // Ignora exceções ao remover a última coluna
                AlertaCache.AdicionarAlerta("Aviso",
                                            ex.Message.ToString(),
                                            $"Erro ao remover última coluna. Isto é um erro mas não causa interferência no funcionamento do sistema. Possíveis motivos:\n" +
                                            "- Aplicações com bugs;\n" +
                                            "- Tabela de dados vazia;\n" +
                                            "- Problema ao carregar dados;\n" +
                                            "- Impossibilidade de remover a última coluna.",
                                            "- Feche e abra a tela de Banco de Dados");

                Console.WriteLine($"Erro ao remover última coluna: {ex.Message}");
            }
        }

        // Evento disparado quando o texto de pesquisa é alterado
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (!dadosCarregados && TabelaComboBox.SelectedItem != null)
                {
                    string? tabelaSelecionada = TabelaComboBox.SelectedItem?.ToString();
                    if (tabelaSelecionada != null)
                    {
                        AtualizarTabelaDadosCache(tabelaSelecionada);
                    }
                }

                string searchText = SearchBox.Text.ToLower();
                var filteredData = dadosFiltrados.Where(item =>
                    item.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Any(prop => prop.GetValue(item)?.ToString()?.ToLower().Contains(searchText) == true)
                ).ToList();
                DadosDataGrid.ItemsSource = filteredData;
                RemoverUltimaColuna();
            }
            catch (Exception ex)
            {
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            $"Erro ao filtrar dados. Possíveis motivos:\n" +
                                            "- Sistema com bugs;\n" +
                                            "- Caracteres inválidos na pesquisa;\n" +
                                            "- Problema ao carregar dados.",
                                            "- Feche e abra a tela de Banco de Dados;\n" +
                                            "- Tente reiniciar a aplicação.");

                Console.WriteLine($"Erro ao filtrar dados: {ex.Message}");
            }
        }

        // Evento disparado quando o botão de atualizar é clicado
        private void AtualizarDataGrid_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                if (TabelaComboBox.SelectedItem != null)
                {
                    string? tabelaSelecionada = TabelaComboBox.SelectedItem?.ToString();
                    if (tabelaSelecionada != null)
                    {
                        AtualizarTabelaDadosCache(tabelaSelecionada);
                    }
                }
            }
            catch (Exception ex)
            {
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            $"Erro ao atualizar tabela de dados. Possíveis motivos:\n" +
                                            "- Componentes da interface corrompidos;\n" +
                                            "- Erro ao reconhecer a tabela selecionada;\n" +
                                            "- Serviço do banco de dados indisponível.",
                                            "- Feche e abra a tela de Banco de Dados;\n" +
                                            "- Reinicie a aplicação.");

                Console.WriteLine($"Erro ao atualizar DataGrid: {ex.Message}");
            }
        }

        private async Task<List<object>> ObterDadosDoFirebaseAsync()
        {
            var db = DatabaseConnect.Database; // Supondo que DatabaseConnect.Database esteja configurado com a instância do Firestore
            var produtos = new List<object>();

            try
            {
                var produtosRef = db.Collection("Produtos");
                var snapshot = await produtosRef.GetSnapshotAsync();

                foreach (var doc in snapshot.Documents)
                {
                    // Converte o documento para o tipo ProdutoData (ajuste para o tipo específico que você usa)
                    var produto = doc.ConvertTo<ProdutoData>();
                    produtos.Add(produto);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao obter dados de Produtos: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            $"Erro ao obter dados de Produtos. Possíveis motivos:\n" +
                                            "- Problemas de conexão com a internet;\n" +
                                            "- Configurações incorretas do banco de dados;\n" +
                                            "- Serviço do banco de dados indisponível.",
                                            "- Verifique sua conexão com a internet;\n" +
                                            "- Verifique as configurações do banco de dados.");
            }

            return produtos;
        }


        // Evento disparado quando o botão de exportar é clicado
        private async void ExportarDados_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                // Adiciona tabelas ao ListBox
                ExportTabelasListBox.Items.Clear();
                foreach (var tabela in DadosCache.Tabelas.Keys)
                {
                    ExportTabelasListBox.Items.Add(tabela);
                }

                // Abre o popup de configuração de exportação
                ExportConfigPopup.IsOpen = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao iniciar exportação de dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            $"Erro ao iniciar exportação de dados. Possíveis motivos:\n" +
                                            "- Função de exportação removida por terceiros;\n" +
                                            "- Tabela de dados vazia ou corrompida.",
                                            "- Verifique se a função de exportação está disponível;\n" +
                                            "- Verifique se há dados disponíveis para exportação.");

                ShowProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        // Evento disparado ao selecionar todas as tabelas
        private async void ConfirmarExportacao_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowProgressBar.Visibility = Visibility.Visible;
                ProgressBar.Value = 0;

                var dadosProdutos = await ObterDadosDoFirebaseAsync();

                if (dadosProdutos == null || !dadosProdutos.Any())
                {
                    MessageBox.Show("Nenhum dado disponível para exportação.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    AlertaCache.AdicionarAlerta("Erro",
                                                "Nenhum dado disponível para exportação.",
                                                $"Erro ao exportar dados. Possíveis motivos:\n" +
                                                "- Dados não encontrados;\n" +
                                                "- Dados corrompidos;\n" +
                                                "- Erro ao acessar os dados.",
                                                "- Verifique se há dados disponíveis para exportação.");

                    ShowProgressBar.Visibility = Visibility.Collapsed;
                    return;
                }

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    Title = "Salvar dados como Excel",
                    FileName = "radiadoreslemosdb-export.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        await Task.Run(() =>
                        {
                            using (var workbook = new XLWorkbook())
                            {
                                // Captura tabelas selecionadas
                                Dispatcher.Invoke(() =>
                                {
                                    foreach (var item in ExportTabelasListBox.SelectedItems)
                                    {
                                        tabelasSelecionadas.Add(item.ToString());
                                    }
                                });

                                int totalTabelas = tabelasSelecionadas.Count;
                                int tabelaAtual = 0;

                                foreach (var tabela in tabelasSelecionadas)
                                {
                                    var dadosTabela = DadosCache.Tabelas[tabela];
                                    var worksheet = workbook.Worksheets.Add(tabela);

                                    if (dadosTabela.Any())
                                    {
                                        var properties = dadosTabela.First().GetType().GetProperties();
                                        for (int i = 0; i < properties.Length; i++)
                                        {
                                            var cell = worksheet.Cell(1, i + 1);
                                            cell.Value = properties[i].Name;
                                            cell.Style.Fill.BackgroundColor = XLColor.UltramarineBlue;
                                            cell.Style.Font.FontColor = XLColor.White;
                                            cell.Style.Font.Bold = true;
                                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                                        }

                                        for (int i = 0; i < dadosTabela.Count; i++)
                                        {
                                            var item = dadosTabela[i];
                                            for (int j = 0; j < properties.Length; j++)
                                            {
                                                var cell = worksheet.Cell(i + 2, j + 1);
                                                cell.Value = properties[j].GetValue(item, null)?.ToString() ?? "";
                                                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                                                if (i % 2 == 0)
                                                {
                                                    cell.Style.Fill.BackgroundColor = XLColor.White;
                                                }
                                                else
                                                {
                                                    cell.Style.Fill.BackgroundColor = XLColor.Gainsboro;
                                                }
                                            }
                                        }

                                        worksheet.Columns().AdjustToContents();
                                    }

                                    tabelaAtual++;
                                    Dispatcher.Invoke(() =>
                                    {
                                        ProgressBar.Value = (double)tabelaAtual / totalTabelas * 100;
                                        ProgressBarMessage.Text = $"Exportando dados da tabela {tabelaAtual} de {totalTabelas}...";
                                    });
                                }

                                workbook.SaveAs(saveFileDialog.FileName);
                            }
                        });

                        var log = new LogData
                        {
                            Data = DateTime.UtcNow,
                            Tipo = "INFORMATIVO",
                            Nivel = "Usuário",
                            Detalhes = "Exportação de Dados",
                            Usuario = MainWindow.UsuarioLogado.Nome
                        };
                        await LogHistorico.RegistrarLogAsync(log);

                        MessageBox.Show("Dados exportados com sucesso como Excel!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao exportar dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                        AlertaCache.AdicionarAlerta("Erro",
                                                    ex.Message.ToString(),
                                                    $"Erro ao exportar dados. Possíveis motivos:\n" +
                                                    "- Problemas ao salvar o arquivo;\n" +
                                                    "- Dados corrompidos;\n" +
                                                    "- Erro ao acessar os dados.",
                                                    "- Verifique se o arquivo realmente foi salvo;\n" +
                                                    "- Verifique se os dados estão corretos.");
                    }
                }

                // Limpa tabelas selecionadas
                tabelasSelecionadas.Clear();

                ShowProgressBar.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao iniciar exportação de dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            $"Erro ao iniciar exportação de dados. Possíveis motivos:\n" +
                                            "- Função de exportação removida por terceiros;\n" +
                                            "- Tabela de dados vazia ou corrompida.",
                                            "- Verifique se a função de exportação está disponível;\n" +
                                            "- Verifique se há dados disponíveis para exportação.");

                ShowProgressBar.Visibility = Visibility.Collapsed;
            }
        }


        // Evento disparado quando o botão de importar é clicado
        private void ImportarDados_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Lê o arquivo de importação
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    Title = "Importar dados do Excel"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    ImportConfigPopup.Tag = openFileDialog.FileName; // Salva o caminho do arquivo no Tag do popup
                }

                // Adiciona tabelas do arquivo ao ListBox
                ImportTabelasListBox.Items.Clear();
                using (var workbook = new XLWorkbook(openFileDialog.FileName))
                {
                    foreach (var worksheet in workbook.Worksheets)
                    {
                        ImportTabelasListBox.Items.Add(worksheet.Name);
                    }
                }

                // Abre o popup de configuração de importação
                ImportConfigPopup.IsOpen = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao iniciar importação de dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            $"Erro ao iniciar importação de dados. Possíveis motivos:\n" +
                                            "- Função de importação removida por terceiros;\n" +
                                            "- Tabela de dados vazia ou corrompida.",
                                            "- Verifique se a função de importação está disponível;\n" +
                                            "- Verifique se há dados disponíveis para importação.");

                ShowProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        // Evento disparado ao confirmar a importação no popup
        private async void ConfirmarImportacao_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string? filePath = ImportConfigPopup.Tag as string; // Recupera o caminho do arquivo do Tag do popup
                var selectedOption = ImportComboBox.SelectedItem as ComboBoxItem;

                if (filePath == null)
                {
                    MessageBox.Show("Caminho do arquivo não encontrado.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Preenche a lista de tabelas selecionadas
                foreach (var item in ImportTabelasListBox.SelectedItems)
                {
                    tabelasSelecionadas.Add(item.ToString());
                }


                if (selectedOption != null)
                {
                    if (selectedOption.Content.ToString() == "Substituir Dados")
                    {
                        ImportConfigPopup.IsOpen = false;
                        await SubstituirTodosOsDadosAsync(filePath);
                        tabelasSelecionadas.Clear();
                    }
                    else if (selectedOption.Content.ToString() == "Adicionar Dados")
                    {
                        ImportConfigPopup.IsOpen = false;
                        await AdicionarNovosDadosAsync(filePath);
                        tabelasSelecionadas.Clear();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao iniciar importação de dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            $"Erro ao iniciar importação de dados. Possíveis motivos:\n" +
                                            "- Função de importação removida por terceiros;\n" +
                                            "- Tabela de dados vazia ou corrompida.",
                                            "- Verifique se a função de importação está disponível;\n" +
                                            "- Verifique se há dados disponíveis para importação.");
            }
        }


        // Função para ou substituir todos os dados ou adicionar novos dados
        private async Task SubstituirTodosOsDadosAsync(string filePath)
        {
            var db = DatabaseConnect.Database;

            try
            {
                ShowProgressBar.Visibility = Visibility.Visible;
                ProgressBar.Value = 0;
                ProgressBarMessage.Text = "Iniciando a substituição dos dados...";

                using (var workbook = new XLWorkbook(filePath))
                {
                    // Apagar todas as tabelas do banco de dados
                    var collections = await db.ListRootCollectionsAsync().ToListAsync();
                    foreach (var collection in collections)
                    {
                        var snapshot = await collection.GetSnapshotAsync();
                        foreach (var doc in snapshot.Documents)
                        {
                            await doc.Reference.DeleteAsync();
                        }
                    }

                    // Incluir novas tabelas a partir do arquivo Excel
                    foreach (var worksheet in workbook.Worksheets)
                    {
                        var data = new List<Dictionary<string, object>>();
                        var firstRow = worksheet.FirstRowUsed();
                        if (firstRow != null)
                        {
                            var headers = firstRow.Cells().Select(cell => cell.GetValue<string>()).ToList();

                            foreach (var row in worksheet.RowsUsed().Skip(1))
                            {
                                var rowData = new Dictionary<string, object>();
                                for (int i = 0; i < headers.Count; i++)
                                {
                                    var cellValue = row.Cell(i + 1).GetValue<string>();
                                    if (double.TryParse(cellValue, out double numericValue))
                                    {
                                        rowData[headers[i]] = numericValue;
                                    }
                                    else
                                    {
                                        rowData[headers[i]] = cellValue;
                                    }
                                }
                                data.Add(rowData);
                            }
                        }
                        else
                        {
                            MessageBox.Show($"A planilha '{worksheet.Name}' está vazia ou não contém uma linha de cabeçalho.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                            AlertaCache.AdicionarAlerta("Erro",
                                                        $"Planilha '{worksheet.Name}' vazia ou sem cabeçalho.",
                                                        $"Erro ao substituir dados. Possíveis motivos:\n" +
                                                        "- Planilha vazia;\n" +
                                                        "- Planilha sem cabeçalho;\n" +
                                                        "- Erro ao acessar os dados.",
                                                        "- Verifique se a planilha contém dados;\n" +
                                                        "- Verifique se a planilha contém um cabeçalho.");

                            continue;
                        }

                        ProgressBarMessage.Text = $"Adicionando novos dados na tabela '{worksheet.Name}'...";
                        int totalItems = data.Count;
                        int processedItems = 0;

                        foreach (var item in data)
                        {
                            if (item.ContainsKey("Codigo") && item["Codigo"] != null)
                            {
                                string? codigo = item["Codigo"]?.ToString();
                                if (!string.IsNullOrEmpty(codigo))
                                {
                                    var docRef = db.Collection(worksheet.Name).Document(codigo);
                                    await docRef.SetAsync(item);
                                }
                            }

                            processedItems++;
                            ProgressBar.Value = (double)processedItems / totalItems * 100;
                            ProgressBarMessage.Text = $"Processando item {processedItems} de {totalItems} na tabela '{worksheet.Name}'...";
                        }
                    }
                }

                MessageBox.Show("Todos os dados foram substituídos com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao substituir dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            $"Erro ao substituir dados. Possíveis motivos:\n" +
                                            "- Dados corrompidos;\n" +
                                            "- Erro ao acessar os dados;\n" +
                                            "- Problemas ao salvar os dados.",
                                            "- Verifique se os dados estão corretos;\n" +
                                            "- Verifique se o arquivo foi salvo corretamente.");
            }
            finally
            {
                ShowProgressBar.Visibility = Visibility.Collapsed;
                ProgressBarMessage.Text = "Processo concluído.";
            }
        }

        private async Task AdicionarNovosDadosAsync(string filePath)
        {
            var db = DatabaseConnect.Database;

            try
            {
                ShowProgressBar.Visibility = Visibility.Visible;
                ProgressBar.Value = 0;
                ProgressBarMessage.Text = "Iniciando a adição dos dados...";

                using (var workbook = new XLWorkbook(filePath))
                {
                    foreach (var tabela in tabelasSelecionadas)
                    {
                        var worksheet = workbook.Worksheet(tabela);
                        if (worksheet == null)
                        {
                            MessageBox.Show($"A planilha '{tabela}' não foi encontrada.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                            AlertaCache.AdicionarAlerta("Erro",
                                                        $"Planilha '{tabela}' não encontrada.",
                                                        $"Erro ao adicionar dados. Possíveis motivos:\n" +
                                                        $"- Planilha '{tabela}' não encontrada;\n" +
                                                        "- Erro ao acessar os dados.",
                                                        "- Verifique se a planilha está presente.");

                            continue;
                        }

                        var data = new List<Dictionary<string, object>>();
                        var firstRow = worksheet.FirstRowUsed();
                        if (firstRow != null)
                        {
                            var headers = firstRow.Cells().Select(cell => cell.GetValue<string>()).ToList();

                            foreach (var row in worksheet.RowsUsed().Skip(1))
                            {
                                var rowData = new Dictionary<string, object>();
                                for (int i = 0; i < headers.Count; i++)
                                {
                                    var cellValue = row.Cell(i + 1).GetValue<string>();
                                    if (double.TryParse(cellValue, out double numericValue))
                                    {
                                        rowData[headers[i]] = numericValue;
                                    }
                                    else
                                    {
                                        rowData[headers[i]] = cellValue;
                                    }
                                }
                                data.Add(rowData);
                            }
                        }
                        else
                        {
                            MessageBox.Show($"A planilha '{tabela}' está vazia ou não contém uma linha de cabeçalho.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                            AlertaCache.AdicionarAlerta("Erro",
                                                        $"Planilha '{tabela}' vazia ou sem cabeçalho.",
                                                        $"Erro ao adicionar dados. Possíveis motivos:\n" +
                                                        "- Planilha vazia;\n" +
                                                        "- Planilha sem cabeçalho;\n" +
                                                        "- Erro ao acessar os dados.",
                                                        "- Verifique se a planilha contém dados;\n" +
                                                        "- Verifique se a planilha contém um cabeçalho.");

                            continue;
                        }

                        ProgressBar.Value = 0;
                        int totalItems = data.Count;
                        int processedItems = 0;

                        ProgressBarMessage.Text = $"Adicionando novos dados na tabela '{tabela}'...";
                        foreach (var item in data)
                        {
                            if (item.ContainsKey("Codigo") && item["Codigo"] != null)
                            {
                                string? codigo = item["Codigo"]?.ToString();
                                if (!string.IsNullOrEmpty(codigo))
                                {
                                    var docRef = db.Collection(tabela).Document(codigo);
                                    var docSnapshot = await docRef.GetSnapshotAsync();

                                    if (!docSnapshot.Exists)
                                    {
                                        await docRef.SetAsync(item);
                                    }
                                }
                            }

                            processedItems++;
                            ProgressBar.Value = (double)processedItems / totalItems * 100;
                            ProgressBarMessage.Text = $"Processando item {processedItems} de {totalItems} na tabela '{tabela}'...";
                        }
                    }
                }

                MessageBox.Show("Todos os dados foram adicionados com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao adicionar dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            $"Erro ao adicionar dados. Possíveis motivos:\n" +
                                            "- Dados corrompidos;\n" +
                                            "- Erro ao acessar os dados;\n" +
                                            "- Problemas ao salvar os dados.",
                                            "- Verifique se os dados estão corretos;\n" +
                                            "- Verifique se o arquivo foi salvo corretamente.");
            }
            finally
            {
                ShowProgressBar.Visibility = Visibility.Collapsed;
                ProgressBarMessage.Text = "Processo concluído.";
            }
        }


        // Evento disparado quando o botão de reconectar é clicado
        private async void Reconectar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                await Task.Run(() =>
                {
                    AtualizarCache();
                });
                MessageBox.Show("Reconexão realizada com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao reconectar: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            $"Erro ao reconectar. Possíveis motivos:\n" +
                                            "- Problemas de conexão com a internet;\n" +
                                            "- Serviço do banco de dados indisponível.",
                                            "- Verifique sua conexão com a internet;\n" +
                                            "- Verifique as configurações do banco de dados.");

                ShowProgressBar.Visibility = Visibility.Collapsed;
            }
        }

        // Evento disparado quando o botão de gerar tabela é clicado
        private async void GerarTabela_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                ShowProgressBar.Visibility = Visibility.Visible;
                ProgressBar.Value = 0;

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    Title = "Salvar estrutura das tabelas como Excel",
                    FileName = "radiadoreslemosdb-estrutura.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    try
                    {
                        await Task.Run(async () =>
                        {
                            using (var workbook = new XLWorkbook())
                            {
                                var db = DatabaseConnect.Database;
                                var collections = await db.ListRootCollectionsAsync().ToListAsync();
                                int totalTabelas = collections.Count;
                                int tabelaAtual = 0;

                                foreach (var collection in collections)
                                {
                                    var snapshot = await collection.GetSnapshotAsync();
                                    if (snapshot.Count > 0)
                                    {
                                        var firstDoc = snapshot.Documents.First();
                                        var properties = firstDoc.ToDictionary().Keys.ToList();
                                        var worksheet = workbook.Worksheets.Add(collection.Id);

                                        for (int i = 0; i < properties.Count; i++)
                                        {
                                            var cell = worksheet.Cell(1, i + 1);
                                            cell.Value = properties[i];
                                            cell.Style.Fill.BackgroundColor = XLColor.UltramarineBlue;
                                            cell.Style.Font.FontColor = XLColor.White;
                                            cell.Style.Font.Bold = true;
                                            cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                                        }

                                        worksheet.Columns().AdjustToContents();
                                    }

                                    tabelaAtual++;
                                    Dispatcher.Invoke(() =>
                                    {
                                        ProgressBar.Value = (double)tabelaAtual / totalTabelas * 100;
                                        ProgressBarMessage.Text = $"Gerando estrutura da tabela {tabelaAtual} de {totalTabelas}...";
                                    });
                                }

                                workbook.SaveAs(saveFileDialog.FileName);
                            }
                        });

                        var log = new LogData
                        {
                            Data = DateTime.UtcNow,
                            Tipo = "INFORMATIVO",
                            Nivel = "Usuário",
                            Detalhes = "Geração de Estrutura de Tabelas",
                            Usuario = MainWindow.UsuarioLogado.Nome
                        };
                        await LogHistorico.RegistrarLogAsync(log);

                        MessageBox.Show("Estrutura das tabelas gerada com sucesso como Excel!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao gerar estrutura das tabelas: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                        AlertaCache.AdicionarAlerta("Erro",
                                                    ex.Message.ToString(),
                                                    $"Erro ao gerar estrutura das tabelas. Possíveis motivos:\n" +
                                                    "- Problemas ao salvar o arquivo;\n" +
                                                    "- Erro ao acessar os dados.",
                                                    "- Verifique se o arquivo realmente foi salvo;\n" +
                                                    "- Verifique se os dados estão corretos.");
                    }
                }

                ShowProgressBar.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao iniciar geração de estrutura das tabelas: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            $"Erro ao iniciar geração de estrutura das tabelas. Possíveis motivos:\n" +
                                            "- Função de geração removida por terceiros;\n" +
                                            "- Problemas ao acessar os dados.",
                                            "- Verifique se a função de geração está disponível;\n" +
                                            "- Verifique se há dados disponíveis.");
                ShowProgressBar.Visibility = Visibility.Collapsed;
            }
        }



        // Método para atualizar o cache de dados
        private async void AtualizarCache()
        {
            try
            {
                // Configura o ambiente para conectar ao Firestore
                DatabaseConnect.SetEnvironmentVarible();

                // Obtém a instância do Firestore
                var db = DatabaseConnect.Database;

                if (db == null)
                {
                    MessageBox.Show("Não foi possível conectar ao Firestore.");
                    return;
                }

                // Limpa o cache atual
                DadosCache.Tabelas.Clear();

                // Obtém todas as coleções do Firestore
                var colecoes = await db.ListRootCollectionsAsync().ToListAsync();

                foreach (var colecao in colecoes)
                {
                    var documentos = await colecao.ListDocumentsAsync().ToListAsync();
                    var dados = new List<object>();

                    foreach (var documento in documentos)
                    {
                        var snapshot = await documento.GetSnapshotAsync();
                        if (snapshot.Exists)
                        {
                            dados.Add(snapshot.ToDictionary());
                        }
                    }

                    // Adiciona os dados da coleção ao cache
                    DadosCache.Tabelas[colecao.Id] = dados;
                }

                dadosCarregados = true;
                MessageBox.Show("Cache atualizado com sucesso.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar o cache: {ex.Message}");
            }
        }
    }
}