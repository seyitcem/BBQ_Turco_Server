using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace Server
{
    class DatabaseManager
    {
        static public SqlConnection sqlConnection = new SqlConnection(@"Data Source=SEYIT-PC;Initial Catalog=DB;Integrated Security=True;");
        static public SqlCommand sqlCommand;
        static public SqlDataReader sqlDataReader;

        static public object[] GetUserData(string username)
        {
            object[] list = null;
            try
            {
                sqlConnection.Open();
                sqlCommand = new SqlCommand("Select password, salt, status, authority from Users where username = @username", sqlConnection);
                sqlCommand.Parameters.Add(new SqlParameter("@username", username));
                Console.WriteLine(sqlCommand.CommandText);
                sqlDataReader = sqlCommand.ExecuteReader();
                if (sqlDataReader.Read())
                {
                    list = new object[4];
                    list[0] = sqlDataReader["password"];
                    list[1] = sqlDataReader["salt"];
                    list[2] = sqlDataReader["status"];
                    list[3] = sqlDataReader["authority"];
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
            return list;
        }
        static public object ConvertTypeByColumnType(string table_name, string column_name, string value)
        {
            object return_val = null;
            try
            {
                sqlConnection.Open();
                string cmd = "Select DATA_TYPE from INFORMATION_SCHEMA.COLUMNS where table_name = @table_name AND column_name = @column_name";
                sqlCommand = new SqlCommand(cmd, sqlConnection);
                sqlCommand.Parameters.Add(new SqlParameter("table_name", table_name));
                sqlCommand.Parameters.Add(new SqlParameter("column_name", column_name));
                sqlDataReader = sqlCommand.ExecuteReader();
                if (sqlDataReader.Read())
                {
                    if (sqlDataReader["DATA_TYPE"].ToString() == "varchar")
                    {
                        return_val = value.ToString().Trim();
                    }
                    else if (sqlDataReader["DATA_TYPE"].ToString() == "int")
                    {
                        return_val = Convert.ToInt32(value);
                    }
                    else if (sqlDataReader["DATA_TYPE"].ToString() == "bit")
                    {
                        return_val = value.ToString() == "True" ? true : false;
                    }
                    else if (sqlDataReader["DATA_TYPE"].ToString() == "float")
                    {
                        return_val = Convert.ToDouble(value.Replace('.',','));
                    }
                    else
                    {
                        Console.WriteLine("Undefined data type.");
                        return_val = value;
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
            return return_val;
        }
    }
}
