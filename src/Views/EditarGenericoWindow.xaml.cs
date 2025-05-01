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

        private Type ObterModelo(string tabela)
        {
            return tabela.ToLower() switch
            {
                "usuarios" => typeof(UsuarioData),
                "produtos" => typeof(ProdutoData),
                "historico" => typeof(LogData),
                "movimentacoes" => typeof(MovimentacaoData),
                "alertas" => typeof(AlertaData),
                _ => throw new InvalidOperationException($"Modelo para a tabela '{tabela}' não encontrado.")
            };
        }

        private void GerarCampos()
        {
            foreach (var chave in _registro.Keys)
            {
                if (chave == "_id") continue;

                var label = new TextBlock
                {
                    Text = $"{chave}:",
                    Margin = new Thickness(0, 0, 0, 5),
                    Foreground = (Brush)FindResource("TextBrush")
                };
                CamposStackPanel.Children.Add(label);

                // Remove aspas duplas do valor antes de exibir
                var valorSemAspas = _registro[chave]?.ToString()?.Replace("\"", string.Empty) ?? string.Empty;

                var campo = new TextBox
                {
                    Text = valorSemAspas,
                    Background = (Brush)FindResource("SecondaryBrush"),
                    Foreground = (Brush)FindResource("TextBrush"),
                    Margin = new Thickness(0, 0, 0, 15)
                };

                CamposStackPanel.Children.Add(campo);
                _campos[chave] = campo;
            }
        }

        private void Salvar_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Captura os valores
                var valoresCapturados = new Dictionary<string, object>();
                foreach (var campo in _campos)
                {
                    var valorTexto = (campo.Value as TextBox)?.Text ?? string.Empty;
                    valoresCapturados[campo.Key] = valorTexto;
                }

                // Trata os valores de acordo com cada atributo e sua classe
                foreach (var chave in valoresCapturados.Keys.ToList())
                {
                    var propriedade = _modelo.GetProperty(chave);
                    if (propriedade == null) continue;

                    var tipo = propriedade.PropertyType;
                    var valorTexto = valoresCapturados[chave]?.ToString() ?? string.Empty;

                    try
                    {
                        // Converte o valor para o tipo correto
                        object? valorConvertido = tipo switch
                        {
                            Type t when t == typeof(double) =>
                                double.TryParse(valorTexto, out var doubleValue) ? doubleValue : throw new FormatException($"O valor '{valorTexto}' não é um número decimal válido."),
                            Type t when t == typeof(int) =>
                                int.TryParse(valorTexto, out var intValue) ? intValue : throw new FormatException($"O valor '{valorTexto}' não é um número inteiro válido."),
                            Type t when t == typeof(DateTime) =>
                                DateTime.TryParse(valorTexto, out var dateValue) ? dateValue : throw new FormatException($"O valor '{valorTexto}' não é uma data válida."),
                            Type t when t == typeof(string) => valorTexto, // Strings não precisam de conversão
                            _ => throw new FormatException($"O tipo '{tipo.Name}' não é suportado.")
                        };

                        valoresCapturados[chave] = valorConvertido;
                    }
                    catch (FormatException ex)
                    {
                        // Caso esteja errado, pede ao usuário para inserir um valor válido
                        MessageBox.Show(ex.Message, "Erro de Validação", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }
                }

                // Adiciona o novo dado à tabela ou edita o existente
                var db = DatabaseConnect.Database;
                var collection = db.GetCollection(_tabela);

                // Cria uma instância do modelo e preenche com os valores tratados
                var registro = Activator.CreateInstance(_modelo);
                foreach (var chave in valoresCapturados.Keys)
                {
                    var propriedade = _modelo.GetProperty(chave);
                    if (propriedade != null)
                    {
                        propriedade.SetValue(registro, valoresCapturados[chave]);
                    }
                }

                // Verifica se o registro já existe (edição) ou é novo (inserção)
                var idPropriedade = _modelo.GetProperty("Id");
                if (idPropriedade != null)
                {
                    var idValor = idPropriedade.GetValue(registro);
                    if (idValor != null && collection.Exists(Query.EQ("_id", new BsonValue(idValor))))
                    {
                        collection.Update(BsonMapper.Global.ToDocument(registro)); // Atualiza o registro existente
                    }
                    else
                    {
                        collection.Insert(BsonMapper.Global.ToDocument(registro)); // Insere um novo registro
                    }
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

        private void Cancelar_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
