using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Google.Cloud.Firestore;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class UsuariosUserControl : UserControl
    {
        private List<UsuarioData> usuarios = [];
        private bool usuariosCarregados = false;
        private bool precisaAtualizarUsuarios = true;

        public UsuariosUserControl()
        {
            InitializeComponent();
            CarregarDadosIniciais();
        }

        private void CarregarDadosIniciais()
        {
            if (DadosCache.Tabelas.TryGetValue("Usuarios", out List<object>? value))
            {
                usuarios = value.Cast<UsuarioData>().ToList();
                UsuariosDataGrid.ItemsSource = usuarios;
            }
        }

        private void AtualizarTabelaUsuariosCache()
        {
            if (DadosCache.Tabelas.TryGetValue("Usuarios", out List<object>? value))
            {
                usuarios = value.Cast<UsuarioData>().ToList();
                UsuariosDataGrid.ItemsSource = usuarios;
                usuariosCarregados = true;
                precisaAtualizarUsuarios = false;
            }
        }

        private async Task AtualizarTabelaUsuariosBanco()
        {
            try
            {
                var db = DatabaseConnect.Database ?? throw new InvalidOperationException("Conexão com o banco de dados não estabelecida.");
                var usuariosSnapshot = await db.Collection("Usuarios").GetSnapshotAsync();
                usuarios = usuariosSnapshot.Documents.Select(doc =>
                {
                    var usuario = doc.ConvertTo<UsuarioData>();
                    usuario.Id = doc.Id;
                    return usuario;
                }).ToList();

                DadosCache.Tabelas["Usuarios"] = usuarios.Cast<object>().ToList();
                UsuariosDataGrid.ItemsSource = usuarios;
                usuariosCarregados = true;
                precisaAtualizarUsuarios = false;
            }
            catch (Exception ex)
            {
                precisaAtualizarUsuarios = true;
                MessageBox.Show($"Erro ao carregar usuários do banco de dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AbaUsuarios_Loaded(object sender, RoutedEventArgs e)
        {
            if (!usuariosCarregados)
            {
                AtualizarTabelaUsuariosCache();
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchBox.Text.ToLower();
            var filteredUsuarios = usuarios.Where(u =>
                u.Nome.ToLower().Contains(searchText)  || 
                u.Email.ToLower().Contains(searchText) ||
                u.Matrícula.ToLower().Contains(searchText) ||
                u.Cargo.ToLower().Contains(searchText)
                ).ToList();

            UsuariosDataGrid.ItemsSource = filteredUsuarios;
        }

        private async void AtualizarDataGrid_Click(object sender, RoutedEventArgs e)
        {
            if (precisaAtualizarUsuarios)
            {
                await AtualizarTabelaUsuariosBanco();
            }
            else
            {
                AtualizarTabelaUsuariosCache();
            }
        }

        private async void EditarUsuario_Click(object sender, RoutedEventArgs e)
        {
            if (UsuariosDataGrid.SelectedItem is UsuarioData usuarioSelecionado)
            {
                var editarUsuarioWindow = new EditarUsuarioWindow(usuarioSelecionado);
                if (editarUsuarioWindow.ShowDialog() == true)
                {
                    // Atualiza o usuário na lista local
                    var usuarioEditado = new UsuarioData
                    {
                        Id = usuarioSelecionado.Id,
                        Nome = editarUsuarioWindow.NomeTextBox.Text,
                        Email = editarUsuarioWindow.EmailTextBox.Text,
                        Matrícula = editarUsuarioWindow.MatriculaTextBox.Text,
                        Senha = editarUsuarioWindow.SenhaPasswordBox.Password,
                        Cargo = (editarUsuarioWindow.PermissaoComboBox.SelectedItem as ComboBoxItem)?.Content.ToString() ?? string.Empty
                    };

                    var index = usuarios.FindIndex(u => u.Id == usuarioEditado.Id);
                    if (index >= 0)
                    {
                        usuarios[index] = usuarioEditado;
                    }

                    // Atualiza o cache local
                    DadosCache.Tabelas["Usuarios"] = usuarios.Cast<object>().ToList();

                    // Atualiza o banco de dados
                    try
                    {
                        var db = DatabaseConnect.Database ?? throw new InvalidOperationException("Conexão com o banco de dados não estabelecida.");
                        var docRef = db.Collection("Usuarios").Document(usuarioEditado.Id);
                        await docRef.SetAsync(usuarioEditado);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao atualizar usuário no banco de dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    }

                    // Atualiza a fonte de dados do DataGrid
                    UsuariosDataGrid.ItemsSource = null;
                    UsuariosDataGrid.ItemsSource = usuarios;

                    MessageBox.Show($"Usuário '{usuarioEditado.Nome}' atualizado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Selecione um usuário para editar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void AdicionarUsuario_Click(object sender, RoutedEventArgs e)
        {
            var adicionarUsuarioWindow = new EditarUsuarioWindow(null);
            if (adicionarUsuarioWindow.ShowDialog() == true)
            {
                // Cria um novo usuário com os dados da janela de edição
                var novoUsuario = new UsuarioData
                {
                    Nome = adicionarUsuarioWindow.NomeTextBox.Text,
                    Email = adicionarUsuarioWindow.EmailTextBox.Text,
                    Matrícula = adicionarUsuarioWindow.MatriculaTextBox.Text,
                    Senha = adicionarUsuarioWindow.SenhaPasswordBox.Password,
                    Cargo = (adicionarUsuarioWindow.PermissaoComboBox.SelectedItem as ComboBoxItem)?.Content.ToString()
                };

                try
                {
                    // Adiciona o novo usuário ao banco de dados
                    var db = DatabaseConnect.Database ?? throw new InvalidOperationException("Conexão com o banco de dados não estabelecida.");
                    var docRef = await db.Collection("Usuarios").AddAsync(novoUsuario);
                    novoUsuario.Id = docRef.Id;

                    // Atualiza a lista local
                    usuarios.Add(novoUsuario);

                    // Atualiza o cache local
                    DadosCache.Tabelas["Usuarios"] = usuarios.Cast<object>().ToList();

                    // Atualiza a fonte de dados do DataGrid
                    UsuariosDataGrid.ItemsSource = null;
                    UsuariosDataGrid.ItemsSource = usuarios;

                    MessageBox.Show("Usuário adicionado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao adicionar usuário ao banco de dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void DeletarUsuario_Click(object sender, RoutedEventArgs e)
        {
            if (UsuariosDataGrid.SelectedItem is UsuarioData usuarioSelecionado)
            {
                MessageBoxResult result = MessageBox.Show($"Tem certeza que deseja deletar o usuário '{usuarioSelecionado.Nome}'?",
                                                          "Confirmação de Exclusão",
                                                          MessageBoxButton.YesNo,
                                                          MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var db = DatabaseConnect.Database ?? throw new InvalidOperationException("Conexão com o banco de dados não estabelecida.");
                        var docRef = db.Collection("Usuarios").Document(usuarioSelecionado.Id);
                        await docRef.DeleteAsync();

                        // Remove o usuário da lista local
                        usuarios.Remove(usuarioSelecionado);

                        // Atualiza o cache local
                        DadosCache.Tabelas["Usuarios"] = usuarios.Cast<object>().ToList();

                        // Atualiza a fonte de dados do DataGrid
                        UsuariosDataGrid.ItemsSource = null;
                        UsuariosDataGrid.ItemsSource = usuarios;

                        MessageBox.Show("Usuário deletado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao deletar usuário do banco de dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Selecione um usuário para deletar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}
