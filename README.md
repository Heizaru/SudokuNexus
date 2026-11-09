# SudokuNexus

**SudokuNexus** is a .NET 10 Sudoku solver, logical grader, and symmetric puzzle generator with an interactive web UI.

It combines Donald Knuth's **Algorithm X (Dancing Links)** for fast exact-cover solving and uniqueness verification with a **Human Logical Grader** that solves boards step-by-step using human deduction strategies (*Naked Singles*, *Hidden Singles*, *Pointing Pairs*, *Naked Pairs*, and *X-Wings*).

---

## Features

- **Bitmask Candidate Tracking**: Uses `ushort` bitmasks and `System.Numerics.BitOperations` hardware intrinsics (`PopCount`, `TrailingZeroCount`, `LeadingZeroCount`) for candidate manipulation and constraint propagation.
- **Algorithm X / Dancing Links (`DlxSolver`)**: Maps 9x9 Sudoku to a 729x324 toroidal doubly-linked list with the minimum-size column heuristic (S-Heuristic) to solve boards and verify solution uniqueness in microseconds.
- **Human-Style Logical Grader (`HumanLogicalGrader`)**: Applies extensible `IDeductionStrategy` implementations ordered by difficulty weight to generate step-by-step solve traces and difficulty ratings.
- **Symmetric Puzzle Generator (`PuzzleGenerator`)**: Seeds independent diagonal boxes, solves the full grid via DLX, and removes clues in 180-degree rotationally symmetric pairs while preserving a unique solution.
- **Background Pool Replenishment**: Uses `System.Threading.Channels` and a `BackgroundService` worker (`PuzzlePoolBackgroundWorker`) with EF Core 10 + SQLite to maintain a warm pool of pre-generated puzzles per difficulty tier.
- **Interactive Web UI**: Built-in frontend (`wwwroot`) for inspecting candidate pencil marks, stepping through logical hints, running full grading traces, and generating new puzzles.

---

## Project Structure

- **`src/SudokuNexus.Domain`**: Core domain models (`Board`, `SudokuTopology`, `SolveStep`), bitmask primitives (`CandidateMask`, `CellCoordinate`), and `Result<T>`.
- **`src/SudokuNexus.Engine`**: Stateless algorithmic solver (`DlxSolver`), logical deduction strategies (`IDeductionStrategy`), `HumanLogicalGrader`, and `PuzzleGenerator`.
- **`src/SudokuNexus.Application`**: Application service (`PuzzleApplicationService`), DTO contracts, and repository/queue abstractions.
- **`src/SudokuNexus.Infrastructure`**: EF Core 10 SQLite persistence (`SudokuDbContext`, `EfPuzzleRepository`) and bounded channel background worker (`PuzzlePoolBackgroundWorker`).
- **`src/SudokuNexus.Api`**: ASP.NET Core 10 Minimal API endpoints, OpenAPI configuration, `ProblemDetails` exception handling, and static web UI.
- **`tests/SudokuNexus.UnitTests`**: Unit test suite covering bitmask operations, DLX exact cover, logical deduction strategies, symmetric puzzle generation, and background worker persistence.
- **`benchmarks/SudokuNexus.Benchmarks`**: BenchmarkDotNet suite measuring solver and grader execution times and allocations.

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
Open the URL printed in the console (e.g., `http://localhost:5000`) to launch the interactive Sudoku workbench, or view `/openapi/v1.json` for the OpenAPI specification.

### Run Benchmarks
```bash
dotnet run -c Release --project benchmarks/SudokuNexus.Benchmarks -- --filter "*"
```

| Method | Mean | Allocated |
| :--- | ---: | ---: |
| `DLX Solve + Uniqueness (Easy Board)` | `15.05 µs` | `184 B` |
| `DLX Solve + Uniqueness (AI Escargot Hardest)` | `345.11 µs` | `184 B` |
| `Human Logical Grader (Step-by-Step Trace)` | `17.50 µs` | `15.23 KB` |

---

## API Endpoints

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/puzzles/presets` | Returns curated puzzles for testing different deduction techniques. |
| `POST` | `/api/puzzles/inspect` | Parses an 81-character puzzle string and returns cell states and candidate masks. |
| `POST` | `/api/puzzles/solve` | Solves a puzzle via Algorithm X (DLX) and checks solution uniqueness. |
| `POST` | `/api/puzzles/hint` | Finds the next human logical deduction step (`SolveStep`) for the current board. |
| `POST` | `/api/puzzles/analyze` | Runs both DLX verification and a full step-by-step logical grading trace. |
| `GET` | `/api/puzzles/generate?difficulty=Hard` | Fetches a puzzle from the SQLite pool (or generates one) and queues background replenishment. |
| `GET` | `/api/puzzles/pool-stats` | Returns the number of cached puzzles per difficulty rating in SQLite. |
