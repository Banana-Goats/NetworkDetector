using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Threading.Tasks;

public class DatabaseService
{
    private readonly string _connectionString;

    public DatabaseService()
    {
        _connectionString = ConfigurationManager.ConnectionStrings["SQL"]?.ConnectionString;
        if (string.IsNullOrEmpty(_connectionString))
        {
            throw new InvalidOperationException("Connection string is missing.");
        }
    }

    public async Task<int> ExecuteNonQueryAsync(string query, Dictionary<string, object> parameters)
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                AddParameters(command, parameters);
                return await command.ExecuteNonQueryAsync().ConfigureAwait(false);
            }
        }
    }

    public async Task<object> ExecuteScalarAsync(string query, Dictionary<string, object> parameters)
    {
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                AddParameters(command, parameters);
                return await command.ExecuteScalarAsync().ConfigureAwait(false);
            }
        }
    }

    public async Task<DataTable> ExecuteQueryAsync(string query, Dictionary<string, object> parameters)
    {
        DataTable dt = new DataTable();
        using (SqlConnection connection = new SqlConnection(_connectionString))
        {
            await connection.OpenAsync().ConfigureAwait(false);
            using (SqlCommand command = new SqlCommand(query, connection))
            {
                AddParameters(command, parameters);
                using (SqlDataReader reader = await command.ExecuteReaderAsync().ConfigureAwait(false))
                {
                    dt.Load(reader);
                }
            }
        }
        return dt;
    }

    private void AddParameters(SqlCommand command, Dictionary<string, object> parameters)
    {
        if (parameters != null)
        {
            foreach (var kvp in parameters)
            {
                // Use DBNull.Value for null parameters
                command.Parameters.AddWithValue(kvp.Key, kvp.Value ?? DBNull.Value);
            }
        }
    }
}
