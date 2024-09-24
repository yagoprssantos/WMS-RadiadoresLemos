using MaterialSkin;
using MaterialSkin.Controls;
using System.Data;
using System.Drawing; // Adicione esta linha para usar a classe Color

namespace TestandoInterfaces
{
    public partial class Form1 : MaterialForm
    {
        public Form1()
        {
            InitializeComponent();

            var materialSkinManager = MaterialSkinManager.Instance;
            materialSkinManager.AddFormToManage(this);
            materialSkinManager.Theme = MaterialSkinManager.Themes.LIGHT;
            materialSkinManager.ColorScheme = new ColorScheme(
             Primary.Blue400,    // Azul Claro
             Primary.Blue500,    // Azul Médio
             Primary.Blue200,    // Azul Muito Claro
             Accent.Blue700,     // Azul Bem Escuro
             TextShade.WHITE     // Preto
         );


            // Personalizar a cor de fundo do formulário
            this.BackColor = Color.LightBlue; // Altere para a cor desejada
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void materialDrawer1_Click(object sender, EventArgs e)
        {

        }

        private void materialButton1_Click(object sender, EventArgs e)
        {

        }

        private void materialListBox1_SelectedIndexChanged(object sender, MaterialListBoxItem selectedItem)
        {

        }

        private void Estoque_Click(object sender, EventArgs e)
        {

        }

        private void Venda_Click(object sender, EventArgs e)
        {

        }

        private void materialLabel1_Click(object sender, EventArgs e)
        {

        }

        private void materialLabel6_Click(object sender, EventArgs e)
        {

        }

        private void txt_nmProduto_TextChanged(object sender, EventArgs e)
        {

        }

        private void txt_codProduto_TextChanged(object sender, EventArgs e)
        {

        }

        private void txt_marcaProduto_TextChanged(object sender, EventArgs e)
        {

        }

        private void rb_caixa_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void rb_radiador_CheckedChanged(object sender, EventArgs e)
        {

        }

        private void btn_cadastrarProduto_Click(object sender, EventArgs e)
        {
            try
            {
                // Recuperar os dados inseridos pelo usuário
                int codigo = int.Parse(txt_codProduto.Text);  // O código do produto
                string nome = txt_nmProduto.Text;
                string marca = txt_marcaProduto.Text;

                // Verificar qual RadioButton está selecionado para determinar o tipo do produto
                string tipo = "";
                if (rb_caixa.Checked)
                {
                    tipo = "Caixa";
                }
                else if (rb_radiador.Checked)
                {
                    tipo = "Radiador";
                }
                else
                {
                    // Caso nenhum tipo tenha sido selecionado, mostrar uma mensagem de erro
                    MessageBox.Show("Por favor, selecione o tipo do produto.", "Erro de Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Criar uma instância do banco de dados
                BancoDeDados bd = new BancoDeDados();

                // Inserir o produto no banco de dados
                bd.InserirProduto(codigo, nome, marca, tipo);

                // Exibir mensagem de sucesso
                MessageBox.Show($"Produto {txt_nmProduto.Text} inserido com sucesso!");

                // Atualizar a lista de produtos na aba de estoque
                CarregarProdutos();
            }
            catch (Exception ex)
            {
                // Exibir mensagem de erro caso ocorra (por exemplo, código duplicado)
                MessageBox.Show($"Erro: {ex.Message}");
            }
        }

        private void CarregarProdutos()
        {
            // Criar uma instância da classe BancoDeDados
            BancoDeDados bd = new BancoDeDados();

            // Carregar os produtos do banco de dados em um DataTable
            DataTable dt = bd.ListarProdutos();

            // Preencher o DataGridView com os dados do DataTable
            dataGridViewProdutos.DataSource = dt;

            // Ajustar as colunas para preencher a largura do DataGridView
            dataGridViewProdutos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

            // Definir os títulos das colunas (opcional)
            dataGridViewProdutos.Columns["Codigo"].HeaderText = "Código";
            dataGridViewProdutos.Columns["Nome"].HeaderText = "Nome";
            dataGridViewProdutos.Columns["Marca"].HeaderText = "Marca";
            dataGridViewProdutos.Columns["Tipo"].HeaderText = "Tipo";
        }


        private void dataGridViewProdutos_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void tabControl_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Verificar se a aba selecionada é a de Estoque
            if (materialTabControl2.SelectedTab == Estoque) // Suponha que 'Estoque' seja o nome da aba de Estoque
            {
                // Criar uma instância da classe BancoDeDados
                BancoDeDados bd = new BancoDeDados();

                // Carregar os produtos do banco de dados em um DataTable
                DataTable dt = bd.ListarProdutos();

                // Preencher o DataGridView com os dados do DataTable
                dataGridViewProdutos.DataSource = dt;

                // Ajustar as colunas para preencher a largura do DataGridView
                dataGridViewProdutos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

                // Definir os títulos das colunas (opcional)
                dataGridViewProdutos.Columns["Codigo"].HeaderText = "Código";
                dataGridViewProdutos.Columns["Nome"].HeaderText = "Nome";
                dataGridViewProdutos.Columns["Marca"].HeaderText = "Marca";
                dataGridViewProdutos.Columns["Tipo"].HeaderText = "Tipo";
            }
        }

    }
}

