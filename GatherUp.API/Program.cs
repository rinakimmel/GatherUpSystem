using System;
using System.IO;
using System.Linq;
using GatherUp.API;
using GatherUp.BL;
using GatherUp.Core;
using GatherUp.Core.DO;
using GatherUp.Infrastructure;
using GatherUp.Infrastructure.Data;
using GatherUp.API.Services;
using Microsoft.AspNetCore.Http;

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

// credential service for simple auth demo
builder.Services.AddSingleton<CredentialService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<GlobalExceptionMiddleware>();
app.UseStaticFiles();
app.UseRouting();

// helper to create cookie options based on environment
CookieOptions CreateCookieOptions()
{
    if (app.Environment.IsDevelopment())
    {
        return new CookieOptions { HttpOnly = true, Secure = false, SameSite = SameSiteMode.Lax, Path = "/" };
    }
    return new CookieOptions { HttpOnly = true, Secure = true, SameSite = SameSiteMode.None, Path = "/" };
}

// enhanced auth middleware: read cookie, resolve role from CredentialService, attach to Items
app.Use(async (context, next) =>
{
    var credService = context.RequestServices.GetRequiredService<CredentialService>();
    var credCookie = context.Request.Cookies["gatherup_user"];
    if (!string.IsNullOrEmpty(credCookie))
    {
        context.Items["UserEmail"] = credCookie;
        var rec = credService.GetByEmail(credCookie);
        if (rec != null)
        {
            context.Items["UserRole"] = rec.Role;
            context.Items["UserLinkedId"] = rec.LinkedId;
        }
    }
    await next();
});

// API protection middleware: require auth for non-GET API calls (except /auth/*), and manager role for creating polls and for registering participants when accounts exist
app.Use(async (context, next) =>
{
    var path = context.Request.Path.Value ?? string.Empty;
    if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase))
    {
        // allow GET anywhere
        if (!string.Equals(context.Request.Method, "GET", StringComparison.OrdinalIgnoreCase))
        {
            // allow auth endpoints
            if (!path.StartsWith("/auth", StringComparison.OrdinalIgnoreCase) && !path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase))
            {
                // In Development allow unauthenticated access to finance endpoints for integration tests
                if (app.Environment.IsDevelopment() && path.StartsWith("/api/finance", StringComparison.OrdinalIgnoreCase))
                {
                    await next();
                    return;
                }

                // require login
                var email = context.Items["UserEmail"] as string;
                if (string.IsNullOrEmpty(email))
                {
                    context.Response.StatusCode = 401;
                    await context.Response.WriteAsJsonAsync(new { error = "Authentication required" });
                    return;
                }

                // manager-only checks
                // creating a poll: POST /api/polls/{eventId}
                if (string.Equals(context.Request.Method, "POST", StringComparison.OrdinalIgnoreCase) && path.StartsWith("/api/polls/", StringComparison.OrdinalIgnoreCase) && !path.Contains("/vote"))
                {
                    var credService = context.RequestServices.GetRequiredService<CredentialService>();
                    var rec = credService.GetByEmail(email);
                    if (rec == null || rec.Role != UserRole.Manager)
                    {
                        context.Response.StatusCode = 403;
                        await context.Response.WriteAsJsonAsync(new { error = "Manager role required" });
                        return;
                    }
                }

                // uploading receipts & other finance actions might be manager-only in your rules; keeping generic requiring login is ok
            }
        }
    }

    await next();
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// minimal endpoints for registration and login (demo)
app.MapPost("/auth/register/participant", async (CredentialService creds, IRepository<Event> evRepo, IRepository<Participant> pr, HttpRequest req, HttpContext ctx, HttpResponse res) =>
{
    var form = await req.ReadFormAsync();
    var name = form["name"].ToString();
    var email = form["email"].ToString();
    var password = form["password"].ToString();
    var eventIdStr = form["eventId"].ToString();
    int eventId = int.TryParse(eventIdStr, out var e) ? e : 0;

    // If no accounts exist, make the first registered account a Manager and log them in
    if (!creds.HasAnyAccounts())
    {
        var created = await creds.RegisterManagerAsync(name, email, password);
        if (!created) return Results.Conflict(new { error = "Email already registered." });
        // set login cookie for new manager
        res.Cookies.Append("gatherup_user", email, CreateCookieOptions());
        app.Logger.LogInformation("Created first manager account: {email}", email);
        return Results.Ok(new { email = email, role = UserRole.Manager.ToString() });
    }

    // if accounts exist, only manager can register participants
    if (creds.HasAnyAccounts())
    {
        var callerEmail = ctx.Items["UserEmail"] as string;
        if (string.IsNullOrEmpty(callerEmail)) return Results.StatusCode(401);
        var caller = creds.GetByEmail(callerEmail);
        if (caller == null || caller.Role != UserRole.Manager) return Results.StatusCode(403);
    }

    var ok = await creds.RegisterParticipantAsync(name, email, password, eventId);
    if (!ok) return Results.Conflict(new { error = "Email already registered." });

    app.Logger.LogInformation("Registered participant: {email} (event {eventId})", email, eventId);
    return Results.Ok(new { email = email });
});

