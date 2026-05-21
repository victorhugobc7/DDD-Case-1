using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.DTOs;
using Domain.Aggregates.Faturamento;
using Domain.Enums.Autorizacoes;
using Domain.Repositories.Autorizacoes;
using Domain.Repositories.Faturamento;

namespace Application.UseCases.Faturamento;

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

            if (unitValue < 0)
                throw new ArgumentException("O valor unitário não pode ser negativo.", nameof(dto.UnitValuesByItemId));

            bill.AddItem(new BillItem(
                Guid.NewGuid(),
                authorization.Id,
                item.Description,
                item.ApprovedQuantity,
                unitValue));
        }

        await _hospitalBillRepository.AddAsync(bill);

        return bill.Id;
    }
}
