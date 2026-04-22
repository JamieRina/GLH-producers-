using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;


namespace GLH_producers.Models
{
    public class Database
    {
        // Properties
        string connString = "Data Source=(LocalDB)\\MSSQLLocalDB;AttachDbFilename=|DataDirectory|\\GLH Producers.mdf;Integrated Security=True";
        SqlConnection connection = null;
        

        // Methods
        public static string HashPassword(string password)
        {
            byte[] tmpPassword = ASCIIEncoding.ASCII.GetBytes(password);
            byte[] tmpHash = new MD5CryptoServiceProvider().ComputeHash(tmpPassword);
            string hash = System.Text.Encoding.UTF8.GetString(tmpHash, 0, tmpHash.Length);

            return hash.Replace("'", "").Replace("\"", "").Replace(",","");
        }

        public int AuthenticateUser(string email, string password)
        {
            int result = -1;

            try
            {
                password = HashPassword(password);

                string sql = $"SELECT UserId FROM Users WHERE Email='{email}' AND PasswordHash='{password}'";

                result = Select(sql);
            }
            catch (Exception ex) { }

            return result;
        }

        public void Open() 
        {
            try
            {
                connection = new SqlConnection(connString);
                connection.Open();
            }
            catch (Exception ex) { }
        }
        public void Close() 
        { 
            try
            {
                if (connection != null)
                    connection.Close();

                connection = null;
            }
            catch (Exception ex) { }
        }

        public int Insert(string sql, bool returnId = false)
        {
            int result = 0;

            try
            {
                Open();

                // Exectute the SQl statement
                SqlCommand cmd = new SqlCommand(sql, connection);

                // Check to see if a new row was created in the Users table
                result = cmd.ExecuteNonQuery();

                if (result == 1)
                {
                    sql = "SELECT @@IDENTITY";
                    cmd = new SqlCommand(sql, connection);
                    object obj = cmd.ExecuteScalar();

                    if (obj != null && obj != DBNull.Value)
                        result = Convert.ToInt32(obj);
                }

                Close();
            }
            catch (Exception ex) { Close(); }

            return result;
        }
        public int GetId()
        {
            int result = -1;

            try
            {
                Open();

                string sql = "SELECT @@IDENTITY";
                SqlCommand cmd = new SqlCommand(sql, connection);
                object obj = cmd.ExecuteScalar();

                if (obj != null && obj != DBNull.Value)
                    result = Convert.ToInt32(obj);

                Close();
            }
            catch (Exception ex) { Close(); }

            return result;
        }

        public int Select(string sql) 
        {
            int result = -1;

            try
            {
                Open();

                SqlCommand cmd = new SqlCommand(sql, connection);
                object obj = cmd.ExecuteScalar();

                if (obj != null)
                    result = Convert.ToInt32(obj);

                Close();

            }
            catch (Exception ex) { Close(); }

            return result;
        }
    }
}
