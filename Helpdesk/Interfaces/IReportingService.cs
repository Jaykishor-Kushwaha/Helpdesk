namespace Helpdesk.Interfaces
{
    public class ReportResult
    {
        public bool IsAsyncProcessing { get; set; }
        public string? MimeType { get; set; }
        public byte[]? FileContent { get; set; }
        public string? FileName { get; set; }
        public string? Message { get; set; }
        public string? DownloadToken { get; set; }
    }

    public interface IReportingService
    {
        Task<ReportResult> GenerateExportAsync(Helpdesk.DTOs.ReportFilterDto filter);
        Task<(byte[] content, string mimeType)> BuildReportCoreAsync(Helpdesk.DTOs.ReportFilterDto filter);
    }
}
