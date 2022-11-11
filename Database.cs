using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;

namespace HttpServer
{
    public class Database
    {
        private readonly IDbConnection _connection;
        private readonly IDbCommand _command;

        public Database(string connectionString)
        {
            this._connection = new SqlConnection(connectionString);
            this._command = _connection.CreateCommand();
        }

        public Database AddParameter<T>(string name, T value)
        {
            SqlParameter parameter = new SqlParameter
            {
                ParameterName = name,
                Value = value
            };
            _command.Parameters.Add(parameter);
            return this;
        }

        public int ExecuteNonQuery(string query, bool isStoredProcedure = false)
        {
            int affected;
            using (_connection)
            {
                if (isStoredProcedure)
                {
                    _command.CommandType = CommandType.StoredProcedure;
                }
                _command.CommandText = query;
                _connection.Open();
                affected = _command.ExecuteNonQuery();
            }

            return affected;
        }

        public IEnumerable<T> ExecuteQuery<T>(string query, bool isStoredProcedure = false)
        {
            IList<T> result = new List<T>();
            Type t = typeof(T);

            using (_connection)
            {
                if (isStoredProcedure)
                {
                    _command.CommandType = CommandType.StoredProcedure;
                }
                _command.CommandText = query;
                _connection.Open();
                var reader = _command.ExecuteReader();
                while (reader.Read())
                {
                    T obj = (T)Activator.CreateInstance(t);
                    t.GetProperties().ToList().ForEach(p =>
                    {
                        p.SetValue(obj, reader[p.Name]);
                    });
                    result.Add(obj);
                }
            }

            return result;
        }

        public T ExecuteScalar<T>(string query, bool isStoredProcedure = false)
        {
            T result = default(T);
            using (_connection)
            {
                if (isStoredProcedure)
                {
                    _command.CommandType = CommandType.StoredProcedure;
                }
                _command.CommandText = query;
                _connection.Open();
                result = (T)_command.ExecuteScalar();
            }

            return result;
        }

        public IEnumerable<T> Select<T>()
        {
            string t = typeof(T).Name;
            string query = $"SELECT * FROM {t.ToString().Split(".")[^1]}s";
            return ExecuteQuery<T>(query);
        }

        public int Insert<T>(T obj)
        {
            Type t = typeof(T);
            string query = $"INSERT INTO {t.ToString().Split(".")[^1]}s VALUES(";
            t.GetProperties().ToList().ForEach(p =>
            {
                var value = p.GetValue(obj);
                if (p.Name != "Id")
                {
                    AddParameter(p.Name, value);
                    query += "@" + p.Name + ",";
                    Console.WriteLine(this._command.Parameters);
                }
            });
            return ExecuteNonQuery(query[..^1] + ")");
        }

        public int Delete<T>(T obj)
        {
            Type t = typeof(T);
            string query = $"DELETE FROM {t}s WHERE ";
            t.GetProperties().ToList().ForEach(p =>
            {
                AddParameter(p.Name, obj.GetType().GetField(p.Name)?.GetValue(obj));
                query += p.Name + " = @" + p.Name + " AND ";
            });
            return ExecuteNonQuery(query[..^4] + ";");
        }
    }
}
