using SharedKernel;

namespace HrAdministration.Application.EventSourcing;

/// <summary>Refusals of the event store. Codes are part of the API (section 6 of the architecture), messages are shown to the employee.</summary>
public static class EventStoreErrors
{
    public static readonly Error ConcurrencyConflict = new(
        "concurrency.conflict",
        "Ces données viennent d'être modifiées par une autre opération. Rechargez-les puis réessayez.");
}
