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
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message,
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
                                            ex.Message,
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
                                            ex.Message,
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
                                            ex.Message,
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
                                            ex.Message,
                                            $"Erro ao filtrar dados. Possíveis motivos:\n" +
                                            "- Sistema com bugs;\n" +
                                            "- Caracteres inválidos na pesquisa;\n" +
                                            "- Problema ao carregar dados.",
                                            "- Feche e abra a tela de Banco de Dados;\n" +
                                            "- Tente reiniciar a aplicação.");

                Console.WriteLine($"Erro ao filtrar dados: {ex.Message}");
            }
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
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message,
                                            $"Erro ao obter dados de Produtos. Possíveis motivos:\n" +
                                            "- Problemas de conexão com a internet;\n" +
                                            "- Configurações incorretas do banco de dados;\n" +
                                            "- Serviço do banco de dados indisponível.",
                                            "- Verifique sua conexão com a internet;\n" +
                                            "- Verifique as configurações do banco de dados.");

                MessageBox.Show($"Erro ao obter dados de Produtos: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return produtos;
        }

        private async void ExportarDados_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
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

                        // Adiciona log
                        var log = new LogData
                        {
                            Data = DateTime.UtcNow,
                            Tipo = "INFORMATIVO",
                            Nivel = "Usuário",
                            Detalhes = "Exportação de Dados",
                            Usuario = "NomeDoUsuario" // Substitua pelo nome do usuário real
                        };
                        await LogHistorico.RegistrarLogAsync(log);

                        MessageBox.Show("Dados exportados com sucesso como Excel!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        AlertaCache.AdicionarAlerta("Erro",
                                                    ex.Message,
                                                    $"Erro ao exportar dados. Possíveis motivos:\n" +
                                                    "- Problemas ao salvar o arquivo;\n" +
                                                    "- Dados corrompidos;\n" +
                                                    "- Erro ao acessar os dados.",
                                                    "- Verifique se o arquivo realmente foi salvo;\n" +
                                                    "- Verifique se os dados estão corretos.");

                        MessageBox.Show($"Erro ao exportar dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message,
                                            $"Erro ao iniciar exportação de dados. Possíveis motivos:\n" +
                                            "- Função de exportação removida por terceiros;\n" +
                                            "- Tabela de dados vazia ou corrompida.",
                                            "- Verifique se a função de exportação está disponível;\n" +
                                            "- Verifique se há dados disponíveis para exportação.");

                MessageBox.Show($"Erro ao iniciar exportação de dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
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
                                            ex.Message,
                                            $"Erro ao atualizar tabela de dados. Possíveis motivos:\n" +
                                            "- Componentes da interface corrompidos;\n" +
                                            "- Erro ao reconhecer a tabela selecionada;\n" +
                                            "- Serviço do banco de dados indisponível.",
                                            "- Feche e abra a tela de Banco de Dados;\n" +
                                            "- Reinicie a aplicação.");

                Console.WriteLine($"Erro ao atualizar DataGrid: {ex.Message}");
            }
        }
    }
}
