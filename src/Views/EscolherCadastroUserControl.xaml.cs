using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    public partial class EscolherCadastroUserControl : UserControl
    {
        public EscolherCadastroUserControl()
        {
            InitializeComponent();
        }

        private void BtnProdutos_Click(object sender, RoutedEventArgs e)
        {
            // Cria uma instância do CadastroUserControl com a tabela Produtos já selecionada
            var cadastroControl = new CadastroUserControl("Produtos");

            // Navega para a nova tela e atualiza o título e ícone
            NavigateTo(
                cadastroControl,
                "Cadastro de Produtos",
                "/assets/Icons/Selected/CaixaS-2.png"
            );
        }

        private void BtnClientes_Click(object sender, RoutedEventArgs e)
        {
            var cadastroControl = new CadastroUserControl("Clientes");
            NavigateTo(
                cadastroControl,
                "Cadastro de Clientes",
                "/assets/Icons/Selected/ClientesS.png"
            );
        }

        private void BtnUsuarios_Click(object sender, RoutedEventArgs e)
        {
            var cadastroControl = new CadastroUserControl("Usuários");
            NavigateTo(
                cadastroControl,
                "Cadastro de Usuários",
                "/assets/Icons/Selected/UsuárioS.png" 
            );
        }

        private void BtnFornecedores_Click(object sender, RoutedEventArgs e)
        {
            var cadastroControl = new CadastroUserControl("Fornecedores");
            NavigateTo(
                cadastroControl,
                "Cadastro de Fornecedores",
                "/assets/Icons/Selected/FornecedoresS.png"
            );
        }

        // Método para navegar para a tela de cadastro
        private void NavigateTo(UserControl userControl, string title, string iconPath)
        {
            // Encontra a janela principal
            Window mainWindow = Window.GetWindow(this);

            if (mainWindow != null)
            {
                // Obtém o ContentControl chamado "ContentArea" na janela principal
                ContentControl contentArea = (ContentControl)mainWindow.FindName("ContentArea");
                if (contentArea != null)
                {
                    // Define o novo conteúdo
                    contentArea.Content = userControl;

                    // Atualiza o título
                    var titleTextBlock = (TextBlock)mainWindow.FindName("TitleTextBlock");
                    if (titleTextBlock != null)
                    {
                        titleTextBlock.Text = title;
                    }

                    // Atualiza o ícone
                    var iconImage = (Image)mainWindow.FindName("IconImage");
                    if (iconImage != null)
                    {
                        iconImage.Source = new BitmapImage(new Uri(iconPath, UriKind.Relative));
                    }
                }
            }
        }
        private void BtnProdutos_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            IconProdutos.Source = new BitmapImage(new Uri("/assets/Icons/Selected/CaixaS-2.png", UriKind.Relative)); // MUDAR - FICA COR AZUL
        }

        private void BtnProdutos_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            IconProdutos.Source = new BitmapImage(new Uri("/assets/Icons/NotSelected/CaixaNS.png", UriKind.Relative));
        }

        private void BtnClientes_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            IconClientes.Source = new BitmapImage(new Uri("/assets/Icons/Selected/ClientesS.png", UriKind.Relative)); // MUDAR 
        }

        private void BtnClientes_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            IconClientes.Source = new BitmapImage(new Uri("/assets/Icons/NotSelected/ClientesNS.png", UriKind.Relative)); // MUDAR
        }

        private void BtnUsuarios_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            IconUsuarios.Source = new BitmapImage(new Uri("/assets/Icons/Selected/UsuárioS.png", UriKind.Relative));
        }

        private void BtnUsuarios_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            IconUsuarios.Source = new BitmapImage(new Uri("/assets/Icons/NotSelected/UsuárioNS.png", UriKind.Relative));
        }

        private void BtnFornecedores_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            IconFornecedores.Source = new BitmapImage(new Uri("/assets/Icons/Selected/FornecedoresS.png", UriKind.Relative)); // MUDAR
        }

        private void BtnFornecedores_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            IconFornecedores.Source = new BitmapImage(new Uri("/assets/Icons/NotSelected/FornecedoresNS.png", UriKind.Relative)); // MUDAR
        }
    }
}