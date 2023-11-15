using FluentAssertions;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Xunit.Abstractions;

namespace MassTransitActiveMQTimeoutTest;

public class MassTransitActiveMQReconnectTest : IClassFixture<ActiveMqContainerFixture>
{
    private readonly ActiveMqContainerFixture _activeMqFixture;
    private readonly ITestOutputHelper _testOutputHelper;

    public MassTransitActiveMQReconnectTest(ActiveMqContainerFixture activeMqFixture, ITestOutputHelper testOutputHelper)
    {
        _activeMqFixture = activeMqFixture;
        _testOutputHelper = testOutputHelper;
    }

    [Fact]
    public async Task RequestResponse_SameHostName_Success()
    {
        // arrange
        var activeMqContainer = await _activeMqFixture.ActiveMqContainer;

        var consumerApp = new ConsumerTestApp(_testOutputHelper, host: "127.0.0.1", activeMqContainer.GetMappedPublicPort(61616));
        consumerApp.RunAsync();

        var producerApp = new ProducerTestApp(_testOutputHelper, host: "127.0.0.1", activeMqContainer.GetMappedPublicPort(61616));
        producerApp.RunAsync();

        var producerBus = producerApp.TestHost.Services.GetService<IBus>();
        var testRequest = new TestRequest { Message = "test-message" };

        // act && assert

        // make sure response can be received 
        var requestClient = producerBus.CreateRequestClient<TestRequest>(new Uri("queue:TestCommand"), RequestTimeout.Default);
        var response = await requestClient.GetResponse<TestRequest>(testRequest, timeout: RequestTimeout.Default).ConfigureAwait(false);

        response.Message.Message.Should().Be(testRequest.Message);

        await consumerApp.DisposeAsync();
        await producerApp.DisposeAsync();
    }


    [Fact]
    public async Task RequestResponse_DifferentHostName_RequestTimeoutException()
    {
        // arrange
        var activeMqContainer = await _activeMqFixture.ActiveMqContainer;

        var consumerApp = new ConsumerTestApp(_testOutputHelper, "127.0.0.1", activeMqContainer.GetMappedPublicPort(61616));
        consumerApp.RunAsync();

        var producerApp = new ProducerTestApp(_testOutputHelper, "localhost", activeMqContainer.GetMappedPublicPort(61616));
        producerApp.RunAsync();

        var producerBus = producerApp.TestHost.Services.GetService<IBus>();
        var testRequest = new TestRequest { Message = "test-message" };

        // act && assert

        // make sure response can be received 
        var requestClient = producerBus.CreateRequestClient<TestRequest>(new Uri("queue:TestCommand"), RequestTimeout.Default);
        var responseAction = async () => await requestClient.GetResponse<TestRequest>(testRequest, timeout: RequestTimeout.Default).ConfigureAwait(false);

        await responseAction.Should().ThrowAsync<RequestTimeoutException>();

        await consumerApp.DisposeAsync();
        await producerApp.DisposeAsync();
    }

    public ValueTask DisposeAsync()
    {
        return _activeMqFixture.DisposeAsync();
    }
}