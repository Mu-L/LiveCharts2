# LiveCharts2 Copilot Instructions

This document helps coding agents work efficiently with the LiveCharts2 repository.

## ⚠️ IMPORTANT: Branch Structure

**The `dev` branch contains the most complete and up-to-date structure**, including:
- UI Testing infrastructure (`tests/UITests` and `tests/SharedUITests`)
- More complete test coverage (`tests/CoreTests` - renamed from `LiveChartsCore.UnitTesting`)
- Latest features and improvements

**The `master` branch** may be behind `dev` and missing some directories/features mentioned in this document.

**When working on this repository**: Always check which branch you're on and prefer the `dev` branch for the most complete view of the codebase.

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
│   ├── CoreTests/                         # Core unit tests using MSTest (dev branch)
│   │   ├── ChartTests/                    # High-level chart tests
│   │   ├── SeriesTests/                   # Series-specific tests
│   │   ├── LayoutTests/                   # Layout tests
│   │   ├── CoreObjectsTests/              # Core objects tests
│   │   └── OtherTests/                    # Axes, events, etc.
│   ├── LiveChartsCore.UnitTesting/        # Core unit tests (master branch - older name)
│   ├── UITests/                           # UI testing orchestrator (dev branch)
│   │   └── Program.cs                     # Factos-based multi-platform test runner
│   └── SharedUITests/                     # Shared UI tests (dev branch)
│       ├── CartesianChartTests.cs
│       ├── PieChartTests.cs
│       ├── PolarChartTests.cs
│       └── MapChartTests.cs
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

### Unit Tests (Core Library)

**Location (dev branch)**: `tests/CoreTests/`
**Location (master branch)**: `tests/LiveChartsCore.UnitTesting/`

**Framework**: MSTest with coverlet for code coverage

**Run Tests:**
```bash
# On dev branch
dotnet test tests/CoreTests/

# On master branch
dotnet test tests/LiveChartsCore.UnitTesting/

# Run for specific framework
dotnet test tests/CoreTests/ --framework net8.0

# Run with coverage
dotnet test tests/CoreTests/ --collect:"XPlat Code Coverage"
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

### UI Testing (dev branch only)

**Location**: `tests/UITests/` (orchestrator) and `tests/SharedUITests/` (shared tests)

**Framework**: [Factos](https://github.com/beto-rodriguez/Factos) - A multi-platform UI testing framework

**How it works:**
1. Shared UI tests are defined in `tests/SharedUITests/` (shared project)
2. Each sample application references `SharedUITests` 
3. The `tests/UITests/Program.cs` orchestrator:
   - Starts various sample applications (Avalonia, WPF, MAUI, Blazor, etc.)
   - Connects to them via Factos
   - Runs the shared UI tests against each platform
4. Tests ensure charts render correctly across all supported UI frameworks

**Test Coverage:**
- `CartesianChartTests.cs`: Cartesian chart rendering and behavior
- `PieChartTests.cs`: Pie/Doughnut chart tests
- `PolarChartTests.cs`: Polar chart tests  
- `MapChartTests.cs`: Map chart tests
- `AvaloniaTests.cs`: Avalonia-specific tests

**Running UI Tests:**
```bash
# Run UI tests (requires sample apps to be built)
dotnet run --project tests/UITests/

# Run against specific platform (see Program.cs for options)
dotnet run --project tests/UITests/ -- --select wpf
dotnet run --project tests/UITests/ -- --select avalonia-desktop
dotnet run --project tests/UITests/ -- --select maui --test-env "tf=net10.0-windows10.0.19041.0"
```

**Important Notes:**
- UI testing requires the Factos package
- Each platform may need specific prerequisites (emulators for mobile, browsers for web)
- In Debug mode, tests use project references; in Release mode, they use NuGet packages
- The orchestrator supports testing against multiple target frameworks
- Mobile platforms (Android, iOS) require running emulators

**Build Configuration for UI Tests:**
The `build/UITestsLinks.Build.props` file links SharedUITests to sample applications. When `UITesting=true` is set, samples include the shared UI test project.

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

### Working with Branches
```bash
# Check current branch
git branch

