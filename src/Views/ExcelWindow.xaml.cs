using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
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

        // Importa os dados do Excel para pré-visualização
        private async void ImportarDados_Click(object sender, RoutedEventArgs e)
        {
            ResetarProgresso();

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
                        int totalTabelas = TabelasDisponiveis.Length;
                        int tabelaAtual = 0;

                        foreach (var tabela in TabelasDisponiveis)
                        {
                            var worksheet = workbook.Worksheet(tabela);
                            if (worksheet != null)
                            {
                                TabelaAtual = tabela;
                                DadosPreVisualizacao.Clear();

                                var headers = worksheet.FirstRowUsed().Cells().Select(c => c.Value.ToString()).ToList();
                                foreach (var row in worksheet.RowsUsed().Skip(1))
                                {
                                    var document = new BsonDocument();
                                    for (int i = 0; i < headers.Count; i++)
                                    {
                                        document[headers[i]] = row.Cell(i + 1).Value.ToString();
                                    }
                                    DadosPreVisualizacao.Add(document);
                                }

                                // Atualiza o DataGrid com os dados para pré-visualização
                                PreviewDataGrid.ItemsSource = DadosPreVisualizacao.Select(d => d.ToDictionary(k => k.Key, v => v.Value));
                                tabelaAtual++;
                                AtualizarProgresso((double)tabelaAtual / totalTabelas * 100, $"Status: Dados carregados para pré-visualização da tabela '{tabela}'.");
                                await Task.Delay(500); // Simula um pequeno atraso para visualização do progresso
                            }
                        }

                        if (tabelaAtual == 0)
                        {
                            MessageBox.Show("Nenhuma tabela correspondente foi encontrada no arquivo Excel.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao importar dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Confirma a importação dos dados para o banco de dados
        private async void ConfirmarImportacao_Click(object sender, RoutedEventArgs e)
        {
            ResetarProgresso();

            try
            {
                if (string.IsNullOrEmpty(TabelaAtual) || !DadosPreVisualizacao.Any())
                {
                    MessageBox.Show("Nenhum dado para importar. Por favor, importe os dados antes de confirmar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var collection = DatabaseConnect.Database.GetCollection<BsonDocument>(TabelaAtual);
                collection.DeleteAll(); // Remove os dados existentes na tabela
                collection.InsertBulk(DadosPreVisualizacao); // Insere os novos dados

                AtualizarProgresso(100, $"Status: Dados confirmados e importados para a tabela '{TabelaAtual}'.");
                await Task.Delay(500); // Simula um pequeno atraso para visualização do progresso

                MessageBox.Show($"Dados importados com sucesso para a tabela '{TabelaAtual}'.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);

                // Limpa a pré-visualização
                PreviewDataGrid.ItemsSource = null;
                DadosPreVisualizacao.Clear();
                TabelaAtual = string.Empty;
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
                            var collection = DatabaseConnect.Database.GetCollection<BsonDocument>(tabela);
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
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao gerar tabela padrão: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
