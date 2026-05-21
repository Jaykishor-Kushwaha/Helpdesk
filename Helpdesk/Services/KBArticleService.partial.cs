using Helpdesk.DTOs;
using Helpdesk.Exceptions;
using Helpdesk.Models;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Helpdesk.Services
{
    public partial class KBArticleService
    {
        public async Task<IEnumerable<KBArticleDto>> SuggestAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<KBArticleDto>();

            var lowerQuery = query.ToLower();
            
            var articlesQuery = _unitOfWork.KBArticles.Query()
                .Include(k => k.Category)
                .Include(k => k.Author)
                .Where(k => k.Status == Helpdesk.Enums.KBArticleStatus.Published);

            var articles = await articlesQuery.ToListAsync();

            var scoredArticles = articles.Select(a => new
            {
                Article = a,
                Score = (a.Title.ToLower().Contains(lowerQuery) ? 3 : 0) +
                        (a.Tags.ToLower().Contains(lowerQuery) ? 2 : 0) +
                        (a.Content.ToLower().Contains(lowerQuery) ? 1 : 0)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Article.ViewCount)
            .Take(3)
            .Select(x => x.Article)
            .ToList();

            return _mapper.Map<IEnumerable<KBArticleDto>>(scoredArticles);
        }

        public async Task<bool> RecordSolvedAsync(int ticketId, int articleId, bool isHelpful)
        {
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(ticketId);
            if (ticket == null && ticketId != 0) // if 0, it means it was a new ticket creation flow that was dismissed
            {
                // We'll still record it even without a ticket if ticketId == 0 for reporting purposes
            }

            var kbSolveEvent = new KBSolveEvent
            {
                TicketId = ticketId,
                KBArticleId = articleId,
                UserId = _currentUser.UserId,
                IsHelpful = isHelpful,
                SolvedAt = DateTime.UtcNow
            };

            await _unitOfWork.KBSolveEvents.AddAsync(kbSolveEvent);
            await _unitOfWork.SaveChangesAsync();

            // Also increment Helpful/NotHelpful counters on the article
            await SubmitFeedbackAsync(articleId, isHelpful);

            return true;
        }

        public async Task<bool> AttachToCommentAsync(int commentId, int articleId)
        {
            var comment = await _unitOfWork.Comments.GetByIdAsync(commentId);
            if (comment == null) throw new NotFoundException("Comment", commentId);

            var article = await _unitOfWork.KBArticles.GetByIdAsync(articleId);
            if (article == null) throw new NotFoundException("KBArticle", articleId);

            // Enforce unique index guard programmatically to avoid DB constraint violation exceptions
            var existing = await _unitOfWork.KBCommentAttachments.Query()
                .FirstOrDefaultAsync(k => k.CommentId == commentId && k.KBArticleId == articleId);

            if (existing != null)
                return false; // Already attached

            var attachment = new KBCommentAttachment
            {
                CommentId = commentId,
                KBArticleId = articleId,
                AttachedByUserId = _currentUser.UserId,
                AttachedAt = DateTime.UtcNow
            };

            await _unitOfWork.KBCommentAttachments.AddAsync(attachment);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogAsync(
                Helpdesk.Enums.AuditEventType.KBArticleUpdated, // Generic or new enum value
                Helpdesk.Enums.AuditEntityType.Ticket,
                comment.TicketId,
                $"KB Article '{article.Title}' attached to comment {commentId}.",
                _currentUser.UserId,
                comment.TicketId);

            return true;
        }
    }
}