# Switch to dev branch for latest features
git checkout dev

# Create feature branch from dev
git checkout -b feature/my-feature dev
```

**Recommendation**: Start development from the `dev` branch to access the latest features and testing infrastructure.

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
# === Branch Management ===
# Check current branch
git branch

# Switch to dev branch (recommended)
git checkout dev

# === Building ===
# Build core library (requires workloads OR see workarounds above)
dotnet build src/LiveChartsCore/LiveChartsCore.csproj

# Build SkiaSharp core (requires workloads OR see workarounds above)
dotnet build src/skiasharp/LiveChartsCore.SkiaSharp/LiveChartsCore.SkiaSharpView.csproj

# Build platform-specific projects (no workload required)
dotnet build src/skiasharp/LiveChartsCore.SkiaSharp.WPF/LiveChartsCore.SkiaSharpView.Wpf.csproj
dotnet build src/skiasharp/LiveChartsCore.SkiaSharp.Avalonia/LiveChartsCore.SkiaSharpView.Avalonia.csproj

# Build all Windows platform views
.\build\build-windows.ps1 -configuration Debug

# === Install Workloads ===
# Install Android workload
dotnet workload install android --skip-sign-check

# Install all mobile workloads
dotnet workload install android ios maccatalyst maui --skip-sign-check

# Check installed workloads
dotnet workload list

# === Testing ===
# Run core unit tests (dev branch)
dotnet test tests/CoreTests/ --framework net8.0

# Run core unit tests (master branch)
dotnet test tests/LiveChartsCore.UnitTesting/ --framework net8.0

# Run UI tests (dev branch only)
dotnet run --project tests/UITests/

# Run UI tests for specific platform
dotnet run --project tests/UITests/ -- --select wpf
dotnet run --project tests/UITests/ -- --select avalonia-desktop

# === Running Samples ===
# Run WPF sample
dotnet run --project samples/WPFSample/WPFSample.csproj

# Run Avalonia sample
dotnet run --project samples/AvaloniaSample/AvaloniaSample.csproj

# Run Console sample (no UI)
dotnet run --project samples/ConsoleSample/ConsoleSample.csproj

# === Troubleshooting ===
# Clean build artifacts
dotnet clean
find . -type d -name "bin" -o -name "obj" | xargs rm -rf

# Restore packages
dotnet restore

# Check for workload issues
dotnet workload restore --skip-sign-check
```

## Documented Errors and Workarounds

This section documents actual errors encountered when working with this repository and their solutions.

### Error 1: NETSDK1147 - Missing Workloads

**Error Message:**
```
error NETSDK1147: To build this project, the following workloads must be installed: android
To install these workloads, run the following command: dotnet workload restore
```

**Context**: This occurs when trying to build `LiveChartsCore` or `LiveChartsCore.SkiaSharp` projects because they multi-target mobile platforms (Android, iOS, macOS Catalyst).

**Why it happens**: The core library supports multiple platforms including:
- `net8.0-android`
- `net8.0-ios`
- `net8.0-maccatalyst`
- `net8.0-windows10.0.19041.0`

Even when building with `-f netstandard2.0`, MSBuild evaluates all target frameworks.

**Workarounds:**

**Option 1: Install Required Workloads** (Recommended for full development)
```bash
# Install Android workload
dotnet workload install android --skip-sign-check

# Install iOS workload (macOS only)
dotnet workload install ios --skip-sign-check

# Install macOS workload (macOS only)
dotnet workload install maccatalyst --skip-sign-check

# Or install all at once
dotnet workload install android ios maccatalyst maui --skip-sign-check
```

**Note**: After running `dotnet workload restore`, you still need to explicitly install workloads with `dotnet workload install`.

