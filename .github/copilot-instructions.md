# LiveCharts2 Copilot Instructions

This document helps coding agents work efficiently with the LiveCharts2 repository.

## Repository Overview

LiveCharts2 is a flexible, cross-platform charting library for .NET. It follows a layered architecture where:
- **Core library** (`LiveChartsCore`) is platform-agnostic and handles all chart mathematics
- **SkiaSharp backend** renders the charts using SkiaSharp
- **Platform-specific views** provide UI controls for various frameworks (WPF, Avalonia, MAUI, Blazor, etc.)

## Repository Structure

```
LiveCharts2/
├── src/
│   ├── LiveChartsCore/                    # Platform-agnostic core library
│   │   ├── Kernel/                        # Core charting engine
│   │   ├── Drawing/                       # Drawing abstractions
│   │   ├── Motion/                        # Animation system
│   │   ├── Measure/                       # Chart measurement logic
│   │   └── [Series types]/                # Line, Bar, Pie, Scatter, etc.
│   ├── skiasharp/                         # SkiaSharp rendering implementations
│   │   ├── LiveChartsCore.SkiaSharp/      # Core SkiaSharp provider
│   │   ├── LiveChartsCore.SkiaSharp.WPF/
│   │   ├── LiveChartsCore.SkiaSharp.Avalonia/
│   │   ├── LiveChartsCore.SkiaSharpView.Maui/
│   │   ├── LiveChartsCore.SkiaSharpView.Blazor/
│   │   └── [other platforms]/
│   └── _Shared.Native/                    # Native platform interop
├── samples/                               # Sample applications
│   ├── ViewModelsSamples/                 # Shared ViewModels for all samples
│   │   └── Index.cs                       # List of all sample paths
│   ├── WPFSample/
│   ├── AvaloniaSample/
│   ├── MauiSample/
│   ├── VorticeSample/                     # DirectX sample (core without SkiaSharp)
│   └── [other platforms]/
├── tests/
│   └── LiveChartsCore.UnitTesting/        # Core unit tests using MSTest
│       ├── ChartTests/                    # High-level chart tests
│       ├── SeriesTests/                   # Series-specific tests
│       ├── LayoutTests/                   # Layout tests
│       ├── CoreObjectsTests/              # Core objects tests
│       └── OtherTests/                    # Axes, events, etc.
├── docs/                                  # Documentation
├── generators/                            # Code generators
└── build/                                 # Build scripts
```

## Key Architecture Concepts

### 1. Layered Design
- **LiveChartsCore**: Pure .NET, no UI dependencies, handles all calculations
- **SkiaSharp Provider**: Implements `IDrawingProvider` to render using SkiaSharp
- **Platform Views**: WPF/Avalonia/MAUI/etc. specific controls that host the renderer

### 2. Multi-Platform Targeting
Projects target multiple frameworks including:
- `netstandard2.0`, `netstandard2.1`
- `net462`, `net6.0`, `net8.0`
- `net8.0-android`, `net8.0-ios`, `net8.0-maccatalyst`
- `net8.0-windows10.0.19041.0`

### 3. Sample Structure
- **ViewModelsSamples**: Contains shared ViewModels used across all UI frameworks
- **Index.cs**: Defines available samples as string paths (e.g., "Lines/Basic", "Pies/Doughnut")
- Each platform sample project (WPF, Avalonia, etc.) references ViewModelsSamples and creates platform-specific views

### 4. VorticeSample
A special sample demonstrating how to use LiveChartsCore without SkiaSharp, using DirectX instead. This shows the core library is truly rendering-agnostic.

## Building the Repository

### Prerequisites
- .NET SDK 9.0.101 or later (see `global.json`)
- Required workloads for multi-platform builds:
  ```bash
  dotnet workload install android
  dotnet workload install ios
  dotnet workload install maccatalyst
  dotnet workload install maui
  ```

### Build Methods

#### Quick Build (Specific Projects)
```bash
# Build specific platform views (recommended for development)
dotnet build src/skiasharp/LiveChartsCore.SkiaSharp.WPF/LiveChartsCore.SkiaSharpView.Wpf.csproj
dotnet build src/skiasharp/LiveChartsCore.SkiaSharp.Avalonia/LiveChartsCore.SkiaSharpView.Avalonia.csproj
```

#### Full Build (Windows)
```bash
# Uses build/build-windows.ps1
.\build\build-windows.ps1 -configuration Debug
```

#### Build with Solution Files
```bash
# Use platform-specific solution files
dotnet build LiveCharts.WPF.slnx
dotnet build LiveCharts.Avalonia.slnx
dotnet build LiveCharts.Maui.slnx
```

### Common Build Issues and Workarounds

#### Issue: Missing workload errors (NETSDK1147)
```
error NETSDK1147: To build this project, the following workloads must be installed: android
```

