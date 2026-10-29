using System.Text;
using EquiApi.BackgroundServices;
using EquiApi.Core.Services;
using EquiApi.Test.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace EquiApi.Test;

public sealed class MeasurementDataBluetoothHandlerTests
{
    private const string DeviceId = "EP0701";

    private readonly IClock _clock = TestData.CreateClock();
    private readonly IMeasurementService _measurementServiceMock = Substitute.For<IMeasurementService>();
    private readonly MeasurementDataBluetoothHandler _sut;

    public MeasurementDataBluetoothHandlerTests()
    {
        var serviceProvider = Substitute.For<IServiceProvider>();
        serviceProvider.GetService(typeof(IMeasurementService)).Returns(_measurementServiceMock);
        var scopeFactory = Substitute.For<IServiceScopeFactory>();
        scopeFactory.CreateScope().ServiceProvider.Returns(serviceProvider);

        var options = Options.Create(new BluetoothOptions { MeasurementDeviceId = DeviceId });
        _sut = new MeasurementDataBluetoothHandler(scopeFactory, _clock, options,
                                                   NullLogger<MeasurementDataBluetoothHandler>.Instance);
    }

    [Fact]
    public async Task HandleAsync_MessageSplitIntoChunks_StoresCompleteMessage()
    {
        await HandleAsync("[12.5, 13.1, 14.8, 1");
        await HandleAsync("5.0, 11.2]\n");

        await _measurementServiceMock.Received(1)
                                     .AddDataToLatestMeasurementOfDeviceAsync(DeviceId, "[12.5, 13.1, 14.8, 15.0, 11.2]",
                                                                              _clock.GetCurrentInstant());
    }

    [Fact]
    public async Task HandleAsync_SeveralMessagesInOneChunk_StoresEachMessage()
    {
        await HandleAsync("12.5\n13.1\n");

        await _measurementServiceMock.Received(1)
                                     .AddDataToLatestMeasurementOfDeviceAsync(DeviceId, "12.5", Arg.Any<Instant>());
        await _measurementServiceMock.Received(1)
                                     .AddDataToLatestMeasurementOfDeviceAsync(DeviceId, "13.1", Arg.Any<Instant>());
    }

    [Fact]
    public async Task HandleAsync_CarriageReturnAndEmptyLines_StoresOnlyTrimmedMessages()
    {
        await HandleAsync("12.5\r\n\r\n\n");

        await _measurementServiceMock.Received(1)
                                     .AddDataToLatestMeasurementOfDeviceAsync(DeviceId, "12.5", Arg.Any<Instant>());
        await _measurementServiceMock.Received(1)
                                     .AddDataToLatestMeasurementOfDeviceAsync(Arg.Any<string>(), Arg.Any<string>(),
                                                                              Arg.Any<Instant>());
    }

    [Fact]
    public async Task HandleAsync_NoLineTerminator_StoresNothing()
    {
        await HandleAsync("12.5");

        await _measurementServiceMock.DidNotReceive()
                                     .AddDataToLatestMeasurementOfDeviceAsync(Arg.Any<string>(), Arg.Any<string>(),
                                                                              Arg.Any<Instant>());
    }

    [Fact]
    public async Task Reset_IncompleteMessageOfPreviousConnection_IsNotPrependedToNextMessage()
    {
        await HandleAsync("12");

        _sut.Reset();
        await HandleAsync("3.4\n");

        await _measurementServiceMock.Received(1)
                                     .AddDataToLatestMeasurementOfDeviceAsync(DeviceId, "3.4", Arg.Any<Instant>());
        await _measurementServiceMock.DidNotReceive()
                                     .AddDataToLatestMeasurementOfDeviceAsync(DeviceId, "123.4", Arg.Any<Instant>());
    }

    private ValueTask HandleAsync(string chunk) =>
        _sut.HandleAsync(Encoding.UTF8.GetBytes(chunk), CancellationToken.None);
}