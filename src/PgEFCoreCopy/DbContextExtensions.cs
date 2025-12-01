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
    private static readonly Dictionary<Type, NpgsqlDbType> TypeCache = new();
    
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
        if (TypeCache.TryGetValue(type, out var cachedType))
            return cachedType;

        NpgsqlDbType npgsqlDbType;
        
        if (type == typeof(int) || type == typeof(int?))
            npgsqlDbType = NpgsqlDbType.Integer;
        else if (type == typeof(long) || type == typeof(long?))
            npgsqlDbType = NpgsqlDbType.Bigint;
        else if (type == typeof(short) || type == typeof(short?))
            npgsqlDbType = NpgsqlDbType.Smallint;
        else if (type == typeof(bool) || type == typeof(bool?))
            npgsqlDbType = NpgsqlDbType.Boolean;
        else if (type == typeof(string))
            npgsqlDbType = NpgsqlDbType.Text;
        else if (type == typeof(DateOnly) || type == typeof(DateOnly?))
            npgsqlDbType = NpgsqlDbType.Date;
        else if (type == typeof(DateTime) || type == typeof(DateTime?))
            npgsqlDbType = NpgsqlDbType.TimestampTz;
        else if (type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?))
            npgsqlDbType = NpgsqlDbType.TimestampTz;
        else if (type == typeof(TimeSpan) || type == typeof(TimeSpan?))
            npgsqlDbType = NpgsqlDbType.Interval;
        else if (type == typeof(NodaTime.Instant) || type == typeof(NodaTime.Instant?))
            npgsqlDbType = NpgsqlDbType.TimestampTz;
        else if (type == typeof(NodaTime.LocalDate) || type == typeof(NodaTime.LocalDate?))
            npgsqlDbType = NpgsqlDbType.Date;
        else if (type == typeof(NodaTime.LocalDateTime) || type == typeof(NodaTime.LocalDateTime?))
            npgsqlDbType = NpgsqlDbType.Timestamp;
        else if (type == typeof(NodaTime.LocalTime) || type == typeof(NodaTime.LocalTime?))
            npgsqlDbType = NpgsqlDbType.Time;
        else if (type == typeof(NodaTime.Duration) || type == typeof(NodaTime.Duration?))
            npgsqlDbType = NpgsqlDbType.Interval;
        else if (type == typeof(NodaTime.Interval) || type == typeof(NodaTime.Interval?))
            npgsqlDbType = NpgsqlDbType.TimestampTzRange;
        else if (type == typeof(Guid) || type == typeof(Guid?))
            npgsqlDbType = NpgsqlDbType.Uuid;
        else if (type == typeof(decimal) || type == typeof(decimal?))
            npgsqlDbType = NpgsqlDbType.Numeric;
        else if (type == typeof(double) || type == typeof(double?))
            npgsqlDbType = NpgsqlDbType.Double;
        else if (type == typeof(float) || type == typeof(float?))
            npgsqlDbType = NpgsqlDbType.Real;
        else if (type == typeof(byte[]))
            npgsqlDbType = NpgsqlDbType.Bytea;
        else if (type.IsEnum)
            npgsqlDbType = NpgsqlDbType.Integer;
        else if (typeof(Geometry).IsAssignableFrom(type) ||
            (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>) &&
             typeof(Geometry).IsAssignableFrom(Nullable.GetUnderlyingType(type))))
            npgsqlDbType = NpgsqlDbType.Geometry;
        else
            throw new NotSupportedException($"Type {type.Name} is not supported for binary import.");

        TypeCache[type] = npgsqlDbType;
        return npgsqlDbType;
    }
}