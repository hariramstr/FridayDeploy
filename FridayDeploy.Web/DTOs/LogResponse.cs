namespace FridayDeploy.Web.DTOs;

public sealed record LogResponse(long Id, DateTime TimestampUtc, string Application, string? Environment, string? Machine, string Level, string Message, string? Source, string? CorrelationId, string? RequestId, double? Duration);

public sealed record PagedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
