using Oracle.ManagedDataAccess.Client;
using System;
using System.Configuration;
using System.Windows;

namespace WMS_RadiadoresLemos_WPF
{
    public class DatabaseConnect
    {
        public bool IsConnected { get; private set; }
        static DatabaseConnect()
        {
            // Caminho para a pasta da Wallet usando sqlnet.ora e tnsnames.ora
            string walletPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Wallet");

            // Configurações do Oracle para usar a Wallet
            OracleConfiguration.TnsAdmin = walletPath;
            OracleConfiguration.WalletLocation = walletPath;
        }

        public void Connect()
        {
            // String de conexão com as credenciais do banco de dados
            string conString = "User Id=<admin>;Password=<Grupodoyagointegrador2>;Data Source=<radiadoreslemosdb_high>;Connection Timeout=60;";

            // Cria uma nova conexão Oracle
            using (OracleConnection con = new OracleConnection(conString))
            {
                using (OracleCommand cmd = con.CreateCommand())
                {
                    try
                    {
                        // Abre a conexão
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

                        // Fecha a conexão
                        con.Close();

                        // Deixa a conexão como falsa
                        IsConnected = false;
                    }
                    catch (Exception ex)
                    {
                        // Printa na tela erro de conexão
                        MessageBox.Show("Erro ao conectar com o banco de dados: " + ex.Message);
                        Console.WriteLine("Exception: " + ex.Message);
                        Console.WriteLine("Stack Trace: " + ex.StackTrace);

                        // Fecha a conexão
                        con.Close();

                        // Deixa a conexão como falsa
                        IsConnected = false;
                    }
                }
            }
        }

        public void Disconnect()
        {
            // String de conexão com as credenciais do banco de dados
            string conString = "User Id=<admin>;Password=<Grupodoyagointegrador2>;Data Source=<radiadoreslemosdb_high>;Connection Timeout=60;";

            // Cria uma nova conexão Oracle
            using (OracleConnection con = new OracleConnection(conString))
            {
                try
                {
                    // Fecha a conexão
                    con.Close();
                    // Printa na tela confirmação de desconexão
                    Console.WriteLine("Disconnected from Oracle Database");
                    IsConnected = false;
                }
                catch (OracleException ex)
                {
                    // Printa na tela erro de desconexão
                    MessageBox.Show("Erro ao desconectar do banco de dados: " + ex.Message);
                    Console.WriteLine("OracleException: " + ex.Message);
                    Console.WriteLine("Error Code: " + ex.ErrorCode);
                    Console.WriteLine("Data Source: " + ex.DataSource);
                    Console.WriteLine("Stack Trace: " + ex.StackTrace);
                    IsConnected = true;
                }
                catch (Exception ex)
                {
                    // Printa na tela erro de desconexão
                    MessageBox.Show("Erro ao desconectar do banco de dados: " + ex.Message);
                    Console.WriteLine("Exception: " + ex.Message);
                    Console.WriteLine("Stack Trace: " + ex.StackTrace);
                    IsConnected = true;
                }
            }
        }
    }
}
