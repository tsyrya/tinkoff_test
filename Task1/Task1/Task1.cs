using Microsoft.Extensions.Logging;

namespace ConsoleApp1;

class Handler(
    IClient service1,
    IClient service2,
    ILogger<Handler> logger
) : IHandler
{
    private readonly IClient _client1 = service1;
    private readonly IClient _client2 = service2;
    private readonly ILogger<Handler> _logger = logger;
    
    private const int MaxExecutionTime = 15000; //15s

    public async Task<IApplicationStatus> GetApplicationStatus(string id)
    {
        var globalCancellationToken = new CancellationTokenSource(TimeSpan.FromMilliseconds(MaxExecutionTime));
        
        var lastRequestTime = DateTime.Now;
        var retriesCount = 0;

        async Task<IApplicationStatus> MakeRequests(CancellationToken cancellationToken)
        {
            while (true)
            {
                CancellationTokenRegistration? cancellationRegistration = null;
                try
                {
                    var localCancellationToken = new CancellationTokenSource();

                    cancellationRegistration = cancellationToken.Register(() => localCancellationToken.Cancel());

                    var whenTask = await Task.WhenAny(
                        _client1.GetApplicationStatus(id, localCancellationToken.Token),
                        _client2.GetApplicationStatus(id, localCancellationToken.Token)
                    );

                    var response = await whenTask;

                    await localCancellationToken.CancelAsync();

                    switch (response)
                    {
                        case RetryResponse retryResponse:
                            lastRequestTime = DateTime.Now;
                            retriesCount++;
                            await Task.Delay(retryResponse.Delay, cancellationToken);
                            continue;
                        case SuccessResponse successResponse:
                            return new SuccessStatus(successResponse.Id, successResponse.Status);
                        case FailureResponse:
                            return new FailureStatus(lastRequestTime, retriesCount);
                    }
                }
                finally
                {
                    if (cancellationRegistration != null)
                        await cancellationRegistration.Value.DisposeAsync();
                }
            }
        }
        
        try
        {
            return await MakeRequests(globalCancellationToken.Token);
        }
        catch (Exception)
        {
            return new FailureStatus(lastRequestTime, retriesCount);
        }
        finally
        {
            globalCancellationToken.Dispose();
        }
    }
}

internal class Client(int delay): IClient
{
    public async Task<IResponse> GetApplicationStatus(string id, CancellationToken cancellationToken)
    {
        await Task.Delay(delay, cancellationToken);
        return new SuccessResponse(id, delay.ToString());
    }
}

internal class ClientWitException(): IClient
{
    public Task<IResponse> GetApplicationStatus(string id, CancellationToken cancellationToken)
    {
        throw new Exception();
    }
}


internal class ClientWithFail(int delay): IClient
{
    public async Task<IResponse> GetApplicationStatus(string id, CancellationToken cancellationToken)
    {
        await Task.Delay(delay, cancellationToken);
        return new FailureResponse();
    }
}


internal class ClientWithRetry(int delay, TimeSpan retryDelay): IClient
{
    public async Task<IResponse> GetApplicationStatus(string id, CancellationToken cancellationToken)
    {
        await Task.Delay(delay, cancellationToken);
        return new RetryResponse(retryDelay);
    }
}