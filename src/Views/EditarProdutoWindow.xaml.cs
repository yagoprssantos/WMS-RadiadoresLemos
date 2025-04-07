using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;
using WMS_RadiadoresLemos_WPF.src.Views;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class EditarProdutoWindow : Window
    {
        private ProdutoData produto;
        private bool isModified = false;
        private bool isNewProduct = false;
        private List<ProdutoData> produtos;

        // Propriedade pública para acessar o produto editado
        public ProdutoData Produto => produto;

        // Construtor que inicializa a janela com os dados do produto ou vazio
        public EditarProdutoWindow(ProdutoData? produto)
        {
            InitializeComponent();
            InicializarProduto(produto);
            PreencherCampos();
            isModified = false;
        }

        // Inicializa o produto com dados existentes ou cria um novo produto
        private void InicializarProduto(ProdutoData? produto)
        {
            // Se produto for nulo, cria um novo produto
            if (produto == null)
            {
                isNewProduct = true;
                this.produto = new ProdutoData
                {
                    Nome = string.Empty,
                    Tipo = string.Empty,
                    Marca = string.Empty,
                    Codigo = string.Empty,
                    Preco = 0.0,
                    Quantidade = 0,
                    Id = Guid.NewGuid().ToString()
                };
            }
            // Se produto não for nulo, usa os dados existentes selecionados
            else
            {
                this.produto = produto;
            }

            produtos = new List<ProdutoData>();
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
                PrecoProduto.Text = produto.Preco.ToString("F2");
                QuantidadeInicial.Text = produto.Quantidade.ToString("N0");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao preencher campos: {ex.Message}");

                // Adiciona alerta
                Alerta.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Erro ao preencher campos da janela de edição de produto. Possíveis motivos:\n" +
                                            "- Dados do produto não encontrados;\n" +
                                            "- Impossibilidade de acessar os dados do produto;\n" +
                                            "- Erro ao preencher campos da janela de edição de produto.",
                                            "- Verifique se os dados do produto estão corretos;\n" +
                                            "- Verifique se o produto existe no banco de dados;\n" +
                                            "- Verifique se o banco de dados está acessível.");
            }
        }

        // Evento disparado ao clicar no botão de salvar produto
        private void Salvar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var confirmarSenhaWindow = new ConfirmarSenhaWindow();
                confirmarSenhaWindow.ShowDialog();

                if (confirmarSenhaWindow.IsConfirmed)
                {
                    if (ValidarCampos())
                    {
                        if (isNewProduct)
                        {
                            CadastrarProduto();
                        }
                        else
                        {
                            AtualizarProduto();
                        }
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
                //MessageBox.Show($"Erro ao salvar produto: {ex.Message}");

                // Adiciona alerta
                Alerta.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Erro ao salvar produto. Possíveis motivos:\n" +
                                            "- Dados do produto não são válidos;\n" +
                                            "- Produto inexistente no banco de dados;\n" +
                                            "- Banco de dados inacessível.",
                                            "- Verifique se os dados do produto estão corretos;\n" +
                                            "- Verifique se o produto existe no banco de dados;\n" +
                                            "- Verifique se o banco de dados está acessível.");
            }
        }

        // Cadastra um novo produto no banco de dados
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
                var produtoExistente = collection.FindOne(p => p.Codigo == produto.Codigo);
                if (produtoExistente != null)
                {
                    MessageBox.Show("Já existe um produto com este código.", "Produto Existente", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                ProdutoData data = DadosDoProduto();

                // Garante que o Id seja igual ao código
                data.Id = data.Codigo;

                // Tenta inserir o novo produto
                collection.Insert(data);

                // Adiciona log
                var log = new LogData
                {
                    Data = DateTime.UtcNow,
                    Tipo = "OPERACIONAL",
                    Nivel = "Usuário",
                    Detalhes = $"Produto cadastrado: {data.Nome}, Código: {data.Codigo}",
                    Usuario = MainWindow.UsuarioLogado?.Nome ?? "Sistema"
                };
                await LogHistorico.SalvarLog(log);

                MessageBox.Show("Produto cadastrado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao cadastrar produto: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                Alerta.AdicionarAlerta("Erro",
                    ex.Message.ToString(),
                    "Erro ao cadastrar produto. Possíveis motivos:\n" +
                    "- Dados do produto não são válidos;\n" +
                    "- Banco de dados inacessível.",
                    "- Verifique se os dados do produto estão corretos;\n" +
                    "- Verifique se o banco de dados está acessível.");
            }
        }

        // Atualiza os dados do produto com os valores dos campos
        private async void AtualizarProduto()
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

                ProdutoData data = DadosDoProduto();

                // Garante que o Id seja igual ao código
                data.Id = data.Codigo;

                // Tenta atualizar o produto
                collection.Update(data);

                // Adiciona log
                var log = new LogData
                {
                    Data = DateTime.UtcNow,
                    Tipo = "OPERACIONAL",
                    Nivel = "Usuário",
                    Detalhes = $"Produto atualizado: {data.Nome}, Código: {data.Codigo}",
                    Usuario = MainWindow.UsuarioLogado?.Nome ?? "Sistema"
                };
                await LogHistorico.SalvarLog(log);

                MessageBox.Show("Produto atualizado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar produto: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                Alerta.AdicionarAlerta("Erro",
                    ex.Message.ToString(),
                    "Erro ao atualizar produto. Possíveis motivos:\n" +
                    "- Dados do produto não são válidos;\n" +
                    "- Banco de dados inacessível.",
                    "- Verifique se os dados do produto estão corretos;\n" +
                    "- Verifique se o banco de dados está acessível.");
            }
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
                                            "Edição de produto.",
                                            "Erro ao cancelar edição de produto. Possíveis motivos:\n" +
                                            "- Erro ao fechar janela de edição de produto;\n" +
                                            "- Impossibilidade de fechar janela de edição de produto.",
                                            "- Verifique se a janela de edição de produto está aberta.");
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
    }
}
