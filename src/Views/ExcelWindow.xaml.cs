using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using ClosedXML.Excel;
using LiteDB;
using Microsoft.Win32;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class ExcelWindow : Window
    {
        private static readonly string[] TabelasDisponiveis = { "usuarios", "produtos", "historico", "movimentacoes" };
        private List<BsonDocument> DadosPreVisualizacao = new List<BsonDocument>();
        private string TabelaAtual = string.Empty;

        public ExcelWindow()
        {
            InitializeComponent();
            CarregarTabelasDisponiveis();
        }

        // Preenche o ComboBox com as tabelas disponíveis
        private void CarregarTabelasDisponiveis()
        {
            TabelaComboBox.ItemsSource = TabelasDisponiveis;
            TabelaComboBox.SelectedIndex = 0; // Seleciona a primeira tabela por padrão
        }

        // Evento disparado ao alterar a tabela selecionada no ComboBox
        private void TabelaComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabelaComboBox.SelectedItem is string tabelaSelecionada)
            {
                AtualizarPreviewDataGrid(tabelaSelecionada);
            }
        }

        // Atualiza o DataGrid com os dados da tabela selecionada
        private void AtualizarPreviewDataGrid(string tabela)
        {
            TabelaAtual = tabela;
            var collection = DatabaseConnect.Database?.GetCollection<BsonDocument>(tabela);

            if (collection != null)
            {
                var dadosTabela = collection.FindAll().ToList();
                PreviewDataGrid.ItemsSource = dadosTabela.Select(d => d.ToDictionary(k => k.Key, v => v.Value));
            }
            else
            {
                PreviewDataGrid.ItemsSource = null;
                MessageBox.Show($"A tabela '{tabela}' não possui dados disponíveis.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Importa os dados do Excel para o DataGrid
        private async void ImportarDados_Click(object sender, RoutedEventArgs e)
        {
            // Reseta a barra de progresso e a mensagem de status
            ResetarProgresso();

            // Esconde a pré-visualização dos dados
            PreviewPanel.Visibility = Visibility.Collapsed;

            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    Title = "Importar dados do Excel"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    using (var workbook = new XLWorkbook(openFileDialog.FileName))
                    {
                        List<string> abasInvalidas = new List<string>();
                        int totalTabelas = TabelasDisponiveis.Length;
                        int tabelaAtual = 0;

                        foreach (var tabela in TabelasDisponiveis)
                        {
                            var worksheet = workbook.Worksheets.FirstOrDefault(ws => ws.Name.Equals(tabela, StringComparison.OrdinalIgnoreCase));
                            if (worksheet != null && ValidarFormatoAba(worksheet, tabela))
                            {
                                ProcessarAba(worksheet, tabela);
                                tabelaAtual++;
                                AtualizarProgresso((double)tabelaAtual / totalTabelas * 100, $"Status: Dados carregados para pré-visualização da tabela '{tabela}'.");
                                await Task.Delay(500); // Simula um pequeno atraso para visualização do progresso
                            }
                            else
                            {
                                abasInvalidas.Add(tabela);
                                AtualizarProgresso((double)tabelaAtual / totalTabelas * 100, $"Status: Aba '{tabela}' ignorada (formato inválido).");
                            }
                        }

                        ExibirResultadosImportacao(abasInvalidas, tabelaAtual);

                        // Mostra a pré-visualização dos dados
                        PreviewPanel.Visibility = Visibility.Visible;
                    }
                }
            }
            catch (Exception ex)
            {
                // Esconde a pré-visualização dos dados
                PreviewPanel.Visibility = Visibility.Collapsed;

                MessageBox.Show($"Erro ao importar dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Processa uma aba válida do Excel
        private void ProcessarAba(IXLWorksheet worksheet, string tabela)
        {
            TabelaAtual = tabela;
            DadosPreVisualizacao.Clear();

            var headers = worksheet.FirstRowUsed()?.Cells().Select(c => NormalizarTexto(c.Value.ToString())).ToList() ?? new List<string>();
            var propriedades = ObterPropriedadesDoModelo(tabela);

            foreach (var row in worksheet.RowsUsed().Skip(1))
            {
                var document = new BsonDocument();
                for (int i = 0; i < headers.Count; i++)
                {
                    string header = headers[i];
                    if (propriedades.Contains(header))
                    {
                        string value = row.Cell(i + 1).Value.ToString();
                        document[header] = value;
                    }
                }
                DadosPreVisualizacao.Add(document);
            }

            // Atualiza o DataGrid com os dados para pré-visualização
            PreviewDataGrid.ItemsSource = DadosPreVisualizacao.Select(d => d.ToDictionary(k => k.Key, v => v.Value));
        }

        private List<string> ObterPropriedadesDoModelo(string tabela)
        {
            Type? tipoModelo = tabela.ToLower() switch
            {
                "usuarios" => typeof(UsuarioData),
                "produtos" => typeof(ProdutoData),
                "movimentacoes" => typeof(MovimentacaoData),
                "historico" => typeof(LogData),
                "alertas" => typeof(AlertaData),
                _ => null
            };

            if (tipoModelo == null)
            {
                Console.WriteLine($"Tabela '{tabela}' não possui um modelo correspondente.");
                return new List<string>();
            }

            // Obtém os nomes das propriedades do modelo
            return tipoModelo.GetProperties()
                             .Select(p => NormalizarTexto(p.Name))
                             .ToList();
        }

        private string NormalizarTexto(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

            // Remove espaços, acentos e converte para minúsculas
            return new string(texto
                .Normalize(NormalizationForm.FormD)
                .Where(c => char.GetUnicodeCategory(c) != System.Globalization.UnicodeCategory.NonSpacingMark)
                .ToArray())
                .Replace(" ", "")
                .ToLower();
        }

        // Exibe os resultados da importação
        private void ExibirResultadosImportacao(List<string> abasInvalidas, int tabelasValidas)
        {
            if (abasInvalidas.Any())
            {
                MessageBox.Show($"As seguintes abas foram ignoradas por formato inválido:\n{string.Join(", ", abasInvalidas)}", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }

            if (tabelasValidas == 0)
            {
                MessageBox.Show("Nenhuma tabela válida foi encontrada no arquivo Excel.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            else
            {
                PreviewPanel.Visibility = Visibility.Visible;
                MessageBox.Show("Importação concluída com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        // Valida o formato da aba do Excel
        private bool ValidarFormatoAba(IXLWorksheet worksheet, string tabela)
        {
            try
            {
                // Obtém os cabeçalhos da primeira linha
                var headers = worksheet.FirstRowUsed()?.Cells().Select(c => NormalizarTexto(c.Value.ToString())).ToList();

                if (headers == null || !headers.Any())
                {
                    MessageBox.Show($"A aba '{worksheet.Name}' não possui cabeçalhos.");
                    return false;
                }

                // Obtém os nomes das propriedades do modelo correspondente
                var propriedades = ObterPropriedadesDoModelo(tabela);

                // Verifica se pelo menos um cabeçalho corresponde a uma propriedade do modelo
                if (headers.Any(h => propriedades.Contains(h)))
                {
                    return true;
                }

                MessageBox.Show($"A aba '{worksheet.Name}' possui cabeçalhos inválidos: {string.Join(", ", headers)}");
                return false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao validar a aba '{worksheet.Name}': {ex.Message}");
                return false;
            }
        }

        // Confirma a importação dos dados para o banco de dados
        private void ConfirmarImportacao_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrEmpty(TabelaAtual) || !DadosPreVisualizacao.Any())
                {
                    MessageBox.Show("Nenhum dado para importar. Por favor, importe os dados antes de confirmar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var collection = DatabaseConnect.Database?.GetCollection<BsonDocument>(TabelaAtual);

                if (collection == null)
                {
                    MessageBox.Show("Erro ao acessar a coleção do banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Verifica a opção de importação selecionada no ComboBox
                var selectedMode = (ImportModeComboBox.SelectedItem as ComboBoxItem)?.Content.ToString();
                if (selectedMode == "Adicionar Dados")
                {
                    collection.InsertBulk(DadosPreVisualizacao); // Adiciona os novos dados
                }
                else if (selectedMode == "Substituir Dados")
                {
                    collection.DeleteAll(); // Remove os dados existentes
                    collection.InsertBulk(DadosPreVisualizacao); // Insere os novos dados
                }

                StatusMessage.Text = $"Status: Dados confirmados e importados para a tabela '{TabelaAtual}'.";
                MessageBox.Show($"Dados importados com sucesso para a tabela '{TabelaAtual}'.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);

                // Limpa a pré-visualização
                PreviewDataGrid.ItemsSource = null;
                DadosPreVisualizacao.Clear();
                TabelaAtual = string.Empty;
                PreviewPanel.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao confirmar a importação: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Exporta os dados do banco de dados para o Excel
        private async void ExportarDados_Click(object sender, RoutedEventArgs e)
        {
            ResetarProgresso();

            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    Title = "Exportar dados para Excel",
                    FileName = "dados-exportados.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        int totalTabelas = TabelasDisponiveis.Length;
                        int tabelaAtual = 0;

                        foreach (var tabela in TabelasDisponiveis)
                        {
                            var collection = DatabaseConnect.Database?.GetCollection<BsonDocument>(tabela);
                            if (collection == null)
                            {
                                MessageBox.Show($"Erro ao acessar a coleção '{tabela}' do banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                                continue;
                            }

                            var dadosTabela = collection.FindAll().ToList();
                            var worksheet = workbook.Worksheets.Add(tabela);

                            if (dadosTabela.Any())
                            {
                                // Adiciona cabeçalhos dinamicamente
                                var headers = dadosTabela.First().Keys.ToList();
                                for (int i = 0; i < headers.Count; i++)
                                {
                                    var cell = worksheet.Cell(1, i + 1);
                                    cell.Value = headers[i];
                                    cell.Style.Font.Bold = true;
                                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#6680E8"); // Cor alterada
                                    cell.Style.Font.FontColor = XLColor.White;
                                    cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                                }

                                // Adiciona os dados
                                for (int i = 0; i < dadosTabela.Count; i++)
                                {
                                    var item = dadosTabela[i];
                                    for (int j = 0; j < headers.Count; j++)
                                    {
                                        var cell = worksheet.Cell(i + 2, j + 1);
                                        var value = item[headers[j]]?.ToString().Replace("\"", "") ?? "";

                                        // Formata o campo de data, se aplicável
                                        if (headers[j].ToLower().Contains("data") && DateTime.TryParse(value, out var dateValue))
                                        {
                                            value = dateValue.ToString("dd/MM/yyyy HH:mm:ss");
                                        }

                                        cell.Value = value;
                                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                                        // Alterna a cor de fundo das linhas
                                        cell.Style.Fill.BackgroundColor = i % 2 == 0 ? XLColor.White : XLColor.FromHtml("#F0F0F0");
                                    }
                                }

                                // Ajusta as colunas
                                worksheet.Columns().AdjustToContents();
                            }

                            tabelaAtual++;
                            AtualizarProgresso((double)tabelaAtual / totalTabelas * 100, $"Status: Exportando tabela '{tabela}'.");
                            await Task.Delay(500); // Simula um pequeno atraso para visualização do progresso
                        }

                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    AtualizarProgresso(100, "Status: Dados exportados com sucesso!");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao exportar dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        // Gera uma tabela padrão com cabeçalhos dinâmicos
        private void GerarTabela_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Filter = "Excel Workbook (*.xlsx)|*.xlsx",
                    Title = "Gerar tabela padrão",
                    FileName = "tabela-padrao.xlsx"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (var workbook = new XLWorkbook())
                    {
                        foreach (var tabela in TabelasDisponiveis)
                        {
                            var worksheet = workbook.Worksheets.Add(tabela);

                            // Obtem os cabeçalhos dinamicamente com base no tipo de dado
                            var headers = GetHeadersForTable(tabela);

                            // Adiciona cabeçalhos
                            for (int i = 0; i < headers.Count; i++)
                            {
                                var cell = worksheet.Cell(1, i + 1);
                                cell.Value = headers[i];
                                cell.Style.Font.Bold = true;
                                cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#6680E8"); // Cor alterada
                                cell.Style.Font.FontColor = XLColor.White;
                                cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
                                cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                            }

                            // Ajusta as colunas
                            worksheet.Columns().AdjustToContents();
                        }

                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    StatusMessage.Text = "Status: Tabela padrão gerada com sucesso!";
                    MessageBox.Show("Tabela padrão gerada com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao gerar tabela padrão: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Reseta a barra de progresso
        private void ResetarProgresso()
        {
            ProgressBar.Value = 0;
            StatusMessage.Text = "Status: Aguardando ação...";
        }

        // Atualiza a barra de progresso
        private void AtualizarProgresso(double progresso, string mensagem)
        {
            ProgressBar.Value = progresso;
            StatusMessage.Text = mensagem;
        }

        // Evento disparado ao alterar o texto na barra de pesquisa
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (PreviewDataGrid.ItemsSource is IEnumerable<Dictionary<string, object>> dados)
            {
                string filtro = SearchBox.Text.ToLower();
                PreviewDataGrid.ItemsSource = dados
                    .Where(d => d.Values.Any(v => v.ToString().ToLower().Contains(filtro)))
                    .ToList();
            }
        }

        // Evento disparado ao clicar no botão "Filtrar por"
        private void FiltrarButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Funcionalidade de filtro ainda não implementada.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Evento disparado ao clicar no botão "Cancelar"
        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            // Limpa a pré-visualização e reseta o estado
            PreviewDataGrid.ItemsSource = null;
            DadosPreVisualizacao.Clear();
            TabelaAtual = string.Empty;
            PreviewPanel.Visibility = Visibility.Collapsed;
            StatusMessage.Text = "Status: Ação cancelada.";
            MessageBox.Show("Ação cancelada com sucesso.", "Informação", MessageBoxButton.OK, MessageBoxImage.Information);
        }


        // Obtém os cabeçalhos dinamicamente com base no tipo de dado
        private List<string> GetHeadersForTable(string tabela)
        {
            return tabela.ToLower() switch
            {
                "usuarios" => typeof(UsuarioData).GetProperties().Select(p => p.Name).ToList(),
                "movimentacoes" => typeof(MovimentacaoData).GetProperties().Select(p => p.Name).ToList(),
                "historico" => typeof(LogData).GetProperties().Select(p => p.Name).ToList(),
                "alertas" => typeof(AlertaData).GetProperties().Select(p => p.Name).ToList(),
                _ => new List<string> { "Id", "Nome", "Descrição" }
            };
        }
    }
}
