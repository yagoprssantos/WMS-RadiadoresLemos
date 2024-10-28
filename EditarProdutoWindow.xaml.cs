using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WMS_RadiadoresLemos_WPF.Classes;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class EditarProdutoWindow : Window
    {
        private ProdutoData produto;
        private bool isModified = false;
        private List<ProdutoData> produtos;

        // Propriedade pública para acessar o produto editado
        public ProdutoData Produto => produto;

        // Construtor que inicializa a janela com os dados do produto ou vazio
        public EditarProdutoWindow(ProdutoData? produto)
        {
            InitializeComponent();
            this.produto = produto ?? new ProdutoData
            {
                Nome = string.Empty,
                Tipo = string.Empty,
                Marca = string.Empty,
                Codigo = string.Empty
            };
            produtos = new List<ProdutoData>();
            PreencherCampos();
        }

        // Preenche os campos da interface com os dados do produto
        private void PreencherCampos()
        {
            try
            {
                NomeProduto.Text = produto.Nome;
                TipoProduto.Text = produto.Tipo;
                MarcaProduto.Text = produto.Marca;
                CodigoProduto.Text = produto.Codigo;
                QuantidadeInicial.Text = produto.Quantidade.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao preencher campos: {ex.Message}");
            }
        }

        // Evento disparado ao clicar no botão de atualizar produto
        private void Salvar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (ValidarCampos())
                {
                    AtualizarProduto();
                    DialogResult = true;
                    Close();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar produto: {ex.Message}");
            }
        }

        // Atualiza os dados do produto com os valores dos campos
        private void AtualizarProduto()
        {
            produto.Nome = NomeProduto.Text;
            produto.Tipo = TipoProduto.Text;
            produto.Marca = MarcaProduto.Text;
            produto.Codigo = CodigoProduto.Text;
            produto.Quantidade = int.Parse(QuantidadeInicial.Text);
        }

        // Evento disparado ao clicar no botão de cancelar
        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isModified && ConfirmarSaidaSemSalvar())
                {
                    return;
                }
                DialogResult = false;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao cancelar: {ex.Message}");
            }
        }

        // Confirma se o usuário deseja sair sem salvar as alterações
        private bool ConfirmarSaidaSemSalvar()
        {
            var result = MessageBox.Show("Existem alterações não salvas. Deseja sair sem salvar?", "Confirmação", MessageBoxButton.YesNo);
            return result == MessageBoxResult.No;
        }

        // Restrições de entrada de texto nos TextBoxes
        private void QuantidadeInicial_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text, "[^0-9]+");
        }

        private void QuantidadeInicial_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            HandlePasting(e, "[^0-9]+");
        }

        private void MarcaProduto_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text, "[^a-zA-Z ]+");
        }

        private void MarcaProduto_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            HandlePasting(e, "[^a-zA-Z ]+");
        }

        // Verifica se o texto é permitido com base no padrão fornecido
        private static bool IsTextAllowed(string text, string pattern)
        {
            return !Regex.IsMatch(text, pattern);
        }

        // Lida com a colagem de texto, verificando se o texto colado é permitido
        private static void HandlePasting(DataObjectPastingEventArgs e, string pattern)
        {
            if (e.DataObject.GetDataPresent(typeof(string)))
            {
                string text = (string)e.DataObject.GetData(typeof(string));
                if (!IsTextAllowed(text, pattern))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        // Evento disparado ao mudar a seleção do tipo de produto
        private void TipoProduto_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TipoProduto.SelectedItem is ComboBoxItem selectedItem && selectedItem.Content != null)
            {
                produto.Tipo = selectedItem.Content.ToString() ?? string.Empty;
                isModified = true;
            }
        }

        // Evento disparado ao modificar qualquer campo de texto
        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            isModified = true;
        }

        // Valida os campos antes de salvar
        private bool ValidarCampos()
        {
            if (string.IsNullOrWhiteSpace(NomeProduto.Text))
            {
                MessageBox.Show("O campo Nome do Produto deve ser preenchido.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(TipoProduto.Text))
            {
                MessageBox.Show("O campo Tipo do Produto deve ser preenchido.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(MarcaProduto.Text))
            {
                MessageBox.Show("O campo Marca do Produto deve ser preenchido.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(CodigoProduto.Text))
            {
                MessageBox.Show("O campo Código do Produto deve ser preenchido.");
                return false;
            }
            if (!int.TryParse(QuantidadeInicial.Text, out _))
            {
                MessageBox.Show("O campo Quantidade Inicial deve ser um número válido.");
                return false;
            }
            return true;
        }
    }
}
