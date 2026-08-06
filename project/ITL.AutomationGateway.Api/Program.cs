using ITL.AutomationGateway.Api.Abstractions;
using ITL.AutomationGateway.Api.Contracts;
using ITL.AutomationGateway.Api.Domain;
using ITL.AutomationGateway.Api.Infrastructure;
using ITL.AutomationGateway.Api.Services;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((ctx, config) =>
{
	config.ReadFrom.Configuration(ctx.Configuration)
		.Enrich.FromLogContext();
});

builder.Services.Configure<GatewayOptions>(builder.Configuration.GetSection(GatewayOptions.SectionName));
builder.Services.Configure<LegacyTcpOptions>(builder.Configuration.GetSection(LegacyTcpOptions.SectionName));
builder.Services.Configure<StorageOptions>(builder.Configuration.GetSection(StorageOptions.SectionName));
builder.Services.Configure<WebhookOptions>(builder.Configuration.GetSection(WebhookOptions.SectionName));

builder.Services.AddSingleton<IJobRepository, SqliteJobRepository>();
builder.Services.AddSingleton<IWebhookSubscriptionRepository, SqliteWebhookSubscriptionRepository>();
builder.Services.AddSingleton<ILegacyAutomationAdapter, LegacyTcpAutomationAdapter>();
builder.Services.AddSingleton<IEventDispatcher, WebhookEventDispatcher>();
builder.Services.AddSingleton<IJobOrchestrator, JobOrchestratorService>();
builder.Services.AddHttpClient("webhook");
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
	options.SwaggerDoc("v1", new OpenApiInfo
	{
		Title = "ITL Automation Gateway API",
		Version = "v1",
		Description = "Interleaver automation gateway API for third-party integration.",
	});
});
builder.Services.AddHostedService(sp => (SqliteJobRepository)sp.GetRequiredService<IJobRepository>());
builder.Services.AddHostedService(sp => (LegacyTcpAutomationAdapter)sp.GetRequiredService<ILegacyAutomationAdapter>());
builder.Services.AddHostedService(sp => (JobOrchestratorService)sp.GetRequiredService<IJobOrchestrator>());

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(options =>
{
	options.DocumentTitle = "ITL Automation Gateway API Docs";
	options.SwaggerEndpoint("/swagger/v1/swagger.json", "ITL Automation Gateway API v1");
	options.RoutePrefix = "docs";
});

app.MapGet("/", () => Results.Ok(new { service = "ITL Automation Gateway", version = "0.1.0" }));

app.MapGet("/openapi/v1.json", () => Results.Redirect("/swagger/v1/swagger.json", false));

app.MapGet("/healthz", () => Results.Ok(new { ok = true, utc = DateTimeOffset.UtcNow }));

app.MapPost("/api/v1/stations/{stationId}/jobs", async (
	string stationId,
	SubmitJobRequest request,
	HttpContext http,
	IJobOrchestrator orchestrator,
	CancellationToken ct) =>
{
	if (string.IsNullOrWhiteSpace(request.Operation))
	{
		return Results.BadRequest(new ApiError(ErrorCodes.BadRequest, "operation is required."));
	}

	var idempotencyKey = http.Request.Headers["Idempotency-Key"].FirstOrDefault() ?? request.ClientReqId;
	if (string.IsNullOrWhiteSpace(idempotencyKey))
	{
		return Results.BadRequest(new ApiError(ErrorCodes.BadRequest, "Idempotency-Key header or clientReqId is required."));
	}

	var submit = new JobSubmitModel(
		stationId,
		request.Operation,
		request.Sn,
		request.Port,
		request.Parameters,
		idempotencyKey,
		request.TimeoutSec);

	JobSubmitResult result;
	try
	{
		result = await orchestrator.SubmitAsync(submit, ct);
	}
	catch (NotSupportedException ex)
	{
		return Results.BadRequest(new ApiError(ErrorCodes.OperationNotSupported, ex.Message));
	}

	return Results.Accepted($"/api/v1/jobs/{result.JobId}", result);
});

app.MapGet("/api/v1/jobs/{jobId:guid}", async (Guid jobId, IJobRepository repo, CancellationToken ct) =>
{
	var job = await repo.GetByIdAsync(jobId, ct);
	return job is null
		? Results.NotFound(new ApiError(ErrorCodes.NotFound, "Job not found."))
		: Results.Ok(JobView.From(job));
});

app.MapPost("/api/v1/jobs/{jobId:guid}/cancel", async (Guid jobId, IJobOrchestrator orchestrator, CancellationToken ct) =>
{
	var canceled = await orchestrator.CancelAsync(jobId, ct);
	return canceled
		? Results.Ok(new { jobId, canceled = true })
		: Results.Conflict(new ApiError(ErrorCodes.Conflict, "Job cannot be canceled in current state."));
});

app.MapGet("/api/v1/stations/{stationId}/state", async (
	string stationId,
	ILegacyAutomationAdapter adapter,
	IJobRepository repo,
	CancellationToken ct) =>
{
	var queued = await repo.CountQueuedByStationAsync(stationId, ct);
	var running = await repo.CountRunningByStationAsync(stationId, ct);
	return Results.Ok(new
	{
		stationId,
		adapterConnected = adapter.IsConnected,
		queued,
		running,
		now = DateTimeOffset.UtcNow,
	});
});

app.MapPost("/api/v1/stations/{stationId}/subscriptions/webhooks", async (
	string stationId,
	CreateWebhookSubscriptionRequest request,
	IWebhookSubscriptionRepository subscriptionRepository,
	CancellationToken ct) =>
{
	if (!Uri.TryCreate(request.Url, UriKind.Absolute, out var parsed)
		|| (parsed.Scheme != Uri.UriSchemeHttp && parsed.Scheme != Uri.UriSchemeHttps))
	{
		return Results.BadRequest(new ApiError(ErrorCodes.BadRequest, "url must be valid http/https URI."));
	}

	var subscription = new WebhookSubscription
	{
		SubscriptionId = Guid.NewGuid(),
		StationId = stationId,
		Url = request.Url,
		Secret = request.Secret,
		CreatedUtc = DateTimeOffset.UtcNow,
	};

	await subscriptionRepository.CreateAsync(subscription, ct);
	return Results.Created(
		$"/api/v1/stations/{stationId}/subscriptions/webhooks/{subscription.SubscriptionId}",
		WebhookSubscriptionView.From(subscription));
});

app.MapGet("/api/v1/stations/{stationId}/subscriptions/webhooks", async (
	string stationId,
	IWebhookSubscriptionRepository subscriptionRepository,
	CancellationToken ct) =>
{
	var list = await subscriptionRepository.ListByStationAsync(stationId, ct);
	return Results.Ok(list.Select(WebhookSubscriptionView.From));
});

app.Run();