app.MapPost("/auth/login", async (CredentialService creds, HttpResponse res, HttpRequest req) =>
{
    var form = await req.ReadFormAsync();
    var email = form["email"].ToString();
    var password = form["password"].ToString();

    var user = await creds.ValidateAsync(email, password);
    if (user == null)
    {
        app.Logger.LogInformation("Failed login attempt for {email}", email);
        return Results.Unauthorized();
    }

    // set cookie (demo only) based on environment so dev (http) can receive the cookie
    res.Cookies.Append("gatherup_user", email, CreateCookieOptions());
    app.Logger.LogInformation("Successful login for {email} as {role}", email, user.Role);
    return Results.Ok(new { email = email, role = user.Role.ToString() });
});

// development-only debug endpoint to list stored credentials
if (app.Environment.IsDevelopment())
{
    app.MapGet("/auth/debug", (CredentialService creds) =>
    {
        var list = creds.GetAllForDebug();
        return Results.Ok(list.Select(r => new { r.Email, Role = r.Role.ToString(), r.LinkedId }));
    });
}

// new: whoami
app.MapGet("/auth/me", (HttpContext ctx) =>
{
    var email = ctx.Items["UserEmail"] as string;
    if (string.IsNullOrEmpty(email)) return Results.Unauthorized();
    var credService = ctx.RequestServices.GetRequiredService<CredentialService>();
    var rec = credService.GetByEmail(email);
    if (rec == null) return Results.Unauthorized();
    return Results.Ok(new { email = rec.Email, role = rec.Role.ToString(), linkedId = rec.LinkedId });
});

// new: logout
app.MapPost("/auth/logout", (HttpResponse res, HttpContext ctx) =>
{
    // delete cookie
    res.Cookies.Delete("gatherup_user", CreateCookieOptions());
    ctx.Items.Remove("UserEmail");
    ctx.Items.Remove("UserRole");
    ctx.Items.Remove("UserLinkedId");
    app.Logger.LogInformation("User logged out");
    return Results.Ok(new { loggedOut = true });
});

// serve SPA index at root
app.MapFallbackToFile("index.html");

// Seed demo data so the UI default IDs (event id 1 etc.) work when the Data folder is empty
using (var scope = app.Services.CreateScope())
{
    var evRepo = scope.ServiceProvider.GetRequiredService<IRepository<Event>>();
    var partRepo = scope.ServiceProvider.GetRequiredService<IRepository<Participant>>();
    var credService = scope.ServiceProvider.GetRequiredService<CredentialService>();

    var existing = evRepo.GetAllAsync().GetAwaiter().GetResult();
    if (!existing.Any())
    {
        var p = new Participant(0, "Demo Participant", "demo@example.com");
        partRepo.AddAsync(p).GetAwaiter().GetResult();

        var ev = new Event(0, "Demo Event", "Auto-seeded event for development");
        ev.ParticipantIds.Add(p.Id);
        // add a demo vendor so upload endpoint doesn't 404 for missing vendor
        ev.Vendors.Add(new VendorAllocation("TestVendor") { AmountOwed = 0m });
        evRepo.AddAsync(ev).GetAwaiter().GetResult();
    }

    // ensure at least one manager account exists for development so login works
    if (!credService.HasAnyAccounts())
    {
        // create default manager
        var created = credService.RegisterManagerAsync("Admin", "admin@example.com", "admin").GetAwaiter().GetResult();
        if (created)
        {
            app.Logger.LogInformation("Created default manager account: admin@example.com (password 'admin')");
        }
    }
}

app.Run();

// Expose Program class for WebApplicationFactory in integration tests
public partial class Program { }
