# SudokuNexus — Your .NET 10 Learning & Implementation Guide

Welcome to **SudokuNexus**! This repository is designed so you can focus 100% on writing high-signal, idiomatic C# 14 / .NET 10 code without getting bogged down in project setup, Dependency Injection wiring, or UI boilerplate.

Everything builds out of the box, and the pre-written xUnit test suite is ready to verify your work as you complete each module.

---

## Quick Start Commands

> **Note for your Linux environment**: Your .NET 10 SDK is installed at `/usr/bin/dotnet`. If `/usr/bin` is not in your shell's default `PATH`, either run `/usr/bin/dotnet` or add `export PATH="$PATH:/usr/bin"` to your `~/.bashrc`.

```bash
# 1. Verify that the scaffold infrastructure tests pass right now (Module 0):
dotnet test --filter "Module=0"

# 2. Run the tests for the module you are currently implementing (e.g. Module 1):
dotnet test --filter "Module=1"

# 3. Start the API + Interactive Workbench UI:
dotnet run --project src/SudokuNexus.Api
# Then open http://localhost:5000 (or the port printed in the console)
```

---

## Progressive Implementation Roadmap

Work through the modules in order (1 → 5). Search the solution for `// TODO [Module` to jump directly to each exercise.

### Module 1: Value Types, Hardware Bit Intrinsics & Board Domain Model
**Goal**: Master `readonly record struct`, `System.Numerics.BitOperations`, and constraint propagation.

1. **`Module 1.1` — `src/SudokuNexus.Domain/Primitives/CandidateMask.cs`**
   - Implement `Count` using `BitOperations.PopCount(Bits)`.
   - Implement `HasCandidate(int digit)`, `WithCandidate(int digit)`, and `WithoutCandidate(int digit)` using bitwise shifts (`1 << digit`), OR (`|`), and AND-NOT (`& ~`).
   - Implement `GetSingleDigit()`: first check `if (Count != 1) throw new InvalidOperationException(...);`, then return `BitOperations.TrailingZeroCount(Bits)`.
2. **`Module 1.2` — `src/SudokuNexus.Domain/Models/Board.cs`**
   - Implement `PlaceDigit(int cellIndex, int digit, bool isInitialClue)`:
     - Store `digit` in `_values[cellIndex]` and `isInitialClue` in `_isGiven[cellIndex]`.
     - Set `_candidates[cellIndex] = CandidateMask.Empty`.
     - Loop through all 20 peers in `SudokuTopology.Peers[cellIndex]` and remove `digit` from each peer's `_candidates` using `WithoutCandidate(digit)`.
   - Implement `IsValid()`:
     - Verify no Row, Column, or Box (`SudokuTopology.RowIndices`, `ColumnIndices`, `BoxIndices`) contains duplicate placed digits (`1..9`).
     - Verify no empty cell (`_values[i] == 0`) has zero remaining candidates (`_candidates[i].Bits == 0`).
3. **Verify**:
   ```bash
   dotnet test --filter "Module=1"
   ```
   Once Module 1 passes, launch the UI and click **Inspect Board** — you will see all pencil-mark candidates rendered on the 9x9 grid!

---

### Module 2: Donald Knuth's Algorithm X & Dancing Links (DLX)
**Goal**: Implement a classic, blazing-fast Exact Cover solver using toroidal doubly-linked lists.

1. **`Module 2.1` — `src/SudokuNexus.Engine/ExactCover/DlxSolver.cs` (`Cover` & `Uncover`)**
   - Follow the exact step-by-step pointer-manipulation specification in the XML doc comments above `Cover(ColumnNode column)` and `Uncover(ColumnNode column)`.
   - *Key insight*: Because a doubly-linked node still points to its old neighbors even after being removed, `Uncover` restores the matrix in $O(1)$ per link without allocating a single object—as long as `Uncover` iterates in the exact reverse direction (`Up` and `Left`) of `Cover` (`Down` and `Right`).
