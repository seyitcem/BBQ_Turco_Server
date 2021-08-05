using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Threading;
using System.Linq;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace Server
{
    class Program
    {
        static private TcpListener tcpListener = new TcpListener(IPAddress.Any, 1234);
        static private List<TcpClient> tcpClientsList = new List<TcpClient>();
        static private SqlConnection sqlConnection = new SqlConnection(@"Data Source=SEYIT-PC;Initial Catalog=DB;Integrated Security=True;");
        static private SqlCommand sqlCommand;
        static private SqlDataReader sqlDataReader;
        static void Main(string[] args)
        {
            tcpListener.Start();
            Console.WriteLine("Server is started." + "\n");

            while (true)
            {
                TcpClient tcpClient = tcpListener.AcceptTcpClient();
                tcpClientsList.Add(tcpClient);
                Thread thread_TCP = new Thread(TCPServerListener);
                thread_TCP.Start(tcpClient);
            }
        }
        static public void UDPServerSender(string message)
        {
            UdpClient client = new UdpClient();
            client.Client.SetSocketOption(SocketOptionLevel.IP, SocketOptionName.ReuseAddress, true);
            IPEndPoint ip = new IPEndPoint(IPAddress.Broadcast, 11000);
            byte[] bytes = Encoding.ASCII.GetBytes(message);
            client.Send(bytes, bytes.Length, ip);
            client.Close();
            Console.WriteLine("Sent Broadcast Message: " + message + "\n");
        }
        static public void TCPServerListener(object obj)
        {
            TcpClient tcpClient = (TcpClient)obj;
            StreamReader reader = new StreamReader(tcpClient.GetStream());

            Console.WriteLine("A client is connected.");
            Console.WriteLine("Active number of connection is: " + tcpClientsList.Count + "\n");

            while (true)
            {
                try
                {
                    string message = reader.ReadLine();
                    if (message == null)
                    {
                        throw new IOException();
                    }
                    List<string> message_splitted = message.Split().ToList();
                    Console.WriteLine(message);
                    if (message_splitted[0].ToString() == "LOGIN_ATTEMPT")
                    {
                        LoginAttempt(message_splitted, tcpClient);
                    }
                    else if (message_splitted[0].ToString() == "QUERY_GET")
                    {
                        QueryGet(message, tcpClient);
                    }
                    else if (message_splitted[0].ToString() == "QUERY_DELETE")
                    {
                        QueryDelete(message, tcpClient);
                    }
                    else if (message_splitted[0].ToString() == "QUERY_UPDATE")
                    {
                        QueryUpdate(message, tcpClient);
                    }
                    else if (message_splitted[0].ToString() == "QUERY_INSERT")
                    {
                        QueryInsert(message, tcpClient);
                    }
                    else
                    {
                        BroadCast(message, tcpClient);
                    }
                }
                catch (IOException)
                {
                    Console.WriteLine("A connection is terminated.");
                    tcpClientsList.RemoveAt(tcpClientsList.IndexOf(tcpClient));
                    Console.WriteLine("Active number of connection is: " + tcpClientsList.Count + "\n");
                    tcpClient.Close();
                    break;
                }
            }
        }
        static public void TCPServerSender(string message, TcpClient client)
        {
            StreamWriter sWriter = new StreamWriter(client.GetStream());
            Console.WriteLine("Sent message: " + message + "\n");
            sWriter.WriteLine(message);
            sWriter.Flush();
        }
        static public void QueryInsert(string message, TcpClient senderClient)
        {
            List<string> message_tokens = message.Split(' ').ToList();
            string table_name = message_tokens[1];
            message = message.Substring("QUERY_INSERT".Length + 1 + table_name.Length + 1);
            message_tokens = message.Split(',').ToList();
            List<string> column_names = new List<string>();
            List<object> values = new List<object>();
            for (int i = 0; i < message_tokens.Count; i += 2)
            {
                column_names.Add(message_tokens[i].Trim());
                values.Add(DatabaseManager.ConvertTypeByColumnType(table_name, message_tokens[i].Trim(), message_tokens[i + 1].Trim()));
            }
            int affected_rows = 0;
            try
            {
                sqlConnection.Open();
                string query = "Insert into " + table_name + "(";
                for (int i = 0; i < column_names.Count; i++)
                {
                    query += column_names[i] + ",";
                }
                query = query.TrimEnd(',') + ") values (";
                for (int i = 0; i < values.Count; i++)
                {
                    query += "@" + column_names[i] + ",";
                }
                query = query.TrimEnd(',') + ")";
                sqlCommand = new SqlCommand(query, sqlConnection);
                for (int i = 0; i < values.Count; i++)
                {
                    sqlCommand.Parameters.AddWithValue("@" + column_names[i], values[i]);

                }
                Console.WriteLine(sqlCommand.CommandText);
                affected_rows = sqlCommand.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                sqlConnection.Close();
            }
            if (affected_rows != 0)
            {
                UDPServerSender("INSERT " + table_name + " " + message);
                TCPServerSender("OK", senderClient);
            }
            else
            {
                TCPServerSender("Error !!!", senderClient);
            }
        }
        static public void QueryUpdate(string message, TcpClient senderClient)
        {
            string[] message_tokens = message.Split(' ');
            string table_name = message_tokens[1];
            message = message.Substring("QUERY_UPDATE".Length + 1 + table_name.Length + 1);
            message_tokens = message.Split(',');
            string column_name = message_tokens[0];
            object value = DatabaseManager.ConvertTypeByColumnType(table_name, column_name, message_tokens[1]);
            string[] conditions = message.Substring(column_name.Length + 1 + value.ToString().Length + 1).TrimEnd(',').Split(',');

            List<string> column_names = new List<string>();
            List<object> values = new List<object>();

            for (int i = 0; i < conditions.Length; i += 2)
            {
                column_names.Add(conditions[i].Trim());
                values.Add(DatabaseManager.ConvertTypeByColumnType(table_name, conditions[i].Trim(), conditions[i + 1].Trim()));
            }
            int affected_rows = 0;
            try
            {
                sqlConnection.Open();
                string command = "Update " + table_name + " set " + column_name + " = @value where ";
                for (int i = 0; i < values.Count; i++)
                {
                    command += column_names[i] + " = @" + column_names[i];
                    if (i + 1 != values.Count)
                    {
                        command += " and ";
                    }
                }
                sqlCommand = new SqlCommand(command, sqlConnection);
                sqlCommand.Parameters.Add(new SqlParameter("value", value));
                for (int i = 0; i < column_names.Count; i++)
                {
                    sqlCommand.Parameters.Add(new SqlParameter(column_names[i], values[i]));
                }
                Console.WriteLine(sqlCommand.CommandText);
                affected_rows = sqlCommand.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                sqlConnection.Close();
            }
            if (affected_rows != 0)
            {
                TCPServerSender("OK", senderClient);
                UDPServerSender("UPDATE " + table_name + " " + message);
            }
            else
            {
                TCPServerSender("Error !!!", senderClient);
            }
        }
        static public void QueryGet(string message, TcpClient senderClient)
        {
            List<string> message_tokens = message.Split(' ').ToList();
            string table_name = message_tokens[1];
            message = message.Substring("QUERY_GET".Length + 1 + table_name.Length + 1);
            message_tokens = message.Split(',').ToList();

            List<string> return_column_names = new List<string>();
            List<string> column_names = new List<string>();
            List<object> values = new List<object>();

            string query = "Select ";
            bool contains_where = false;
            string return_val = null;

            for (int i = 0; i < message_tokens.Count; i++)
            {
                if (!contains_where)
                {
                    if (message_tokens[i] != "{ where }")
                    {
                        return_column_names.Add(message_tokens[i]);
                        query += message_tokens[i];
                        if (i + 1 != message_tokens.Count)
                        {
                            query += ", ";
                        }
                    }
                    else
                    {
                        query = query.Trim().TrimEnd(',') + " from " + table_name + " where ";
                        contains_where = true;
                    }
                }
                else
                {
                    column_names.Add(message_tokens[i]);
                    query += column_names.Last() + " = @" + column_names.Last();
                    values.Add(DatabaseManager.ConvertTypeByColumnType(table_name, message_tokens[i], message_tokens[i + 1]));
                    i++;
                    if (i + 1 != message_tokens.Count)
                    {
                        query += " and ";
                    }
                }
            }
            if (!contains_where)
            {
                query += " from " + table_name;
            }

            Console.WriteLine("Received command: " + query);

            try
            {
                sqlConnection.Open();
                sqlCommand = new SqlCommand(query, sqlConnection);
                for (int i = 0; i < column_names.Count; i++)
                {
                    sqlCommand.Parameters.Add(new SqlParameter(column_names[i], values[i]));
                }
                sqlDataReader = sqlCommand.ExecuteReader();
                if (sqlDataReader.HasRows)
                {
                    return_val = "";
                    while (sqlDataReader.Read())
                    {
                        for (int i = 0; i < return_column_names.Count; i++)
                        {
                            if (sqlDataReader[return_column_names[i]] is bool)
                            {
                                if (Convert.ToBoolean(sqlDataReader[return_column_names[i]]))
                                {
                                    return_val += "True";
                                }
                                else
                                {
                                    return_val += "False";
                                }
                            }
                            else if (sqlDataReader[return_column_names[i]] is double)
                            {
                                return_val += sqlDataReader[return_column_names[i]].ToString().Replace(",", ".");
                            }
                            else
                            {
                                return_val += sqlDataReader[return_column_names[i]].ToString();
                            }
                            return_val += ",";
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                sqlConnection.Close();
            }
            if (return_val != null)
            {
                TCPServerSender(return_val.TrimEnd(','), senderClient);
            }
            else
            {
                TCPServerSender("null", senderClient);
            }
        }
        static public void QueryDelete(string message, TcpClient senderClient)
        {
            message = message.Substring("QUERY_DELETE".Length + 1);
            string table_name = message.Substring(0, message.IndexOf(' '));
            message = message.Substring(table_name.Length + 1);
            string[] message_tokens = message.Split(',');

            List<string> column_names = new List<string>();
            List<object> values = new List<object>();

            for (int i = 0; i < message_tokens.Length; i += 2)
            {
                column_names.Add(message_tokens[i]);
                values.Add(DatabaseManager.ConvertTypeByColumnType(table_name, message_tokens[i], message_tokens[i + 1]));
            }
            int affected_rows = 0;
            try
            {
                sqlConnection.Open();
                string command = "Delete from " + table_name + " where ";
                for (int i = 0; i < values.Count; i++)
                {
                    command += column_names[i] + " = @" + column_names[i];
                    if (i + 1 != values.Count)
                    {
                        command += " and ";
                    }
                }
                sqlCommand = new SqlCommand(command, sqlConnection);
                for (int i = 0; i < column_names.Count; i++)
                {

                    sqlCommand.Parameters.Add(new SqlParameter(column_names[i], values[i]));
                }
                Console.WriteLine(sqlCommand.CommandText);
                affected_rows = sqlCommand.ExecuteNonQuery();
            }
            catch (SqlException ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                sqlConnection.Close();
            }
            if (affected_rows != 0)
            {
                TCPServerSender("OK", senderClient);
                UDPServerSender("DELETE " + table_name + " " + message);
            }
            else
            {
                TCPServerSender("Error !!!", senderClient);
            }
        }
        static public void LoginAttempt(List<string> message, TcpClient senderClient)
        {
            string return_val = "";
            object[] row = DatabaseManager.GetUserData(message[1]);

            if (row == null)
            {
                TCPServerSender("LOGIN_DENIED WRONG_ID_OR_PASSWORD", senderClient);
                return;
            }

            string password = row[0].ToString();
            string salt = row[1].ToString();
            bool status = Convert.ToBoolean(row[2]);
            string authority = row[3].ToString();

            if (Hasher.HashPassword(message[2], salt) == password)
            {
                if (status)
                {
                    if (authority == "Cashier" || authority == "Chef" || authority == "Waiter")
                    {
                        return_val = "LOGIN_ACCEPTED " + authority;

                    }
                    else
                    {
                        return_val = "LOGIN_DENIED AUTHORIZATION";
                    }
                }
                else
                {
                    return_val = "LOGIN_DENIED NOT_ALLOWED";
                }
            }
            else
            {
                return_val = "LOGIN_DENIED WRONG_ID_OR_PASSWORD";
            }
            TCPServerSender(return_val, senderClient);
        }

        static public void BroadCast(string msg, TcpClient senderClient)
        {
            for (int i = 0; i < tcpClientsList.Count; i++)
            {
                if (tcpClientsList[i] != senderClient)       // Do not show the same message in the sender.
                {
                    if (tcpClientsList[i].Connected)        // Is connection alive??
                    {
                        StreamWriter sWriter = new StreamWriter(tcpClientsList[i].GetStream());
                        sWriter.WriteLine(msg);
                        sWriter.Flush();
                    }
                    else                                    // Delete not connected client from list.
                    {
                        tcpClientsList.RemoveAt(i);
                        i--;
                    }
                }
            }
        }
    }
}