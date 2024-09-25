using MaterialSkin;
using MaterialSkin.Controls;
using System.Data;
using System.Drawing; // Adicione esta linha para usar a classe Color

namespace TestandoInterfaces
{
    public partial class Form1 : MaterialForm
    {
        private DataTable produtosDataTable;
        private bool isUpdatingComboBox = false;
        // Remova esta linha se o campo não for necessário

        public Form1()
        {
            InitializeComponent();

            produtosDataTable = new DataTable(); // Inicializar o DataTable

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
            CarregarProdutosNaComboBox();
            // Adicione o evento de texto alterado
            comboBoxProdutos.TextChanged += ComboBoxProdutos_TextChanged;
            // Adicione o evento de seleção alterada
            comboBoxProdutos.SelectedIndexChanged += ComboBoxProdutos_SelectedIndexChanged;

            // Personalizar a cor de fundo do formulário
            this.BackColor = Color.LightBlue; // Altere para a cor desejada
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            CarregarProdutos();
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
                string tipo = ""; // Inicializar a variável tipo

                // Verificar qual RadioButton está selecionado para determinar o tipo do produto
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

                // Inserir o produto no banco de dados com quantidade zero
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
            dataGridViewProdutos.Columns["Quantidade"].HeaderText = "Quantidade";
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

        private void materialLabel2_Click(object sender, EventArgs e)
        {

        }

        private void btn_Pesquisar_Click(object sender, EventArgs e)
        {
            // Recuperar o termo de pesquisa inserido pelo usuário
            string termo = txt_pesquisarProduto.Text;

            // Criar uma instância da classe BancoDeDados
            BancoDeDados bd = new BancoDeDados();

            // Pesquisar produtos no banco de dados
            DataTable dt = bd.PesquisarProduto(termo);

            // Preencher o DataGridView com os dados do DataTable
            dataGridViewProdutos.DataSource = dt;

            // Ajustar as colunas para preencher a largura do DataGridView
            dataGridViewProdutos.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;

        }

        private void txt_pesquisarProduto_TextChanged(object sender, EventArgs e)
        {

        }
        private void CarregarProdutosNaComboBox()
        {
            // Criar uma instância da classe BancoDeDados
            BancoDeDados bd = new BancoDeDados();

            // Carregar os produtos do banco de dados em um DataTable
            produtosDataTable = bd.ListarProdutos();

            // Adicionar uma coluna calculada para exibir "Código - Nome"
            produtosDataTable.Columns.Add("CodigoNome", typeof(string), "Codigo + ' - ' + Nome");

            // Adicionar um item especial "Limpar Seleção" para limpar a seleção
            DataRow row = produtosDataTable.NewRow();
            row["Codigo"] = DBNull.Value;
            row["CodigoNome"] = "Limpar Seleção";
            produtosDataTable.Rows.InsertAt(row, 0);

            // Preencher a ComboBox com os dados do DataTable
            comboBoxProdutos.DisplayMember = "CodigoNome"; // Defina o nome da coluna calculada a ser exibida
            comboBoxProdutos.ValueMember = "Codigo"; // Defina o nome da coluna do valor
            comboBoxProdutos.DataSource = produtosDataTable;
        }

        private void ComboBoxProdutos_TextChanged(object? sender, EventArgs? e)
        {
            if (isUpdatingComboBox) return; // Evitar loops infinitos

            isUpdatingComboBox = true;

            string termo = comboBoxProdutos.Text.ToLower();
            DataView dv = produtosDataTable.DefaultView;
            dv.RowFilter = $"CodigoNome LIKE '%{termo}%'";

            // Adicionar o item "Limpar Seleção" novamente após a filtragem
            DataTable filteredTable = dv.ToTable();
            DataRow row = filteredTable.NewRow();
            row["Codigo"] = DBNull.Value;
            row["CodigoNome"] = "Limpar Seleção";
            filteredTable.Rows.InsertAt(row, 0);

            comboBoxProdutos.DataSource = filteredTable;

            // Restaurar o texto digitado pelo usuário
            comboBoxProdutos.Text = termo;

            isUpdatingComboBox = false;
        }

        private void ComboBoxProdutos_SelectedIndexChanged(object? sender, EventArgs? e)
        {
            if (comboBoxProdutos.SelectedValue == DBNull.Value)
            {
                // Limpar a seleção e recarregar os dados originais
                comboBoxProdutos.SelectedIndex = -1;
                CarregarProdutosNaComboBox();
            }
        }

        // Evento KeyPress para permitir apenas dígitos e controlar teclas de controle
        private void NumericTextBox_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Permitir apenas dígitos, tecla de backspace e tecla de delete
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
            {
                e.Handled = true;
            }
        }

        // Evento TextChanged para remover caracteres não numéricos (caso o usuário cole texto)
        private void NumericTextBox_TextChanged(object sender, EventArgs e)
        {
            MaterialTextBox? textBox = sender as MaterialTextBox;
            if (textBox != null)
            {
                string text = textBox.Text;
                textBox.Text = string.Concat(text.Where(char.IsDigit));
                textBox.SelectionStart = textBox.Text.Length; // Manter o cursor no final
            }
        }

        private void btn_adicionarProduto_Click(object sender, EventArgs e)
        {
            try
            {
                // Verificar se um produto foi selecionado na ComboBox
                if (comboBoxProdutos.SelectedValue == null || comboBoxProdutos.SelectedValue == DBNull.Value)
                {
                    MessageBox.Show("Por favor, selecione um produto.", "Erro de Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Recuperar o código do produto selecionado
                int codigoProduto = Convert.ToInt32((long)comboBoxProdutos.SelectedValue);

                // Recuperar a quantidade inserida pelo usuário
                int quantidade;
                if (!int.TryParse(txt_qtdProdutos.Text, out quantidade) || quantidade <= 0)
                {
                    MessageBox.Show("Por favor, insira uma quantidade válida.", "Erro de Validação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                // Criar uma instância do banco de dados
                BancoDeDados bd = new BancoDeDados();

                // Adicionar o produto ao estoque
                bd.AdicionarProdutoAoEstoque(codigoProduto, quantidade);

                // Exibir mensagem de sucesso
                MessageBox.Show("Produto adicionado ao estoque com sucesso!");

                // Atualizar a lista de produtos na aba de estoque
                CarregarProdutos();
            }
            catch (Exception ex)
            {
                // Exibir mensagem de erro caso ocorra
                MessageBox.Show($"Erro: {ex.Message}");
            }
        }




        private void numericUpDown1_ValueChanged(object sender, EventArgs e)
        {

        }

        private void materialTextBox1_TextChanged(object sender, EventArgs e)
        {

        }
    }

}


