using System;
using System.IO;

namespace WMS_RadiadoresLemos_WPF.src.Config
{
    public static class DatabaseConfig
    {
        private static string _databasePath;

        public static string DatabasePath
        {
            get
            {
                if (string.IsNullOrEmpty(_databasePath))
                {
                    _databasePath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "WMS-RadiadoresLemos",
                        "Database.db"
                    );

                    // Garante que o diretório existe
                    var directory = Path.GetDirectoryName(_databasePath);
                    if (!Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }
                }
                return _databasePath;
            }
        }
    }
} 