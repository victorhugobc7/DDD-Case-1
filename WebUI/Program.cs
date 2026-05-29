using Application.Interfaces;
using Application.Services;
using Application.UseCases.Authorizations;
using Application.UseCases.Billing;
using Domain.Authorizations;
using Domain.Beneficiaries;
using Domain.Billing;
using Domain.Plans;
using Domain.Procedures;
using Infra.Repositories;
using WebUI;

var builder = WebApplication.CreateBuilder(args);

var databasePath = Environment.GetEnvironmentVariable("HEALTH_INSURANCE_DB");
if (string.IsNullOrWhiteSpace(databasePath))
    databasePath = "health-insurance-web.db";

var databaseDirectory = Path.GetDirectoryName(databasePath);
if (!string.IsNullOrWhiteSpace(databaseDirectory))
    Directory.CreateDirectory(databaseDirectory);

builder.Services.AddRazorPages();
builder.Services.AddSingleton<IAuthorizationRepository>(_ => new AuthorizationRepository(databasePath));
builder.Services.AddSingleton<IHospitalBillRepository>(_ => new HospitalBillRepository(databasePath));
builder.Services.AddSingleton<IBeneficiaryRepository>(_ => new BeneficiaryRepository(databasePath));
builder.Services.AddSingleton<IPlanRepository>(_ => new PlanRepository(databasePath));
builder.Services.AddSingleton<IProcedureCatalogRepository>(_ => new ProcedureCatalogRepository(databasePath));
builder.Services.AddSingleton<EligibilityService>();
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
builder.Services.AddSingleton<CloseHospitalBillUseCase>();
builder.Services.AddSingleton<IAuthorizationService, AuthorizationService>();
builder.Services.AddSingleton<IBillingService, BillingService>();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    await DemoData.SeedAsync(scope.ServiceProvider);
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
}

app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.Run();
