using System;
using System.Windows;
using System.Windows.Controls;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class EditarUsuarioWindow : Window
    {
        private Usuario usuario;

        public EditarUsuarioWindow(Usuario usuario)
        {
            InitializeComponent();
            this.usuario = usuario;

            // Preenche os campos com os valores existentes, se houver
            NomeUsuario.Text = usuario.Nome;
            LoginUsuario.Text = usuario.Email; // Supondo que o login seja o email
        }

        private void Salvar_Click(object sender, RoutedEventArgs e)
        {
            // Validações básicas
            if (string.IsNullOrWhiteSpace(NomeUsuario.Text) || string.IsNullOrWhiteSpace(LoginUsuario.Text) || string.IsNullOrWhiteSpace(SenhaUsuario.Password))
            {
                MessageBox.Show("Por favor, preencha todos os campos.", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Atualiza o objeto usuário
            usuario.Nome = NomeUsuario.Text;
            usuario.Email = LoginUsuario.Text;
            // Aqui você pode adicionar lógica para atualizar a senha, se necessário

            DialogResult = true; // Confirma a operação
            Close();
        }

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // Cancela a operação
            Close();
        }

        private void TextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            // Lógica para tratar mudanças nos campos de texto, se necessário
        }
    }
}
