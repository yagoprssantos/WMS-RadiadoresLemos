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
        public bool IsConnected { get; private set; }

        public void Connect()
        {
            // String de conexão com as credenciais do banco de dados
            string conString = "User Id=<admin>;Password=<Grupodoyagointegrador2>;Data Source=<radiadoreslemosdb_high>;Connection Timeout=60;";

            // Caminho para a pasta da Wallet usando sqlnet.ora e tnsnames.ora
            string walletPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Wallet");

            // para confirmar o caminho da wallet
            // MessageBox.Show(walletPath);

            OracleConfiguration.TnsAdmin = walletPath;
            OracleConfiguration.WalletLocation = walletPath;

            using (OracleConnection con = new OracleConnection(conString))
            {
                using (OracleCommand cmd = con.CreateCommand())
                {
                    try
                    {
                        con.Open();
                        // Printa na tela confirmação de conexão
                        Console.WriteLine("Connected to Oracle Database {0}", con.ServerVersion);
                        IsConnected = true;
                    }
                    catch (OracleException ex)
                    {
                        // Printa na tela erro de conexão
                        MessageBox.Show("Erro ao conectar com o banco de dados: " + ex.Message);
                        Console.WriteLine("OracleException: " + ex.Message);
                        Console.WriteLine("Error Code: " + ex.ErrorCode);
                        Console.WriteLine("Data Source: " + ex.DataSource);
                        Console.WriteLine("Stack Trace: " + ex.StackTrace);
                        IsConnected = false;
                    }
                    catch (Exception ex)
                    {
                        // Printa na tela erro de conexão
                        MessageBox.Show("Erro ao conectar com o banco de dados: " + ex.Message);
                        Console.WriteLine("Exception: " + ex.Message);
                        Console.WriteLine("Stack Trace: " + ex.StackTrace);
                        IsConnected = false;
                    }
                }
            }
        }
    }
}
