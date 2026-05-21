import re

with open('Services/TicketService.cs', 'r', encoding='utf-8') as f:
    content = f.read()

content = re.sub(
    r'await _auditLogService\.LogAsync\(\s*AuditEventType\.TicketCreated,\s*AuditEntityType\.Ticket,\s*ticket\.Id,\s*\$\"Ticket \'\{ticket\.Title\}\' created\",\s*currentUserId,\s*ticket\.Id\);',
    r'await _auditLogService.LogTicketCreatedAsync(ticket.Id, f\"Ticket \'{ticket.Title}\' created\", currentUserId);',
    content
)

content = re.sub(
    r'var notifyUser = await _unitOfWork\.Users\.GetByIdAsync\(notifyUserId\);\s*if \(notifyUser != null\s*&& !string\.IsNullOrEmpty\(notifyUser\.Email\)\s*&& notifyUser\.NotificationPreferences\?\.EmailOnTicketCreated == true\)\s*\{\s*var category = ticket\.Category\?\.Name \?\? \"N/A\";[\s\S]*?await _notificationService\.QueueEmailAsync\([\s\S]*?\);\s*\}',
    r'var notifyUser = await _unitOfWork.Users.GetByIdAsync(notifyUserId);\n                if (notifyUser != null)\n                {\n                    await _notificationService.SendTicketCreatedAsync(notifyUser, ticket);\n                }',
    content
)

content = re.sub(
    r'await _auditLogService\.LogAsync\(\s*AuditEventType\.TicketStatusChanged,\s*AuditEntityType\.Ticket,\s*ticket\.Id,\s*\$\"Status changed from \{oldStatus\} to \{ticket\.Status\}\",\s*_currentUser\.UserId,\s*ticket\.Id\);',
    r'await _auditLogService.LogTicketStatusChangedAsync(ticket.Id, oldStatus.ToString(), ticket.Status.ToString(), _currentUser.UserId);',
    content
)

content = re.sub(
    r'var notifyUser = await _unitOfWork\.Users\.GetByIdAsync\(notifyUserId\);\s*if \(notifyUser != null && !string\.IsNullOrEmpty\(notifyUser\.Email\)\s*&& notifyUser\.NotificationPreferences\?\.EmailOnStatusChange == true\)\s*\{[\s\S]*?await _notificationService\.QueueEmailAsync\([\s\S]*?\);\s*\}',
    r'var notifyUser = await _unitOfWork.Users.GetByIdAsync(notifyUserId);\n                    if (notifyUser != null)\n                    {\n                        await _notificationService.SendStatusChangedAsync(notifyUser, ticket, oldStatus.ToString());\n                    }',
    content
)

content = re.sub(
    r'var newAgent = await _unitOfWork\.Users\.GetByIdAsync\(ticket\.AssignedToAgentId\.Value\);\s*if \(newAgent != null && !string\.IsNullOrEmpty\(newAgent\.Email\)\s*&& newAgent\.NotificationPreferences\?\.EmailOnAssignment == true\)\s*\{[\s\S]*?await _notificationService\.QueueEmailAsync\([\s\S]*?\);\s*\}',
    r'var newAgent = await _unitOfWork.Users.GetByIdAsync(ticket.AssignedToAgentId.Value);\n                if (newAgent != null)\n                {\n                    await _notificationService.SendAssignmentAsync(newAgent, ticket);\n                }',
    content
)

content = re.sub(
    r'await _auditLogService\.LogAsync\(\s*AuditEventType\.TicketArchived,\s*AuditEntityType\.Ticket,\s*ticket\.Id,\s*\"Ticket archived\",\s*_currentUser\.UserId,\s*ticket\.Id\);',
    r'await _auditLogService.LogTicketArchivedAsync(ticket.Id, _currentUser.UserId);',
    content
)

content = re.sub(
    r'await _auditLogService\.LogAsync\(\s*AuditEventType\.TicketEscalated,\s*AuditEntityType\.Ticket,\s*ticket\.Id,\s*\$\"Ticket escalated: \{dto\.Reason\}\",\s*_currentUser\.UserId,\s*ticket\.Id\);',
    r'await _auditLogService.LogTicketEscalatedAsync(ticket.Id, dto.Reason ?? \"\", _currentUser.UserId);',
    content
)

content = re.sub(
    r'var notifyUser = await _unitOfWork\.Users\.GetByIdAsync\(notifyUserId\);\s*if \(notifyUser != null && !string\.IsNullOrEmpty\(notifyUser\.Email\)\)\s*\{[\s\S]*?await _notificationService\.QueueEmailAsync\([\s\S]*?\);\s*\}',
    r'var notifyUser = await _unitOfWork.Users.GetByIdAsync(notifyUserId);\n                    if (notifyUser != null)\n                    {\n                        await _notificationService.SendEscalationAsync(notifyUser, ticket, dto.Reason ?? \"\");\n                    }',
    content
)

content = re.sub(
    r'await _auditLogService\.LogAsync\(\s*AuditEventType\.SlaDeadlineOverridden,\s*AuditEntityType\.Ticket,\s*ticket\.Id,\s*\$\"SLA overridden: \{dto\.Reason\}\",\s*_currentUser\.UserId,\s*ticket\.Id\);',
    r'await _auditLogService.LogSlaOverriddenAsync(ticket.Id, oldDeadline?.ToString() ?? \"None\", ticket.SLADeadline?.ToString() ?? \"None\", dto.Reason ?? \"\", _currentUser.UserId);',
    content
)


with open('Services/TicketService.cs', 'w', encoding='utf-8') as f:
    f.write(content)
