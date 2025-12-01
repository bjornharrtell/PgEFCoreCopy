using System.Data;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Npgsql;
using NpgsqlTypes;

namespace Wololo.PgEFCoreCopy;

public record struct ExecuteInsertRangeOptions(bool IncludePrimaryKey = false)
{
}

public static class DbContextExtensions
{
    public static async ValueTask<ulong> ExecuteInsertRangeAsync<T>(this DbContext context, IEnumerable<T> entities, ExecuteInsertRangeOptions options = new ExecuteInsertRangeOptions(), CancellationToken token = default) where T : class
    {
        var entityType = context.Model.FindEntityType(typeof(T)) ??
            throw new InvalidOperationException($"Type {typeof(T).Name} is not an entity type in this context.");

        var schemaName = entityType.GetSchema();
        var tableName = entityType.GetTableName();
        if (string.IsNullOrEmpty(tableName))
            throw new InvalidOperationException($"Entity type {typeof(T).Name} does not map to a table.");

        var properties = entityType.GetProperties()
            .Where(p => !p.IsShadowProperty() && !(!options.IncludePrimaryKey && p.IsPrimaryKey()))
            .ToList();

        var columnNames = properties
            .Select(p => p.GetColumnName())
            .Where(name => !string.IsNullOrEmpty(name))
            .ToList();

        if (columnNames.Count == 0)
            throw new InvalidOperationException($"No columns found for entity type {typeof(T).Name}.");

        var columns = string.Join(", ", columnNames.Select(c => $"\"{c}\""));

        if (context.Database.GetDbConnection() is not NpgsqlConnection conn)
            throw new InvalidOperationException("Database connection is not a NpgsqlConnection.");
        
        var wasConnectionOpen = conn.State == ConnectionState.Open;
        if (!wasConnectionOpen)
            await conn.OpenAsync(token);
            
        var tableIdentifier = schemaName == null ? $"\"{tableName}\"" : $"\"{schemaName}\".\"{tableName}\"";
        using var writer = await conn.BeginBinaryImportAsync($"COPY {tableIdentifier} ({columns}) FROM STDIN (FORMAT BINARY)", token);

        foreach (var entity in entities)
        {
            await writer.StartRowAsync(token);
            foreach (var property in properties)
            {
                var propertyInfo = typeof(T).GetProperty(property.Name);
                if (propertyInfo == null)
                    continue;
                var value = propertyInfo.GetValue(entity);
                if (value == null)
                {
                    await writer.WriteNullAsync(token);
                    continue;
                }
                var npgsqlDbType = MapToNpgsqlDbType(property.ClrType);
                await writer.WriteAsync(value, npgsqlDbType, token);
            }
        }

        var rows = await writer.CompleteAsync(token);
        
        if (!wasConnectionOpen)
            await conn.CloseAsync();

        return rows;
    }

    private static NpgsqlDbType MapToNpgsqlDbType(Type type)
    {
        if (type == typeof(int) || type == typeof(int?))
            return NpgsqlDbType.Integer;
        if (type == typeof(long) || type == typeof(long?))
            return NpgsqlDbType.Bigint;
        if (type == typeof(short) || type == typeof(short?))
            return NpgsqlDbType.Smallint;
        if (type == typeof(bool) || type == typeof(bool?))
            return NpgsqlDbType.Boolean;
        if (type == typeof(string))
            return NpgsqlDbType.Text;
        if (type == typeof(DateOnly) || type == typeof(DateOnly?))
            return NpgsqlDbType.Date;
        if (type == typeof(DateTime) || type == typeof(DateTime?))
            return NpgsqlDbType.TimestampTz;
        if (type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?))
            return NpgsqlDbType.TimestampTz;
        if (type == typeof(TimeSpan) || type == typeof(TimeSpan?))
            return NpgsqlDbType.Interval;
        if (type == typeof(NodaTime.Instant) || type == typeof(NodaTime.Instant?))
            return NpgsqlDbType.TimestampTz;
        if (type == typeof(NodaTime.LocalDate) || type == typeof(NodaTime.LocalDate?))
            return NpgsqlDbType.Date;
        if (type == typeof(NodaTime.LocalDateTime) || type == typeof(NodaTime.LocalDateTime?))
            return NpgsqlDbType.Timestamp;
        if (type == typeof(NodaTime.LocalTime) || type == typeof(NodaTime.LocalTime?))
            return NpgsqlDbType.Time;
        if (type == typeof(NodaTime.Duration) || type == typeof(NodaTime.Duration?))
            return NpgsqlDbType.Interval;
        if (type == typeof(NodaTime.Interval) || type == typeof(NodaTime.Interval?))
            return NpgsqlDbType.TimestampTzRange;
        if (type == typeof(Guid) || type == typeof(Guid?))
            return NpgsqlDbType.Uuid;
        if (type == typeof(decimal) || type == typeof(decimal?))
            return NpgsqlDbType.Numeric;
        if (type == typeof(double) || type == typeof(double?))
            return NpgsqlDbType.Double;
        if (type == typeof(float) || type == typeof(float?))
            return NpgsqlDbType.Real;
        if (type == typeof(byte[]))
            return NpgsqlDbType.Bytea;
        if (type.IsEnum)
            return NpgsqlDbType.Integer;
        if (typeof(Geometry).IsAssignableFrom(type) ||
            (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
             typeof(Geometry).IsAssignableFrom(Nullable.GetUnderlyingType(type))))
            return NpgsqlDbType.Geometry;
        throw new NotSupportedException($"Type {type.Name} is not supported for binary import.");
    }
}