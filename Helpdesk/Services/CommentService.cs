using AutoMapper;
using Helpdesk.DTOs;
using Helpdesk.Enums;
using Helpdesk.Exceptions;
using Helpdesk.Interfaces;
using Helpdesk.Models;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Services
{
    public class CommentService : ICommentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IAuditLogService _auditLogService;
        private readonly ICurrentUserService _currentUser;
        private readonly INotificationService _notificationService;

        public CommentService(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            IAuditLogService auditLogService,
            ICurrentUserService currentUser,
            INotificationService notificationService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _auditLogService = auditLogService;
            _currentUser = currentUser;
            _notificationService = notificationService;
        }

        // Note: no row-level visibility filter applied here — any authenticated user
        // can read comments for any ticket ID. Intentional; restrict here if needed.
        public async Task<IEnumerable<CommentResponseDto>> GetCommentsByTicketIdAsync(GetCommentsByTicketDto dto)
        {
            var comments = await _unitOfWork.Comments
                .Query()
                .Where(c => c.TicketId == dto.TicketId)
                .Include(c => c.AuthorUser)
                .ToListAsync();

            return _mapper.Map<IEnumerable<CommentResponseDto>>(comments);
        }

        public async Task<CommentResponseDto> AddCommentAsync(AddCommentDto dto)
        {
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(dto.TicketId);

            if (ticket == null)
                throw new NotFoundException("Ticket", dto.TicketId);

            if (ticket.Status == TicketStatus.Archived)
                throw new InvalidOperationException("Archived tickets are read-only.");

            var author = await _unitOfWork.Users.GetByIdAsync(_currentUser.UserId);

            var comment = new Comment
            {
                Content = dto.Content,
                TicketId = dto.TicketId,
                AuthorUserId = _currentUser.UserId,
                AuthorUser = author,
                CreatedAt = DateTime.UtcNow
            };

            ticket.LastUpdatedAt = DateTime.UtcNow;

            await _unitOfWork.Comments.AddAsync(comment);
            await _unitOfWork.Tickets.UpdateAsync(ticket);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogCommentAddedAsync(dto.TicketId, comment.Id, comment.Content, _currentUser.UserId);

            var recipients = new HashSet<int>();
            if (ticket.AssignedToAgentId.HasValue) recipients.Add(ticket.AssignedToAgentId.Value);
            if (ticket.RaisedForUserId.HasValue) recipients.Add(ticket.RaisedForUserId.Value);
            recipients.Add(ticket.CreatedByUserId);
            recipients.Remove(_currentUser.UserId);

            foreach (var recipientId in recipients)
            {
                var notifyUser = await _unitOfWork.Users.GetByIdAsync(recipientId);
                if (notifyUser != null)
                    await _notificationService.SendNewCommentAsync(notifyUser, ticket, comment);
            }

            return _mapper.Map<CommentResponseDto>(comment);
        }

        public async Task<bool> DeleteCommentAsync(CommentActionDto dto)
        {
            var comment = await _unitOfWork.Comments.GetByIdAsync(dto.Id);

            if (comment == null)
                return false;

            var ticket = await _unitOfWork.Tickets.GetByIdAsync(comment.TicketId);

           
            if (ticket?.Status == TicketStatus.Archived)
                throw new InvalidOperationException("Archived tickets are read-only.");

            if (_currentUser.UserRole != UserRole.Admin &&
                comment.AuthorUserId != _currentUser.UserId)
                throw new ForbiddenException("Not allowed");

            var ticketId = comment.TicketId;

            await _unitOfWork.Comments.DeleteAsync(comment);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogCommentDeletedAsync(ticketId, dto.Id, comment.Content, _currentUser.UserId);

            return true;
        }
    }
}
