using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Google.Cloud.Firestore;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;
using WMS_RadiadoresLemos_WPF.src.Views;

namespace WMS_RadiadoresLemos_WPF
{
    public partial class UsuariosUserControl : UserControl
    {
        private List<UsuarioData> usuarios = new();
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

        // Método chamado ao alterar o texto da caixa de busca
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!usuariosCarregados)
            {
                // Garante que usuários estejam sempre carregados
                AtualizarTabelaUsuariosCache();
            }
        }

        // Método para atualizar a tabela de estoque com os usuários do cache
        private void AtualizarTabelaUsuariosCache()
        {
            if (DadosCache.Tabelas.TryGetValue("Usuarios", out List<object>? value))
            {
                usuarios = value.Cast<UsuarioData>().ToList();
                UsuariosDataGrid.ItemsSource = usuarios;
                usuariosCarregados = true;
                precisaAtualizarUsuarios = false;
            }
            else
            {
                precisaAtualizarUsuarios = true;
            }
        }

        // Método chamado ao clicar no botão de atualizar tabela de usuários
        private async void AtualizarDataGrid_Click(object sender, RoutedEventArgs e)
        {
            await AtualizarTabelaUsuarios();
            MessageBox.Show("Tabela de usuários atualizada.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Método para atualizar a tabela de estoque com os usuários
        private async Task AtualizarTabelaUsuarios()
        {
            try
            {
                var db = DatabaseConnect.Database;

                if (db == null || !DatabaseConnect.IsConnected)
                {
                    // Utiliza o arquivo JSON
                    var caminhoArquivoUsuarios = new DatabaseFileManager().ObterCaminhoArquivo("Usuarios");

                    if (File.Exists(caminhoArquivoUsuarios))
                    {
                        usuarios = await DatabaseFileManager.LerDoArquivoAsync<UsuarioData>(caminhoArquivoUsuarios);
                    }
                }
                else
                {
                    // Utiliza o banco de dados normalmente
                    var usuariosSnapshot = await db.Collection("Usuarios").GetSnapshotAsync();
                    usuarios = usuariosSnapshot.Documents.Select(doc =>
                    {
                        var usuario = doc.ConvertTo<UsuarioData>();
                        usuario.Id = doc.Id;
                        return usuario;
                    }).ToList();

                    // Atualiza o cache local e a fonte de dados do DataGrid
                    DadosCache.Tabelas["Usuarios"] = usuarios.Cast<object>().ToList();
                    UsuariosDataGrid.ItemsSource = usuarios;
                    usuariosCarregados = true;
                    precisaAtualizarUsuarios = false;
                }
            }
            catch (Exception ex)
            {
                precisaAtualizarUsuarios = true;
                MessageBox.Show($"Erro ao carregar usuários do banco de dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                // Adiciona alerta
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Erro ao carregar usuários do banco de dados. Possíveis motivos:\n" +
                                            "- Falha na conexão com o banco de dados.\n" +
                                            "- Falha na leitura dos dados do banco de dados.\n" +
                                            "- Falha na conversão dos dados do banco de dados.",
                                            "- Verifique a conexão com o banco de dados.\n" +
                                            "- Verifique se os dados estão corretos e acessíveis.\n" +
                                            "- Verifique se os dados estão no formato correto.");
            }
        }

        // Método chamado ao clicar no botão de adicionar usuário
        private async void AdicionarUsuario_Click(object sender, RoutedEventArgs e)
        {
            if (UsuariosDataGrid.SelectedItem is UsuarioData usuarioSelecionado)
            {
                EditarUsuarioWindow editarUsuarioWindow = null;
                if (editarUsuarioWindow.ShowDialog() == true)
                {

                    // Atualiza o usuário no banco de dados
                    await AtualizarUsuario(usuarioSelecionado);

                    // Atualiza a fonte de dados do DataGrid
                    UsuariosDataGrid.ItemsSource = null;
                    UsuariosDataGrid.ItemsSource = usuarios;

                    // Avisa o usuário que a quantidade foi alterada
                    MessageBox.Show("Quantidade alterada com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        // Método chamado ao clicar no botão de editar usuário
        private async void EditarUsuario_Click(object sender, RoutedEventArgs e)
        {
            if (UsuariosDataGrid.SelectedItem is UsuarioData usuarioSelecionado)
            {
                EditarUsuarioWindow editarUsuarioWindow = new(usuarioSelecionado);
                if (editarUsuarioWindow.ShowDialog() == true)
                {
                    // Obtém o usuário editado
                    var usuarioEditado = editarUsuarioWindow.Usuario;

                    // Atualiza o banco de dados
                    await AtualizarUsuario(usuarioEditado);

                    // Atualiza a fonte de dados do DataGrid
                    UsuariosDataGrid.ItemsSource = null;
                    UsuariosDataGrid.ItemsSource = usuarios;

                    // Avisa o usuário que o usuário foi editado
                    MessageBox.Show("Usuário editado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        // Método chamado ao clicar no botão de deletar produto
        private async void DeletarUsuario_Click(object sender, RoutedEventArgs e)
        {
            if (UsuariosDataGrid.SelectedItem is UsuarioData usuarioSelecionado)
            {
                var confirmarSenhaWindow = new ConfirmarSenhaWindow();
                confirmarSenhaWindow.ShowDialog();

                if (confirmarSenhaWindow.IsConfirmed)
                {
                    // Exibe confirmação
                    var result = MessageBox.Show("Tem certeza que deseja deletar este usuário?", "Confirmação", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (result == MessageBoxResult.Yes)
                    {
                        // Deleta o produto do banco de dados
                        await DeletarUsuario(usuarioSelecionado);

                        // Atualiza a fonte de dados do DataGrid
                        UsuariosDataGrid.ItemsSource = null;
                        UsuariosDataGrid.ItemsSource = usuarios;

                        MessageBox.Show("Produto deletado com sucesso", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                else
                {
                    MessageBox.Show("Ação cancelada. Senha não confirmada.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            else
            {
                MessageBox.Show("Selecione um produto para deletar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // Método para atualizar um usuário
        private static async Task AtualizarUsuario(UsuarioData usuario)
        {
            var db = DatabaseConnect.Database;

            try
            {
                // Se não estiver conectado ao banco
                if (db == null || !DatabaseConnect.IsConnected)
                {
                    // Ativa modo offline
                    new MainWindow().ativarModoOffline();
                }
                else
                {
                    // Atualiza o usuário no banco de dados Firestore
                    DocumentReference docRef = db.Collection("Usuarios").Document(usuario.Id);
                    await docRef.SetAsync(usuario, SetOptions.Overwrite);
                }

                // Atualiza o cache local
                if (DadosCache.Tabelas.TryGetValue("Usuarios", out List<object>? value))
                {
                    var posicao = value.FindIndex(u => ((UsuarioData)u).Id == usuario.Id);
                    if (posicao >= 0)
                    {
                        value[posicao] = usuario;
                    }
                }

                // Atualiza o usuário no arquivo JSON
                var caminhoArquivoUsuarios = new DatabaseFileManager().ObterCaminhoArquivo("Usuarios");
                var usuarios = await DatabaseFileManager.LerDoArquivoAsync<UsuarioData>(caminhoArquivoUsuarios);
                var index = usuarios.FindIndex(u => u.Id == usuario.Id);
                if (index >= 0)
                {
                    usuarios[index] = usuario;
                    await DatabaseFileManager.SalvarNoArquivoAsync(caminhoArquivoUsuarios, usuarios);
                }
                else
                {
                    throw new Exception("Usuário não encontrado no arquivo JSON.");
                }

                // Adiciona log
                var log = new LogData
                {
                    Data = DateTime.UtcNow,
                    Tipo = "OPERACIONAL",
                    Nivel = "Usuário",
                    Detalhes = $"Usuário atualizado: {usuario.Nome}, Email: {usuario.Email}, Matrícula: {usuario.Matrícula}, Cargo: {usuario.Cargo}",
                    Usuario = MainWindow.UsuarioLogado.Nome
                };
                await LogHistorico.RegistrarLogAsync(log);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao atualizar usuário no banco de dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                // Adiciona alerta
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Erro ao atualizar usuário no banco de dados. Possíveis Motivos\n: " +
                                            "- Falha na conexão com o banco de dados;\n" +
                                            "- Falha ao atualizar o usuário no banco de dados.",
                                            "- Verifique a conexão com o banco de dados;\n" +
                                            "- Verifique se o usuário foi atualizado corretamente.");
            }
        }

        // Método para deletar um usuário
        private async Task DeletarUsuario(UsuarioData usuario)
        {
            var db = DatabaseConnect.Database;

            try
            {
                // Se não estiver conectado ao banco
                if (db == null || !DatabaseConnect.IsConnected)
                {
                    // Ativa modo offline
                    new MainWindow().ativarModoOffline();
                }
                else
                {
                    // Deleta o usuário do banco de dados Firestore
                    DocumentReference docRef = db.Collection("Usuarios").Document(usuario.Id);
                    await docRef.DeleteAsync();
                }

                // Atualiza o cache local
                if (DadosCache.Tabelas.TryGetValue("Usuarios", out List<object>? value))
                {
                    var usuarioParaRemover1 = value.FirstOrDefault(u => ((UsuarioData)u).Id == usuario.Id);
                    if (usuarioParaRemover1 != null)
                    {
                        value.Remove(usuarioParaRemover1);
                    }
                }

                // Atualiza o arquivo JSON
                var caminhoArquivoUsuarios = new DatabaseFileManager().ObterCaminhoArquivo("Usuarios");
                var usuarios = await DatabaseFileManager.LerDoArquivoAsync<UsuarioData>(caminhoArquivoUsuarios);
                var usuarioParaRemover2 = usuarios.FirstOrDefault(u => u.Id == usuario.Id);
                if (usuarioParaRemover2 != null)
                {
                    usuarios.Remove(usuarioParaRemover2);
                    await DatabaseFileManager.SalvarNoArquivoAsync(caminhoArquivoUsuarios, usuarios);
                }

                // Adiciona log
                var log = new LogData
                {
                    Data = DateTime.UtcNow,
                    Tipo = "OPERACIONAL",
                    Nivel = "Usuário",
                    Detalhes = $"Usuário deletado: {usuario.Nome}, Email: {usuario.Email}, Matrícula: {usuario.Matrícula}, Cargo: {usuario.Cargo}",
                    Usuario = MainWindow.UsuarioLogado.Nome
                };
                await LogHistorico.RegistrarLogAsync(log);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao deletar usuário no banco de dados: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);

                // Adiciona alerta
                AlertaCache.AdicionarAlerta("Erro",
                                            ex.Message.ToString(),
                                            "Erro ao deletar usuário no banco de dados. Possíveis Motivos\n: " +
                                            "- Falha na conexão com o banco de dados;\n" +
                                            "- Falha ao deletar o usuário no banco de dados.",
                                            "- Verifique a conexão com o banco de dados;\n" +
                                            "- Verifique se o usuário foi deletado corretamente.");
            }
        }

        // Método para abrir edição de usuário ao dar duplo clique
        private async void UsuariosDataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            // Exatamente igual ao método EditarUsuario_Click
            if (UsuariosDataGrid.SelectedItem is UsuarioData usuarioSelecionado)
            {
                EditarUsuarioWindow editarUsuarioWindow = new(usuarioSelecionado);
                if (editarUsuarioWindow.ShowDialog() == true)
                {
                    var usuarioEditado = editarUsuarioWindow.Usuario;
                    await AtualizarUsuario(usuarioEditado);
                    UsuariosDataGrid.ItemsSource = null;
                    UsuariosDataGrid.ItemsSource = usuarios;
                    MessageBox.Show("Usuário editado com sucesso.", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
    }
    }
}
