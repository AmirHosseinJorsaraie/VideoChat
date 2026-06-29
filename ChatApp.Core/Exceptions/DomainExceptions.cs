namespace ChatApp.Core.Exceptions;

public class NotFoundException(string entity, object key)
    : Exception($"{entity} with key '{key}' was not found.");

public class UnauthorizedException(string message = "You are not authorized to perform this action.")
    : Exception(message);

public class RoomNotLiveException(Guid roomId)
    : Exception($"Room '{roomId}' is not currently live.");

public class RoomAlreadyLiveException(Guid roomId)
    : Exception($"Room '{roomId}' is already live.");

public class CallAlreadyActiveException(Guid userId)
    : Exception($"User '{userId}' already has an active call.");

public class CallNotFoundException(Guid callId)
    : Exception($"Call '{callId}' was not found or has already ended.");

public class InvalidCallStateException(string message)
    : Exception(message);
