using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;
using LiteDB;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class UsuariosUserControl : UserControl
    {
        private static readonly string CollectionName = "usuarios";

        public UsuariosUserControl()
        {
            InitializeComponent();
            AtualizarTabelaUsuarios();
        }

        public void AtualizarTabelaUsuarios()
        {
            try
            {
                if (DatabaseConnect.Database == null)
                {
                    MessageBox.Show("Erro ao conectar ao banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var collection = DatabaseConnect.Database.GetCollection<UsuarioData>(CollectionName);
                var usuarios = collection.FindAll().ToList();
                UsuariosDataGrid.ItemsSource = usuarios;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao carregar usuários: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AdicionarUsuario_Click(object sender, RoutedEventArgs e)
        {
            var novoUsuario = new UsuarioData
            {
                Id = Guid.NewGuid().ToString(),
                Nome = "",
                Email = "",
                Matricula = "",
                Senha = "",
                Cargo = "Usuário"
            };

            var window = new EditarUsuarioWindow(novoUsuario);
            if (window.ShowDialog() == true)
            {
                AtualizarTabelaUsuarios();
            }
        }

        private void EditarUsuario_Click(object sender, RoutedEventArgs e)
        {
            var usuario = (sender as Button)?.DataContext as UsuarioData;
            if (usuario == null) return;

            var window = new EditarUsuarioWindow(usuario);
            if (window.ShowDialog() == true)
            {
                AtualizarTabelaUsuarios();
            }
        }

        private void DeletarUsuario_Click(object sender, RoutedEventArgs e)
        {
            var usuario = (sender as Button)?.DataContext as UsuarioData;
            if (usuario == null) return;

            var result = MessageBox.Show(
                $"Tem certeza que deseja deletar o usuário {usuario.Nome}?",
                "Confirmar exclusão",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    if (DatabaseConnect.Database == null)
                    {
                        MessageBox.Show("Erro ao conectar ao banco de dados.", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var collection = DatabaseConnect.Database.GetCollection<UsuarioData>(CollectionName);
                    collection.Delete(usuario.Id);
                    AtualizarTabelaUsuarios();
                }   
                catch (Exception ex)
                {
                    MessageBox.Show($"Erro ao deletar usuário: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void UsuariosDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var usuario = UsuariosDataGrid.SelectedItem as UsuarioData;
            if (usuario == null) return;

            var window = new EditarUsuarioWindow(usuario);
            if (window.ShowDialog() == true)
            {
                AtualizarTabelaUsuarios();
            }
        }
    }
}
