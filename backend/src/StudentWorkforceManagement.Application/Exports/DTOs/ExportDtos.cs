namespace StudentWorkforceManagement.Application.Exports.DTOs;

public enum ExportFormat { Csv, Xlsx, Pdf }
public enum ExportType { Tasks, Workload, Students, Semester, PersonalData }

public sealed record ExportRequestContractDto(ExportType Type, ExportFormat Format, Guid RequestedById, Guid? ScopeId, string PrivacyBoundary, string PersistenceGap);
