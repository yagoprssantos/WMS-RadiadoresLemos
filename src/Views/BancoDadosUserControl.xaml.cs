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

namespace WMS_RadiadoresLemos_WPF
{
    public partial class BancoDadosUserControl : UserControl
    {
        private List<object> dadosFiltrados = new List<object>();
        private bool dadosCarregados = false;

        public BancoDadosUserControl()
        {
            InitializeComponent();
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
                // Log de erro ou exibição de mensagem para o usuário
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
                // Log de erro ou exibição de mensagem para o usuário
                Console.WriteLine($"Erro ao selecionar tabela: {ex.Message}");
            }
        }

        // Método para atualizar a tabela de dados com os dados do cache
        private void AtualizarTabelaDadosCache(string tabela)
        {
            if (DadosCache.Tabelas.TryGetValue(tabela, out List<object>? value))
            {
                dadosFiltrados = value;
                DadosDataGrid.ItemsSource = dadosFiltrados;
                dadosCarregados = true;
                RemoverUltimaColuna();
            }
        }

        // Método para remover a última coluna do DataGrid
        private void RemoverUltimaColuna()
        {
            if (DadosDataGrid.Columns.Count > 0)
            {
                DadosDataGrid.Columns.RemoveAt(DadosDataGrid.Columns.Count - 1);
            }
        }

        // Evento disparado quando o texto de pesquisa é alterado
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
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

        private async Task<List<object>> ObterDadosProdutosDoFirebaseAsync()
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
            }

            return produtos;
        }
        private async void ExportarDados_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            // Busca todos os dados da coleção "Produtos" do Firebase
            var dadosProdutos = await ObterDadosProdutosDoFirebaseAsync();

            if (dadosProdutos == null || !dadosProdutos.Any())
            {
                MessageBox.Show("Nenhum dado disponível para exportação.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Configura o local para salvar o arquivo
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
                    using (var workbook = new XLWorkbook())
                    {
                        foreach (var tabela in DadosCache.Tabelas.Keys)
                        {
                            var dadosTabela = DadosCache.Tabelas[tabela];
                            var worksheet = workbook.Worksheets.Add(tabela);

                            if (dadosTabela.Any())
                            {
                                // Escrever os cabeçalhos das colunas
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

                                // Escrever cada linha de dados
                                for (int i = 0; i < dadosTabela.Count; i++)
                                {
                                    var item = dadosTabela[i];
                                    for (int j = 0; j < properties.Length; j++)
                                    {
                                        var cell = worksheet.Cell(i + 2, j + 1);
                                        cell.Value = properties[j].GetValue(item, null)?.ToString() ?? "";
                                        cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;

                                        // Alternar cor de fundo entre branco e cinza claro
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

                                // Ajustar a largura das colunas
                                worksheet.Columns().AdjustToContents();
                            }
                        }

                        // Salvar o arquivo Excel
                        workbook.SaveAs(saveFileDialog.FileName);
                    }

                    MessageBox.Show("Dados exportados com sucesso como Excel!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao exportar dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
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
                // Log de erro ou exibição de mensagem para o usuário
                Console.WriteLine($"Erro ao atualizar DataGrid: {ex.Message}");
            }
        }
    }
}
