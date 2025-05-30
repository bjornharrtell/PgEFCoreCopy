# PgEFCoreCopy

Extension method(s) for DbContext that use `COPY` to speed up bulk operations.

Currently only implements `ExecuteInsertRangeAsync`.

## Benchmarks

> dotnet run -c Release --project src/Benchmarks

