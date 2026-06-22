using System.Text.Json.Serialization;
using Application.DTOs;
using Application.Interfaces;
using Application.Services;
using Application.UseCases.Authorizations;
using Application.UseCases.Billing;
using Domain.Beneficiario;
using Domain.Beneficiario.Interfaces;
using Domain.Common.Enums;
using Domain.Common.ValueObjects;
using Domain.Faturamento.Interfaces;
using Domain.Plano;
using Domain.Plano.Enums;
using Domain.Plano.Interfaces;
using Domain.Procedimento;
using Domain.Procedimento.Interfaces;
using Domain.Solicitacao.Interfaces;
using Domain.Solicitacao.Services;
using Infra.Repositories;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

// --- JSON serialization: enums as strings ---
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    options.SerializerOptions.PropertyNamingPolicy = null; // PascalCase to match DTOs
});

// --- CORS ---
builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader()));

// --- Dependency Injection (same registrations as original) ---
var dbPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "health-insurance.db"));
builder.Services.AddSingleton<IAuthorizationRepository>(sp => new AuthorizationRepository(dbPath));
builder.Services.AddSingleton<IHospitalBillRepository>(sp => new HospitalBillRepository(dbPath));
builder.Services.AddSingleton<IBeneficiaryRepository>(sp => new BeneficiaryRepository(dbPath));
builder.Services.AddSingleton<IPlanRepository>(sp => new PlanRepository(dbPath));
builder.Services.AddSingleton<IProcedureCatalogRepository>(sp => new ProcedureCatalogRepository(dbPath));

builder.Services.AddSingleton<AuthorizationEligibilityValidator>();

builder.Services.AddSingleton<RequestAuthorizationUseCase>();
builder.Services.AddSingleton<ApproveAuthorizationUseCase>();
builder.Services.AddSingleton<ApproveAuthorizationPartiallyUseCase>();
builder.Services.AddSingleton<DenyAuthorizationUseCase>();
builder.Services.AddSingleton<RegisterDocumentPendingUseCase>();
builder.Services.AddSingleton<GetAuthorizationStatusUseCase>();

builder.Services.AddSingleton<CreateHospitalBillFromAuthorizationUseCase>();
builder.Services.AddSingleton<GetHospitalBillUseCase>();
builder.Services.AddSingleton<ApplyGlosaToHospitalBillItemUseCase>();
builder.Services.AddSingleton<FileGlosaAppealUseCase>();
builder.Services.AddSingleton<EvaluateGlosaAppealUseCase>();
builder.Services.AddSingleton<CloseHospitalBillUseCase>();

builder.Services.AddSingleton<IAuthorizationService, AuthorizationService>();
builder.Services.AddSingleton<IBillingService, BillingService>();

var app = builder.Build();

app.UseCors();

// --- Serve the UI folder as static files ---
var uiPath = Path.GetFullPath(Path.Combine(builder.Environment.ContentRootPath, "..", "UI"));
if (Directory.Exists(uiPath))
{
    var fileProvider = new PhysicalFileProvider(uiPath);
    app.UseDefaultFiles(new DefaultFilesOptions { FileProvider = fileProvider });
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = fileProvider,
        RequestPath = ""
    });
}

// ============================================================
//  SEED ENDPOINT
// ============================================================