**Option 2: Build Platform-Specific Projects** (For focused development)
```bash
# Build only WPF (Windows only)
dotnet build src/skiasharp/LiveChartsCore.SkiaSharp.WPF/LiveChartsCore.SkiaSharpView.Wpf.csproj

# Build only Avalonia (cross-platform)
dotnet build src/skiasharp/LiveChartsCore.SkiaSharp.Avalonia/LiveChartsCore.SkiaSharpView.Avalonia.csproj

# Use platform-specific solution files
dotnet build LiveCharts.WPF.slnx
```

**Option 3: Modify Target Frameworks Temporarily** (Not recommended for commits)
Edit the `.csproj` file to remove platforms you don't need:
```xml
<!-- Before -->
<TargetFrameworks>netstandard2.0;netstandard2.1;net8.0;net8.0-android;net8.0-ios;net8.0-maccatalyst;</TargetFrameworks>

<!-- After (for desktop-only development) -->
<TargetFrameworks>netstandard2.0;netstandard2.1;net8.0</TargetFrameworks>
```

### Error 2: Visual Studio Component Required

**Error Message:**
```
Unhandled exception: The imported file "$(MSBuildExtensionsPath32)/Microsoft/VisualStudio/v$(VisualStudioVersion)/CodeSharing/Microsoft.CodeSharing.Common.Default.props" does not exist and appears to be part of a Visual Studio component.
```

**Context**: Appears when running `dotnet workload restore` on non-Windows systems or when Visual Studio is not installed.

**Why it happens**: The `src/skiasharp/_Shared.WinUI/_Shared.WinUI.shproj` shared project requires Visual Studio components that are Windows-specific.

**Workaround**: This error can be ignored if you're not building WinUI projects. The workload installation succeeds despite this error. If you need to build WinUI:
- Use Windows with Visual Studio 2022 installed
- Use `msbuild` instead of `dotnet build` for WinUI projects (see `build/build-windows.ps1`)

### Error 3: Ambiguous Argument with Git

**Error Message:**
```
fatal: ambiguous argument 'origin/dev': unknown revision or path not in the working tree.
```

**Context**: After fetching a branch with `git fetch origin dev`, trying to reference it as `origin/dev`.

**Why it happens**: Git fetch stores the ref as `FETCH_HEAD`, not as a trackable remote branch.

**Solution**: Use `FETCH_HEAD` or create a local tracking branch:
```bash
# Option 1: Use FETCH_HEAD directly
git log FETCH_HEAD

# Option 2: Create tracking branch
git fetch origin dev
git checkout -b dev --track origin/dev

# Option 3: Fetch with branch creation
git fetch origin dev:dev
```

### Error 4: Package Not Found During Build

**Context**: Sample applications may fail to build if NuGet packages are not found.

**Why it happens**: `UseNuGetForSamples` flag in `Directory.Build.props` controls whether samples use project references or NuGet packages.

**Solution**: Ensure you're using project references during development:
```xml
<!-- In Directory.Build.props -->
<UseNuGetForSamples>false</UseNuGetForSamples>
```

Or restore NuGet packages if building from packages:
```bash
dotnet restore
```

### Error 5: Strong Name Assembly Conflicts

**Context**: When building for .NET Framework 4.6.2, you may encounter assembly version conflicts.

**Why it happens**: .NET Framework uses strong-named assemblies, and SkiaSharp has different versioning.

**Referenced Issue**: https://github.com/mono/SkiaSharp/issues/3153

**Solution**: The project is configured to handle this, but if you encounter issues:
1. Clean the solution: `dotnet clean`
2. Delete `bin` and `obj` folders
3. Restore and rebuild: `dotnet restore && dotnet build`

### Error 6: Test Build Failures on CI

**Context**: UI tests may fail with target framework mismatches in CI.

**Solution**: The UI test infrastructure uses special MSBuild properties:
- `TestBuildTargetFramework`: Override target framework for test builds
- `IsTestBuild`: Flag to indicate test build
- `UITesting`: Flag to include shared UI tests

Example from `tests/UITests/Program.cs`:
```csharp
MSBuildArg tf_n10w = new("TestBuildTargetFramework", "net10.0-windows");
MSBuildArg isTest = new("IsTestBuild", "true");
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
