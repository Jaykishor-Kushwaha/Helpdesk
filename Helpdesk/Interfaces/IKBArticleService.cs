using Helpdesk.DTOs;

namespace Helpdesk.Interfaces
{
    public interface IKBArticleService
    {
        Task<PagedResponse<KBArticleDto>> GetAllAsync(int page = 1, int pageSize = 10, int? categoryId = null);
        Task<KBArticleDto?> GetByIdAsync(int id);
        Task<KBArticleDto> CreateAsync(CreateKBArticleDto dto);
        Task<KBArticleDto?> UpdateAsync(int id, UpdateKBArticleDto dto);
        Task<bool> DeleteAsync(int id);
        
        Task<IEnumerable<KBArticleVersionDto>> GetArticleVersionsAsync(int articleId);
        Task<KBArticleVersionDto?> GetVersionAsync(int articleId, int versionNumber);
        Task<KBArticleDto?> RevertToVersionAsync(int articleId, int versionNumber);
        Task IncrementViewCountAsync(int id);
        
        Task SubmitFeedbackAsync(int id, bool isHelpful);
        Task<IEnumerable<KBArticleDto>> SearchAsync(string query);
        Task<IEnumerable<KBArticleDto>> SuggestAsync(string query);
        Task<KBArticleDto?> SubmitForReviewAsync(int id);
        Task<KBArticleDto?> ApproveAsync(int id);
        Task<KBArticleDto?> RejectAsync(int id, string reason);
        
        // Gap 3 Additions
        Task<bool> RecordSolvedAsync(int ticketId, int articleId, bool isHelpful);
        Task<bool> AttachToCommentAsync(int commentId, int articleId);
    }
}
