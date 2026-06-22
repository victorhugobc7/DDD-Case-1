using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using Domain.Faturamento;
using Domain.Faturamento.Entities;
using Domain.Faturamento.Interfaces;
using Domain.Solicitacao.Enums;
using Domain.Solicitacao.Interfaces;

namespace Application.UseCases.Billing;

public class CreateHospitalBillFromAuthorizationUseCase
{
    private readonly IAuthorizationRepository _authorizationRepository;
    private readonly IHospitalBillRepository _hospitalBillRepository;

    public CreateHospitalBillFromAuthorizationUseCase(
        IAuthorizationRepository authorizationRepository,
        IHospitalBillRepository hospitalBillRepository)
    {
        _authorizationRepository = authorizationRepository;
        _hospitalBillRepository = hospitalBillRepository;
    }

    public async Task<Guid> ExecuteAsync(CreateHospitalBillDto dto)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        var authorization = await _authorizationRepository.GetByIdAsync(dto.AuthorizationId);
        if (authorization == null)
            throw new KeyNotFoundException("Solicitação de autorização não encontrada.");

        if (authorization.Status != AuthorizationStatus.AprovadaIntegralmente &&
            authorization.Status != AuthorizationStatus.AprovadaParcialmente)
            throw new InvalidOperationException("Somente autorizações aprovadas podem ser faturadas.");

        var approvedItems = authorization.Items
            .Where(item => item.ApprovedQuantity > 0)
            .ToList();

        if (!approvedItems.Any())
            throw new InvalidOperationException("A autorização aprovada não possui itens faturáveis.");

        var bill = new HospitalBill(
            Guid.NewGuid(),
            authorization.BeneficiaryId,
            authorization.ExecutingEstablishment);

        foreach (var item in approvedItems)
        {
            if (dto.UnitValuesByItemId == null ||
                !dto.UnitValuesByItemId.TryGetValue(item.Id, out var unitValue))
                throw new ArgumentException("Valor unitário não informado para item aprovado.", nameof(dto.UnitValuesByItemId));

            var money = BillingUseCaseSupport.ToMoney(unitValue);

            bill.AddItem(new BillItem(
                Guid.NewGuid(),
                authorization.Id,
                item.Description,
                item.ApprovedQuantity,
                money));
        }

        await _hospitalBillRepository.AddAsync(bill);

        return bill.Id;
    }
}
