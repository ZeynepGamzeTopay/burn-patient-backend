using BurnAnalysisApp;
using BurnAnalysisApp.Data;
using BurnAnalysisApp.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.StaticFiles;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// JWT Ayarları
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var key = Encoding.UTF8.GetBytes(jwtSettings["Key"]!);

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = jwtSettings["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSettings["Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

// DbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IEmailService, EmailService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("https://burn-application-frontend.onrender.com")
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<ReminderBackgroundService>();
//builder.WebHost.UseUrls("http://0.0.0.0:10000");

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowReactApp");
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

var provider = new FileExtensionContentTypeProvider();
provider.Mappings[".mp4"] = "audio/mp4";

app.UseStaticFiles(new StaticFileOptions
{
    ContentTypeProvider = provider
});

app.MapControllers();

await RunDatabaseUpdateAsync(app); // 👈 Veritabanı işlemi

await app.RunAsync(); // 👈 await kullanıldı

// 👇 Veritabanı update fonksiyonu
static async Task RunDatabaseUpdateAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

     // Eğer migration uygulanmamışsa uygula:
    await context.Database.MigrateAsync(); // 👈 Migration'ları otomatik uygular

    var notifications = await context.Notifications
        .Where(n => n.ForumPostID == null)
        .ToListAsync();

    foreach (var notification in notifications)
    {
        var doctor = await context.Doctors
            .FirstOrDefaultAsync(d => d.DoctorID == notification.DoctorID);

        if (doctor != null)
        {
            var relatedPost = await context.ForumPosts
                .FirstOrDefaultAsync(fp => fp.DoctorName == doctor.Name);

            if (relatedPost != null)
            {
                notification.ForumPostID = relatedPost.ForumPostID;
            }
        }
    }

    await context.SaveChangesAsync();
}
