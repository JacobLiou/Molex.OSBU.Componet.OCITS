namespace ITL.AutomationGateway.Api.Domain;

public static class ErrorCodes
{
    public const string BadRequest = "BAD_REQUEST";

    public const string NotFound = "NOT_FOUND";

    public const string Conflict = "CONFLICT";

    public const string Timeout = "TIMEOUT";

    public const string AdapterDisconnected = "ADAPTER_DISCONNECTED";

    public const string OperationNotSupported = "OPERATION_NOT_SUPPORTED";

    public const string LegacyError = "LEGACY_ERROR";

    public const string Unhandled = "UNHANDLED";

    public const string Canceled = "CANCELED";
}
