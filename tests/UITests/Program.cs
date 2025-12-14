using Factos.Server;
using Factos.Server.Settings;
using Factos.Server.Settings.Apps;
using Microsoft.Testing.Extensions;
using Microsoft.Testing.Platform.Builder;

var root = "../../samples";

var testsBuilder = await TestApplication.CreateBuilderAsync(args);
var testedApps = new List<TestedApp>();

testedApps
    .Add(project: $"{root}/WpfSample");

testsBuilder
    .AddFactos(new FactosSettings()
    {
        ConnectionTimeout = 180,
        TestedApps = testedApps
    })
    .AddTrxReportProvider(); // optional, add TRX if needed

using var testApp = await testsBuilder.BuildAsync();

return await testApp.RunAsync();