**Workaround Options:**
1. Install the required workload: `dotnet workload install android`
2. Build only the platform you need (e.g., WPF on Windows)
3. Use platform-specific solution files that don't include all targets
4. Temporarily remove platform targets from .csproj if only working on desktop platforms

#### Issue: SkiaSharp version conflicts
The project supports multiple SkiaSharp versions:
- `MinSkiaSharpVersion`: 2.88.9 (minimum supported)
- `LatestSkiaSharpVersion`: 3.119.0 (default for GPU support)

Defined in `Directory.Build.props`.

#### Issue: Multi-targeting complexity
When building fails for specific targets, you can:
1. Use `-f` to target specific framework: `dotnet build -f net8.0`
2. Edit `TargetFrameworks` in .csproj to focus on needed platforms

## Testing

### Unit Tests
Location: `tests/LiveChartsCore.UnitTesting/`

**Framework**: MSTest with coverlet for code coverage

**Run Tests:**
```bash
# Run all tests
dotnet test tests/LiveChartsCore.UnitTesting/

# Run for specific framework
dotnet test tests/LiveChartsCore.UnitTesting/ --framework net8.0

# Run with coverage
dotnet test tests/LiveChartsCore.UnitTesting/ --collect:"XPlat Code Coverage"
```

**Test Structure:**
- `ChartTests/`: High-level chart functionality
- `SeriesTests/`: Tests for Line, Bar, Pie, Scatter, Heat, etc.
- `LayoutTests/`: Stack and table layouts
- `CoreObjectsTests/`: Transitions, colors, labels
- `OtherTests/`: Axes, events, data providers, visual elements
- `MockedObjects/`: Test helpers and mocks
- `TestsInitializer.cs`: MSTest assembly initialization

**Important**: Tests use `CoreMotionCanvas.IsTesting = true` to disable animations during testing.

### UI Testing
**Note**: The problem statement mentions `tests/UITests` with multi-environment testing, but this directory was not found in the current repository structure. This may be:
- Planned but not yet implemented
- In a different branch
- Performed through external CI/CD processes
- Referenced in the original documentation but deprecated

When UI testing is needed, sample applications serve as integration tests across platforms.

## Running Samples

### Sample Applications
Each platform has its own sample application that references `ViewModelsSamples`:

```bash
# Run WPF sample
dotnet run --project samples/WPFSample/WPFSample.csproj

# Run Avalonia sample
dotnet run --project samples/AvaloniaSample/AvaloniaSample.csproj

# Run Console sample (no UI)
dotnet run --project samples/ConsoleSample/ConsoleSample.csproj
```

### Adding New Samples
1. Add ViewModel class in `samples/ViewModelsSamples/[Category]/[Name].cs`
2. Add path to `samples/ViewModelsSamples/Index.cs`
3. Create platform-specific view files in each sample project (WPF, Avalonia, etc.)

## Code Style and Conventions

