using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;

namespace ShopifyStoreProductImporter
{
    public class ServerConnection : IDisposable
    {
        private SqlConnection _connection;

        void IDisposable.Dispose()
        {
            if (_connection != null)
            {
                _connection.Close();
                _connection.Dispose();
                _connection = null;
            }
        }

        public ServerConnection(string connectionString)
        {
            _connection = new SqlConnection(connectionString);
            _connection.Open();
        }

        public SqlConnection GetConnection()
        {
            return _connection;
        }

        public SqlCommand SetProcedure(string procedure_name)
        {
            return new SqlCommand(procedure_name, _connection)
            {
                CommandType = CommandType.StoredProcedure
            };
        }

        public SqlCommand SetStatement(string query)
        {
            return new SqlCommand(query, _connection)
            {
                CommandType = CommandType.Text
            };
        }

        public SqlTransaction BeginTransaction()
        {
            return _connection.BeginTransaction();
        }

        public SqlCommand CreateCommand()
        {
            return _connection.CreateCommand();
        }
    }

    public static class ServerConnectionExtensions
    {
        public static T GetValueOrDefault<T>(this SqlDataReader dataReader, string columnName)
        {
            var index = dataReader.GetOrdinal(columnName);
            return !dataReader.IsDBNull(index) ? (T)dataReader.GetValue(index) : default(T);
        }

        public static SqlParameter AddParameter(this SqlCommand command, string parameterName, SqlDbType sqlDbType, object value)
        {
            var parameter = command.Parameters.Add(parameterName, sqlDbType);
            parameter.Value = value;

            return parameter;
        }
    }
}