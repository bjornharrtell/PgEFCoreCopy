# PgEFCoreCopy

[![NuGet Version](https://img.shields.io/nuget/v/PgEFCoreCopy)](https://www.nuget.org/packages/PgEFCoreCopy)

Extension method(s) for EF Core DbContext that use PostgreSQL `COPY` to speed up bulk operations.

Currently only implements `ExecuteInsertRangeAsync`.

## Benchmarks

> dotnet run -c Release --project src/Benchmarks

### Results:

```sh
BenchmarkDotNet v0.15.0, Linux Ubuntu 24.04.2 LTS (Noble Numbat)
AMD Ryzen 7 PRO 8840U w/ Radeon 780M Graphics 5.13GHz, 1 CPU, 16 logical and 8 physical cores
.NET SDK 9.0.106
  [Host]     : .NET 9.0.5 (9.0.525.21509), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-GFSXSY : .NET 9.0.5 (9.0.525.21509), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

InvocationCount=1  UnrollFactor=1  

| Method               | SampleCount | Mean     | Error   | StdDev  |
|--------------------- |------------ |---------:|--------:|--------:|
| PgEFCoreCopy         | 200000      | 224.7 ms | 4.30 ms | 5.88 ms |
| EFCoreBulkExtensions | 200000      | 449.7 ms | 7.78 ms | 7.27 ms |
```
