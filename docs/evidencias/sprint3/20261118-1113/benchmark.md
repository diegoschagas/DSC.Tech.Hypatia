```

BenchmarkDotNet v0.15.8, Windows 11 (10.0.22621.4317/22H2/2022Update/SunValley2)
Intel Core i7-5500U CPU 2.40GHz (Broadwell), 1 CPU, 4 logical and 2 physical cores
.NET SDK 10.0.404
  [Host]     : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3
  DefaultJob : .NET 10.0.15 (10.0.15, 10.0.1526.53008), X64 RyuJIT x86-64-v3


```
| Method            | Mean          | Error       | StdDev      | Median        | Ratio    | RatioSD  | Gen0   | Allocated | Alloc Ratio |
|------------------ |--------------:|------------:|------------:|--------------:|---------:|---------:|-------:|----------:|------------:|
| DirectHandler     |     0.8079 ns |   0.1630 ns |   0.4806 ns |     0.7082 ns |     1.49 |     1.43 |      - |         - |          NA |
| SendWithoutResult | 1,249.6873 ns |  65.4978 ns | 193.1217 ns | 1,171.3464 ns | 2,300.33 | 1,556.91 | 0.1297 |     272 B |          NA |
| SendWithResult    | 1,562.6783 ns |  92.3961 ns | 272.4319 ns | 1,622.1292 ns | 2,876.46 | 1,966.26 | 0.2098 |     440 B |          NA |
| Query             | 1,310.2691 ns | 113.0751 ns | 333.4044 ns | 1,246.5860 ns | 2,411.84 | 1,732.03 | 0.2098 |     440 B |          NA |
