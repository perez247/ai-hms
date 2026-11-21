namespace Hms.Application.Common;

// Application-level exceptions mapped to 401/403/404/409 by the API middleware.

public sealed class AppUnauthorizedException(string message) : Exception(message);
public sealed class AppForbiddenException(string message) : Exception(message);
public sealed class AppNotFoundException(string message) : Exception(message);
public sealed class AppConflictException(string message) : Exception(message);
