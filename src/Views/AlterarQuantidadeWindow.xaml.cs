using System.Windows;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class AlterarQuantidadeWindow : Window
    {
        public int Quantidade { get; private set; }

        public AlterarQuantidadeWindow(ProdutoData produto)
        {
            try
            {
                InitializeComponent();
                Quantidade = produto.Quantidade;
                QuantidadeTextBox.Text = Quantidade.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar os dados do produto: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                // Adiciona alerta
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Não foi possível carregar os dados do produto. Possíveis motivos:\n" +
                                            "- Produto não encontrado;\n" +
                                            "- Dados do produto corrompidos;\n" +
                                            "- Problemas de conexão com a internet.\n",
                                            "- Verifique se o produto existe;\n" +
                                            "- Verifique se os dados do produto estão corretos;\n" +
                                            "- Faça a reconexão com banco de dados.");
                Close();
            }
        }

        private void Salvar_Click(object sender, RoutedEventArgs e)
        {
            try
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
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar a nova quantidade: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                // Adiciona alerta
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Não foi possível salvar a nova quantidade. Possíveis motivos:\n" +
                                            "- Valor inserido não é numérico;\n" +
                                            "- Valor inserido é negativo;\n" +
                                            "- Quantidade do produto corrompida.\n",
                                            "- Verifique se o valor inserido é numérico;\n" +
                                            "- Verifique se a quantidade do produto está correta e atualizada;\n" +
                                            "- Insira um valor que seja válido.");
            }
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                DialogResult = false; // Cancela a alteração
                Close();
            }
            catch (Exception)
            {
                // Força o fechamento da janela em caso de erro
                Close();
            }
        }

        private void QuantidadeTextBox_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            try
            {
                e.Handled = !int.TryParse(e.Text, out _); // Apenas números são permitidos
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao processar a entrada de texto: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                // Adiciona alerta
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Erro ao processar a entrada de texto. Possíveis motivos:\n" +
                                            "- Valor inserido não é numérico;\n" +
                                            "- Existem espaços ou caracteres inválidos.\n",
                                            "- Verifique se o valor inserido é numérico;\n" +
                                            "- Verifique se existem espaços ou caracteres inválidos.");
            }
        }
    }
}
