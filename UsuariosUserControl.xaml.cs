using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class UsuariosUserControl : UserControl
    {
        private List<Usuario> usuarios;

        public UsuariosUserControl()
        {
            InitializeComponent();
            CarregarUsuarios();
        }

        private void CarregarUsuarios()
        {
            // Exemplo de usuários fictícios
            usuarios = new List<Usuario>
            {
                new Usuario { Nome = "Ana Souza", Email = "ana.souza@example.com", Permissao = "Admin" },
                new Usuario { Nome = "Carlos Silva", Email = "carlos.silva@example.com", Permissao = "Usuário" },
                new Usuario { Nome = "Mariana Oliveira", Email = "mariana.oliveira@example.com", Permissao = "Convidado" }
            };

            UsuariosDataGrid.ItemsSource = usuarios;
        }

        private void AdicionarUsuario_Click(object sender, RoutedEventArgs e)
        {
            // Abre o modal para adicionar um novo usuário
            Usuario novoUsuario = new Usuario();
            EditarUsuarioWindow janela = new EditarUsuarioWindow(novoUsuario);

            if (janela.ShowDialog() == true)
            {
                usuarios.Add(novoUsuario); // Adiciona o novo usuário à lista
                UsuariosDataGrid.Items.Refresh(); // Atualiza o DataGrid
            }
        }

        private void EditarUsuario_Click(object sender, RoutedEventArgs e)
        {
            if (UsuariosDataGrid.SelectedItem is Usuario usuarioSelecionado)
            {
                // Abre o modal para editar o usuário selecionado
                EditarUsuarioWindow janela = new EditarUsuarioWindow(usuarioSelecionado);

                if (janela.ShowDialog() == true)
                {
                    UsuariosDataGrid.Items.Refresh(); // Atualiza o DataGrid após a edição
                }
            }
            else
            {
                MessageBox.Show("Selecione um usuário para editar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void DeletarUsuario_Click(object sender, RoutedEventArgs e)
        {
            if (UsuariosDataGrid.SelectedItem is Usuario usuarioSelecionado)
            {
                MessageBoxResult result = MessageBox.Show($"Tem certeza que deseja deletar o usuário '{usuarioSelecionado.Nome}'?",
                                                          "Confirmação de Exclusão",
                                                          MessageBoxButton.YesNo,
                                                          MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    usuarios.Remove(usuarioSelecionado); // Remove o usuário da lista
                    UsuariosDataGrid.Items.Refresh(); // Atualiza o DataGrid
                }
            }
            else
            {
                MessageBox.Show("Selecione um usuário para deletar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Função chamada quando o texto no campo de pesquisa é alterado
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchBox.Text.ToLower();

            // Filtra a lista de usuários com base no texto de pesquisa
            var filteredUsuarios = usuarios.Where(u =>
                u.Nome.ToLower().Contains(searchText) || u.Email.ToLower().Contains(searchText)).ToList();

            // Atualiza a fonte de dados do DataGrid com os resultados filtrados
            UsuariosDataGrid.ItemsSource = filteredUsuarios;
        }

        // Função chamada quando o botão de atualizar é clicado
        private void AtualizarDataGrid_Click(object sender, RoutedEventArgs e)
        {
            UsuariosDataGrid.Items.Refresh();
        }
    }
}
