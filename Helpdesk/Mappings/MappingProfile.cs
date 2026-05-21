using AutoMapper;
using Helpdesk.DTOs;
using Helpdesk.Models;
namespace Helpdesk.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Ticket

            // ✅ CREATE MAPPING (for creating tickets)
            CreateMap<CreateTicketDto, Ticket>()
       .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
       .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
       .ForMember(dest => dest.Status, opt => opt.Ignore())
       .ForMember(dest => dest.Id, opt => opt.Ignore());

            // ✅ UPDATE MAPPING - Fixed to exclude invalid values
            CreateMap<UpdateTicketDto, Ticket>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByUserId, opt => opt.Ignore())
                .ForMember(dest => dest.LastUpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.RaisedForUser, opt => opt.Ignore())
                .ForMember(dest => dest.CreatedByUser, opt => opt.Ignore())
                .ForMember(dest => dest.AssignedToAgent, opt => opt.Ignore())
                .ForMember(dest => dest.Category, opt => opt.Ignore())
                .ForMember(dest => dest.RelatedTicket, opt => opt.Ignore())
                .ForMember(dest => dest.Title, opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.Title)))
                .ForMember(dest => dest.Description, opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.Description)))
                .ForMember(dest => dest.Status, opt => opt.Condition(src => src.Status.HasValue))
                .ForMember(dest => dest.Priority, opt => opt.Condition(src => src.Priority.HasValue))
                .ForMember(dest => dest.CategoryId, opt => opt.Condition(src => src.CategoryId.HasValue && src.CategoryId.Value > 0))
                .ForMember(dest => dest.DepartmentId, opt => opt.Condition(src => src.DepartmentId.HasValue && src.DepartmentId.Value > 0))
                .ForMember(dest => dest.AssignedToAgentId, opt => opt.Condition(src => src.AssignedToAgentId.HasValue && src.AssignedToAgentId.Value > 0))
                .ForMember(dest => dest.AffectedAsset, opt => opt.Condition(src => !string.IsNullOrWhiteSpace(src.AffectedAsset)))
                .ForMember(dest => dest.RelatedTicketId, opt => opt.Condition(src => src.RelatedTicketId.HasValue && src.RelatedTicketId.Value > 0));

            // ✅ RESPONSE MAPPING
            CreateMap<Ticket, TicketResponseDto>()
                .ForMember(dest => dest.CreatedByUserName,
                    opt => opt.MapFrom(src =>
                        src.CreatedByUser != null
                            ? src.CreatedByUser.FirstName + " " + src.CreatedByUser.LastName
                            : string.Empty))
                .ForMember(dest => dest.RaisedForUserName,
                    opt => opt.MapFrom(src =>
                        src.RaisedForUser != null
                            ? src.RaisedForUser.FirstName + " " + src.RaisedForUser.LastName
                            : null))
                .ForMember(dest => dest.AssignedToAgentName,
                    opt => opt.MapFrom(src =>
                        src.AssignedToAgent != null
                            ? src.AssignedToAgent.FirstName + " " + src.AssignedToAgent.LastName
                            : null))
                .ForMember(dest => dest.CategoryName,
                    opt => opt.MapFrom(src =>
                        src.Category != null ? src.Category.Name : string.Empty))
                .ForMember(dest => dest.DepartmentName,
                    opt => opt.MapFrom(src =>
                        src.Department != null ? src.Department.Name : string.Empty))
                .ForMember(dest => dest.Status,
                    opt => opt.MapFrom(src => src.Status.ToString()))
                .ForMember(dest => dest.Priority,
                    opt => opt.MapFrom(src => src.Priority.ToString()));

            CreateMap<Comment, CommentResponseDto>()
                .ForMember(dest => dest.AuthorName,
                    opt => opt.MapFrom(src =>
                        src.AuthorUser != null
                            ? src.AuthorUser.FirstName + " " + src.AuthorUser.LastName
                            : string.Empty));

            CreateMap<CreateCommentDto, Comment>();

            // Category
            CreateMap<Category, CategoryResponseDto>();
            CreateMap<CreateCategoryDto, Category>();
            // User

            // User → UserResponseDto (only validate destination has all members mapped)
            CreateMap<NotificationPreferences, NotificationPreferencesDto>();
            CreateMap<User, UserResponseDto>(MemberList.Destination)
                .ForMember(dest => dest.Role, opt => opt.Ignore())
                .ForMember(dest => dest.DepartmentName,
                    opt => opt.MapFrom(src => src.Department != null ? src.Department.Name : null))
                .ForMember(dest => dest.NotificationPreferences, opt => opt.MapFrom(src => src.NotificationPreferences));

            // CreateUserDto → User (only validate source members are used)  
            CreateMap<CreateUserDto, User>(MemberList.Source)
                .ForMember(dest => dest.UserName, opt => opt.MapFrom(src => src.Email));

            // UpdateUserDto → User (only validate source members are used)
            CreateMap<UpdateUserDto, User>(MemberList.Source)
                .ForMember(dest => dest.UserName, opt => opt.Ignore())
                .ForMember(dest => dest.Email, opt => opt.Ignore())
                .ForMember(dest => dest.PasswordHash, opt => opt.Ignore());


            // AuditLog
            CreateMap<AuditLog, AuditLogResponseDto>()
     .ForMember(dest => dest.EventType,
         opt => opt.MapFrom(src => src.EventType.ToString()))
     .ForMember(dest => dest.EntityType,
         opt => opt.MapFrom(src => src.EntityType.ToString()))
     .ForMember(dest => dest.PerformedByUserName,
         opt => opt.MapFrom(src =>
             src.PerformedByUser.FirstName + " " + src.PerformedByUser.LastName))
     .ForMember(dest => dest.Details,
         opt => opt.MapFrom(src => src.AuditLogDetails));

            CreateMap<AuditLogDetail, AuditLogDetailResponseDto>();

            // ================== V2.0 MAPPINGS ================== //

            // Department
            CreateMap<Department, DepartmentDto>()
                .ForMember(dest => dest.DepartmentHeadName,
                    opt => opt.MapFrom(src => src.DepartmentHead != null ? src.DepartmentHead.FirstName + " " + src.DepartmentHead.LastName : null));
            CreateMap<CreateDepartmentDto, Department>();
            CreateMap<UpdateDepartmentDto, Department>()
                .ForMember(dest => dest.Name, opt => opt.Condition(src => src.Name != null))
                .ForMember(dest => dest.DepartmentHeadId, opt => opt.Condition(src => src.DepartmentHeadId.HasValue))
                .ForMember(dest => dest.IsActive, opt => opt.Condition(src => src.IsActive.HasValue));

            // KBArticle
            CreateMap<KBArticle, KBArticleDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty))
                .ForMember(dest => dest.AuthorName, opt => opt.MapFrom(src => src.Author != null ? src.Author.FirstName + " " + src.Author.LastName : string.Empty))
                .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()));
            CreateMap<CreateKBArticleDto, KBArticle>();
            CreateMap<UpdateKBArticleDto, KBArticle>()
                .ForMember(dest => dest.Title, opt => opt.Condition(src => src.Title != null))
                .ForMember(dest => dest.Tags, opt => opt.Condition(src => src.Tags != null))
                .ForMember(dest => dest.Content, opt => opt.Condition(src => src.Content != null))
                .ForMember(dest => dest.CategoryId, opt => opt.Condition(src => src.CategoryId.HasValue))
                .ForMember(dest => dest.Status, opt => opt.Condition(src => src.Status.HasValue));

            // KBArticleVersion
            CreateMap<KBArticleVersion, KBArticleVersionDto>()
                .ForMember(dest => dest.CreatedByUserName, opt => opt.MapFrom(src => src.CreatedByUser != null ? src.CreatedByUser.FirstName + " " + src.CreatedByUser.LastName : string.Empty));

            // SurveyResponse
            CreateMap<SurveyResponse, SurveyResponseDto>()
                .ForMember(dest => dest.SubmittedByUserName, opt => opt.MapFrom(src => src.SubmittedByUser != null ? src.SubmittedByUser.FirstName + " " + src.SubmittedByUser.LastName : null));
            CreateMap<CreateSurveyResponseDto, SurveyResponse>();

            // RecurringTemplate
            CreateMap<RecurringTemplate, RecurringTemplateDto>()
                .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category != null ? src.Category.Name : string.Empty))
                .ForMember(dest => dest.AssignToAgentName, opt => opt.MapFrom(src => src.AssignToAgent != null ? src.AssignToAgent.FirstName + " " + src.AssignToAgent.LastName : null))
                .ForMember(dest => dest.RaiseOnBehalfOfName, opt => opt.MapFrom(src => src.RaiseOnBehalfOf != null ? src.RaiseOnBehalfOf.FirstName + " " + src.RaiseOnBehalfOf.LastName : null))
                .ForMember(dest => dest.Priority, opt => opt.MapFrom(src => src.Priority.ToString()));
            CreateMap<CreateRecurringTemplateDto, RecurringTemplate>();
            CreateMap<UpdateRecurringTemplateDto, RecurringTemplate>()
                .ForMember(dest => dest.Name, opt => opt.Condition(src => src.Name != null))
                .ForMember(dest => dest.TicketTitle, opt => opt.Condition(src => src.TicketTitle != null))
                .ForMember(dest => dest.Description, opt => opt.Condition(src => src.Description != null))
                .ForMember(dest => dest.CategoryId, opt => opt.Condition(src => src.CategoryId.HasValue))
                .ForMember(dest => dest.Priority, opt => opt.Condition(src => src.Priority.HasValue))
                .ForMember(dest => dest.AssignToAgentId, opt => opt.Condition(src => src.AssignToAgentId.HasValue))
                .ForMember(dest => dest.RaiseOnBehalfOfId, opt => opt.Condition(src => src.RaiseOnBehalfOfId.HasValue))
                .ForMember(dest => dest.CronExpression, opt => opt.Condition(src => src.CronExpression != null))
                .ForMember(dest => dest.StartDate, opt => opt.Condition(src => src.StartDate.HasValue))
                .ForMember(dest => dest.EndDate, opt => opt.Condition(src => src.EndDate != null))
                .ForMember(dest => dest.MaxOccurrences, opt => opt.Condition(src => src.MaxOccurrences.HasValue))
                .ForMember(dest => dest.IsActive, opt => opt.Condition(src => src.IsActive.HasValue));

            // SystemSetting
            CreateMap<SystemSetting, SystemSettingDto>();
            CreateMap<UpdateSystemSettingDto, SystemSetting>();
        }
    }
}
