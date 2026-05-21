
using System.Threading.Channels;

namespace Helpdesk.Services
{
    public class ReportRequest
    {
        public string ReportToken { get; set; } = string.Empty;
        public int RequestingUserId { get; set; }
        public Helpdesk.DTOs.ReportFilterDto Filter { get; set; } = null!;
    }

    public interface IReportQueue
    {
        void Enqueue(ReportRequest request);
        Task<ReportRequest> DequeueAsync(CancellationToken cancellationToken);
    }

    public class ReportQueue : IReportQueue
    {
        private readonly Channel<ReportRequest> _queue;

        public ReportQueue()
        {
            var options = new BoundedChannelOptions(100)
            {
                FullMode = BoundedChannelFullMode.Wait
            };
            _queue = Channel.CreateBounded<ReportRequest>(options);
        }

        public void Enqueue(ReportRequest request)
        {
            _queue.Writer.TryWrite(request);
        }

        public async Task<ReportRequest> DequeueAsync(CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}
