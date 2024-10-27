using System.Windows.Controls;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class BancoDadosUserControl : UserControl
    {
        public BancoDadosUserControl()
        {
            InitializeComponent();
            CarregarTabelas();
        }

        // Método para carregar as tabelas no ComboBox
        private void CarregarTabelas()
        {
            // Implementação futura
        }

        // Evento disparado quando uma tabela é selecionada no ComboBox
        private void TabelaComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TabelaComboBox.SelectedItem != null)
            {
                string tabelaSelecionada = TabelaComboBox.SelectedItem.ToString();
                CarregarDadosTabela(tabelaSelecionada);
            }
        }

        // Método para carregar os dados da tabela selecionada no DataGrid
        private void CarregarDadosTabela(string tabela)
        {
            // Implementação futura
        }
    }
}
