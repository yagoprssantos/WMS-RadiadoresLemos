using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class ControleEstoqueUserControl : UserControl
    {
        private List<Produto> produtos;

        public ControleEstoqueUserControl()
        {
            InitializeComponent();
            CarregarProdutos();
        }


        // Aba de Cadastro de Produtos

        // Função foco e perda de foco dos TextBoxes
        private void TextBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && (textBox.Text == "Nome do Produto" || textBox.Text == "Tipo do Produto" || textBox.Text == "Marca do Produto" || textBox.Text == "Quantidade"))
            {
                textBox.Text = string.Empty;
            }
        }

        private void TextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = sender as TextBox;
            if (textBox != null && textBox.Text == string.Empty)
            {
                if (textBox.Name == "NomeProduto")
                {
                    textBox.Text = "Nome do Produto";
                }
                else if (textBox.Name == "TipoProduto")
                {
                    textBox.Text = "Tipo do Produto";
                }
                else if (textBox.Name == "MarcaProduto")
                {
                    textBox.Text = "Marca do Produto";
                }
                else if (textBox.Name == "QuantidadeInicial")
                {
                    textBox.Text = "Quantidade";
                }
            }
        }


        // Restrições de entrada de texto nos TextBoxes
        // Quantidade inicial
        private void QuantidadeInicial_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text, "[^0-9]+"); // Apenas números
        }
        private void QuantidadeInicial_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            HandlePasting(e, "[^0-9]+"); // Apenas números
        }

        // Nome do Produto
        private void NomeProduto_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text, "[^a-zA-Z ]+"); // Apenas letras e espaços
        }
        private void NomeProduto_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            HandlePasting(e, "[^a-zA-Z ]+"); // Apenas letras e espaços
        }

        // Tipo do Produto
        private void MarcaProduto_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text, "[^a-zA-Z ]+"); // Apenas letras e espaços
        }
        private void MarcaProduto_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            HandlePasting(e, "[^a-zA-Z ]+"); // Apenas letras e espaços
        }

        // Marca do Produto
        private void CodigoProduto_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !IsTextAllowed(e.Text, "[^a-zA-Z0-9]+"); // Letras e números
        }
        private void CodigoProduto_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            HandlePasting(e, "[^a-zA-Z0-9]+"); // Letras e números
        }


        // Função para verificar se o texto é permitido
        private static bool IsTextAllowed(string text, string pattern)
        {
            Regex regex = new Regex(pattern);
            return !regex.IsMatch(text);
        }

        // Função para lidar com a colagem de texto
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


        // Botões

        private void CadastrarProduto_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para cadastrar o produto no banco de dados
            // Verificar se todos os campos estão preenchidos
            if (NomeProduto.Text != "" && TipoProduto.Text != "" && MarcaProduto.Text != "" && CodigoProduto.Text != "" && QuantidadeInicial.Text != "")
            {
                // Lógica para cadastrar o produto no banco de dados
                // CadastrarProdutoNoBanco();

                // FUNCIONANDO PARA A FUNÇÃO TESTE TEMPORÁRIA
                // Adiciona produto no List<Produto> produtos
                produtos.Add(new Produto
                {
                    Nome = NomeProduto.Text,
                    Tipo = TipoProduto.Text,
                    Marca = MarcaProduto.Text,
                    Codigo = CodigoProduto.Text,
                    Quantidade = int.Parse(QuantidadeInicial.Text)
                });

                // Avisa o usuário que o produto foi cadastrado
                MessageBox.Show("Produto cadastrado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);

                // Limpar os campos após o cadastro
                LimparCamposCadastro();
            }
            else
            {
                // Avisar o usuário para preencher todos os campos
                MessageBox.Show("Preencha todos os campos para cadastrar o produto.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void LimparCamposCadastro()
        {
            NomeProduto.Text = "";
            TipoProduto.Text = "";
            MarcaProduto.Text = "";
            CodigoProduto.Text = "";
            QuantidadeInicial.Text = "";
        }






        // Aba de Estoque


        // FUNÇÃO TESTE TEMPORÁRIA PARA CARREGAR PRODUTOS FICTÍCIOS
        private void CarregarProdutos()
        {
            // Carregar produtos do banco de dados ou outra fonte
            produtos = new List<Produto>
    {
        new Produto { Nome = "Radiador de Alumínio", Tipo = "Radiador", Marca = "Valeo", Codigo = "VA123456", Quantidade = 10 },
        new Produto { Nome = "Mangueira de Arrefecimento", Tipo = "Mangueira", Marca = "Gates", Codigo = "GA654321", Quantidade = 20 },
        new Produto { Nome = "Termostato", Tipo = "Termostato", Marca = "Mahle", Codigo = "MA789012", Quantidade = 15 },
        new Produto { Nome = "Bomba D'água", Tipo = "Bomba D'água", Marca = "Bosch", Codigo = "BO345678", Quantidade = 5 },
        new Produto { Nome = "Ventoinha Elétrica", Tipo = "Ventoinha", Marca = "Denso", Codigo = "DE901234", Quantidade = 8 },
        new Produto { Nome = "Filtro de Óleo", Tipo = "Filtro", Marca = "Fram", Codigo = "FR112233", Quantidade = 30 },
        new Produto { Nome = "Correia Dentada", Tipo = "Correia", Marca = "Contitech", Codigo = "CT445566", Quantidade = 25 },
        new Produto { Nome = "Velas de Ignição", Tipo = "Vela", Marca = "NGK", Codigo = "NG778899", Quantidade = 40 },
        new Produto { Nome = "Amortecedor", Tipo = "Amortecedor", Marca = "Monroe", Codigo = "MO223344", Quantidade = 12 },
        new Produto { Nome = "Pastilha de Freio", Tipo = "Freio", Marca = "Brembo", Codigo = "BR556677", Quantidade = 18 },
        new Produto { Nome = "Disco de Freio", Tipo = "Freio", Marca = "TRW", Codigo = "TR889900", Quantidade = 22 },
        new Produto { Nome = "Filtro de Ar", Tipo = "Filtro", Marca = "Mann", Codigo = "MA334455", Quantidade = 28 },
        new Produto { Nome = "Bateria", Tipo = "Bateria", Marca = "Hella", Codigo = "HE667788", Quantidade = 7 },
        new Produto { Nome = "Alternador", Tipo = "Alternador", Marca = "Delphi", Codigo = "DE990011", Quantidade = 6 },
        new Produto { Nome = "Sensor de Oxigênio", Tipo = "Sensor", Marca = "Bosch", Codigo = "BO112244", Quantidade = 14 },
        new Produto { Nome = "Injetor de Combustível", Tipo = "Injetor", Marca = "Magneti Marelli", Codigo = "MM556677", Quantidade = 9 },
        new Produto { Nome = "Cabo de Vela", Tipo = "Cabo", Marca = "NGK", Codigo = "NG334466", Quantidade = 16 },
        new Produto { Nome = "Bobina de Ignição", Tipo = "Bobina", Marca = "Bosch", Codigo = "BO778899", Quantidade = 11 },
        new Produto { Nome = "Filtro de Combustível", Tipo = "Filtro", Marca = "Fram", Codigo = "FR990022", Quantidade = 19 },
        new Produto { Nome = "Sensor de Temperatura", Tipo = "Sensor", Marca = "Delphi", Codigo = "DE334477", Quantidade = 13 },
        new Produto { Nome = "Cilindro Mestre", Tipo = "Freio", Marca = "TRW", Codigo = "TR556688", Quantidade = 8 },
        new Produto { Nome = "Eixo de Transmissão", Tipo = "Transmissão", Marca = "Spicer", Codigo = "SP112233", Quantidade = 5 },
        new Produto { Nome = "Junta Homocinética", Tipo = "Transmissão", Marca = "SKF", Codigo = "SK445566", Quantidade = 10 },
        new Produto { Nome = "Kit de Embreagem", Tipo = "Embreagem", Marca = "Sachs", Codigo = "SA778899", Quantidade = 7 },
        new Produto { Nome = "Radiador de Cobre", Tipo = "Radiador", Marca = "Valeo", Codigo = "VA334455", Quantidade = 9 },
        new Produto { Nome = "Mangueira de Alta Pressão", Tipo = "Mangueira", Marca = "Gates", Codigo = "GA667788", Quantidade = 12 },
        new Produto { Nome = "Termostato Eletrônico", Tipo = "Termostato", Marca = "Mahle", Codigo = "MA990011", Quantidade = 6 },
        new Produto { Nome = "Bomba de Óleo", Tipo = "Bomba", Marca = "Bosch", Codigo = "BO223344", Quantidade = 8 },
        new Produto { Nome = "Ventoinha Mecânica", Tipo = "Ventoinha", Marca = "Denso", Codigo = "DE556677", Quantidade = 10 },
        new Produto { Nome = "Filtro de Cabine", Tipo = "Filtro", Marca = "Mann", Codigo = "MA889900", Quantidade = 15 },
        new Produto { Nome = "Correia Poly-V", Tipo = "Correia", Marca = "Contitech", Codigo = "CT112244", Quantidade = 20 },
        new Produto { Nome = "Velas de Platina", Tipo = "Vela", Marca = "NGK", Codigo = "NG334455", Quantidade = 25 },
        new Produto { Nome = "Amortecedor Traseiro", Tipo = "Amortecedor", Marca = "Monroe", Codigo = "MO667788", Quantidade = 10 },
        new Produto { Nome = "Pastilha de Freio Cerâmica", Tipo = "Freio", Marca = "Brembo", Codigo = "BR990011", Quantidade = 18 },
        new Produto { Nome = "Disco de Freio Ventilado", Tipo = "Freio", Marca = "TRW", Codigo = "TR223344", Quantidade = 12 },
        new Produto { Nome = "Filtro de Ar Esportivo", Tipo = "Filtro", Marca = "K&N", Codigo = "KN556677", Quantidade = 14 },
        new Produto { Nome = "Bateria de Gel", Tipo = "Bateria", Marca = "Hella", Codigo = "HE778899", Quantidade = 5 },
        new Produto { Nome = "Alternador de Alta Capacidade", Tipo = "Alternador", Marca = "Delphi", Codigo = "DE334455", Quantidade = 7 },
        new Produto { Nome = "Sensor de Pressão", Tipo = "Sensor", Marca = "Bosch", Codigo = "BO667788", Quantidade = 9 },
        new Produto { Nome = "Injetor de Alta Performance", Tipo = "Injetor", Marca = "Magneti Marelli", Codigo = "MM990011", Quantidade = 6 },
        new Produto { Nome = "Cabo de Vela de Silicone", Tipo = "Cabo", Marca = "NGK", Codigo = "NG223344", Quantidade = 11 },
        new Produto { Nome = "Bobina de Ignição de Alta Voltagem", Tipo = "Bobina", Marca = "Bosch", Codigo = "BO556677", Quantidade = 8 },
        new Produto { Nome = "Filtro de Combustível de Alta Vazão", Tipo = "Filtro", Marca = "Fram", Codigo = "FR778899", Quantidade = 10 },
        new Produto { Nome = "Sensor de Temperatura Digital", Tipo = "Sensor", Marca = "Delphi", Codigo = "DE112233", Quantidade = 12 },
        new Produto { Nome = "Cilindro Mestre de Freio", Tipo = "Freio", Marca = "TRW", Codigo = "TR445566", Quantidade = 7 },
        new Produto { Nome = "Eixo de Transmissão Reforçado", Tipo = "Transmissão", Marca = "Spicer", Codigo = "SP667788", Quantidade = 5 },
        new Produto { Nome = "Junta Homocinética de Alta Performance", Tipo = "Transmissão", Marca = "SKF", Codigo = "SK990011", Quantidade = 8 },
        new Produto { Nome = "Kit de Embreagem Reforçado", Tipo = "Embreagem", Marca = "Sachs", Codigo = "SA223344", Quantidade = 6 }
    };

            EstoqueDataGrid.ItemsSource = produtos;
        }


        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Lógica para filtrar produtos na tabela de estoque, pesquisando por nome, tipo, marca ou código simultaneamente
            string searchText = SearchBox.Text.ToLower();
            var filteredProducts = produtos.Where(p => p.Nome.ToLower().Contains(searchText) ||
                                                       p.Tipo.ToLower().Contains(searchText) ||
                                                       p.Marca.ToLower().Contains(searchText) ||
                                                       p.Codigo.ToLower().Contains(searchText)).ToList();
            EstoqueDataGrid.ItemsSource = filteredProducts;
        }

        private void EditarProduto_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para editar o produto selecionado
            if (EstoqueDataGrid.SelectedItem != null)
            {
                // Obter o produto selecionado e abrir uma nova janela ou diálogo para 
            }
            else
            {
                MessageBox.Show("Selecione um produto para editar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AlterarQuantidade_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para adicionar ou remover quantidade do produto selecionado
            if (EstoqueDataGrid.SelectedItem != null)
            {
                // Obter o produto selecionado e abrir uma nova janela ou diálogo para editar quantidade 
            }
            else
            {
                MessageBox.Show("Selecione um produto para adicionar/remover quantidade.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeletarProduto_Click(object sender, RoutedEventArgs e)
        {
            // Lógica para deletar o produto selecionado
            if (EstoqueDataGrid.SelectedItem != null)
            {
                // Obter o produto selecionado
                var produtoSelecionado = EstoqueDataGrid.SelectedItem;
                MessageBoxResult result = MessageBox.Show("Tem certeza que deseja deletar este produto?", "Confirmação", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    DeletarProdutoDoBanco(produtoSelecionado);
                    AtualizarTabelaEstoque();
                }
            }
            else
            {
                MessageBox.Show("Selecione um produto para deletar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void AtualizarTabelaEstoque()
        {
            // TODO
            // Lógica para atualizar a tabela de estoque
        }

        private void DeletarProdutoDoBanco(object produtoSelecionado)
        {
            // TODO
            // Lógica para deletar o produto do banco de dados
        }
    }

    public class Produto
    {
        public string Nome { get; set; }
        public string Tipo { get; set; }
        public string Marca { get; set; }
        public string Codigo { get; set; }
        public int Quantidade { get; set; }
    }
}
