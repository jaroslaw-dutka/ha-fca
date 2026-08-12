using FcaAssistant.App;
using FcaAssistant.Fca;
using FcaAssistant.Tests.Fakes;
using Flurl.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FcaAssistant.Tests.App;

public class AppLoopTests
{
    private readonly FakeFcaClient _fca = new();
    private readonly RecordingHaMqttClient _mqtt = new();
    private readonly FakeRefreshTrigger _refreshTrigger = new();
    private readonly CancellationTokenSource _cts = new();

    // RefreshInterval = 0 makes the between-tick WaitAny return immediately, so the loop is driven
    // purely by how soon the processor cancels the token.
    private AppLoop CreateLoop(IVehicleProcessor processor) => new(
        NullLogger<AppLoop>.Instance,
        Options.Create(new AppSettings { StartDelaySeconds = 0, RefreshInterval = 0 }),
        Options.Create(new FcaSettings()),
        _fca,
        _mqtt,
        _refreshTrigger,
        processor);

    [Fact]
    public async Task ConnectsBothBackendsBeforeProcessing()
    {
        var connectedBeforeFirstPass = false;
        var processor = new FakeVehicleProcessor(_ =>
        {
            connectedBeforeFirstPass = _fca.Connected && _mqtt.Connected;
            _cts.Cancel();
        });

        await CreateLoop(processor).RunAsync(_cts.Token);

        Assert.True(connectedBeforeFirstPass);
        Assert.Equal(1, processor.Calls);
    }

    [Fact]
    public async Task KeepsLoopingAfterProcessorThrows()
    {
        var processor = new FakeVehicleProcessor(call =>
        {
            switch (call)
            {
                case 1: throw new InvalidOperationException("boom");
                case 2: throw new FlurlHttpException(new FlurlCall { Request = new FlurlRequest("https://x") });
                default: _cts.Cancel(); break;
            }
        });

        await CreateLoop(processor).RunAsync(_cts.Token);

        Assert.Equal(3, processor.Calls);
    }

    [Fact]
    public async Task StopsWhenTokenIsCancelledDuringProcessing()
    {
        var processor = new FakeVehicleProcessor(_ => _cts.Cancel());

        await CreateLoop(processor).RunAsync(_cts.Token);

        Assert.Equal(1, processor.Calls);
    }

    private sealed class FakeVehicleProcessor(Action<int> onProcess) : IVehicleProcessor
    {
        public int Calls { get; private set; }

        public Task ProcessAsync(CancellationToken cancellationToken)
        {
            Calls++;
            onProcess(Calls);
            return Task.CompletedTask;
        }
    }
}
