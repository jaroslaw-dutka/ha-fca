using FcaAssistant.App;
using FcaAssistant.Fca;
using FcaAssistant.Ha;
using FcaAssistant.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var cts = new CancellationTokenSource();
Console.CancelKeyPress += delegate { cts.Cancel(); };

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json")
    .AddEnvironmentVariables("FcaAssistant_")
    .AddUserSecrets<Program>()
    .Build();

var services = new ServiceCollection();

services.AddLogger(configuration);
services.AddHttp(configuration);

services.AddFca(configuration);
services.AddHa(configuration);
services.AddApp(configuration);

var provider = services.BuildServiceProvider();

var app = provider.GetRequiredService<IAppService>();
if (!cts.IsCancellationRequested)
    await app.RunAsync(cts.Token);