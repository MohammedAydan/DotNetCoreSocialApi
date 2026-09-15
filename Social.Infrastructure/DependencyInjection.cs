using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Social.Core.Configuration;
using Social.Core.Entities;
using Social.Core.Interfaces;
using Social.Infrastructure.Data;
using Social.Infrastructure.Repositories;
using Social.Infrastructure.Token;
using System.Text;

namespace Social.Infrastructure
{
    public static class DependencyInjection
    {
        public static WebApplicationBuilder AddInfrastructureDI(this WebApplicationBuilder builder)
        {
            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            builder.Services.AddDbContext<ApplicationDbContext>(options =>
            {
                options.UseMySql(connectionString, new MySqlServerVersion(new Version(8, 0, 36)));
            });



            builder.Services.AddIdentity<User, IdentityRole>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
             .AddJwtBearer(options =>
             {
                 var jwtKey = builder.Configuration["Jwt:Key"];
                 if (string.IsNullOrWhiteSpace(jwtKey))
                     throw new Exception("JWT Key is missing in configuration");

                 options.TokenValidationParameters = new TokenValidationParameters
                 {
                     ValidateIssuer = true,
                     ValidateAudience = true,
                     ValidateLifetime = true,
                     ValidateIssuerSigningKey = true,
                     RequireExpirationTime = true,
                     ValidIssuer = builder.Configuration["Jwt:Issuer"],
                     ValidAudience = builder.Configuration["Jwt:Audience"],
                     IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
                     ClockSkew = TimeSpan.Zero
                 };

                 options.Events = new JwtBearerEvents
                 {
                     OnMessageReceived = context =>
                     {
                         if (string.IsNullOrEmpty(context.Token))
                         {
                             if (context.Request.Cookies.TryGetValue("admin_token", out var cookieToken) && !string.IsNullOrWhiteSpace(cookieToken))
                             {
                                 context.Token = cookieToken;
                             }
                         }
                         return Task.CompletedTask;
                     }
                 };
             });


            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<IPostRepository, PostRepository>();
            builder.Services.AddScoped<Social.Core.Interfaces.ITokenService, TokenService>();
            builder.Services.AddScoped<IFollowRepository, FollowRepository>();
            builder.Services.AddScoped<ICommentRepository, CommentRepository>();
            builder.Services.AddScoped<ILikeRepository, LikeRepository>();
            builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
            builder.Services.AddScoped<IBlockUserRepository, BlockUserRepository>();
            builder.Services.AddScoped<IAuditLogRepository, AuditLogRepository>();
            builder.Services.AddScoped<IAdminRepository, AdminRepository>();
            builder.Services.AddScoped<ISystemMetricsService, Social.Infrastructure.Diagnostics.SystemMetricsService>();
            builder.Services.AddScoped<IDatabaseSeeder, Social.Infrastructure.Services.DatabaseSeeder>();

            // email services
            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("EmailSettings"));
            builder.Services.AddTransient<IEmailRepository, EmailRepository>();

            // general config
            builder.Services.Configure<GeneralConfig>(builder.Configuration.GetSection("GeneralConfig"));

            return builder;
        }
    }
}
