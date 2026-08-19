using Microsoft.OpenApi;
using Serilog;
using SimpleTelegramBot.Components;
using SimpleTelegramBot.Endpoints;
using SimpleTelegramBot.Options;
using SimpleTelegramBot.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext());

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "SimpleTelegramBot API",
        Version = "v1",
        Description = "REST API per inviare messaggi Telegram e consultare la memoria delle richieste."
    });
});

builder.Services
    .AddOptions<TelegramOptions>()
    .Bind(builder.Configuration.GetSection(TelegramOptions.SectionName));

builder.Services
    .AddOptions<RequestMemoryOptions>()
    .Bind(builder.Configuration.GetSection(RequestMemoryOptions.SectionName))
    .Validate(options => options.MaxEntries > 0, $"{RequestMemoryOptions.SectionName}:MaxEntries must be greater than 0.")
    .ValidateOnStart();

builder.Services
    .AddOptions<ApiCorsOptions>()
    .Bind(builder.Configuration.GetSection(ApiCorsOptions.SectionName));

var corsOptions = builder.Configuration
    .GetSection(ApiCorsOptions.SectionName)
    .Get<ApiCorsOptions>() ?? new ApiCorsOptions();

if (corsOptions.AllowedOrigins.Length > 0)
{
    builder.Services.AddCors(options =>
    {
        options.AddPolicy(ApiCorsOptions.PolicyName, policy =>
            policy.WithOrigins(corsOptions.AllowedOrigins)
                .AllowAnyHeader()
                .AllowAnyMethod());
    });
}

builder.Services.AddHttpClient<ITelegramBotClient, TelegramBotClient>();
builder.Services.AddSingleton<IRequestMemoryStore, InMemoryRequestMemoryStore>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "SimpleTelegramBot API v1");
        options.RoutePrefix = "swagger";
    });
}

if (corsOptions.AllowedOrigins.Length > 0)
{
    app.UseCors(ApiCorsOptions.PolicyName);
}

app.UseAntiforgery();

app.MapTelegramApi();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
