using AutoMapper;
using Helpdesk.Data;
using Helpdesk.Interfaces;
using Helpdesk.Mappings;
using Helpdesk.Middleware;
using Helpdesk.Models;
using Helpdesk.Repositories;
using Helpdesk.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;

if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")))
{
    Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
}

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity
builder.Services.AddIdentityCore<User>()
    .AddRoles<IdentityRole<int>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

// CORS Configuration Add here!
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngularApp",
        corsBuilder => corsBuilder.WithOrigins("http://localhost:4200", "https://localhost:4200", "http://127.0.0.1:4200")
                                  .AllowAnyMethod()
                                  .AllowAnyHeader()
                                  .AllowCredentials());
});

// JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = jwtSettings["Key"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateIssuerSigningKey = true,
        ValidateLifetime = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
        ClockSkew = TimeSpan.Zero
    };
});

// Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<ITicketService, TicketService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IDepartmentService, DepartmentService>();
builder.Services.AddScoped<ISystemSettingService, SystemSettingService>();
builder.Services.AddScoped<ISurveyService, SurveyService>();
builder.Services.AddScoped<ICommentService, CommentService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IAuditLogService, AuditLogService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<ISlaCalculationEngine, SlaCalculationEngine>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IEmailTemplateService, EmailTemplateService>();
builder.Services.AddScoped<IRecurringTemplateService, RecurringTemplateService>();
builder.Services.AddScoped<IKBArticleService, KBArticleService>();
builder.Services.AddScoped<IReportingService, ReportingService>();
builder.Services.AddSingleton<IReportQueue, ReportQueue>();

// Register Hosted Services
builder.Services.AddHostedService<Helpdesk.Workers.EmailBackgroundWorker>();
builder.Services.AddHostedService<Helpdesk.Workers.SlaMonitorWorker>();
builder.Services.AddHostedService<Helpdesk.Workers.RecurringTicketWorker>();
builder.Services.AddHostedService<Helpdesk.Workers.ReportBackgroundWorker>();
builder.Services.AddHostedService<Helpdesk.Workers.TicketArchivalWorker>();

// UnitOfWork
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

// CurrentUser
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

// AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Controllers
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        options.JsonSerializerOptions.DictionaryKeyPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });
builder.Services.AddEndpointsApiExplorer();

// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Helpdesk API",
        Version = "v1"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Enter your JWT token"
    });

    // CA1825 + CA1861: Use Array.Empty<string>() instead of new string[] {}
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// Duplicate QuestPDF license line removed from here

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    var userManager = services.GetRequiredService<UserManager<User>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();

    await DbInitializer.SeedAsync(
        context,
        userManager,
        roleManager,
        builder.Configuration["AdminSettings:Email"]!,
        builder.Configuration["AdminSettings:Password"]!
    );
}

// Middleware pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

// Use CORS before Authentication to allow the preflight requests!
app.UseCors("AllowAngularApp");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();