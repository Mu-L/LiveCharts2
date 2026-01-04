using Factos.Server;
using Factos.Server.Settings;
using Factos.Server.Settings.Apps;
using Microsoft.Testing.Extensions;
using Microsoft.Testing.Platform.Builder;

// About UI test:
// we use https://github.com/beto-rodriguez/Factos
// it allows us to run the same UI tests against multiple UI frameworks,
// including desktop, web and mobile platforms.

#if DEBUG
// To select which app to run, set the appToRun variable here, options are:
// "manual-start",
// "wpf", "avalonia-desktop", "blazor", "maui", "uno", "winforms", "winui", "eto", "avalonia-android", "avalonia-browser", "avalonia-ios"
// setting appToRun to "manual-start" will wait for you to start the app manually.
// uno and maui require also to pass the target framework, see tf variable below, will be ignored if not required by the tested app.
// emulators must be running before starting the tests.

var appToRun = "maui";
var tf = "net10.0-android";

args = [
    "--select", appToRun,
    "--test-env", $"tf={tf}"
];

// in debug we use the relative path from the bin\debug folder to the samples
var root = "../../../../../samples";
#else
// in release we use the relative path from the root of the repo for CI purposes
var root = "samples";
#endif

var testsBuilder = await TestApplication.CreateBuilderAsync(args);
var testedApps = new List<TestedApp>();

var avaloniaDir = "AvaloniaSample/Platforms/AvaloniaSample";

MSBuildArg[] msBuildArgs = [];

#if !DEBUG
// in CI we use the nuget packages for everything
// we pack and test the nuget packages against the samples
msBuildArgs = [
    new("UseNuGetForSamples", "true"),
    new("LiveChartsVersionSuffix", "[lvcversionsuffix]"),
];
#endif

testedApps
    .AddManuallyStartedApp()
    .Add(project: $"{root}/WpfSample", uid: "wpf", msBuildArgs: msBuildArgs)
    .Add(project: $"{root}/{avaloniaDir}.Android", uid: "avalonia-android", msBuildArgs: msBuildArgs)
    .Add(project: $"{root}/{avaloniaDir}.Browser", appHost: AppHost.HeadlessChrome, uid: "avalonia-browser", msBuildArgs: msBuildArgs)
    .Add(project: $"{root}/{avaloniaDir}.Desktop", uid: "avalonia-desktop", msBuildArgs: msBuildArgs)
    .Add(project: $"{root}/{avaloniaDir}.iOS", uid: "avalonia-ios", msBuildArgs: msBuildArgs)
    .Add(project: $"{root}/BlazorSample", appHost: AppHost.HeadlessChrome, uid: "blazor", msBuildArgs: msBuildArgs)
    .Add(project: $"{root}/MauiSample", targetFramework: "[tf]", uid: "maui", msBuildArgs: msBuildArgs)
    .Add(project: $"{root}/UnoPlatformSample/UnoPlatformSample", targetFramework: "[tf]", uid: "uno", msBuildArgs: msBuildArgs)
    .Add(project: $"{root}/WinFormsSample", uid: "winforms", msBuildArgs: msBuildArgs)
    .Add(project: $"{root}/WinUISample/WinUISample", uid: "winui",
        runtimeIdentifier: "win-x64",
        msBuildArgs: [
            ..msBuildArgs,
            new("WindowsPackageType", "None"),
            new("WindowsAppSDKSelfContained", "true")
        ])
    .Add(project: $"{root}/EtoFormsSample", uid: "eto", msBuildArgs: msBuildArgs);

testsBuilder
    .AddFactos(new FactosSettings()
    {
        ConnectionTimeout = 180,
        TestedApps = testedApps
    })
    .AddTrxReportProvider(); // optional, add TRX if needed

using var testApp = await testsBuilder.BuildAsync();

return await testApp.RunAsync();
