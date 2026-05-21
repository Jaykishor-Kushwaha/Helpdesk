using AutoMapper;
using Helpdesk.DTOs;
using Helpdesk.Interfaces;
using Helpdesk.Models;
using Microsoft.EntityFrameworkCore;

namespace Helpdesk.Services
{
    public class DepartmentService : IDepartmentService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly IAuditLogService _auditLogService;
        private readonly ICurrentUserService _currentUser;
        private readonly INotificationService _notificationService;
        private readonly IEmailTemplateService _emailTemplateService;
        private readonly IConfiguration _configuration;

        public DepartmentService(
            IUnitOfWork unitOfWork, 
            IMapper mapper, 
            IAuditLogService auditLogService, 
            ICurrentUserService currentUser,
            INotificationService notificationService,
            IEmailTemplateService emailTemplateService,
            IConfiguration configuration)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _auditLogService = auditLogService;
            _currentUser = currentUser;
            _notificationService = notificationService;
            _emailTemplateService = emailTemplateService;
            _configuration = configuration;
        }

        public async Task<IEnumerable<DepartmentDto>> GetAllAsync()
        {
            var departments = await _unitOfWork.Departments.Query()
                .Include(d => d.DepartmentHead)
                .ToListAsync();
            return _mapper.Map<IEnumerable<DepartmentDto>>(departments);
        }

        public async Task<DepartmentDto?> GetByIdAsync(int id)
        {
            var dept = await _unitOfWork.Departments.Query()
                .Include(d => d.DepartmentHead)
                .FirstOrDefaultAsync(d => d.Id == id);
            return dept == null ? null : _mapper.Map<DepartmentDto>(dept);
        }

        public async Task<DepartmentDto> CreateAsync(CreateDepartmentDto dto)
        {
            var normalizedName = dto.Name.Trim();
            var exists = await _unitOfWork.Departments.Query()
                .AnyAsync(d => d.Name.ToLower() == normalizedName.ToLower());
            if (exists)
                throw new InvalidOperationException("Department name must be unique.");

            var dept = _mapper.Map<Department>(dto);
            dept.Name = normalizedName;
            await _unitOfWork.Departments.AddAsync(dept);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogAsync(
                Helpdesk.Enums.AuditEventType.DepartmentChanged,
                Helpdesk.Enums.AuditEntityType.Department,
                dept.Id,
                $"Department '{dept.Name}' created.",
                _currentUser.UserId);

            var adminEmail = _configuration["AdminSettings:Email"];
            if (!string.IsNullOrEmpty(adminEmail))
            {
                var t = _emailTemplateService.RenderSystemAlert(
                    $"Department Created: {dept.Name}",
                    $"A new department '{dept.Name}' has been created in the system.",
                    $"Department: {dept.Name}",
                    "Creation",
                    "Active",
                    "resolved",
                    "Manage Departments",
                    "http://localhost:4200/admin/departments");
                await _notificationService.QueueEmailAsync(adminEmail, $"Department Created: {dept.Name}", t.HtmlBody, t.PlainText);
            }

            return await GetByIdAsync(dept.Id) ?? throw new InvalidOperationException();
        }

        public async Task<DepartmentDto?> UpdateAsync(int id, UpdateDepartmentDto dto)
        {
            var dept = await _unitOfWork.Departments.GetByIdAsync(id);
            if (dept == null) return null;

            if (!string.IsNullOrWhiteSpace(dto.Name))
            {
                var newName = dto.Name.Trim();
                if (dept.IsGeneral && !newName.Equals("General", StringComparison.OrdinalIgnoreCase))
                    throw new InvalidOperationException("The 'General' department cannot be renamed.");

                var exists = await _unitOfWork.Departments.Query()
                    .AnyAsync(d => d.Id != id && d.Name.ToLower() == newName.ToLower());
                if (exists)
                    throw new InvalidOperationException("Department name must be unique.");

                dept.Name = newName;
            }
            if (dto.DepartmentHeadId.HasValue) dept.DepartmentHeadId = dto.DepartmentHeadId;
            if (dto.IsActive.HasValue) 
            {
                if (!dto.IsActive.Value && !dept.CanDeactivate)
                {
                    throw new InvalidOperationException("The 'General' department cannot be deactivated.");
                }

                if (!dto.IsActive.Value)
                {
                    var activeUsers = await _unitOfWork.Users.Query()
                        .AnyAsync(u => u.DepartmentId == id && u.IsActive);
                    if (activeUsers)
                        throw new InvalidOperationException("Cannot deactivate a department with active users.");
                }

                dept.IsActive = dto.IsActive.Value;
            }

            await _unitOfWork.Departments.UpdateAsync(dept);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogAsync(
                Helpdesk.Enums.AuditEventType.DepartmentChanged,
                Helpdesk.Enums.AuditEntityType.Department,
                dept.Id,
                $"Department '{dept.Name}' updated.",
                _currentUser.UserId);

            var adminEmail = _configuration["AdminSettings:Email"];
            if (!string.IsNullOrEmpty(adminEmail))
            {
                var statusText = dept.IsActive ? "Active" : "Inactive";
                var statusColor = dept.IsActive ? "resolved" : "escalated";
                
                var t = _emailTemplateService.RenderSystemAlert(
                    $"Department Updated: {dept.Name}",
                    $"The department '{dept.Name}' has been updated.",
                    $"Department: {dept.Name}",
                    "Update",
                    statusText,
                    statusColor,
                    "Manage Departments",
                    "http://localhost:4200/admin/departments");
                await _notificationService.QueueEmailAsync(adminEmail, $"Department Updated: {dept.Name}", t.HtmlBody, t.PlainText);
            }

            return await GetByIdAsync(dept.Id);
        }

        public async Task<bool> DeactivateAsync(int id)
        {
            var dept = await _unitOfWork.Departments.GetByIdAsync(id);
            if (dept == null) return false;

            if (!dept.CanDeactivate)
            {
                throw new InvalidOperationException("The 'General' department cannot be deactivated.");
            }

            var activeUsers = await _unitOfWork.Users.Query()
                .AnyAsync(u => u.DepartmentId == id && u.IsActive);
            if (activeUsers)
                throw new InvalidOperationException("Cannot deactivate a department with active users.");

            dept.IsActive = false;
            await _unitOfWork.Departments.UpdateAsync(dept);
            await _unitOfWork.SaveChangesAsync();

            await _auditLogService.LogAsync(
                Helpdesk.Enums.AuditEventType.DepartmentChanged,
                Helpdesk.Enums.AuditEntityType.Department,
                dept.Id,
                $"Department '{dept.Name}' deactivated.",
                _currentUser.UserId);

            var adminEmail = _configuration["AdminSettings:Email"];
            if (!string.IsNullOrEmpty(adminEmail))
            {
                var t = _emailTemplateService.RenderSystemAlert(
                    $"Department Deactivated: {dept.Name}",
                    $"The department '{dept.Name}' has been deactivated.",
                    $"Department: {dept.Name}",
                    "Deactivation",
                    "Inactive",
                    "escalated",
                    "Manage Departments",
                    "http://localhost:4200/admin/departments");
                await _notificationService.QueueEmailAsync(adminEmail, $"Department Deactivated: {dept.Name}", t.HtmlBody, t.PlainText);
            }

            return true;
        }

        public async Task<IEnumerable<DepartmentSummaryDto>> GetDepartmentSummaryAsync()
        {
            var thirtyDaysAgo = DateTime.UtcNow.AddDays(-30);

            var departments = await _unitOfWork.Departments.Query()
                .Where(d => d.IsActive)
                .Select(d => new DepartmentSummaryDto
                {
                    DepartmentId = d.Id,
                    DepartmentName = d.Name,
                    ActiveUserCount = _unitOfWork.Users.Query().Count(u => u.DepartmentId == d.Id && u.IsActive),
                    OpenTicketCount = _unitOfWork.Tickets.Query().Count(t => t.DepartmentId == d.Id && t.Status == Helpdesk.Enums.TicketStatus.Open),
                    RecentTicketCount = _unitOfWork.Tickets.Query().Count(t => t.DepartmentId == d.Id && t.CreatedAt >= thirtyDaysAgo)
                })
                .ToListAsync();

            return departments;
        }
    }
}
