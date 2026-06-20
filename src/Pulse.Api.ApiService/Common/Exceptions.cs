namespace Pulse.Api.ApiService.Common;

/// <summary>Base for domain errors that map directly to HTTP problem responses.</summary>
public abstract class ApiException(int statusCode, string message) : Exception(message)
{
    public int StatusCode { get; } = statusCode;
}

public class NotFoundException(string message) : ApiException(StatusCodes.Status404NotFound, message);

public class ForbiddenException(string message) : ApiException(StatusCodes.Status403Forbidden, message);

public class ConflictException(string message) : ApiException(StatusCodes.Status409Conflict, message);

public class DomainRuleException(string message) : ApiException(StatusCodes.Status422UnprocessableEntity, message);

public class ValidationException(string message) : ApiException(StatusCodes.Status400BadRequest, message);
