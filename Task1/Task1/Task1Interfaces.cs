namespace ConsoleApp1;
interface IClient
{
    Task<IResponse> GetApplicationStatus(string id, CancellationToken cancellationToken);
}

interface IResponse
{
}

record SuccessResponse(string Id, string Status): IResponse;
record FailureResponse(): IResponse;
record RetryResponse(TimeSpan Delay): IResponse;

interface IHandler
{
    Task<IApplicationStatus> GetApplicationStatus(string id);
}

interface IApplicationStatus;

record SuccessStatus(string ApplicationId, string Status): IApplicationStatus;
record FailureStatus(DateTime? LastRequestTime, int RetriesCount): IApplicationStatus;