using System.Windows;

public class ThemeService
{
    public enum Theme
    {
        Light,
        Dark,
        Midnight
    }

    public void ChangeTheme(Theme theme)
    {
        // Remove o dicionário de tema atual
        var themeDictionaries = Application.Current.Resources.MergedDictionaries
            .Where(d => d.Source != null && d.Source.ToString().Contains("ThemeColors"))
            .ToList();
        
        foreach (var dict in themeDictionaries)
        {
            Application.Current.Resources.MergedDictionaries.Remove(dict);
        }
        
        // Adiciona o novo tema
        var themeName = theme.ToString();
        var newTheme = new ResourceDictionary
        {
            Source = new Uri($"/Resources/ThemeColors/{themeName}Theme.xaml", UriKind.Relative)
        };
        
        Application.Current.Resources.MergedDictionaries.Add(newTheme);
    }
}