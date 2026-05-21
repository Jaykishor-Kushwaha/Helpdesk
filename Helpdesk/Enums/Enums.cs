namespace Helpdesk.Enums;



  public enum UserRole
{
    Guest = 0,
    User = 1,
    Agent = 2,
    Admin = 3,
    DepartmentHead = 4
}


public enum TicketStatus
{
    Open = 1,
    InProgress = 2,
    OnHold = 3,
    Resolved = 4,
    Closed = 5,
    Reopened = 6,
    Archived = 7
}

public enum TicketPriority
{
    Low = 1,
    Medium = 2,
    High = 3,
    Critical = 4
}

public enum AuditEventType
{
    TicketCreated = 1,
    TicketUpdated = 2,
    TicketDeleted = 3,
    TicketStatusChanged = 4,
    TicketAssigned = 5,

    UserAccountChanged = 6,
    UserCreated = 7,
    UserDeleted = 8,

    CommentAdded = 9,
    CommentDeleted = 10,

    TicketEscalated = 11,
    SlaDeadlineOverridden = 12,
    KBArticleCreated = 13,
    KBArticleUpdated = 14,
    KBArticlePublished = 15,
    SystemSettingChanged = 16,
    DepartmentChanged = 17,
    ReportExported = 18,
    SatisfactionSurveySubmitted = 19,
    TicketArchived = 20,
    UserLoginSucceeded = 21,
    UserLoginFailed = 22,
    UserLoggedOut = 23,
    KBArticleDeleted = 24,
    KBArticleRejected = 25,
    RecurringTemplateChanged = 26,
    RecurringTemplateRun = 27
}

public enum AuditEntityType
{
    Ticket = 1,
    User = 2,
    Comment = 3,
    
    KBArticle = 4,
    Department = 5,
    SystemSetting = 6,
    SurveyResponse = 7
}

public enum KBArticleStatus 
{ 
    Draft = 0,
    Published = 1,
    Archived = 2,
    PendingReview = 3,
    Rejected = 4
}

public enum NotificationStatus 
{ 
    Pending, 
    Sent, 
    Failed 
}

public enum ReportType
{
    TicketVolume = 1,
    AgentPerformance = 2,
    SlaCompliance = 3,
    Ageing = 4,
    Category = 5,
    DepartmentLoad = 6,
    Escalation = 7,
    UserSatisfaction = 8,
    KbUsage = 9
}
