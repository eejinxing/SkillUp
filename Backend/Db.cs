using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;

namespace SKILL_UP.Backend
{
    /// <summary>
    /// Thin ADO.NET wrapper around the "SkillUp" connection string in Web.config.
    /// Every statement is parameterised: @p0, @p1, ... are bound to the arguments in order.
    /// </summary>
    public sealed class Db : IDisposable
    {
        public SqlConnection Connection { get; }
        public SqlTransaction Transaction { get; private set; }

        public static string ConnectionString => ConfigurationManager.ConnectionStrings["SkillUp"].ConnectionString;

        public Db()
        {
            Connection = new SqlConnection(ConnectionString);
            Connection.Open();
        }

        public void Begin() => Transaction = Connection.BeginTransaction();

        public void Commit()
        {
            Transaction?.Commit();
            Transaction = null;
        }

        public void Dispose()
        {
            Transaction?.Dispose();
            Connection.Dispose();
        }

        SqlCommand Command(string sql, object[] args)
        {
            var command = new SqlCommand(sql, Connection, Transaction) { CommandTimeout = 60 };
            for (int i = 0; i < args.Length; i++)
            {
                var parameter = command.Parameters.AddWithValue("@p" + i, args[i] ?? DBNull.Value);
                if (args[i] is DateTime) parameter.SqlDbType = SqlDbType.DateTime2;
            }
            return command;
        }

        public int Exec(string sql, params object[] args)
        {
            using (var command = Command(sql, args)) return command.ExecuteNonQuery();
        }

        /// <summary>Runs an INSERT and returns the new IDENTITY value.</summary>
        public int Insert(string sql, params object[] args)
        {
            using (var command = Command(sql + "; SELECT CAST(SCOPE_IDENTITY() AS INT);", args)) return (int)command.ExecuteScalar();
        }

        public object Scalar(string sql, params object[] args)
        {
            using (var command = Command(sql, args)) return command.ExecuteScalar();
        }

        public List<T> Query<T>(string sql, Func<SqlDataReader, T> map, params object[] args)
        {
            var rows = new List<T>();
            using (var command = Command(sql, args))
            using (var reader = command.ExecuteReader())
                while (reader.Read()) rows.Add(map(reader));
            return rows;
        }
    }

    static class ReaderExtensions
    {
        public static string Str(this SqlDataReader r, string column)
        {
            var value = r[column];
            return value is DBNull ? null : (string)value;
        }

        public static int Int(this SqlDataReader r, string column) => Convert.ToInt32(r[column]);

        public static int? IntN(this SqlDataReader r, string column)
        {
            var value = r[column];
            return value is DBNull ? (int?)null : Convert.ToInt32(value);
        }

        public static bool Bool(this SqlDataReader r, string column) => (bool)r[column];

        public static decimal? DecN(this SqlDataReader r, string column)
        {
            var value = r[column];
            return value is DBNull ? (decimal?)null : Convert.ToDecimal(value);
        }

        public static DateTime Date(this SqlDataReader r, string column) => DateTime.SpecifyKind((DateTime)r[column], DateTimeKind.Utc);

        public static DateTime? DateN(this SqlDataReader r, string column)
        {
            var value = r[column];
            return value is DBNull ? (DateTime?)null : DateTime.SpecifyKind((DateTime)value, DateTimeKind.Utc);
        }
    }
}
