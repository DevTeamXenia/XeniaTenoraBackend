using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Security.Claims;
using System.Text;
using XeniaRentalBackend.Models;
using XeniaRentalBackend.Repositories.AccountGroups;
using XeniaRentalBackend.Repositories.Auth;
using XeniaRentalBackend.Repositories.BedSpace;
using XeniaRentalBackend.Repositories.BedSpacePlan;
using XeniaRentalBackend.Repositories.Category;
using XeniaRentalBackend.Repositories.Charges;
using XeniaRentalBackend.Repositories.Company;
using XeniaRentalBackend.Repositories.Dashboard;
using XeniaRentalBackend.Repositories.Documents;
using XeniaRentalBackend.Repositories.EmployeeMaster;
using XeniaRentalBackend.Repositories.Ledger;
using XeniaRentalBackend.Repositories.ManageMaintenance;
using XeniaRentalBackend.Repositories.MessDetails;
using XeniaRentalBackend.Repositories.MessTypes;
using XeniaRentalBackend.Repositories.Module;
using XeniaRentalBackend.Repositories.Properties;
using XeniaRentalBackend.Repositories.Register;
using XeniaRentalBackend.Repositories.Report;
using XeniaRentalBackend.Repositories.Subscription;
using XeniaRentalBackend.Repositories.Tenant;
using XeniaRentalBackend.Repositories.TenantAssignment;
using XeniaRentalBackend.Repositories.Unit;
using XeniaRentalBackend.Repositories.Units;
using XeniaRentalBackend.Repositories.UserRole;
using XeniaRentalBackend.Repositories.Voucher;
using XeniaRentalBackend.Service.Common;
using XeniaRentalBackend.Service.Notification;
using XeniaRentalBackend.Service.Payment;
using XeniaTenoraBackend.Hubs;
using XeniaTenoraBackend.Repositories.Area;
using XeniaTenoraBackend.Service.Socket;

var builder = WebApplication.CreateBuilder(args);

#region Controllers + JSON

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;

        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });

#endregion

#region Swagger

builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc(
        "v1",
        new OpenApiInfo
        {
            Title = "Xenia Rental API",
            Version = "v1"
        });

    c.AddSecurityDefinition(
        "Bearer",
        new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter JWT token"
        });

    c.AddSecurityRequirement(
        new OpenApiSecurityRequirement
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

#endregion

#region CORS FIX

builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowFrontend",
        policy =>
        {
            policy
                .SetIsOriginAllowed(_ => true)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        });
});

#endregion

#region SignalR

builder.Services.AddSignalR()
    .AddJsonProtocol(options =>
    {
        options.PayloadSerializerOptions.ReferenceHandler =
            System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
    });

#endregion

#region WebSockets

builder.Services.AddWebSockets(options => { });

#endregion

#region Database

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

#endregion

#region Repositories

builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAccountGroupRepository, AccountGroupRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IBedSpaceRepository, BedSpaceRepository>();
builder.Services.AddScoped<IBedSpacePlanRepository, BedSpacePlanRepository>();
builder.Services.AddScoped<IChargesRepository, ChargesRepository>();
builder.Services.AddScoped<ICompanyRepository, CompanyRepository>();
builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
builder.Services.AddScoped<IAccountLedgerRepository, AccountLedgerRepository>();
builder.Services.AddScoped<IMessAttendancesRepository, MessAttendancesRepository>();
builder.Services.AddScoped<IMessTypes, MessTypes>();
//builder.Services.AddScoped<IServiceRepository, ServiceRepository>();
builder.Services.AddScoped<ITenantRepository, TenantRepository>();
builder.Services.AddScoped<ITenantAssignmentRepository, TenantAssignmentRepository>();
builder.Services.AddScoped<IUnitRepository, UnitRepository>();
builder.Services.AddScoped<IVoucherRepository, VoucherRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IDashboardRepsitory, DashboardRepository>();
builder.Services.AddScoped<IPropertiesRepository, PropertiesRepository>();
builder.Services.AddScoped<ICompanyRegistrationRepository, CompanyRegistrationRepository>();
builder.Services.AddScoped<IReportRepository, ReportRepository>();
builder.Services.AddScoped<IEmployeeRepository, EmployeeRepository>();
builder.Services.AddScoped<IMaintenanceRepository, MaintenanceRepository>();
builder.Services.AddScoped<IModuleRepository, ModuleRepository>();
builder.Services.AddScoped<IAreaRepository, AreaRepository>();
builder.Services.AddScoped<ISubscriptionRepository, SubscriptionRepository>();

builder.Services.AddHttpClient<IPaymentService, PaymentService>();

#endregion

#region Services

builder.Services.AddScoped<INotificationService, OTPService>();
builder.Services.AddScoped<ITenoraUpdateService, TenoraUpdateService>();
builder.Services.AddScoped<JwtHelperService>();

builder.Services.AddHttpContextAccessor();

builder.Services.Configure<FtpSettings>(
    builder.Configuration.GetSection("FtpSettings"));

#endregion

#region Authentication

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters =
            new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,

                ValidIssuer =
                    builder.Configuration["JwtSettings:Issuer"],

                ValidAudience =
                    builder.Configuration["JwtSettings:Audience"],

                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(
                            builder.Configuration["JwtSettings:Key"]!
                        )
                    ),

                RoleClaimType = ClaimTypes.Role
            };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken =
                    context.Request.Query["access_token"];

                var path =
                    context.HttpContext.Request.Path;

                if (!string.IsNullOrEmpty(accessToken) &&
                    path.StartsWithSegments("/tenorahub"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };
    });

#endregion

#region Authorization

builder.Services.AddAuthorization();

#endregion

var app = builder.Build();

#region Middleware

app.UseSwagger();

app.UseSwaggerUI();

app.UseDefaultFiles();

app.UseStaticFiles();

app.UseRouting();


app.UseCors("AllowFrontend");


app.UseAuthentication();

app.UseAuthorization();

app.UseWebSockets();

#endregion

#region Endpoints

app.MapHub<TenoraHub>("/tenorahub");

app.MapControllers();

#endregion

app.Run();