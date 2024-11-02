using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Controls;
using WMS_RadiadoresLemos_WPF.src.Models; // Certifique-se de que o namespace correto está sendo usado

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
