# PgEFCoreCopy

[![NuGet Version](https://img.shields.io/nuget/v/PgEFCoreCopy)](https://www.nuget.org/packages/PgEFCoreCopy)

Extension method(s) for EF Core DbContext that use PostgreSQL `COPY` to speed up bulk operations.

Currently only implements `ExecuteInsertRangeAsync`.

## Benchmarks

> dotnet run -c Release --project src/Benchmarks

### Results:

```sh
BenchmarkDotNet v0.15.8, Linux Ubuntu 24.04.3 LTS (Noble Numbat)
AMD Ryzen 7 PRO 8840U w/ Radeon 780M Graphics 1.10GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 10.0.100
  [Host]     : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v4
  Job-CNUJVU : .NET 10.0.0 (10.0.0, 10.0.25.52411), X64 RyuJIT x86-64-v4

InvocationCount=1  UnrollFactor=1  

| Method               | SampleCount | Mean     | Error   | StdDev  |
|--------------------- |------------ |---------:|--------:|--------:|
| PgEFCoreCopy         | 200000      | 251.1 ms | 4.81 ms | 5.14 ms |
| EFCoreBulkExtensions | 200000      | 255.0 ms | 4.93 ms | 4.84 ms |
```