app.MapPost("/api/seed", async (
    IBeneficiaryRepository beneficiaryRepo,
    IPlanRepository planRepo,
    IProcedureCatalogRepository procedureRepo) =>
{
    try
    {
        var existingPlan = await planRepo.GetByNumberAsync("123456");
        var planId = existingPlan?.Id ?? Guid.NewGuid();

        if (existingPlan == null)
        {
            var plan = new Plan(planId, "123456", PlanType.Individual, 0);
            plan.SetGracePeriod(ProcedureType.ExameSimples, 30);
            plan.SetGracePeriod(ProcedureType.Cirurgia, 0);
            await planRepo.AddAsync(plan);
        }

        var beneficiaryId = Guid.NewGuid();
        await beneficiaryRepo.AddAsync(new Beneficiary(
            beneficiaryId, "Maria Silva",
            DateTime.Today.AddYears(-30),
            DateTime.Today.AddYears(-1),
            planId));

        await procedureRepo.AddAsync(new ProcedureCatalogItem(
            Guid.NewGuid(),
            new ProcedureCode("9999"),
            "Procedimento eletivo de teste",
            ProcedureType.ExameSimples, 18, 65));

        await procedureRepo.AddAsync(new ProcedureCatalogItem(
            Guid.NewGuid(),
            new ProcedureCode("8888"),
            "Atendimento de urgência de teste",
            ProcedureType.Cirurgia, 18, 65));

        return Results.Ok(new
        {
            Message = "Dados de referência criados com sucesso!",
            BeneficiaryId = beneficiaryId,
            BeneficiaryName = "Maria Silva",
            PlanId = planId,
            PlanNumber = "123456",
            ProcedureCodes = new[] { "9999", "8888" }
        });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// ============================================================
//  AUTHORIZATION ENDPOINTS
// ============================================================

app.MapPost("/api/authorizations", async (
    AuthorizationRequestDto dto,
    IAuthorizationService service) =>
{
    try
    {
        var id = await service.RequestAuthorizationAsync(dto);
        return Results.Ok(new { Id = id });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
});

app.MapGet("/api/authorizations/{id:guid}", async (
    Guid id,
    IAuthorizationService service) =>
{
    try
    {
        var status = await service.GetAuthorizationStatusAsync(id);
        return Results.Ok(status);
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound(new { Error = "Autorização não encontrada." });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
});

app.MapPut("/api/authorizations/{id:guid}/approve", async (
    Guid id,
    IAuthorizationService service) =>
{
    try
    {
        await service.ApproveAuthorizationAsync(id);
        return Results.Ok(new { Message = "Autorização aprovada integralmente." });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
});

app.MapPut("/api/authorizations/{id:guid}/approve-partially", async (
    Guid id,
    PartialApprovalRequest request,
    IAuthorizationService service) =>
{
    try
    {
        await service.ApproveAuthorizationPartiallyAsync(id, request.ApprovedQuantities);
        return Results.Ok(new { Message = "Autorização aprovada parcialmente." });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
});

app.MapPut("/api/authorizations/{id:guid}/deny", async (
    Guid id,
    DenyRequest request,
    IAuthorizationService service) =>
{
    try
    {
        await service.DenyAuthorizationAsync(id, request.Reason, request.Details);
        return Results.Ok(new { Message = "Autorização negada." });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
});

app.MapPut("/api/authorizations/{id:guid}/pending-documents", async (
    Guid id,
    PendingDocumentsRequest request,
    IAuthorizationService service) =>
{
    try
    {
        await service.RegisterDocumentPendingAsync(id, request.MissingDocuments);
        return Results.Ok(new { Message = "Pendência de documentos registrada." });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
});

// ============================================================
//  BILLING ENDPOINTS
// ============================================================

app.MapPost("/api/bills", async (
    CreateHospitalBillDto dto,
    IBillingService service) =>
{
    try
    {
        var id = await service.CreateHospitalBillFromAuthorizationAsync(dto);
        return Results.Ok(new { Id = id });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
});

app.MapGet("/api/bills/{id:guid}", async (
    Guid id,
    IBillingService service) =>
{
    try
    {
        var bill = await service.GetHospitalBillAsync(id);
        return Results.Ok(bill);
    }
    catch (KeyNotFoundException)
    {
        return Results.NotFound(new { Error = "Conta hospitalar não encontrada." });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
});

app.MapPost("/api/bills/{billId:guid}/items/{itemId:guid}/glosas", async (
    Guid billId,
    Guid itemId,
    ApplyGlosaRequest request,
    IBillingService service) =>
{
    try
    {
        var glosaId = await service.ApplyGlosaToItemAsync(new ApplyGlosaDto
        {
            HospitalBillId = billId,
            BillItemId = itemId,
            Reason = request.Reason,
            Details = request.Details
        });
        return Results.Ok(new { GlosaId = glosaId });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
});

app.MapPost("/api/bills/{billId:guid}/items/{itemId:guid}/glosas/{glosaId:guid}/appeal",
    async (
        Guid billId,
        Guid itemId,
        Guid glosaId,
        FileAppealRequest request,
        IBillingService service) =>
{
    try
    {
        await service.FileGlosaAppealAsync(new FileGlosaAppealDto
        {
            HospitalBillId = billId,
            BillItemId = itemId,
            GlosaId = glosaId,
            EvidenceDocuments = request.EvidenceDocuments
        });
        return Results.Ok(new { Message = "Recurso de glosa registrado com sucesso." });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
});

app.MapPut("/api/bills/{billId:guid}/items/{itemId:guid}/glosas/{glosaId:guid}/appeal/evaluate",
    async (
        Guid billId,
        Guid itemId,
        Guid glosaId,
        EvaluateGlosaAppealRequest request,
        IBillingService service) =>
{
    try
    {
        await service.EvaluateGlosaAppealAsync(new EvaluateGlosaAppealDto
        {
            HospitalBillId = billId,
            BillItemId = itemId,
            GlosaId = glosaId,
            Approve = request.Approve
        });
        return Results.Ok(new { Message = "Recurso de glosa avaliado com sucesso." });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
});

app.MapPut("/api/bills/{id:guid}/close", async (
    Guid id,
    IBillingService service) =>
{
    try
    {
        await service.CloseHospitalBillAsync(id);
        return Results.Ok(new { Message = "Conta hospitalar encerrada." });
    }
    catch (Exception ex)
    {
        return Results.BadRequest(new { Error = ex.Message });
    }
});

app.Run();

// ============================================================
//  Request record types for endpoint deserialization
// ============================================================
record PartialApprovalRequest(Dictionary<Guid, int> ApprovedQuantities);
record DenyRequest(GlosaReason Reason, string Details);
record PendingDocumentsRequest(string MissingDocuments);
record ApplyGlosaRequest(GlosaReason Reason, string Details);
record FileAppealRequest(List<EvidenceDto> EvidenceDocuments);
record EvaluateGlosaAppealRequest(bool Approve);
