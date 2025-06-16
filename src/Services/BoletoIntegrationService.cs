using System;
using System.IO;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace WMS_RadiadoresLemos_WPF.src.Services
{
    public class BoletoIntegrationService
    {
        private HttpListener? _listener;
        private bool _isListening = false;
        private int _port = 8765; // Porta diferente

        public event Action<BoletoExtraidoWebData>? DadosRecebidos;

        public async Task IniciarServicoAsync()
        {
            try
            {
                // Tenta encontrar uma porta disponível
                _port = EncontrarPortaDisponivel();

                _listener = new HttpListener();
                _listener.Prefixes.Add($"http://localhost:{_port}/");
                _listener.Start();
                _isListening = true;

                // MessageBox.Show($"✅ Serviço de integração iniciado na porta {_port}", "Integração Ativa", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);

                await Task.Run(() => EscutarRequisicoes());
            }
            catch (HttpListenerException ex)
            {
                MessageBox.Show($"❌ Erro de permissão na porta {_port}.\n\n" +
                               "Execute o aplicativo como Administrador ou use outra porta.\n\n" +
                               $"Erro técnico: {ex.Message}",
                               "Erro de Permissão", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao iniciar serviço de integração: {ex.Message}\n\n" +
                               "Tentando modo alternativo...",
                               "Aviso", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);                                                                                                                                
            }
        }

        private int EncontrarPortaDisponivel()
        {
            // Lista de portas para tentar
            int[] portasTeste = { 8765, 8766, 8767, 8768, 8769, 9001, 9002, 9003 };

            foreach (int porta in portasTeste)
            {
                try
                {
                    var testListener = new HttpListener();
                    testListener.Prefixes.Add($"http://localhost:{porta}/");
                    testListener.Start();
                    testListener.Stop();
                    return porta; // Porta disponível
                }
                catch
                {
                    continue; // Tenta próxima porta
                }
            }

            return 8765; // Fallback
        }

        private async void EscutarRequisicoes()
        {
            while (_isListening && _listener != null)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    _ = Task.Run(() => ProcessarRequisicao(context)); // Processa assincronamente
                }
                catch (Exception ex)
                {
                    if (_isListening)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            MessageBox.Show($"Erro no serviço: {ex.Message}", "Erro", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                        });
                    }
                }
            }
        }

        private async Task ProcessarRequisicao(HttpListenerContext context)
        {
            var request = context.Request;
            var response = context.Response;

            try
            {
                // Configurar CORS
                response.Headers.Add("Access-Control-Allow-Origin", "*");
                response.Headers.Add("Access-Control-Allow-Methods", "POST, GET, OPTIONS");
                response.Headers.Add("Access-Control-Allow-Headers", "Content-Type");

                if (request.HttpMethod == "OPTIONS")
                {
                    response.StatusCode = 200;
                    response.Close();
                    return;
                }

                if (request.HttpMethod == "POST" && request.Url?.AbsolutePath == "/boleto-dados")
                {
                    using var reader = new StreamReader(request.InputStream, request.ContentEncoding);
                    string jsonData = await reader.ReadToEndAsync();

                    // 🔍 DEBUG: Mostrar JSON recebido
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        MessageBox.Show($"📋 JSON Recebido:\n\n{jsonData}", "DEBUG - JSON Raw", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
                    });

                    var dadosExtraidos = JsonSerializer.Deserialize<BoletoExtraidoWebData>(jsonData);

                    if (dadosExtraidos != null)
                    {
                        // Notifica o WPF
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            DadosRecebidos?.Invoke(dadosExtraidos);
                        });

                        // Resposta de sucesso
                        var responseData = JsonSerializer.Serialize(new
                        {
                            success = true,
                            message = "Dados recebidos com sucesso no WPF!"
                        });
                        var buffer = Encoding.UTF8.GetBytes(responseData);

                        response.ContentType = "application/json";
                        response.ContentLength64 = buffer.Length;
                        response.StatusCode = 200;

                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                    }
                    else
                    {
                        response.StatusCode = 400;
                    }
                }
                else
                {
                    response.StatusCode = 404;
                }
            }
            catch (Exception ex)
            {
                response.StatusCode = 500;
                var errorData = JsonSerializer.Serialize(new { success = false, message = ex.Message });
                var buffer = Encoding.UTF8.GetBytes(errorData);
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            }
            finally
            {
                response.Close();
            }
        }

        public void PararServico()
        {
            try
            {
                _isListening = false;
                _listener?.Stop();
                _listener?.Close();
            }
            catch { }
        }

        public int ObterPorta()
        {
            return _port;
        }
    }

    public class BoletoExtraidoWebData
    {
        public string? beneficiario { get; set; }
        public string? cnpjBeneficiario { get; set; }
        public string? cepBeneficiario { get; set; }
        public string? estadoBeneficiario { get; set; }
        public string? pagador { get; set; }
        public string? vencimento { get; set; }
        public string? valor { get; set; }
        public string? linhaDigitavel { get; set; }
        public string? nossoNumero { get; set; }
        public string? agenciaCodigoBeneficiario { get; set; }
    }
}