# SudokuNexus

**SudokuNexus** is a .NET 10 Sudoku solver, logical grader, and puzzle generator with a simple web UI. Built as a personal project to learn .NET and C#.

---

## Features & Design

- `ushort` bitmasks for candidate manipulation.
- Maps 9x9 Sudoku to a 729x324 doubly-linked list (Algorithm X / Dancing Links) with the minimum-size heuristic to solve boards and verify uniqueness quickly.
- Logical grader for human-like difficulty ratings using deduction patterns (Naked/Hidden Singles, Pointing/Naked Pairs, X-Wing).
- Symmetric puzzle generator with guaranteed unique solutions.
- Saves generated puzzles in SQLite using EF Core and replenishes the pool via a background worker.
- Built-in frontend for trying it out.

---

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)

### Build & Run Tests
```bash
dotnet build
dotnet test
```

### Run the Web UI & API
```bash
dotnet run --project src/SudokuNexus.Api
```
Open the URL printed in the console (`http://localhost:5000`) to launch the web UI, or view `/openapi/v1.json` for the OpenAPI specification.

### Run Benchmarks
```bash
dotnet run -c Release --project benchmarks/SudokuNexus.Benchmarks -- --filter "*"
```

| Method | Mean | Allocated |
| :--- | ---: | ---: |
| `DLX Solve (Easy Board)` | `15.05 µs` | `184 B` |
| `DLX Solve (AI Escargot Hardest)` | `345.11 µs` | `184 B` |
| `Logical grader` | `17.50 µs` | `15.23 KB` |

