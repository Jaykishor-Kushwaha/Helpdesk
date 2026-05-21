using AutoMapper;
using Helpdesk.DTOs;
using Helpdesk.Exceptions;
using Helpdesk.Interfaces;
using Helpdesk.Models;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Services
{
    public class SurveyService : ISurveyService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ICurrentUserService _currentUser;

        public SurveyService(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUser)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _currentUser = currentUser;
        }

        public async Task<IEnumerable<SurveyResponseDto>> GetAllAsync(int? ticketId = null)
        {
            var query = _unitOfWork.Surveys.Query()
                .Include(s => s.SubmittedByUser)
                .AsQueryable();

            // Individual survey responses are visible only to Admin.
            // Standard users can only see their own submissions (to know they already submitted).
            // Agents cannot view individual responses — they only see aggregate CSAT on dashboards.
            if (_currentUser.Role == "Agent")
            {
                throw new ForbiddenException("Agents cannot view individual survey responses. Use the dashboard for aggregate CSAT scores.");
            }
            else if (_currentUser.Role != "Admin")
            {
                query = query.Where(s => s.SubmittedByUserId == _currentUser.UserId);
            }

            if (ticketId.HasValue)
            {
                query = query.Where(s => s.TicketId == ticketId.Value);
            }

            var surveys = await query.ToListAsync();
            return _mapper.Map<IEnumerable<SurveyResponseDto>>(surveys);
        }

        public async Task<SurveyResponseDto> CreateAsync(CreateSurveyResponseDto dto)
        {
            var ticket = await _unitOfWork.Tickets.GetByIdAsync(dto.TicketId);
            if (ticket == null) throw new NotFoundException("Ticket", dto.TicketId);

            if (ticket.RaisedForUserId != _currentUser.UserId && _currentUser.Role != "Admin")
            {
                throw new ForbiddenException("Only the user who raised the ticket can submit a survey.");
            }

            if (ticket.Status != Helpdesk.Enums.TicketStatus.Closed && ticket.Status != Helpdesk.Enums.TicketStatus.Resolved)
            {
                throw new InvalidOperationException("Surveys can only be submitted for closed or resolved tickets.");
            }

            if ((DateTime.UtcNow - ticket.LastUpdatedAt).TotalDays > 7)
            {
                throw new InvalidOperationException("Survey link has expired.");
            }

            // Note: OptOutSurveys only controls email delivery, not the ability to submit.
            // Users who opted out of survey emails can still submit via the in-app survey link.

            var existingSurvey = await _unitOfWork.Surveys.Query().AnyAsync(s => s.TicketId == dto.TicketId);
            if (existingSurvey)
            {
                throw new InvalidOperationException("A survey has already been submitted for this ticket.");
            }

            var survey = _mapper.Map<SurveyResponse>(dto);
            survey.SubmittedByUserId = _currentUser.UserId;
            survey.SubmittedAt = DateTime.UtcNow;

            await _unitOfWork.Surveys.AddAsync(survey);

            ticket.IsSurveySent = true;
            await _unitOfWork.Tickets.UpdateAsync(ticket);

            await _unitOfWork.SaveChangesAsync();

            var createdSurvey = await _unitOfWork.Surveys.Query()
                .Include(s => s.SubmittedByUser)
                .FirstOrDefaultAsync(s => s.Id == survey.Id);

            return _mapper.Map<SurveyResponseDto>(createdSurvey);
        }
    }
}
