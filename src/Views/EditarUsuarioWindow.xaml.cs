using System;
using System.Windows;
using System.Windows.Controls;
using System.Threading.Tasks;
using Google.Cloud.Firestore;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class EditarUsuarioWindow : Window
    {
        private UsuarioData usuario;

        public EditarUsuarioWindow(UsuarioData usuario = null)
        {
            InitializeComponent();
            this.usuario = usuario ?? new UsuarioData();

            // Preenche os campos com os valores do usuário existente, se fornecido
            if (usuario != null)
            {
                NomeTextBox.Text = usuario.Nome;
                EmailTextBox.Text = usuario.Email;
                PermissaoComboBox.SelectedItem = GetComboBoxItemByContent(usuario.Cargo);
            }
        }

        private async void RegistrarUsuario_Click(object sender, RoutedEventArgs e)
        {
            // Validações básicas
            if (string.IsNullOrWhiteSpace(NomeTextBox.Text) ||
                string.IsNullOrWhiteSpace(EmailTextBox.Text) ||
                string.IsNullOrWhiteSpace(SenhaPasswordBox.Password) ||
                PermissaoComboBox.SelectedItem == null)
            {
                MessageBox.Show("Por favor, preencha todos os campos.", "Erro", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Atualiza ou define os valores do objeto usuário
            usuario.Nome = NomeTextBox.Text;
            usuario.Email = EmailTextBox.Text;
            usuario.Senha = SenhaPasswordBox.Password;
            usuario.Cargo = ((ComboBoxItem)PermissaoComboBox.SelectedItem).Content.ToString();

            try
            {
                // Chama o método para atualizar o usuário no banco de dados
                await AtualizarUsuarioNoBanco(usuario);

                MessageBox.Show("Usuário atualizado com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);

                DialogResult = true; // Confirma a operação
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar usuário no banco de dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private ComboBoxItem GetComboBoxItemByContent(string content)
        {
            foreach (ComboBoxItem item in PermissaoComboBox.Items)
            {
                if (item.Content.ToString() == content)
                    return item;
            }
            return null;
        }

        // Método para atualizar ou adicionar um usuário no banco de dados
        private static async Task AtualizarUsuarioNoBanco(UsuarioData usuario)
        {
            try
            {
                // Obtém a referência ao Firestore
                var db = DatabaseConnect.Database ?? throw new InvalidOperationException("Conexão com o banco de dados não estabelecida.");

                // Verifica se o ID existe para determinar se é um novo usuário ou uma atualização
                if (string.IsNullOrEmpty(usuario.Id))
                {
                    // Adiciona um novo usuário ao Firestore e recebe o ID gerado automaticamente
                    DocumentReference docRef = await db.Collection("Usuarios").AddAsync(usuario);
                    usuario.Id = docRef.Id; // Atualiza o objeto com o ID gerado
                }
                else
                {
                    // Atualiza um usuário existente no Firestore com o ID especificado
                    DocumentReference docRef = db.Collection("Usuarios").Document(usuario.Id);
                    await docRef.SetAsync(usuario, SetOptions.Overwrite);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Erro ao atualizar usuário no banco de dados: {ex.Message}", ex);
            }
        }
    }
}
