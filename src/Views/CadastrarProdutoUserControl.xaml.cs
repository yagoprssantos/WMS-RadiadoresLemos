using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;
using WMS_RadiadoresLemos_WPF.src.Views;
using LiteDB;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class CadastrarProdutoWindow : Window
    {
        private ProdutoData produto;
        private bool isModified = false;
        private List<ProdutoData> produtos = new List<ProdutoData>();

        // Propriedade pública para acessar o produto cadastrado
        public ProdutoData Produto => produto;

        // Construtor que inicializa a janela com um novo produto
        public CadastrarProdutoWindow()
        {
            InitializeComponent();
            this.produto = new ProdutoData
            {
                Nome = string.Empty,
                Tipo = string.Empty,
                Marca = string.Empty,
                Codigo = string.Empty
            };
            isModified = false;
        }

        // Evento disparado ao clicar no botão de cadastrar produto
        private void Cadastrar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var confirmarSenhaWindow = new ConfirmarSenhaWindow();
                confirmarSenhaWindow.ShowDialog();

                if (confirmarSenhaWindow.IsConfirmed)
                {
                    if (ValidarCampos())
                    {
                        CadastrarProduto();
                        DialogResult = true;
                        Close();
                    }
                }
                else
                {
                    MessageBox.Show("Ação cancelada. Senha não confirmada.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                // Adiciona alerta
                Alerta.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Erro ao cadastrar produto. Possíveis motivos:\n" +
                                            "- Dados do produto não são válidos;\n" +
                                            "- Produto inexistente no banco de dados;\n" +
                                            "- Banco de dados inacessível.",
                                            "- Verifique se os dados do produto estão corretos;\n" +
                                            "- Verifique se o produto existe no banco de dados;\n" +
                                            "- Verifique se o banco de dados está acessível.");
            }
        }

        // Cadastra o novo produto com os valores dos campos
        private async void CadastrarProduto()
        {
            try
            {
                var db = DatabaseConnect.Database;
                if (db == null)
                {
                    MessageBox.Show("Erro: Banco de dados não está conectado.");
                    return;
                }

                var collection = db.GetCollection<ProdutoData>("produtos");

                // Verifica se já existe um produto com o mesmo código
                var produtoExistente = collection.FindOne(p => p.Codigo == CodigoProduto.Text.Trim());
                if (produtoExistente != null)
                {
                    var resultado = MessageBox.Show(
                        "Já existe um produto com este código. Deseja atualizar?",
                        "Produto Existente",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Question);

                    if (resultado != MessageBoxResult.Yes)
                    {
                        return;
                    }
                }

                ProdutoData data = DadosDoProduto();

                // Garante que o Id seja igual ao código
                data.Id = data.Codigo;

                // Tenta inserir/atualizar o produto
                collection.Upsert(data);

                // Adiciona log
                var log = new LogData
                {
                    Data = DateTime.UtcNow,
                    Tipo = "OPERACIONAL",
                    Nivel = "Usuário",
                    Detalhes = $"Produto {(produtoExistente != null ? "atualizado" : "cadastrado")}: {data.Nome}, Código: {data.Codigo}",
                    Usuario = MainWindow.UsuarioLogado?.Nome ?? "Sistema"
                };
                await LogHistorico.SalvarLog(log);

                MessageBox.Show(
                    $"Produto {(produtoExistente != null ? "atualizado" : "cadastrado")} com sucesso!",
                    "Sucesso",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erro ao cadastrar produto: {ex.Message}",
                    "Erro",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);

                Alerta.AdicionarAlerta("Erro",
                    ex.Message.ToString(),
                    "Erro ao cadastrar produto. Possíveis motivos:\n" +
                    "- Dados do produto não são válidos;\n" +
                    "- Produto inexistente no banco de dados;\n" +
                    "- Banco de dados inacessível.",
                    "- Verifique se os dados do produto estão corretos;\n" +
                    "- Verifique se o produto existe no banco de dados;\n" +
                    "- Verifique se o banco de dados está acessível.");
            }
        }

        // Evento disparado ao clicar no botão de cancelar
        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (isModified)
                {
                    if (ConfirmarSaidaSemSalvar())
                    {
                        return;
                    }
                }
                DialogResult = false;
                Close();
            }
            catch (Exception)
            {
                Alerta.AdicionarAlerta("Erro",
                                            "Cadastro de produto.",
                                            "Erro ao cancelar cadastro de produto. Possíveis motivos:\n" +
                                            "- Erro ao fechar janela de cadastro de produto;\n" +
                                            "- Impossibilidade de fechar janela de cadastro de produto.",
                                            "- Verifique se a janela de cadastro de produto está aberta.");
                Close();
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

        private void PrecoProduto_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.Text == ",")
            {
                if (((TextBox)sender).Text.Contains(","))
                {
                    e.Handled = true;
                }
                return;
            }

            e.Handled = !IsTextAllowed(e.Text, "[^0-9,]+");
        }

        private void PrecoProduto_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            HandlePasting(e, "[^0-9,]+");
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
            if (!int.TryParse(QuantidadeInicial.Text.Replace(".", ""), out int quantidade) || quantidade < 0)
            {
                MessageBox.Show("O campo Quantidade Inicial deve ser um número válido e positivo.");
                return false;
            }
            if (!double.TryParse(PrecoProduto.Text.Replace(".", "").Replace(",", "."), 
                System.Globalization.NumberStyles.Any, 
                System.Globalization.CultureInfo.InvariantCulture, 
                out double preco) || preco < 0)
            {
                MessageBox.Show("O campo Preço deve ser um valor válido e positivo.");
                return false;
            }
            return true;
        }

        // Método para obter os dados do produto a partir dos TextBoxes
        private ProdutoData DadosDoProduto() => new()
        {
            Nome = NomeProduto.Text.Trim(),
            Tipo = TipoProduto.Text.Trim(),
            Marca = MarcaProduto.Text.Trim(),
            Codigo = CodigoProduto.Text.Trim(),

            // Remove a formatação do preço (1.000,00 -> 1000 OU 1.999,99 -> 1999.99)
            Preco = double.Parse(PrecoProduto.Text.Trim().Replace(".", "").Replace(",", "."), System.Globalization.CultureInfo.InvariantCulture),

            // Remove a formatação da quantidade (1.000 -> 1000)
            Quantidade = int.Parse(QuantidadeInicial.Text.Trim().Replace(".", "")),

            Id = CodigoProduto.Text.Trim()
        };
    }
}
