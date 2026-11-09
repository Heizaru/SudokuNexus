# SudokuNexus

**SudokuNexus** is a .NET 10 backend engine for solving, grading, and generating Sudoku puzzles. Primarily used for learning .NET in an actual project and wanting to know how to solve sudokus programatically.
It contains an Exact Cover solver with a Strategy-Pattern Human Logical Grader capable of explaining every step from *Naked Singles* to *X-Wings*.
---

## Architecture 

- **`SudokuNexus.Domain`**: Core containing value objects (`CellCoordinate`), primitivesandlookup tables (`SudokuTopology`)
- **`SudokuNexus.Engine`**: Stateless algorithmic tier housing Donald Knuth's **Dancing Links (`DlxSolver`)**, extensible human deduction strategies (`IDeductionStrategy`), `HumanLogicalGrader`, and a rotationally symmetric `PuzzleGenerator`.
- **`SudokuNexus.Api`**: **ASP.NET Core 10 Minimal APIs**, RFC 7807 `ProblemDetails` global exception handling (`IExceptionHandler`), OpenAPI specification, and a zero-dependency interactive web workbench (`wwwroot`).
- **`SudokuNexus.Application`**: Use-case orchestration (`PuzzleApplicationService`), DTO mapping, and producer/consumer queue contracts (`IPuzzleGenerationQueue`).
- **`SudokuNexus.Infrastructure`**: **EF Core 10 (SQLite)** persistence (`SudokuDbContext`, `EfPuzzleRepository`) and an asynchronous `BackgroundService` worker (`PuzzlePoolBackgroundWorker`) powered by `System.Threading.Channels.Channel<T>`.

---

## Getting Started

### Prerequisites
- **.NET 10 SDK** (`10.0.100` or newer)

### Build & Run Tests
```bash
dotnet build
dotnet test
```

### Run the API 
```bash
dotnet run --project src/SudokuNexus.Api
```
Navigate to the URL shown in the terminal (e.g., `http://localhost:5000`) to open the UI, or inspect `/openapi/v1.json` for the OpenAPI specification.

### Run Benchmarks
```bash
dotnet run -c Release --project benchmarks/SudokuNexus.Benchmarks -- --filter "*"
```

---

## REST API Endpoints

| Method | Endpoint | Description |
| :--- | :--- | :--- |
| `GET` | `/api/puzzles/presets` | Returns test puzzles |
| `POST` | `/api/puzzles/inspect` | Parses an 81-char puzzle string and computes candidate bitmasks for every cell. |
| `POST` | `/api/puzzles/solve` | Solves the board via Algorithm X (DLX), verifies solution uniqueness. |
| `POST` | `/api/puzzles/hint` | Returns the single easiest human logical deduction step (`SolveStep`) available on the board. |
| `POST` | `/api/puzzles/analyze` | Runs a full human-style logical solve trace and assigns a Sudoku Explainer (SE) difficulty grade. |
| `GET` | `/api/puzzles/generate?difficulty=Hard` | Fetches a graded puzzle from the warm SQLite pool and signals the background channel to replenish. |
| `GET` | `/api/puzzles/pool-stats` | Returns the current count of pre-generated puzzles per difficulty tier in SQLite. |
