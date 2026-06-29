namespace ChatApp.Core.Enums;

public enum CallStatus
{
    Pending,    // caller initiated, waiting for answer
    Active,     // both parties connected
    Ended,      // call finished normally
    Rejected,   // callee declined
    Missed      // callee never answered
}
