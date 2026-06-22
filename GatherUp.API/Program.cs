using System;
using System.IO;
using GatherUp.API;
using GatherUp.BL;
using GatherUp.Core;
using GatherUp.Core.DO;
using GatherUp.Infrastructure;
using GatherUp.Infrastructure.Data;

var builder = WebApplication.CreateBuilder(args);

// add services
string dataDir = Path.Combine(AppContext.BaseDirectory, "Data");
string mailLog = Path.Combine(AppContext.BaseDirectory, "mail_log.txt");

builder.Services.AddSingleton<IRepository<EventManager>>(sp => new XMLRepository<EventManager>(dataDir, "EventManagers"));
builder.Services.AddSingleton<IRepository<EventHost>>(sp => new XMLRepository<EventHost>(dataDir, "EventHosts"));
builder.Services.AddSingleton<IRepository<Participant>>(sp => new XMLRepository<Participant>(dataDir, "Participants"));
builder.Services.AddSingleton<IRepository<Event>>(sp => new XMLRepository<Event>(dataDir, "Events"));
builder.Services.AddSingleton<IRepository<Poll>>(sp => new XMLRepository<Poll>(dataDir, "Polls"));

// receipt repository
builder.Services.AddSingleton<IReceiptRepository>(_ => new ReceiptRepository(dataDir));

builder.Services.AddSingleton<IMailService>(_ => new FileMailService(mailLog));
builder.Services.AddSingleton<IEventNotifications, EventNotificationBus>();

builder.Services.AddScoped<ParticipantService>();
builder.Services.AddScoped<FinanceService>();
builder.Services.AddScoped<PollService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// serve SPA index at root
app.MapFallbackToFile("index.html");

app.Run();

// Expose Program class for WebApplicationFactory in integration tests
public partial class Program { }
