using AutoMapper;
using Helpdesk.DTOs;
using Helpdesk.Exceptions;
using Helpdesk.Interfaces;
using Helpdesk.Models;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Services
{
    public partial class KBArticleService : IKBArticleService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;
        private readonly IAuditLogService _auditLogService;
        private readonly INotificationService _notificationService;
        private readonly IConfiguration _configuration;

        public KBArticleService(
            IUnitOfWork unitOfWork, 
            IMapper mapper, 
            ICurrentUserService currentUser, 
            IAuditLogService auditLogService,
            INotificationService notificationService,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUser = currentUser;
            _auditLogService = auditLogService;
            _notificationService = notificationService;
            _configuration = configuration;
        }

        public async Task<PagedResponse<KBArticleDto>> GetAllAsync(int page = 1, int pageSize = 10, int? categoryId = null)
        {
            var query = _unitOfWork.KBArticles.Query()
                .Include(k => k.Category)
                .Include(k => k.Author)
                .AsQueryable();

            if (_currentUser.Role == Helpdesk.Helper.Roles.User)
            {
                query = query.Where(k => k.Status == Helpdesk.Enums.KBArticleStatus.Published);
            }

            if (categoryId.HasValue)
            {
                query = query.Where(k => k.CategoryId == categoryId.Value);
            }

            var totalItems = await query.CountAsync();
            var articles = await query
                .OrderByDescending(k => k.LastUpdatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var dtos = _mapper.Map<List<KBArticleDto>>(articles);

            return new PagedResponse<KBArticleDto>
            {
                Data = dtos,
                Page = page,
                PageSize = pageSize,
                TotalCount = totalItems,
                TotalPages = (int)Math.Ceiling(totalItems / (double)pageSize)
            };
        }



        public async Task<KBArticleDto?> GetByIdAsync(int id)
        {
            var query = _unitOfWork.KBArticles.Query()
                .Include(k => k.Category)
                .Include(k => k.Author)
                .AsQueryable();

            if (_currentUser.Role == Helpdesk.Helper.Roles.User)
            {
                query = query.Where(k => k.Status == Helpdesk.Enums.KBArticleStatus.Published);
            }

            var article = await query.FirstOrDefaultAsync(k => k.Id == id);

            if (article == null) return null;

            return _mapper.Map<KBArticleDto>(article);
        }

        public async Task IncrementViewCountAsync(int id)
        {
            var article = await _unitOfWork.KBArticles.GetByIdAsync(id);
            if (article != null)
            {
                article.ViewCount++;
                await _unitOfWork.KBArticles.UpdateAsync(article);
                await _unitOfWork.SaveChangesAsync();
            }
        }

        public async Task<KBArticleDto> CreateAsync(CreateKBArticleDto dto)
        {
            var article = _mapper.Map<KBArticle>(dto);
            article.AuthorId = _currentUser.UserId;
            if (!string.Equals(_currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                article.Status = Helpdesk.Enums.KBArticleStatus.Draft;
            article.CreatedAt = DateTime.UtcNow;
            article.LastUpdatedAt = DateTime.UtcNow;
            article.ViewCount = 0;
            article.HelpfulCount = 0;
            article.NotHelpfulCount = 0;

            await _unitOfWork.KBArticles.AddAsync(article);
            await _unitOfWork.SaveChangesAsync();

            // Create initial version
            var version = new KBArticleVersion
            {
                KBArticleId = article.Id,
                VersionNumber = 1,
                Content = article.Content,
                CreatedByUserId = _currentUser.UserId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.KBArticleVersions.AddAsync(version);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogArticleCreatedAsync(article.Id, article.Title, _currentUser.UserId);

            // No typed notification exists for KB admin alerts; keep direct queue for admin
            var adminEmail = _configuration["AdminSettings:Email"];
            if (!string.IsNullOrEmpty(adminEmail))
            {
                await _notificationService.QueueEmailAsync(
                    adminEmail,
                    $"KB Article Created: {article.Title}",
                    $"A new KB Article '{article.Title}' has been created and is pending review.");
            }

            return await GetByIdAsync(article.Id) ?? throw new InvalidOperationException("Failed to load created KB article.");
        }

        public async Task<KBArticleDto?> UpdateAsync(int id, UpdateKBArticleDto dto)
        {
            var article = await _unitOfWork.KBArticles.GetByIdAsync(id);
            if (article == null) throw new NotFoundException("KBArticle", id);

            bool isAdmin = string.Equals(_currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase);
            if (!isAdmin)
            {
                if (article.AuthorId != _currentUser.UserId || article.Status != Helpdesk.Enums.KBArticleStatus.Draft)
                    throw new ForbiddenException("Agents can only edit their own draft articles.");

                if (dto.Status.HasValue &&
                    dto.Status.Value != Helpdesk.Enums.KBArticleStatus.Draft &&
                    dto.Status.Value != Helpdesk.Enums.KBArticleStatus.PendingReview)
                    throw new ForbiddenException("Agents can only submit draft articles for review.");
            }

            bool contentChanged = !string.IsNullOrWhiteSpace(dto.Content) && dto.Content != article.Content;

            if (!string.IsNullOrWhiteSpace(dto.Title)) article.Title = dto.Title;
            if (!string.IsNullOrWhiteSpace(dto.Tags)) article.Tags = dto.Tags;
            if (contentChanged) article.Content = dto.Content!;
            if (dto.CategoryId.HasValue) article.CategoryId = dto.CategoryId.Value;
            if (dto.Status.HasValue) article.Status = dto.Status.Value;

            article.LastUpdatedAt = DateTime.UtcNow;

            await _unitOfWork.KBArticles.UpdateAsync(article);

            if (contentChanged)
            {
                // Find latest version number
                var latestVersion = await _unitOfWork.KBArticleVersions.Query()
                    .Where(v => v.KBArticleId == article.Id)
                    .OrderByDescending(v => v.VersionNumber)
                    .FirstOrDefaultAsync();

                var newVersionNumber = (latestVersion?.VersionNumber ?? 0) + 1;

                var version = new KBArticleVersion
                {
                    KBArticleId = article.Id,
                    VersionNumber = newVersionNumber,
                    Content = article.Content,
                    CreatedByUserId = _currentUser.UserId,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.KBArticleVersions.AddAsync(version);
            }

            await _unitOfWork.SaveChangesAsync();

            var auditEventType = dto.Status == Helpdesk.Enums.KBArticleStatus.Published
                ? Helpdesk.Enums.AuditEventType.KBArticlePublished
                : Helpdesk.Enums.AuditEventType.KBArticleUpdated;

            if (auditEventType == Helpdesk.Enums.AuditEventType.KBArticlePublished)
                await _auditLogService.LogArticlePublishedAsync(article.Id, article.Title, _currentUser.UserId);
            else
                await _auditLogService.LogArticleUpdatedAsync(article.Id, article.Title, article.Status.ToString(), _currentUser.UserId);

            var adminEmail = _configuration["AdminSettings:Email"];
            if (!string.IsNullOrEmpty(adminEmail))
            {
                var eventName = auditEventType == Helpdesk.Enums.AuditEventType.KBArticlePublished ? "Published" : "Updated";
                await _notificationService.QueueEmailAsync(
                    adminEmail,
                    $"KB Article {eventName}: {article.Title}",
                    $"The KB Article '{article.Title}' has been {eventName.ToLower()}.");
            }

            return await GetByIdAsync(article.Id);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var article = await _unitOfWork.KBArticles.GetByIdAsync(id);
            if (article == null) return false;

            await _unitOfWork.KBArticles.DeleteAsync(article);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogArticleDeletedAsync(id, article.Title, _currentUser.UserId);
            return true;
        }

        public async Task<IEnumerable<KBArticleVersionDto>> GetArticleVersionsAsync(int articleId)
        {

            var versions = await _unitOfWork.KBArticleVersions.Query()
                .Include(v => v.CreatedByUser)
                .Where(v => v.KBArticleId == articleId)
                .OrderByDescending(v => v.VersionNumber)
                .ToListAsync();

            return _mapper.Map<IEnumerable<KBArticleVersionDto>>(versions);
        }

        public async Task<KBArticleVersionDto?> GetVersionAsync(int articleId, int versionNumber)
        {
            var version = await _unitOfWork.KBArticleVersions.Query()
                .Include(v => v.CreatedByUser)
                .FirstOrDefaultAsync(v => v.KBArticleId == articleId && v.VersionNumber == versionNumber);

            return version == null ? null : _mapper.Map<KBArticleVersionDto>(version);
        }

        public async Task<KBArticleDto?> RevertToVersionAsync(int articleId, int versionNumber)
        {
            var article = await _unitOfWork.KBArticles.GetByIdAsync(articleId);
            if (article == null) throw new NotFoundException("KBArticle", articleId);

            var version = await _unitOfWork.KBArticleVersions.Query()
                .FirstOrDefaultAsync(v => v.KBArticleId == articleId && v.VersionNumber == versionNumber);

            if (version == null) throw new NotFoundException("KBArticleVersion", versionNumber);

            // Revert content
            var updateDto = new UpdateKBArticleDto
            {
                Content = version.Content
            };

            return await UpdateAsync(articleId, updateDto);
        }

        public async Task SubmitFeedbackAsync(int id, bool isHelpful)
        {
            var article = await _unitOfWork.KBArticles.GetByIdAsync(id);
            if (article == null) throw new NotFoundException("KBArticle", id);

            if (isHelpful)
                article.HelpfulCount++;
            else
                article.NotHelpfulCount++;

            await _unitOfWork.KBArticles.UpdateAsync(article);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<KBArticleDto>> SearchAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<KBArticleDto>();

            var lowerQuery = query.ToLower();
            var articlesQuery = _unitOfWork.KBArticles.Query()
                .Include(k => k.Category)
                .Include(k => k.Author)
                .Where(k => k.Status == Helpdesk.Enums.KBArticleStatus.Published)
                .AsQueryable();

            var articles = await articlesQuery.ToListAsync();

            // Basic relevance scoring
            var scoredArticles = articles.Select(a => new
            {
                Article = a,
                Score = (a.Title.ToLower().Contains(lowerQuery) ? 10 : 0) +
                        (a.Tags.ToLower().Contains(lowerQuery) ? 5 : 0) +
                        (a.Content.ToLower().Contains(lowerQuery) ? 1 : 0)
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Take(10)
            .Select(x => x.Article)
            .ToList();

            return _mapper.Map<IEnumerable<KBArticleDto>>(scoredArticles);
        }



        public async Task<KBArticleDto?> SubmitForReviewAsync(int id)
        {
            var article = await _unitOfWork.KBArticles.GetByIdAsync(id);
            if (article == null) throw new NotFoundException("KBArticle", id);

            if (article.AuthorId != _currentUser.UserId && !string.Equals(_currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                throw new ForbiddenException("Only the author or Admin can submit this article for review.");

            article.Status = Helpdesk.Enums.KBArticleStatus.PendingReview;
            article.LastUpdatedAt = DateTime.UtcNow;
            await _unitOfWork.KBArticles.UpdateAsync(article);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogArticleUpdatedAsync(article.Id, article.Title, article.Status.ToString(), _currentUser.UserId);

            return await GetByIdAsync(id);
        }

        public async Task<KBArticleDto?> ApproveAsync(int id)
        {
            var article = await _unitOfWork.KBArticles.GetByIdAsync(id);
            if (article == null) throw new NotFoundException("KBArticle", id);

            if (!string.Equals(_currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                throw new ForbiddenException("Only Admin can approve KB articles.");

            article.Status = Helpdesk.Enums.KBArticleStatus.Published;
            article.LastUpdatedAt = DateTime.UtcNow;
            await _unitOfWork.KBArticles.UpdateAsync(article);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogArticlePublishedAsync(article.Id, article.Title, _currentUser.UserId);

            return await GetByIdAsync(id);
        }

        public async Task<KBArticleDto?> RejectAsync(int id, string reason)
        {
            var article = await _unitOfWork.KBArticles.GetByIdAsync(id);
            if (article == null) throw new NotFoundException("KBArticle", id);

            if (!string.Equals(_currentUser.Role, "Admin", StringComparison.OrdinalIgnoreCase))
                throw new ForbiddenException("Only Admin can reject KB articles.");

            article.Status = Helpdesk.Enums.KBArticleStatus.Rejected;
            article.LastUpdatedAt = DateTime.UtcNow;
            await _unitOfWork.KBArticles.UpdateAsync(article);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogArticleRejectedAsync(article.Id, article.Title, reason, _currentUser.UserId);

            return await GetByIdAsync(id);
        }
    }
}