### Editor Config
The repository uses `.editorconfig` based on [.NET Runtime coding style](https://github.com/dotnet/runtime/blob/main/docs/coding-guidelines/coding-style.md) with exceptions.

**Key Style Rules:**
- **Indentation**: 4 spaces
- **Line endings**: LF, insert final newline
- **Braces**: New line before open brace (Allman style)
- **var usage**: Use `var` freely (explicitly allowed)
- **Single-line if**: Allowed and preferred when line is short
- **Naming**:
  - Private/internal fields: `_camelCase`
  - Static private fields: `s_camelCase`
  - Constants: `PascalCase`
- **Using directives**: Outside namespace

### File Naming
**Critical for auto-generated documentation:**
- File names MUST match the class name exactly
- `public class Hello` → `Hello.cs`
- `public class Hello<T>` → `Hello.cs` (ignore generics)
- Generic and non-generic with same name → same file (only if inheritance relationship)

### Important Constants
Defined in `Directory.Build.props`:
- `LiveChartsVersion`: Current version (2.0.0-rc6.1)
- `MinSkiaSharpVersion`: 2.88.9
- `LatestSkiaSharpVersion`: 3.119.0
- `GlobalLangVersion`: preview (C# preview features enabled)

## Build Configuration Properties

### Rendering Settings
Configurable in `build/RenderSettings.Build.props`:
- `GPU`: Enable/disable GPU acceleration
- `VSYNC`: Enable/disable vertical sync
- `FPS`: Frame rate (10, 20, 30, 45, 60, 75, 90, 120)
- `Diagnose`: Enable diagnostic mode

These create conditional compilation symbols for testing different rendering modes.

### Development Flags
In `Directory.Build.props`:
- `UseNuGetForSamples`: Use NuGet packages vs project references (default: false)
- `UseNuGetForGenerator`: Use NuGet generator package (default: true)

## CI/CD

### GitHub Actions Workflows

#### 1. Unit Tests (`run-unit-tests.yml`)
- Runs on: `windows-2022`
- Triggers: Push/PR to `master` or `dev`, weekly schedule
- Command: `dotnet test ./tests/LiveChartsCore.UnitTesting`

#### 2. Compile All Views (`compile-all-views.yml`)
- Runs on: `windows-2022`
- Installs MAUI workload
- Executes: `./build/build-windows.ps1 -configuration "Debug"`
- Tests compilation of all platform views

**Note**: Both workflows require Windows runners due to platform-specific dependencies (WPF, WinUI, etc.)

## Common Development Workflows

### Adding a New Series Type
1. Create series class in `src/LiveChartsCore/[SeriesType]/`
2. Implement series interfaces (`ISeries`, etc.)
3. Create SkiaSharp drawable in `src/skiasharp/LiveChartsCore.SkiaSharp/Drawing/Geometries/`
4. Add tests in `tests/LiveChartsCore.UnitTesting/SeriesTests/`
5. Create sample ViewModel in `samples/ViewModelsSamples/`
6. Update `samples/ViewModelsSamples/Index.cs`

### Adding Platform Support
1. Create new project in `src/skiasharp/LiveChartsCore.SkiaSharpView.[Platform]/`
2. Reference `LiveChartsCore.SkiaSharp` project
3. Create platform-specific control classes
4. Add shared code to `_Shared/` if applicable
5. Create sample application in `samples/[Platform]Sample/`
6. Add platform-specific solution file

### Updating Documentation
Documentation in `docs/` folder is auto-generated from:
- XML comments in code
- Template files in `docs/samples/[category]/[name]/template.md`

## Important Notes for Coding Agents

### Do's
- ✅ Use project references during development (not NuGet packages)
- ✅ Follow the exact file naming convention (critical for docs)
- ✅ Run tests after changes to core or series logic
- ✅ Use platform-specific solution files for focused development
- ✅ Consult `CONTRIBUTING.md` for detailed style guide
- ✅ Use shared code in `_Shared/` folders when adding cross-platform features

### Don'ts
- ❌ Don't break multi-platform support when modifying core projects
- ❌ Don't add platform-specific code to `LiveChartsCore` (keep it agnostic)
- ❌ Don't ignore `.editorconfig` warnings
- ❌ Don't remove or modify working tests without good reason
- ❌ Don't add new dependencies without checking compatibility across all target frameworks

### Special Considerations
- The library supports .NET Framework 4.6.2 - maintain compatibility
- SkiaSharp is abstracted - core library should work with other rendering engines
- Animation system (`Motion/`) is critical - changes require extensive testing
- Multi-threading: Chart updates can come from any thread; proper synchronization is essential

## Quick Reference Commands

```bash
# Build core library (requires workloads)
dotnet build src/LiveChartsCore/LiveChartsCore.csproj

# Build SkiaSharp core (requires workloads) 
dotnet build src/skiasharp/LiveChartsCore.SkiaSharp/LiveChartsCore.SkiaSharpView.csproj

# Run unit tests
dotnet test tests/LiveChartsCore.UnitTesting/ --framework net8.0

# Run WPF sample
dotnet run --project samples/WPFSample/WPFSample.csproj

# Build all Windows platform views
.\build\build-windows.ps1 -configuration Debug

# Check for workload issues
dotnet workload restore
```

## Troubleshooting

### Problem: Can't build any projects
**Solution**: Install required workloads or build platform-specific projects only

### Problem: SkiaSharp errors
**Solution**: Check SkiaSharp version in `Directory.Build.props`, ensure NuGet restore succeeded

### Problem: Tests fail with animation issues
**Solution**: Verify `CoreMotionCanvas.IsTesting = true` in test initialization

### Problem: Sample won't run
**Solution**: Ensure platform-specific dependencies are installed (e.g., .NET Desktop Runtime for WPF)

### Problem: Generator errors
**Solution**: Check `UseNuGetForGenerator` setting and ensure LiveChartsGenerators package/project is available

## Resources

- **Main Documentation**: https://livecharts.dev
- **Contributing Guide**: `CONTRIBUTING.md`
- **Repository**: https://github.com/Live-Charts/LiveCharts2
- **Code of Conduct**: `CODE_OF_CONDUCT.md`
- **License**: MIT (see `LICENSE`)

## Version Information

- **Current Version**: 2.0.0-rc6.1 (Release Candidate)
- **SDK Version**: 9.0.101 (minimum, see `global.json`)
- **SkiaSharp**: 2.88.9 (min) to 3.119.0 (latest)
- **Target Frameworks**: netstandard2.0/2.1, net462, net6.0, net8.0, plus mobile platforms
