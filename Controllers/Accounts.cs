using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using HttpServer.Attributes;
using HttpServer.Models;

namespace HttpServer.Controllers
{
    [HttpController("accounts")]
    public class Accounts
    {
        string connectionString =
            @"Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=SteamDB;Integrated Security=True";
        
        [HttpGET("getaccounts")] 
        public List<Account> GetAccounts()
        {
            var list = new List<Account>();
            
            string sqlExpression = "SELECT * FROM  [dbo].[Table]";
            
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(sqlExpression, connection);
                SqlDataReader reader = command.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        list.Add(new Account
                        {
                            Id = Convert.ToInt32(reader.GetValue(0)),
                            Login = reader.GetValue(1).ToString(),
                            Password = reader.GetValue(2).ToString()
                        });
                    }
                }
                connection.Close();
            }
            return list;
        }
        
        [HttpGET("id")]
        public Account GetAccountById(int id)
        {
            Account acc = null;
            string sqlExpression = $"SELECT * FROM Accounts WHERE Id={id}";
            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(sqlExpression, connection);
                SqlDataReader reader = command.ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        acc = new Account
                        {
                            Id = Convert.ToInt32(reader.GetValue(0)),
                            Login = reader.GetValue(1).ToString(),
                            Password = reader.GetValue(2).ToString()
                        };
                    }
                }

                connection.Close();
            }

            return acc;
        }
        
        [HttpPOST("saveaccount")] //add
        public string SaveAccount(string login, string pass)
        {
            var fixedPass = pass.Replace("%20", " ")
                                        .Replace("%27", "'")
                                        .Replace("%2C", ",")
                                        .Replace("%28", "(")
                                        .Replace("%29", ")")
                                        .Replace("%3B", ";")
                                        .Replace("+", " ");
            string sqlExpression = $"INSERT INTO Accounts(Login,Password) VALUES ('{login}','{fixedPass}')";

            using (SqlConnection connection = new SqlConnection(connectionString))
            {
                connection.Open();
                SqlCommand command = new SqlCommand(sqlExpression, connection);
                int number = command.ExecuteNonQuery();

                connection.Close();
            }
            return "Steam_redirect";
        }
    }
}