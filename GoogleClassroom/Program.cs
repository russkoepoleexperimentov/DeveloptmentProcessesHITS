using Application.DTOs.Auth;
using Application.DTOs.Grading.PeerReview;
using Application.DTOs.Post;
using Application.Profiles;
using Application.Services.Abstractions;
using Application.Services.Implementations;
using Application.Services.Implementations.CaptainVoting;
using Application.Services.Interfaces;
using Application.Validators;
using Common.Middlewares;
using Common.Options;
using FluentValidation;
using GoogleClass.DTOs;
using GoogleClass.DTOs.Auth;
using GoogleClass.DTOs.Comment;
using GoogleClass.DTOs.User;
using GoogleClass.Models;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Web.Options;

namespace Web
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.Configure<JwtOptions>(
                builder.Configuration.GetSection("Jwt"));

            builder.Services.Configure<FileStorageOptions>(
                builder.Configuration.GetSection("FileStorage"));

            builder.Services.Configure<IdentityPasswordOptions>(
                builder.Configuration.GetSection("Identity:Password"));

            var jwtOptions = builder.Configuration
                .GetSection("Jwt")
                .Get<JwtOptions>()!;


            var identityOptions = builder.Configuration
                .GetSection("Identity:Password")
                .Get<IdentityPasswordOptions>()!;


            builder.Services.AddLogging(logging =>
                logging.AddConsole());


            builder.Services
                .AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions
                        .Converters
                        .Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                });


            var accessKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtOptions.Access.Secret));

            builder.Services
                .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    options.TokenValidationParameters =
                        new TokenValidationParameters
                        {
                            ValidateIssuer = false,
                            ValidateAudience = false,
                            IssuerSigningKey = accessKey,
                        };
                });

            builder.Services.AddAuthorization();


            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(config =>
            {
                config.AddSecurityDefinition("bearerAuth", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "JWT Authorization header using the Bearer scheme."
                });

                config.OperationFilter<SwaggerAuthorizeFilter>();
            });



            builder.Services
                .AddIdentity<User, IdentityRole<Guid>>(options =>
                {
                    options.Password.RequiredLength = identityOptions.RequiredLength;
                    options.Password.RequireDigit = identityOptions.RequireDigit;
                    options.Password.RequireUppercase = identityOptions.RequireUppercase;
                    options.Password.RequireLowercase = identityOptions.RequireLowercase;
                    options.Password.RequireNonAlphanumeric = identityOptions.RequireNonAlphanumeric;
                })
                .AddEntityFrameworkStores<GcDbContext>()
                .AddDefaultTokenProviders();


            builder.Services
                .AddScoped<ITeamManagerService, TeamManagerService>()
                .AddScoped<ICaptainStrategyFactory, CaptainStrategyFactory>()
                .AddScoped<FirstMemberStrategy>()
                .AddScoped<TeacherFixedStrategy>()
                .AddScoped<VotingAndLotteryStrategy>()
                .AddScoped<IPostService, PostService>()
                .AddScoped<IUserService, UserService>()
                .AddScoped<IAuthService, AuthService>()
                .AddScoped<ICourseService, CourseService>()
                .AddScoped<ICommentService, CommentService>()
                .AddScoped<ISolutionService, SolutionService>()
                .AddScoped<ITeamSolutionService, TeamSolutionService>()
                .AddScoped<IPeerReviewService, PeerReviewService>()
                .AddScoped<IValidator<SubmitPeerReviewDto>, SubmitPeerReviewDtoValidator>()
                .AddScoped<IValidator<CreateUpdatePostDto>, CreateUpdatePostValidator>()
                .AddScoped<IFileService, FileService>()
                .AddScoped<IValidator<UserRegisterDto>, UserRegistrationValidator>()
                .AddScoped<IValidator<UserUpdateDto>, UserUpdateValidator>()
                .AddScoped<IValidator<UserLoginDto>, UserLoginValidator>()
                .AddScoped<IValidator<UserChangePassword>, ChangePasswordValidator>()
                .AddScoped<IValidator<AddCommentRequestDto>, AddCommentValidator>()
                .AddScoped<IValidator<EditCommentRequestDto>, EditCommentValidator>()
                .AddScoped<IValidator<SubmitSolutionRequestDto>, SubmitSolutionRequestDtoValidator>()
                .AddScoped<IValidator<UpdateSolutionRequestDto>, UpdateSolutionRequestDtoValidator>()
                .AddScoped<IValidator<SubmitTeamSolutionRequestDto>, SubmitTeamSolutionValidator>()
                .AddScoped<IValidator<UpdateTeamSolutionRequestDto>, UpdateTeamSolutionValidator>()
                .AddScoped<IGradeDistributionService, GradeDistributionService>()
                .AddSingleton<IGradeCalculator, GradeCalculator>()
                .AddAutoMapper(typeof(UserMapProfile))
                .AddAutoMapper(typeof(PostMappingProfile));

            builder.Services.AddSingleton<IJwtService>(sp =>
            {
                var jwtOptions = sp.GetRequiredService<IOptions<JwtOptions>>().Value;

                return new JWTService(
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.Access.Secret)),
                    jwtOptions.Access.LifetimeMinutes,
                    new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(jwtOptions.Refresh.Secret)),
                    jwtOptions.Refresh.LifetimeDays
                );
            });


            builder.Services.AddDbContext<GcDbContext>(options =>
                options
                    .UseLazyLoadingProxies()
                    .UseNpgsql(
                        builder.Configuration.GetConnectionString("DefaultConnection"),
                        b => b.MigrationsAssembly("GoogleClassroom")
                    ));

            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowFrontend",
                    policy =>
                    {
                        policy.SetIsOriginAllowed(origin => true)
                              .AllowAnyHeader()
                              .AllowAnyMethod()
                              .AllowCredentials();
                    });
            });


            var app = builder.Build();



            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<GcDbContext>();

                if (context.Database.GetPendingMigrations().Any())
                    context.Database.Migrate();
            }

            app.UseSwagger();
            app.UseMiddleware<ExceptionCatchMiddleware>();
            app.UseSwaggerUI();

            app.UseCors("AllowFrontend");

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();

            Console.WriteLine("Classroom backend had started");
        }
    }
}