2. **`Module 2.2` — `src/SudokuNexus.Engine/ExactCover/DlxSolver.cs` (`Search`)**
   - Implement the recursive backtracking search using the **S-Heuristic** (always picking the `ColumnNode` with the smallest `Size` first to minimize the branching factor).
   - Remember to stop early if `solutions.Count >= maxSolutions` so uniqueness checks (`maxSolutions = 2`) finish in microseconds.
3. **Verify**:
   ```bash
   dotnet test --filter "Module=2"
   ```
   Try loading **AI Escargot (Arto Inkala)** in the Web UI and clicking **Fast DLX Solve**!

---

### Module 3: Strategy Pattern & Human-Style Logical Grader
**Goal**: Use the Strategy Pattern, constructor injection of `IEnumerable<IDeductionStrategy>`, C# pattern matching, and bitmask algebra to replicate how humans solve Sudokus.

> Note: `PointingPairStrategy.cs` is already implemented for you as a complete reference example of how a candidate-elimination strategy builds a `SolveStep`!

1. **`Module 3.1` — `src/SudokuNexus.Engine/Grading/Strategies/NakedSingleStrategy.cs`**
   - Find the first empty cell whose `CandidateMask.Count == 1` and return a placement `SolveStep`.
2. **`Module 3.2` — `src/SudokuNexus.Engine/Grading/Strategies/HiddenSingleStrategy.cs`**
   - For each house (Box, Row, Column) and each digit `1..9`, check if only a single empty cell in that house has `HasCandidate(digit)`.
3. **`Module 3.3` — `src/SudokuNexus.Engine/Grading/Strategies/NakedPairStrategy.cs`**
   - Find two empty cells in the same house with identical 2-bit masks (`maskA.Count == 2 && maskA == maskB`) and eliminate those 2 digits from all other empty cells in the house.
4. **`Module 3.4` — `src/SudokuNexus.Engine/Grading/Strategies/XWingStrategy.cs`**
   - Find a digit restricted to the exact same 2 columns across 2 rows (or 2 rows across 2 columns) and eliminate that digit from the rest of the cover lines.
5. **`Module 3.5` — `src/SudokuNexus.Engine/Grading/HumanLogicalGrader.cs`**
   - Implement `Grade(Board initialBoard)` to iteratively call `GetNextHint`, apply each `SolveStep` to a cloned working board, and return the `GradingReport`.
6. **Verify**:
   ```bash
   dotnet test --filter "Module=3"
   ```

---

### Module 4: Rotational Symmetry Puzzle Generator
**Goal**: Combine `DlxSolver` and `HumanLogicalGrader` to generate brand-new puzzles with 180° rotational symmetry and guaranteed unique solutions.

1. **`Module 4.1` — `src/SudokuNexus.Engine/Generation/PuzzleGenerator.cs`**
   - Implement `DigSymmetricClues(Board solvedBoard, int targetClues, Random random)`.
   - Remove clues in symmetric pairs `(i, 80 - i)` and verify after each removal that `_dlxSolver.CountSolutions(working, limit: 2) == 1`. If removing a pair introduces a second solution, restore both digits and try the next pair.
2. **Verify**:
   ```bash
   dotnet test --filter "Module=4"
   ```

---

### Module 5: Scoped DI in Background Workers (`BackgroundService` + `Channels`)
**Goal**: Understand service lifetimes (`Singleton` vs. `Scoped`) in ASP.NET Core background processing.

1. **`Module 5.1` — `src/SudokuNexus.Infrastructure/Background/PuzzlePoolBackgroundWorker.cs`**
   - Implement `ProcessGenerationRequestAsync` using `await using AsyncServiceScope scope = scopeFactory.CreateAsyncScope();` to resolve `IPuzzleRepository` and store generated puzzles in SQLite.
2. **Verify**:
   ```bash
   dotnet test --filter "Module=5"
   ```

---

## Running Performance Benchmarks (For Your Portfolio README)

Once Modules 1–3 are complete, run the **BenchmarkDotNet** suite in `Release` mode:

```bash
dotnet run -c Release --project benchmarks/SudokuNexus.Benchmarks -- --filter "*"
```

Copy the resulting markdown table (showing execution time in microseconds and heap allocations) into `README.md`—it is a great talking point in technical interviews!
