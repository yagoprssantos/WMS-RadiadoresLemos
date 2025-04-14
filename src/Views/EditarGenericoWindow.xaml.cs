using LiteDB;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using WMS_RadiadoresLemos_WPF.src.Models;
using WMS_RadiadoresLemos_WPF.src.Services;

namespace WMS_RadiadoresLemos_WPF.src.Views
{
    /// <summary>
    /// Lógica interna para EditarGenericoWindow.xaml
    /// </summary>
    public partial class EditarGenericoWindow : Window
    {
        private readonly BsonDocument _registro;
        private readonly string _tabela;
        private readonly Type _modelo;
        private readonly Dictionary<string, Control> _campos = new();

        public EditarGenericoWindow(string tabela, BsonDocument? registro = null)
        {
            InitializeComponent();
            _tabela = tabela;
            _registro = registro ?? new BsonDocument();
            _modelo = ObterModelo(tabela);
            GerarCampos();
        }

        // Obtém o modelo correspondente à tabela
        private Type ObterModelo(string tabela)
        {
            return tabela.ToLower() switch
            {
                "usuarios" => typeof(UsuarioData),
                "produtos" => typeof(ProdutoData),
                "historico" => typeof(LogData),
                "movimentacoes" => typeof(MovimentacaoData),
                _ => throw new InvalidOperationException($"Modelo para a tabela '{tabela}' não encontrado.")
            };
        }

        // Gera os campos dinamicamente com base nas chaves do BsonDocument
        private void GerarCampos()
        {
            foreach (var chave in _registro.Keys)
            {
                // Ignorar a chave "_id"
                if (chave == "_id") continue;

                // Cria um rótulo para o campo
                var label = new TextBlock
                {
                    Text = $"{chave}:",
                    Margin = new Thickness(0, 0, 0, 5),
                    Foreground = (Brush)FindResource("TextBrush")
                };
                CamposStackPanel.Children.Add(label);

                // Cria um TextBox para editar o valor
                var campo = new TextBox
                {
                    Text = _registro[chave]?.ToString() ?? string.Empty,
                    Background = (Brush)FindResource("SecondaryBrush"),
                    Foreground = (Brush)FindResource("TextBrush"),
                    Margin = new Thickness(0, 0, 0, 15)
                };

                // Adiciona o controle ao painel e ao dicionário
                CamposStackPanel.Children.Add(campo);
                _campos[chave] = campo;
            }
        }

        // Evento disparado ao clicar no botão "Salvar"
        private void Salvar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                foreach (var campo in _campos)
                {
                    var valor = (campo.Value as TextBox)?.Text ?? string.Empty;
                    _registro[campo.Key] = valor; // Atualiza o valor no BsonDocument
                }

                var db = DatabaseConnect.Database;
                var collection = db.GetCollection(_tabela);

                if (_registro.ContainsKey("_id"))
                {
                    collection.Update(_registro); // Atualiza o registro existente
                }
                else
                {
                    collection.Insert(_registro); // Insere um novo registro
                }

                MessageBox.Show("Registro salvo com sucesso!", "Sucesso", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao salvar registro: {ex.Message}", "Erro", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Evento disparado ao clicar no botão "Cancelar"
        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
