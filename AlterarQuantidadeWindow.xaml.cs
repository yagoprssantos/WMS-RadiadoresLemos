using System.Windows;
using WMS_RadiadoresLemos_WPF.Classes;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class AlterarQuantidadeWindow : Window
    {
        public int Quantidade { get; private set; }

        public AlterarQuantidadeWindow(ProdutoData produto)
        {
            InitializeComponent();
            Quantidade = produto.Quantidade;
            QuantidadeTextBox.Text = Quantidade.ToString();
        }

        private void Salvar_Click(object sender, RoutedEventArgs e)
        {
            // Valida e salva a nova quantidade
            if (int.TryParse(QuantidadeTextBox.Text, out int novaQuantidade) && novaQuantidade >= 0)
            {
                Quantidade = novaQuantidade;
                DialogResult = true; // Define o resultado como "OK" para confirmar o salvamento
                Close();
            }
            else
            {
                MessageBox.Show("Por favor, insira um valor numérico válido para a quantidade.", "Erro de Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // Cancela a alteração
            Close();
        }

        private void QuantidadeTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _); // Apenas números são permitidos
        }
    }
}
