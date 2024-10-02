using Oracle.ManagedDataAccess.Client;
using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace WMS_RadiadoresLemos_WPF
{
    class DatabaseConnect
    {
        public void Connect()
        {
            //Enter your ADB's user id, password, and net service name
            string conString = "User Id=<admin>;Password=<Grupodoyagointegrador2>;Data Source=<radiadoreslemosdb_high>;Connection Timeout=15;";

            //Enter directory where you unzipped your cloud credentials
            string walletPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "RadiadoresLemosDB");
            OracleConfiguration.TnsAdmin = walletPath;
            OracleConfiguration.WalletLocation = OracleConfiguration.TnsAdmin;

            using (OracleConnection con = new OracleConnection(conString))
            {
                {
                    try
                    {
                        con.Open();
                        // Printa na tela confirmação de conexão
                        MessageBox.Show("Conexão com o banco de dados realizada com sucesso!");
                        Console.WriteLine("Connected to Oracle Database {0}", con.ServerVersion);
                    }
                    catch (Exception ex)
                    {
                        // Printa na tela erro de conexão
                        MessageBox.Show("Erro ao conectar com o banco de dados!");
                        Console.WriteLine(ex.Message);
                    }
                }
            }
        }
    }
}
