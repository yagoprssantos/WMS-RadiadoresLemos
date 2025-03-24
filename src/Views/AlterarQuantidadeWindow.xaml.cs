using System.Windows;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class AlterarQuantidadeWindow : Window
    {
        public int Quantidade { get; private set; }
        private bool isModified = false;

        public AlterarQuantidadeWindow(ProdutoData produto)
        {
            InitializeComponent();
            try
            {
                Quantidade = produto.Quantidade;
                QuantidadeTextBox.Text = Quantidade.ToString();
                QuantidadeTextBox.TextChanged += QuantidadeTextBox_TextChanged;
            }
            catch (Exception ex)
            {
                HandleException("Erro ao carregar os dados do produto", ex, "Não foi possível carregar os dados do produto. Possíveis motivos:\n" +
                    "- Produto não encontrado;\n" +
                    "- Dados do produto corrompidos;\n" +
                    "- Problemas de conexão com a internet.\n",
                    "Verifique se o produto existe;\n" +
                    "Verifique se os dados do produto estão corretos;\n" +
                    "Faça a reconexão com banco de dados.");
                Close();
            }
        }

        private void QuantidadeTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            isModified = true;
        }

        private void Salvar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (int.TryParse(QuantidadeTextBox.Text, out int novaQuantidade) && novaQuantidade >= 0)
                {
                    Quantidade = novaQuantidade;
                    DialogResult = true;

                    isModified = false;

                    Dispatcher.InvokeAsync(Close);
                }
                else
                {
                    MessageBox.Show("Por favor, insira um valor numérico válido para a quantidade.", "Erro de Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                HandleException("Erro ao salvar a nova quantidade", ex, "Não foi possível salvar a nova quantidade. Possíveis motivos:\n" +
                    "- Valor inserido não é numérico;\n" +
                    "- Valor inserido é negativo;\n" +
                    "- Quantidade do produto corrompida.\n",
                    "Verifique se o valor inserido é numérico;\n" +
                    "Verifique se a quantidade do produto está correta e atualizada;\n" +
                    "Insira um valor que seja válido.");
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            if (isModified)
            {
                var result = MessageBox.Show("Você tem certeza que deseja sair sem salvar?", "Confirmação de Saída", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    return;
                }
            }
            DialogResult = false;
            Close();
        }

        private void QuantidadeTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out _);
        }

        private void HandleException(string title, Exception ex, string message, string suggestions)
        {
            //MessageBox.Show($"{title}: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            AlertaCache.AdicionarAlerta("Erro", ex.Message, message, suggestions);
        }
    }
}
