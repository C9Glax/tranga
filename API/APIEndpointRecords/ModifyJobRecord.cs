namespace API.APIEndpointRecords;

public record ModifyJobRecord(ulong? RecurrenceMs, bool? Enabled);