namespace TestandoInterfaces
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            materialTabControl2 = new MaterialSkin.Controls.MaterialTabControl();
            Produtos = new TabPage();
            btn_cadastrarProduto = new MaterialSkin.Controls.MaterialButton();
            rb_radiador = new MaterialSkin.Controls.MaterialRadioButton();
            rb_caixa = new MaterialSkin.Controls.MaterialRadioButton();
            materialLabel6 = new MaterialSkin.Controls.MaterialLabel();
            txt_codProduto = new MaterialSkin.Controls.MaterialTextBox();
            materialLabel5 = new MaterialSkin.Controls.MaterialLabel();
            txt_marcaProduto = new MaterialSkin.Controls.MaterialTextBox();
            materialLabel4 = new MaterialSkin.Controls.MaterialLabel();
            txt_nmProduto = new MaterialSkin.Controls.MaterialTextBox();
            materialLabel1 = new MaterialSkin.Controls.MaterialLabel();
            Estoque = new TabPage();
            txt_qtdProdutos = new MaterialSkin.Controls.MaterialTextBox();
            btn_adicionarProduto = new MaterialSkin.Controls.MaterialButton();
            comboBoxProdutos = new MaterialSkin.Controls.MaterialComboBox();
            materialLabel7 = new MaterialSkin.Controls.MaterialLabel();
            btn_Pesquisar = new MaterialSkin.Controls.MaterialButton();
            txt_pesquisarProduto = new MaterialSkin.Controls.MaterialTextBox();
            materialLabel2 = new MaterialSkin.Controls.MaterialLabel();
            dataGridViewProdutos = new DataGridView();
            Venda = new TabPage();
            materialLabel3 = new MaterialSkin.Controls.MaterialLabel();
            imageList1 = new ImageList(components);
            materialTabControl2.SuspendLayout();
            Produtos.SuspendLayout();
            Estoque.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewProdutos).BeginInit();
            Venda.SuspendLayout();
            SuspendLayout();
            // 
            // materialTabControl2
            // 
            materialTabControl2.Appearance = TabAppearance.Buttons;
            materialTabControl2.Controls.Add(Produtos);
            materialTabControl2.Controls.Add(Estoque);
            materialTabControl2.Controls.Add(Venda);
            materialTabControl2.Depth = 0;
            materialTabControl2.Dock = DockStyle.Fill;
            materialTabControl2.ImageList = imageList1;
            materialTabControl2.Location = new Point(3, 64);
            materialTabControl2.MouseState = MaterialSkin.MouseState.HOVER;
            materialTabControl2.Multiline = true;
            materialTabControl2.Name = "materialTabControl2";
            materialTabControl2.SelectedIndex = 0;
            materialTabControl2.Size = new Size(992, 515);
            materialTabControl2.TabIndex = 0;
            materialTabControl2.TabStop = false;
            // 
            // Produtos
            // 
            Produtos.Controls.Add(btn_cadastrarProduto);
            Produtos.Controls.Add(rb_radiador);
            Produtos.Controls.Add(rb_caixa);
            Produtos.Controls.Add(materialLabel6);
            Produtos.Controls.Add(txt_codProduto);
            Produtos.Controls.Add(materialLabel5);
            Produtos.Controls.Add(txt_marcaProduto);
            Produtos.Controls.Add(materialLabel4);
            Produtos.Controls.Add(txt_nmProduto);
            Produtos.Controls.Add(materialLabel1);
            Produtos.ImageKey = "icons8-produto-50.png";
            Produtos.ImeMode = ImeMode.KatakanaHalf;
            Produtos.Location = new Point(4, 40);
            Produtos.Name = "Produtos";
            Produtos.Padding = new Padding(3);
            Produtos.Size = new Size(984, 471);
            Produtos.TabIndex = 0;
            Produtos.Text = "Produtos";
            Produtos.UseVisualStyleBackColor = true;
            // 
            // btn_cadastrarProduto
            // 
            btn_cadastrarProduto.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btn_cadastrarProduto.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            btn_cadastrarProduto.Depth = 0;
            btn_cadastrarProduto.HighEmphasis = true;
            btn_cadastrarProduto.Icon = null;
            btn_cadastrarProduto.Location = new Point(702, 291);
            btn_cadastrarProduto.Margin = new Padding(4, 6, 4, 6);
            btn_cadastrarProduto.MouseState = MaterialSkin.MouseState.HOVER;
            btn_cadastrarProduto.Name = "btn_cadastrarProduto";
            btn_cadastrarProduto.NoAccentTextColor = Color.Empty;
            btn_cadastrarProduto.Size = new Size(106, 36);
            btn_cadastrarProduto.TabIndex = 9;
            btn_cadastrarProduto.Text = "CADASTRAR";
            btn_cadastrarProduto.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            btn_cadastrarProduto.UseAccentColor = false;
            btn_cadastrarProduto.UseVisualStyleBackColor = true;
            btn_cadastrarProduto.Click += btn_cadastrarProduto_Click;
            // 
            // rb_radiador
            // 
            rb_radiador.AutoSize = true;
            rb_radiador.Depth = 0;
            rb_radiador.Location = new Point(676, 211);
            rb_radiador.Margin = new Padding(0);
            rb_radiador.MouseLocation = new Point(-1, -1);
            rb_radiador.MouseState = MaterialSkin.MouseState.HOVER;
            rb_radiador.Name = "rb_radiador";
            rb_radiador.Ripple = true;
            rb_radiador.Size = new Size(99, 37);
            rb_radiador.TabIndex = 8;
            rb_radiador.TabStop = true;
            rb_radiador.Text = "Radiador";
            rb_radiador.UseVisualStyleBackColor = true;
            rb_radiador.CheckedChanged += rb_radiador_CheckedChanged;
            // 
            // rb_caixa
            // 
            rb_caixa.AutoSize = true;
            rb_caixa.Depth = 0;
            rb_caixa.Location = new Point(540, 211);
            rb_caixa.Margin = new Padding(0);
            rb_caixa.MouseLocation = new Point(-1, -1);
            rb_caixa.MouseState = MaterialSkin.MouseState.HOVER;
            rb_caixa.Name = "rb_caixa";
            rb_caixa.Ripple = true;
            rb_caixa.Size = new Size(75, 37);
            rb_caixa.TabIndex = 7;
            rb_caixa.TabStop = true;
            rb_caixa.Text = "Caixa";
            rb_caixa.UseVisualStyleBackColor = true;
            rb_caixa.CheckedChanged += rb_caixa_CheckedChanged;
            // 
            // materialLabel6
            // 
            materialLabel6.AutoSize = true;
            materialLabel6.Depth = 0;
            materialLabel6.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            materialLabel6.Location = new Point(540, 179);
            materialLabel6.MouseState = MaterialSkin.MouseState.HOVER;
            materialLabel6.Name = "materialLabel6";
            materialLabel6.Size = new Size(37, 19);
            materialLabel6.TabIndex = 6;
            materialLabel6.Text = "Tipo:";
            materialLabel6.Click += materialLabel6_Click;
            // 
            // txt_codProduto
            // 
            txt_codProduto.AnimateReadOnly = false;
            txt_codProduto.BorderStyle = BorderStyle.None;
            txt_codProduto.Depth = 0;
            txt_codProduto.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            txt_codProduto.LeadingIcon = null;
            txt_codProduto.Location = new Point(540, 85);
            txt_codProduto.MaxLength = 50;
            txt_codProduto.MouseState = MaterialSkin.MouseState.OUT;
            txt_codProduto.Multiline = false;
            txt_codProduto.Name = "txt_codProduto";
            txt_codProduto.Size = new Size(268, 50);
            txt_codProduto.TabIndex = 5;
            txt_codProduto.Text = "";
            txt_codProduto.TrailingIcon = null;
            txt_codProduto.TextChanged += txt_codProduto_TextChanged;
            // 
            // materialLabel5
            // 
            materialLabel5.AutoSize = true;
            materialLabel5.Depth = 0;
            materialLabel5.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            materialLabel5.Location = new Point(540, 54);
            materialLabel5.MouseState = MaterialSkin.MouseState.HOVER;
            materialLabel5.Name = "materialLabel5";
            materialLabel5.Size = new Size(55, 19);
            materialLabel5.TabIndex = 4;
            materialLabel5.Text = "Código:";
            // 
            // txt_marcaProduto
            // 
            txt_marcaProduto.AnimateReadOnly = false;
            txt_marcaProduto.BorderStyle = BorderStyle.None;
            txt_marcaProduto.Depth = 0;
            txt_marcaProduto.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            txt_marcaProduto.LeadingIcon = null;
            txt_marcaProduto.Location = new Point(189, 218);
            txt_marcaProduto.MaxLength = 50;
            txt_marcaProduto.MouseState = MaterialSkin.MouseState.OUT;
            txt_marcaProduto.Multiline = false;
            txt_marcaProduto.Name = "txt_marcaProduto";
            txt_marcaProduto.Size = new Size(268, 50);
            txt_marcaProduto.TabIndex = 3;
            txt_marcaProduto.Text = "";
            txt_marcaProduto.TrailingIcon = null;
            txt_marcaProduto.TextChanged += txt_marcaProduto_TextChanged;
            // 
            // materialLabel4
            // 
            materialLabel4.AutoSize = true;
            materialLabel4.Depth = 0;
            materialLabel4.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            materialLabel4.Location = new Point(189, 179);
            materialLabel4.MouseState = MaterialSkin.MouseState.HOVER;
            materialLabel4.Name = "materialLabel4";
            materialLabel4.Size = new Size(50, 19);
            materialLabel4.TabIndex = 2;
            materialLabel4.Text = "Marca:";
            // 
            // txt_nmProduto
            // 
            txt_nmProduto.AnimateReadOnly = false;
            txt_nmProduto.BorderStyle = BorderStyle.None;
            txt_nmProduto.Depth = 0;
            txt_nmProduto.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            txt_nmProduto.LeadingIcon = null;
            txt_nmProduto.Location = new Point(189, 85);
            txt_nmProduto.MaxLength = 50;
            txt_nmProduto.MouseState = MaterialSkin.MouseState.OUT;
            txt_nmProduto.Multiline = false;
            txt_nmProduto.Name = "txt_nmProduto";
            txt_nmProduto.Size = new Size(268, 50);
            txt_nmProduto.TabIndex = 1;
            txt_nmProduto.Text = "";
            txt_nmProduto.TrailingIcon = null;
            txt_nmProduto.TextChanged += txt_nmProduto_TextChanged;
            // 
            // materialLabel1
            // 
            materialLabel1.AutoSize = true;
            materialLabel1.Depth = 0;
            materialLabel1.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            materialLabel1.Location = new Point(189, 54);
            materialLabel1.MouseState = MaterialSkin.MouseState.HOVER;
            materialLabel1.Name = "materialLabel1";
            materialLabel1.Size = new Size(47, 19);
            materialLabel1.TabIndex = 0;
            materialLabel1.Text = "Nome:";
            materialLabel1.Click += materialLabel1_Click;
            // 
            // Estoque
            // 
            Estoque.Controls.Add(txt_qtdProdutos);
            Estoque.Controls.Add(btn_adicionarProduto);
            Estoque.Controls.Add(comboBoxProdutos);
            Estoque.Controls.Add(materialLabel7);
            Estoque.Controls.Add(btn_Pesquisar);
            Estoque.Controls.Add(txt_pesquisarProduto);
            Estoque.Controls.Add(materialLabel2);
            Estoque.Controls.Add(dataGridViewProdutos);
            Estoque.ImageKey = "icons8-armazém-50.png";
            Estoque.Location = new Point(4, 40);
            Estoque.Name = "Estoque";
            Estoque.Padding = new Padding(3);
            Estoque.Size = new Size(984, 471);
            Estoque.TabIndex = 1;
            Estoque.Text = "Estoque";
            Estoque.UseVisualStyleBackColor = true;
            Estoque.Click += Estoque_Click;
            // 
            // txt_qtdProdutos
            // 
            txt_qtdProdutos.AnimateReadOnly = false;
            txt_qtdProdutos.BorderStyle = BorderStyle.None;
            txt_qtdProdutos.Depth = 0;
            txt_qtdProdutos.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            txt_qtdProdutos.LeadingIcon = null;
            txt_qtdProdutos.Location = new Point(19, 97);
            txt_qtdProdutos.MaxLength = 50;
            txt_qtdProdutos.MouseState = MaterialSkin.MouseState.OUT;
            txt_qtdProdutos.Multiline = false;
            txt_qtdProdutos.Name = "txt_qtdProdutos";
            txt_qtdProdutos.Size = new Size(186, 50);
            txt_qtdProdutos.TabIndex = 8;
            txt_qtdProdutos.Text = "";
            txt_qtdProdutos.TrailingIcon = null;
            txt_qtdProdutos.TextChanged += materialTextBox1_TextChanged;
            // 
            // btn_adicionarProduto
            // 
            btn_adicionarProduto.AllowDrop = true;
            btn_adicionarProduto.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btn_adicionarProduto.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            btn_adicionarProduto.Depth = 0;
            btn_adicionarProduto.HighEmphasis = true;
            btn_adicionarProduto.Icon = Properties.Resources.icons8_adicionar_50;
            btn_adicionarProduto.Image = Properties.Resources.icons8_adicionar_50;
            btn_adicionarProduto.Location = new Point(229, 97);
            btn_adicionarProduto.Margin = new Padding(4, 6, 4, 6);
            btn_adicionarProduto.MouseState = MaterialSkin.MouseState.HOVER;
            btn_adicionarProduto.Name = "btn_adicionarProduto";
            btn_adicionarProduto.NoAccentTextColor = Color.Empty;
            btn_adicionarProduto.Size = new Size(126, 36);
            btn_adicionarProduto.TabIndex = 7;
            btn_adicionarProduto.Text = "Adicionar";
            btn_adicionarProduto.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            btn_adicionarProduto.UseAccentColor = false;
            btn_adicionarProduto.UseVisualStyleBackColor = true;
            btn_adicionarProduto.Click += btn_adicionarProduto_Click;
            // 
            // comboBoxProdutos
            // 
            comboBoxProdutos.AutoResize = false;
            comboBoxProdutos.BackColor = Color.FromArgb(255, 255, 255);
            comboBoxProdutos.Depth = 0;
            comboBoxProdutos.DrawMode = DrawMode.OwnerDrawVariable;
            comboBoxProdutos.DropDownHeight = 174;
            comboBoxProdutos.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxProdutos.DropDownWidth = 121;
            comboBoxProdutos.Font = new Font("Microsoft Sans Serif", 14F, FontStyle.Bold, GraphicsUnit.Pixel);
            comboBoxProdutos.ForeColor = Color.FromArgb(222, 0, 0, 0);
            comboBoxProdutos.FormattingEnabled = true;
            comboBoxProdutos.IntegralHeight = false;
            comboBoxProdutos.ItemHeight = 43;
            comboBoxProdutos.Location = new Point(19, 25);
            comboBoxProdutos.MaxDropDownItems = 4;
            comboBoxProdutos.MouseState = MaterialSkin.MouseState.OUT;
            comboBoxProdutos.Name = "comboBoxProdutos";
            comboBoxProdutos.Size = new Size(336, 49);
            comboBoxProdutos.StartIndex = 0;
            comboBoxProdutos.TabIndex = 6;
            // 
            // materialLabel7
            // 
            materialLabel7.AutoSize = true;
            materialLabel7.Depth = 0;
            materialLabel7.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            materialLabel7.Location = new Point(19, 3);
            materialLabel7.MouseState = MaterialSkin.MouseState.HOVER;
            materialLabel7.Name = "materialLabel7";
            materialLabel7.Size = new Size(128, 19);
            materialLabel7.TabIndex = 5;
            materialLabel7.Text = "Adicionar Produto";
            // 
            // btn_Pesquisar
            // 
            btn_Pesquisar.AutoSize = false;
            btn_Pesquisar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            btn_Pesquisar.Density = MaterialSkin.Controls.MaterialButton.MaterialButtonDensity.Default;
            btn_Pesquisar.Depth = 0;
            btn_Pesquisar.HighEmphasis = true;
            btn_Pesquisar.Icon = Properties.Resources.icons8_pesquisar_50;
            btn_Pesquisar.Location = new Point(915, 34);
            btn_Pesquisar.Margin = new Padding(4, 6, 4, 6);
            btn_Pesquisar.MouseState = MaterialSkin.MouseState.HOVER;
            btn_Pesquisar.Name = "btn_Pesquisar";
            btn_Pesquisar.NoAccentTextColor = Color.Empty;
            btn_Pesquisar.Size = new Size(42, 30);
            btn_Pesquisar.TabIndex = 4;
            btn_Pesquisar.Type = MaterialSkin.Controls.MaterialButton.MaterialButtonType.Contained;
            btn_Pesquisar.UseAccentColor = false;
            btn_Pesquisar.UseVisualStyleBackColor = true;
            btn_Pesquisar.Click += btn_Pesquisar_Click;
            // 
            // txt_pesquisarProduto
            // 
            txt_pesquisarProduto.AnimateReadOnly = false;
            txt_pesquisarProduto.BorderStyle = BorderStyle.None;
            txt_pesquisarProduto.Cursor = Cursors.No;
            txt_pesquisarProduto.Depth = 0;
            txt_pesquisarProduto.Font = new Font("Roboto", 16F, FontStyle.Regular, GraphicsUnit.Pixel);
            txt_pesquisarProduto.LeadingIcon = null;
            txt_pesquisarProduto.Location = new Point(472, 34);
            txt_pesquisarProduto.Margin = new Padding(2);
            txt_pesquisarProduto.MaximumSize = new Size(400, 35);
            txt_pesquisarProduto.MaxLength = 50;
            txt_pesquisarProduto.MouseState = MaterialSkin.MouseState.OUT;
            txt_pesquisarProduto.Multiline = false;
            txt_pesquisarProduto.Name = "txt_pesquisarProduto";
            txt_pesquisarProduto.Size = new Size(400, 35);
            txt_pesquisarProduto.TabIndex = 3;
            txt_pesquisarProduto.Text = "";
            txt_pesquisarProduto.TrailingIcon = null;
            txt_pesquisarProduto.TextChanged += txt_pesquisarProduto_TextChanged;
            // 
            // materialLabel2
            // 
            materialLabel2.AutoSize = true;
            materialLabel2.Depth = 0;
            materialLabel2.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            materialLabel2.Location = new Point(473, 3);
            materialLabel2.MouseState = MaterialSkin.MouseState.HOVER;
            materialLabel2.Name = "materialLabel2";
            materialLabel2.Size = new Size(161, 19);
            materialLabel2.TabIndex = 2;
            materialLabel2.Text = "Pesquisar por produto:";
            materialLabel2.Click += materialLabel2_Click;
            // 
            // dataGridViewProdutos
            // 
            dataGridViewProdutos.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewProdutos.Location = new Point(472, 82);
            dataGridViewProdutos.Name = "dataGridViewProdutos";
            dataGridViewProdutos.Size = new Size(485, 251);
            dataGridViewProdutos.TabIndex = 1;
            dataGridViewProdutos.CellContentClick += dataGridViewProdutos_CellContentClick;
            // 
            // Venda
            // 
            Venda.Controls.Add(materialLabel3);
            Venda.ImageKey = "icons8-dinheiro-30.png";
            Venda.Location = new Point(4, 40);
            Venda.Name = "Venda";
            Venda.Size = new Size(984, 471);
            Venda.TabIndex = 2;
            Venda.Text = "Venda";
            Venda.UseVisualStyleBackColor = true;
            Venda.Click += Venda_Click;
            // 
            // materialLabel3
            // 
            materialLabel3.AutoSize = true;
            materialLabel3.Depth = 0;
            materialLabel3.Font = new Font("Roboto", 14F, FontStyle.Regular, GraphicsUnit.Pixel);
            materialLabel3.Location = new Point(357, 139);
            materialLabel3.MouseState = MaterialSkin.MouseState.HOVER;
            materialLabel3.Name = "materialLabel3";
            materialLabel3.Size = new Size(107, 19);
            materialLabel3.TabIndex = 0;
            materialLabel3.Text = "materialLabel3";
            // 
            // imageList1
            // 
            imageList1.ColorDepth = ColorDepth.Depth32Bit;
            imageList1.ImageStream = (ImageListStreamer)resources.GetObject("imageList1.ImageStream");
            imageList1.TransparentColor = Color.Transparent;
            imageList1.Images.SetKeyName(0, "icons8-dinheiro-30.png");
            imageList1.Images.SetKeyName(1, "icons8-armazém-50.png");
            imageList1.Images.SetKeyName(2, "icons8-produto-50.png");
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = SystemColors.Control;
            ClientSize = new Size(998, 582);
            Controls.Add(materialTabControl2);
            DrawerTabControl = materialTabControl2;
            Name = "Form1";
            Text = "Radiadores Lemos";
            Load += Form1_Load;
            materialTabControl2.ResumeLayout(false);
            Produtos.ResumeLayout(false);
            Produtos.PerformLayout();
            Estoque.ResumeLayout(false);
            Estoque.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)dataGridViewProdutos).EndInit();
            Venda.ResumeLayout(false);
            Venda.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private MaterialSkin.Controls.MaterialTabControl materialTabControl1;
        private TabPage Venda;
        private TabPage tabPage2;
        private MaterialSkin.Controls.MaterialTabControl materialTabControl2;
        private TabPage Produtos;
        private TabPage Estoque;
        private ImageList imageList1;
        private MaterialSkin.Controls.MaterialLabel materialLabel1;
        private MaterialSkin.Controls.MaterialLabel materialLabel3;
        private MaterialSkin.Controls.MaterialTextBox txt_nmProduto;
        private MaterialSkin.Controls.MaterialLabel materialLabel6;
        private MaterialSkin.Controls.MaterialTextBox txt_codProduto;
        private MaterialSkin.Controls.MaterialLabel materialLabel5;
        private MaterialSkin.Controls.MaterialTextBox txt_marcaProduto;
        private MaterialSkin.Controls.MaterialLabel materialLabel4;
        private MaterialSkin.Controls.MaterialRadioButton rb_radiador;
        private MaterialSkin.Controls.MaterialRadioButton rb_caixa;
        private MaterialSkin.Controls.MaterialButton btn_cadastrarProduto;
        private DataGridView dataGridViewProdutos;
        private MaterialSkin.Controls.MaterialLabel materialLabel2;
        private MaterialSkin.Controls.MaterialTextBox txt_pesquisarProduto;
        private MaterialSkin.Controls.MaterialButton btn_Pesquisar;
        private MaterialSkin.Controls.MaterialLabel materialLabel7;
        private MaterialSkin.Controls.MaterialComboBox comboBoxProdutos;
        private MaterialSkin.Controls.MaterialButton btn_adicionarProduto;
        private MaterialSkin.Controls.MaterialTextBox txt_qtdProdutos;
    }
}
