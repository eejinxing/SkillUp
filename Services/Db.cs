using Npgsql;
using NpgsqlTypes;

namespace SkillUp.Services;

/// <summary>
/// Thin Npgsql wrapper used for one request (and, for a command, one transaction).
/// Every statement is parameterised: @p0, @p1, ... are bound to the arguments in order.
/// </summary>
public sealed class Db : IDisposable
{
    readonly NpgsqlConnection connection;
    NpgsqlTransaction transaction;

    public Db(NpgsqlDataSource source) => connection = source.OpenConnection();

    public void Begin() => transaction = connection.BeginTransaction();

    public void Commit()
    {
        transaction?.Commit();
        transaction = null;
    }

    public void Dispose()
    {
        transaction?.Dispose();   // an open transaction that was never committed rolls back
        connection.Dispose();
    }

    /// <summary>True for a unique-constraint violation, which the API reports as 409.</summary>
    public static bool IsDuplicate(Exception e) => e is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    NpgsqlCommand Command(string sql, object[] args)
    {
        var command = new NpgsqlCommand(sql, connection, transaction) { CommandTimeout = 60 };
        for (var i = 0; i < args.Length; i++) command.Parameters.Add(Parameter("p" + i, args[i]));
        return command;
    }

    /// <summary>
    /// Times are stored as timestamptz (UTC). A local DateTime is converted on the way in. A DateTime of unspecified kind is a
    /// calendar day (the course start date and deadline), so it is sent as a plain timestamp and cast to DATE by the column.
    /// </summary>
    static NpgsqlParameter Parameter(string name, object value)
    {
        switch (value)
        {
            case null:
                return new NpgsqlParameter(name, DBNull.Value);
            case DateTime { Kind: DateTimeKind.Unspecified } day:
                return new NpgsqlParameter(name, NpgsqlDbType.Timestamp) { Value = day };
            case DateTime time:
                return new NpgsqlParameter(name, NpgsqlDbType.TimestampTz) { Value = time.ToUniversalTime() };
            case byte b:
                return new NpgsqlParameter(name, (short)b);
            default:
                return new NpgsqlParameter(name, value);
        }
    }

    public int Exec(string sql, params object[] args)
    {
        using var command = Command(sql, args);
        return command.ExecuteNonQuery();
    }

    /// <summary>
    /// Runs an INSERT into a table with an identity column and returns the new id.
    /// lastval() is the value the identity sequence handed out last on this connection, which is this insert's row.
    /// </summary>
    public int Insert(string sql, params object[] args)
    {
        Exec(sql, args);
        return Scalar("SELECT lastval()") is int id ? id : 0;
    }

    /// <summary>The first column of the first row, or null. COUNT(*) comes back as bigint; it is narrowed to int.</summary>
    public object Scalar(string sql, params object[] args)
    {
        using var command = Command(sql, args);
        var value = command.ExecuteScalar();
        return value switch
        {
            DBNull => DBNull.Value,
            long l => checked((int)l),
            _ => value
        };
    }

    public List<T> Query<T>(string sql, Func<NpgsqlDataReader, T> map, params object[] args)
    {
        var rows = new List<T>();
        using var command = Command(sql, args);
        using var reader = command.ExecuteReader();
        while (reader.Read()) rows.Add(map(reader));
        return rows;
    }
}

/// <summary>Typed column readers: r.Str("title"), r.IntN("program_leader_id"), r.Date("created_at"), ...</summary>
static class ReaderExtensions
{
    public static string Str(this NpgsqlDataReader r, string column)
    {
        var value = r[column];
        return value is DBNull ? null : (string)value;
    }

    public static int Int(this NpgsqlDataReader r, string column) => Convert.ToInt32(r[column]);

    public static int? IntN(this NpgsqlDataReader r, string column)
    {
        var value = r[column];
        return value is DBNull ? null : Convert.ToInt32(value);
    }

    public static bool Bool(this NpgsqlDataReader r, string column) => (bool)r[column];

    public static decimal? DecN(this NpgsqlDataReader r, string column)
    {
        var value = r[column];
        return value is DBNull ? null : Convert.ToDecimal(value);
    }

    public static DateTime Date(this NpgsqlDataReader r, string column) => Local(AsDateTime(r[column]));

    public static DateTime? DateN(this NpgsqlDataReader r, string column)
    {
        var value = r[column];
        return value is DBNull ? null : Local(AsDateTime(value));
    }

    /// <summary>A DATE column arrives as DateOnly, a timestamp as DateTime.</summary>
    static DateTime AsDateTime(object value) => value is DateOnly day ? day.ToDateTime(TimeOnly.MinValue) : (DateTime)value;

    /// <summary>timestamptz arrives as UTC and is shown in the server's local time; a DATE has no zone and is labelled local.</summary>
    static DateTime Local(DateTime value) =>
        value.Kind == DateTimeKind.Utc ? value.ToLocalTime() : DateTime.SpecifyKind(value, DateTimeKind.Local);
}
