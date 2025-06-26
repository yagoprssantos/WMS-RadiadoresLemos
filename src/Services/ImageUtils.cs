using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WMS_RadiadoresLemos_WPF.src.Services
{
    public static class ImageUtils
    {
        public static BitmapSource ColorizeImage(string imagePath, Color color)
        {
            try
            {
                // Garante que o caminho seja tratado como um recurso da aplicação
                Uri resourceUri;
                if (imagePath.StartsWith("/"))
                {
                    // Criar um Pack URI apropriado para recursos
                    resourceUri = new Uri($"pack://application:,,,{imagePath}", UriKind.Absolute);
                }
                else
                {
                    resourceUri = new Uri(imagePath, UriKind.Relative);
                }

                // Carrega a imagem original
                BitmapImage originalImage = new BitmapImage();
                originalImage.BeginInit();
                originalImage.UriSource = resourceUri;
                originalImage.CacheOption = BitmapCacheOption.OnLoad;
                originalImage.EndInit();
                originalImage.Freeze(); // Melhora o desempenho
                
                // Converte para formato que pode ser modificado
                FormatConvertedBitmap formattedBitmap = new FormatConvertedBitmap();
                formattedBitmap.BeginInit();
                formattedBitmap.Source = originalImage;
                formattedBitmap.DestinationFormat = PixelFormats.Pbgra32;
                formattedBitmap.EndInit();
                
                // Cria uma imagem editável
                WriteableBitmap writeableBitmap = new WriteableBitmap(formattedBitmap);
                
                // Obtém os pixels da imagem
                int stride = writeableBitmap.PixelWidth * 4;
                byte[] pixels = new byte[stride * writeableBitmap.PixelHeight];
                writeableBitmap.CopyPixels(pixels, stride, 0);
                
                // Modifica os pixels, preservando o canal alfa
                for (int i = 0; i < pixels.Length; i += 4)
                {
                    if (pixels[i + 3] > 0) // Se o pixel não é totalmente transparente
                    {
                        // Aplicar a nova cor, preservando o canal alfa
                        pixels[i] = color.B;     // Blue
                        pixels[i + 1] = color.G; // Green
                        pixels[i + 2] = color.R; // Red
                        // Mantém o canal alfa original
                    }
                }
                
                // Atualiza a imagem com os novos pixels
                writeableBitmap.WritePixels(
                    new Int32Rect(0, 0, writeableBitmap.PixelWidth, writeableBitmap.PixelHeight),
                    pixels, stride, 0);
                
                return writeableBitmap;
            }
            catch (Exception ex)
            {
                // Em caso de erro, retorna uma imagem vazia e registra o erro
                System.Diagnostics.Debug.WriteLine($"Erro ao colorizar imagem: {ex.Message}");
                return new WriteableBitmap(1, 1, 96, 96, PixelFormats.Pbgra32, null);
            }
        }
    }
}