/* ============================================================
   Health Insurance DDD — Frontend Application Logic
   ============================================================ */

const API = '';

// ── State ──────────────────────────────────────────────────────
let seedData      = null;
let currentAuth   = null; 
let currentUserBill   = null; 
let currentManagerBill = null;
let currentAuthId = null;
let currentUserBillId = null;
let currentManagerBillId = null;

// ============================================================
//  UTILITY HELPERS
// ============================================================

async function apiCall(method, url, body = null) {
    const opts = {
        method,
        headers: { 'Content-Type': 'application/json' }
    };
    if (body) opts.body = JSON.stringify(body);

    const res = await fetch(`${API}${url}`, opts);
    const text = await res.text();
    let data = null;
    try { data = JSON.parse(text); } catch { data = text; }

    if (!res.ok) {
        const msg = data?.Error || data?.detail || data || `HTTP ${res.status}`;
        throw new Error(msg);
    }
    return data;
}

function showToast(message, type = 'info') {
    const icons = { success: 'Sucesso:', error: 'Erro:', warning: 'Aviso:', info: 'Info:' };
    const container = document.getElementById('toastContainer');
    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    toast.innerHTML = `<span style="font-weight: bold; margin-right: 5px;">${icons[type]}</span><span>${escapeHtml(message)}</span>`;
    container.appendChild(toast);
    setTimeout(() => toast.remove(), 4000);
}

function setLoading(btnId, loading) {
    const btn = document.getElementById(btnId);
    if (!btn) return;
    if (loading) btn.classList.add('loading');
    else btn.classList.remove('loading');
}

function escapeHtml(str) {
    if (typeof str !== 'string') return str;
    return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;');
}

function formatGuid(guid) {
    if (!guid) return '—';
    return guid.substring(0, 8) + '…';
}

function copyToClipboard(text) {
    navigator.clipboard.writeText(text).then(() => showToast('ID copiado!', 'info'));
}

function statusBadge(status) {
    const map = {
        'AprovadaIntegralmente': { cls: 'approved',  label: 'Aprovada' },
        'AprovadaParcialmente':  { cls: 'partial',   label: 'Parcial' },
        'Negada':                { cls: 'denied',    label: 'Negada' },
        'Pendente':              { cls: 'pending',   label: 'Pendente' },
        'Open':                  { cls: 'open',      label: 'Aberta' },
        'Closed':                { cls: 'closed',    label: 'Encerrada' }
    };
    const info = map[status] || { cls: 'pending', label: status };
    return `<span class="status-badge ${info.cls}">${info.label}</span>`;
}

function glosaReasonLabel(reason) {
    const map = {
        'ProcedimentoNaoAutorizado': 'Procedimento não autorizado',
        'DivergenciaDeCodigo': 'Divergência de código',
        'ExcessoDeQuantidade': 'Excesso de quantidade',
        'FaltaDeDocumentacao': 'Falta de documentação',
        'ProcedimentoForaDaCoberturaContratual': 'Fora da cobertura'
    };
    return map[reason] || reason;
}

function appealStatusBadge(status) {
    const map = {
        'EmAnalise':      { cls: 'em-analise', label: 'Em Análise' },
        'GlosaMantida':   { cls: 'mantida',    label: 'Mantida' },
        'GlosaRevertida': { cls: 'revertida',  label: 'Revertida' }
    };
    const info = map[status] || { cls: 'em-analise', label: status };
    return `<span class="appeal-badge ${info.cls}">${info.label}</span>`;
}

// ============================================================
//  NAVIGATION
// ============================================================

function showSection(sectionId, navBtn) {
    document.querySelectorAll('.section').forEach(s => s.classList.remove('active'));
    document.querySelectorAll('.nav-item').forEach(n => n.classList.remove('active'));

    const section = document.getElementById('section-' + sectionId);
    if (section) section.classList.add('active');
    if (navBtn) navBtn.classList.add('active');
}

// ============================================================
//  SEED
// ============================================================

async function seedDatabase() {
    setLoading('btnSeed', true);
    try {
        const data = await apiCall('POST', '/api/seed');
        seedData = data;
        showToast(data.Message || 'Dados criados com sucesso!', 'success');

        const badge = document.getElementById('seedBadge');
        badge.classList.add('active');
        document.getElementById('seedBadgeText').textContent = 'Dados OK';

        const info = document.getElementById('seedInfo');
        info.classList.add('visible');
        document.getElementById('seedDataDisplay').innerHTML = `
            <div class="data-item">
                <span class="data-label">Beneficiário</span>
                <span class="data-value" style="cursor:pointer" onclick="copyToClipboard('${data.BeneficiaryId}')" title="Clique para copiar">
                    ${data.BeneficiaryName} <small style="color:var(--text-muted)">(${formatGuid(data.BeneficiaryId)})</small>
                </span>
            </div>
            <div class="data-item">
                <span class="data-label">Plano</span>
                <span class="data-value">${data.PlanNumber}</span>
            </div>
            <div class="data-item">
                <span class="data-label">Procedimentos</span>
                <span class="data-value">${data.ProcedureCodes.join(', ')}</span>
            </div>
        `;

        document.getElementById('authBeneficiaryId').value = data.BeneficiaryId;
        document.getElementById('authPlanNumber').value = data.PlanNumber;
        document.getElementById('authProcedureCode').value = data.ProcedureCodes[0];
    } catch (err) {
        showToast(err.message, 'error');
    } finally {
        setLoading('btnSeed', false);
    }
}

// ============================================================
//  AUTHORIZATION — REQUEST
// ============================================================

document.addEventListener('DOMContentLoaded', () => {
    addRequestedItem();

    document.getElementById('authIsUrgent').addEventListener('change', (e) => {
        document.getElementById('urgentLabel').textContent = e.target.checked ? 'Sim' : 'Não';
    });

    const now = new Date();
    now.setDate(now.getDate() + 1);
    const iso = now.toISOString().slice(0, 16);
    document.getElementById('authExpectedDate').value = iso;
});

function addRequestedItem() {
    const list = document.getElementById('requestedItemsList');
    const row = document.createElement('div');
    row.className = 'item-row';
    row.innerHTML = `
        <div class="form-group">
            <label class="form-label">Descrição</label>
            <input class="form-input item-desc" type="text" placeholder="Ex: Soro" required>
        </div>
        <div class="form-group">
            <label class="form-label">Qtd</label>
            <input class="form-input item-qty" type="number" min="1" value="1" required>
        </div>
        <div class="form-group">
            <label class="form-label">Tipo</label>
            <select class="form-select item-type">
                <option value="Material">Material</option>
                <option value="Medicamento">Medicamento</option>
            </select>
        </div>
        <button type="button" class="btn-remove-item" onclick="removeRow(this)" title="Remover">✕</button>
    `;
    list.appendChild(row);
}

function removeRow(btn) {
    const row = btn.closest('.item-row');
    if (row) row.remove();
}

async function requestAuthorization(event) {
    event.preventDefault();
    setLoading('btnSubmitAuth', true);

    const items = [];
    document.querySelectorAll('#requestedItemsList .item-row').forEach(row => {
        items.push({
            Description: row.querySelector('.item-desc').value,
            Quantity:    parseInt(row.querySelector('.item-qty').value),
            ItemType:    row.querySelector('.item-type').value
        });
    });

    if (items.length === 0) {
        showToast('Adicione pelo menos um item.', 'warning');
        setLoading('btnSubmitAuth', false);
        return;
    }

    const dto = {
        BeneficiaryId:          document.getElementById('authBeneficiaryId').value,
        PlanNumber:             document.getElementById('authPlanNumber').value,
        ProcedureCode:          document.getElementById('authProcedureCode').value,
        CidCode:                document.getElementById('authCidCode').value,
        RequestingProfessional: document.getElementById('authProfessional').value,
        ExecutingEstablishment: document.getElementById('authEstablishment').value,
        ExpectedDate:           new Date(document.getElementById('authExpectedDate').value).toISOString(),
        RequestedItems:         items,
        IsUrgentOrEmergency:    document.getElementById('authIsUrgent').checked
    };

    try {
        const data = await apiCall('POST', '/api/authorizations', dto);
        showToast('Autorização criada com sucesso!', 'success');

        const panel = document.getElementById('authRequestResult');
        panel.classList.add('visible');
        panel.innerHTML = `
            <div class="result-header">
                <span class="result-title">Resultado</span>
            </div>
            <div class="result-body">
                <div class="data-grid">
                    <div class="data-item">
                        <span class="data-label">ID da Autorização</span>
                        <span class="data-value" style="cursor:pointer" onclick="copyToClipboard('${data.Id}')" title="Clique para copiar">
                            ${data.Id}
                        </span>
                    </div>
                </div>
                <div class="btn-group">
                    <button class="btn btn-sm" onclick="document.getElementById('lookupAuthId').value='${data.Id}';showSection('auth-manage',document.querySelector('[data-section=auth-manage]'))">
                        Avaliar Status
                    </button>
                    <button class="btn btn-sm" onclick="document.getElementById('billAuthId').value='${data.Id}';showSection('bill-create',document.querySelector('[data-section=bill-create]'))">
                        Criar Fatura
                    </button>
                </div>
            </div>
        `;
    } catch (err) {
        showToast(err.message, 'error');
    } finally {
        setLoading('btnSubmitAuth', false);
    }
}

// ============================================================
//  AUTHORIZATION — MANAGE
// ============================================================

async function getAuthorizationStatus() {
    const id = document.getElementById('lookupAuthId').value.trim();
    if (!id) { showToast('Informe o ID da autorização.', 'warning'); return; }

    setLoading('btnLookupAuth', true);
    try {
        const data = await apiCall('GET', `/api/authorizations/${id}`);
        currentAuth = data;
        currentAuthId = id;
        renderAuthStatus(data);
    } catch (err) {
        showToast(err.message, 'error');
        document.getElementById('authActionsContainer').style.display = 'none';
    } finally {
        setLoading('btnLookupAuth', false);
    }
}

function renderAuthStatus(data) {
    const panel = document.getElementById('authStatusResult');
    panel.classList.add('visible');

    let itemsHtml = '';
    if (data.Items && data.Items.length > 0) {
        itemsHtml = `
            <table class="items-table">
                <thead><tr>
                    <th>ID</th><th>Descrição</th><th>Solicitado</th><th>Aprovado</th>
                </tr></thead>
                <tbody>
                    ${data.Items.map(item => `
                        <tr>
                            <td class="mono" style="cursor:pointer" onclick="copyToClipboard('${item.Id}')" title="Copiar ID">${formatGuid(item.Id)}</td>
                            <td>${escapeHtml(item.Description)}</td>
                            <td>${item.RequestedQuantity}</td>
                            <td>${item.ApprovedQuantity}</td>
                        </tr>
                    `).join('')}
                </tbody>
            </table>
        `;
    }

    panel.innerHTML = `
        <div class="result-header">
            <span class="result-title">Status da Autorização</span>
            ${statusBadge(data.Status)}
        </div>
        <div class="result-body">
            <div class="data-grid">
                <div class="data-item">
                    <span class="data-label">ID</span>
                    <span class="data-value mono" style="cursor:pointer" onclick="copyToClipboard('${data.Id}')" title="Copiar">${data.Id}</span>
                </div>
                <div class="data-item">
                    <span class="data-label">Auditoria Posterior</span>
                    <span class="data-value">${data.RequiresPostPaymentAudit ? 'Sim' : 'Não'}</span>
                </div>
                ${data.DenialReason ? `<div class="data-item"><span class="data-label">Motivo da Negação</span><span class="data-value">${escapeHtml(data.DenialReason)}</span></div>` : ''}
                ${data.PendingReason ? `<div class="data-item"><span class="data-label">Pendência</span><span class="data-value">${escapeHtml(data.PendingReason)}</span></div>` : ''}
            </div>
            ${itemsHtml}
        </div>
    `;

    document.getElementById('authActionsContainer').style.display = 'block';
    const partialContainer = document.getElementById('partialApprovalItems');
    partialContainer.innerHTML = '';
    if (data.Items) {
        data.Items.forEach(item => {
            const row = document.createElement('div');
            row.className = 'inline-form';
            row.style.marginBottom = '8px';
            row.innerHTML = `
                <div class="form-group" style="flex:2">
                    <label class="form-label">${escapeHtml(item.Description)}</label>
                    <input class="form-input" type="text" value="${item.Id}" readonly style="font-size:0.78rem;color:var(--text-muted)">
                </div>
                <div class="form-group" style="flex:1">
                    <label class="form-label">Qtd Aprovada</label>
                    <input class="form-input partial-qty" type="number" min="0" max="${item.RequestedQuantity}" value="${item.RequestedQuantity}" data-item-id="${item.Id}">
                </div>
            `;
            partialContainer.appendChild(row);
        });
    }
}

async function approveAuthorization() {
    if (!currentAuthId) { showToast('Consulte uma autorização primeiro.', 'warning'); return; }
    setLoading('btnApproveFull', true);
    try {
        await apiCall('PUT', `/api/authorizations/${currentAuthId}/approve`);
        showToast('Autorização aprovada integralmente!', 'success');
        await getAuthorizationStatus(); 
    } catch (err) {
        showToast(err.message, 'error');
    } finally {
        setLoading('btnApproveFull', false);
    }
}

async function approvePartially() {
    if (!currentAuthId) { showToast('Consulte uma autorização primeiro.', 'warning'); return; }
    setLoading('btnApprovePartial', true);

    const approvedQuantities = {};
    document.querySelectorAll('.partial-qty').forEach(input => {
        approvedQuantities[input.dataset.itemId] = parseInt(input.value);
    });

    try {
        await apiCall('PUT', `/api/authorizations/${currentAuthId}/approve-partially`, { ApprovedQuantities: approvedQuantities });
        showToast('Autorização aprovada parcialmente!', 'success');
        await getAuthorizationStatus();
    } catch (err) {
        showToast(err.message, 'error');
    } finally {
        setLoading('btnApprovePartial', false);
    }
}

async function denyAuthorization() {
    if (!currentAuthId) { showToast('Consulte uma autorização primeiro.', 'warning'); return; }
    setLoading('btnDeny', true);

    const body = {
        Reason:  document.getElementById('denyReason').value,
        Details: document.getElementById('denyDetails').value
    };

    try {
        await apiCall('PUT', `/api/authorizations/${currentAuthId}/deny`, body);
        showToast('Autorização negada.', 'success');
        await getAuthorizationStatus();
    } catch (err) {
        showToast(err.message, 'error');
    } finally {
        setLoading('btnDeny', false);
    }
}

async function registerPendingDocuments() {
    if (!currentAuthId) { showToast('Consulte uma autorização primeiro.', 'warning'); return; }
    setLoading('btnPending', true);

    const body = {
        MissingDocuments: document.getElementById('pendingDocs').value
    };

    try {
        await apiCall('PUT', `/api/authorizations/${currentAuthId}/pending-documents`, body);
        showToast('Pendência de documentos registrada.', 'success');
        await getAuthorizationStatus();
    } catch (err) {
        showToast(err.message, 'error');
    } finally {
        setLoading('btnPending', false);
    }
}

// ============================================================
//  BILLING — CREATE
// ============================================================

let authForBill = null;

async function loadAuthForBill() {
    const id = document.getElementById('billAuthId').value.trim();
    if (!id) { showToast('Informe o ID da autorização.', 'warning'); return; }

    setLoading('btnLoadAuthForBill', true);
    try {
        const data = await apiCall('GET', `/api/authorizations/${id}`);
        authForBill = data;

        const approvedItems = data.Items.filter(i => i.ApprovedQuantity > 0);
        if (approvedItems.length === 0) {
            showToast('Nenhum item aprovado nesta autorização.', 'warning');
            document.getElementById('billItemsContainer').style.display = 'none';
            return;
        }

        const container = document.getElementById('billItemsList');
        container.innerHTML = '';
        approvedItems.forEach(item => {
            const row = document.createElement('div');
            row.className = 'item-row';
            row.style.gridTemplateColumns = '2fr 80px 120px';
            row.innerHTML = `
                <div class="form-group">
                    <label class="form-label">Descrição</label>
                    <input class="form-input" type="text" value="${escapeHtml(item.Description)}" readonly style="color:var(--text-secondary)">
                </div>
                <div class="form-group">
                    <label class="form-label">Qtd</label>
                    <input class="form-input" type="text" value="${item.ApprovedQuantity}" readonly style="color:var(--text-secondary)">
                </div>
                <div class="form-group">
                    <label class="form-label">Valor Unit. (R$)</label>
                    <input class="form-input bill-unit-value" type="number" step="0.01" min="0" value="25.50" data-item-id="${item.Id}">
                </div>
            `;
            container.appendChild(row);
        });

        document.getElementById('billItemsContainer').style.display = 'block';
    } catch (err) {
        showToast(err.message, 'error');
    } finally {
        setLoading('btnLoadAuthForBill', false);
    }
}

async function createBill() {
    const authId = document.getElementById('billAuthId').value.trim();
    if (!authId) { showToast('Informe o ID da autorização.', 'warning'); return; }

    setLoading('btnCreateBill', true);

    const unitValues = {};
    document.querySelectorAll('.bill-unit-value').forEach(input => {
        unitValues[input.dataset.itemId] = {
            Amount:   parseFloat(input.value),
            Currency: 'BRL'
        };
    });

    const dto = {
        AuthorizationId:    authId,
        UnitValuesByItemId: unitValues
    };

    try {
        const data = await apiCall('POST', '/api/bills', dto);
        showToast('Fatura hospitalar criada com sucesso!', 'success');

        const panel = document.getElementById('billCreateResult');
        panel.classList.add('visible');
        panel.innerHTML = `
            <div class="result-header"><span class="result-title">Resultado</span></div>
            <div class="result-body">
                <div class="data-grid">
                    <div class="data-item">
                        <span class="data-label">ID da Fatura</span>
                        <span class="data-value" style="cursor:pointer" onclick="copyToClipboard('${data.Id}')" title="Copiar">${data.Id}</span>
                    </div>
                </div>
                <div class="btn-group">
                    <button class="btn btn-sm" onclick="document.getElementById('lookupUserBillId').value='${data.Id}';showSection('user-manage',document.querySelector('[data-section=user-manage]'))">
                        Ver Fatura
                    </button>
                </div>
            </div>
        `;
    } catch (err) {
        showToast(err.message, 'error');
    } finally {
        setLoading('btnCreateBill', false);
    }
}

// ============================================================
//  BILLING — RENDER LOGIC
// ============================================================

function generateBillHtml(data) {
    let itemsHtml = '';
    if (data.Items && data.Items.length > 0) {
        itemsHtml = data.Items.map(item => {
            let glosasHtml = '';
            if (item.Glosas && item.Glosas.length > 0) {
                glosasHtml = item.Glosas.map(g => {
                    let appealHtml = '';
                    if (g.Appeal) {
                        appealHtml = `<div style="margin-top:6px">${appealStatusBadge(g.Appeal.Status)}</div>`;
                    }
                    return `
                        <div class="glosa-card">
                            <div class="glosa-header">
                                <span class="glosa-reason">${glosaReasonLabel(g.Reason)}</span>
                                <span class="mono" style="font-size:0.72rem;cursor:pointer" onclick="copyToClipboard('${g.Id}')" title="Copiar ID da Glosa">${formatGuid(g.Id)}</span>
                            </div>
                            <div class="glosa-details">${escapeHtml(g.Details)}</div>
                            ${g.IsClawback ? '<span class="status-badge denied" style="margin-top:4px;font-size:0.68rem">Clawback</span>' : ''}
                            ${appealHtml}
                        </div>
                    `;
                }).join('');
            }

            return `
                <tr>
                    <td class="mono" style="cursor:pointer" onclick="copyToClipboard('${item.Id}')" title="Copiar ID">${formatGuid(item.Id)}</td>
                    <td>${escapeHtml(item.Description)}</td>
                    <td>${item.Quantity}</td>
                    <td>R$ ${item.UnitValue.Amount.toFixed(2)}</td>
                    <td>R$ ${item.TotalValue.Amount.toFixed(2)}</td>
                </tr>
                ${glosasHtml ? `<tr><td colspan="5" style="padding:4px 14px 14px">${glosasHtml}</td></tr>` : ''}
            `;
        }).join('');

        itemsHtml = `
            <table class="items-table">
                <thead><tr>
                    <th>ID</th><th>Descrição</th><th>Qtd</th><th>Valor Unit.</th><th>Total</th>
                </tr></thead>
                <tbody>${itemsHtml}</tbody>
            </table>
        `;
    }

    return `
        <div class="result-header">
            <span class="result-title">Fatura Hospitalar</span>
            ${statusBadge(data.Status)}
        </div>
        <div class="result-body">
            <div class="data-grid">
                <div class="data-item">
                    <span class="data-label">ID</span>
                    <span class="data-value mono" style="cursor:pointer" onclick="copyToClipboard('${data.Id}')" title="Copiar">${data.Id}</span>
                </div>
                <div class="data-item">
                    <span class="data-label">Estabelecimento</span>
                    <span class="data-value">${escapeHtml(data.ExecutingEstablishment)}</span>
                </div>
                <div class="data-item">
                    <span class="data-label">Total</span>
                    <span class="data-value" style="font-size:1.1rem;font-weight:700;color:var(--accent)">
                        R$ ${data.TotalValue.Amount.toFixed(2)} ${data.TotalValue.Currency}
                    </span>
                </div>
                <div class="data-item">
                    <span class="data-label">Beneficiário</span>
                    <span class="data-value mono">${formatGuid(data.BeneficiaryId)}</span>
                </div>
            </div>
            ${itemsHtml}
        </div>
    `;
}

// ============================================================
//  BILLING — USER MANAGE
// ============================================================

async function getUserBillStatus() {
    const id = document.getElementById('lookupUserBillId').value.trim();
    if (!id) { showToast('Informe o ID da fatura.', 'warning'); return; }

    setLoading('btnLookupUserBill', true);
    try {
        const data = await apiCall('GET', `/api/bills/${id}`);
        currentUserBill = data;
        currentUserBillId = id;
        
        const panel = document.getElementById('userBillStatusResult');
        panel.classList.add('visible');
        panel.innerHTML = generateBillHtml(data);
        
        document.getElementById('userBillActionsContainer').style.display = 'block';

        const appealItemSelect = document.getElementById('userAppealItemId');
        appealItemSelect.innerHTML = '';

        if (data.Items) {
            data.Items.forEach(item => {
                appealItemSelect.appendChild(new Option(`${item.Description} (${formatGuid(item.Id)})`, item.Id));
            });
        }
        updateUserGlosaSelect();
    } catch (err) {
        showToast(err.message, 'error');
        document.getElementById('userBillActionsContainer').style.display = 'none';
    } finally {
        setLoading('btnLookupUserBill', false);
    }
}

function updateUserGlosaSelect() {
    const itemId = document.getElementById('userAppealItemId').value;
    const glosaSelect = document.getElementById('userAppealGlosaId');
    glosaSelect.innerHTML = '';

    if (currentUserBill && currentUserBill.Items) {
        const item = currentUserBill.Items.find(i => i.Id === itemId);
        if (item && item.Glosas) {
            item.Glosas.forEach(g => {
                glosaSelect.appendChild(new Option(`${glosaReasonLabel(g.Reason)} (${formatGuid(g.Id)})`, g.Id));
            });
        }
    }
    if (glosaSelect.options.length === 0) {
        glosaSelect.appendChild(new Option('Nenhuma glosa encontrada', ''));
    }
}

function addUserEvidence() {
    const list = document.getElementById('userEvidenceList');
    const row = document.createElement('div');
    row.className = 'item-row';
    row.style.gridTemplateColumns = '1fr 1fr 40px';
    row.innerHTML = `
        <div class="form-group">
            <label class="form-label">URL do Documento</label>
            <input class="form-input evidence-url" type="text" placeholder="https://..." required>
        </div>
        <div class="form-group">
            <label class="form-label">Descrição</label>
            <input class="form-input evidence-desc" type="text" placeholder="Descreva o documento">
        </div>
        <button type="button" class="btn-remove-item" onclick="removeRow(this)" title="Remover">✕</button>
    `;
    list.appendChild(row);
}

async function userFileAppeal() {
    if (!currentUserBillId) { showToast('Consulte uma fatura primeiro.', 'warning'); return; }
    setLoading('btnUserFileAppeal', true);

    const itemId  = document.getElementById('userAppealItemId').value;
    const glosaId = document.getElementById('userAppealGlosaId').value;

    if (!glosaId) {
        showToast('Selecione uma glosa para recorrer.', 'warning');
        setLoading('btnUserFileAppeal', false);
        return;
    }

    const evidence = [];
    document.querySelectorAll('#userEvidenceList .item-row').forEach(row => {
        evidence.push({
            DocumentUrl: row.querySelector('.evidence-url').value,
            Description: row.querySelector('.evidence-desc').value
        });
    });

    if (evidence.length === 0) {
        showToast('Adicione pelo menos um documento de evidência.', 'warning');
        setLoading('btnUserFileAppeal', false);
        return;
    }

    const body = { EvidenceDocuments: evidence };

    try {
        await apiCall('POST', `/api/bills/${currentUserBillId}/items/${itemId}/glosas/${glosaId}/appeal`, body);
        showToast('Recurso registrado com sucesso!', 'success');
        await getUserBillStatus();
    } catch (err) {
        showToast(err.message, 'error');
    } finally {
        setLoading('btnUserFileAppeal', false);
    }
}

// ============================================================
//  BILLING — MANAGER MANAGE
// ============================================================

async function getManagerBillStatus() {
    const id = document.getElementById('lookupManagerBillId').value.trim();
    if (!id) { showToast('Informe o ID da fatura.', 'warning'); return; }

    setLoading('btnLookupManagerBill', true);
    try {
        const data = await apiCall('GET', `/api/bills/${id}`);
        currentManagerBill = data;
        currentManagerBillId = id;
        
        const panel = document.getElementById('managerBillStatusResult');
        panel.classList.add('visible');
        panel.innerHTML = generateBillHtml(data);
        
        document.getElementById('managerBillActionsContainer').style.display = 'block';

        const glosaItemSelect = document.getElementById('glosaItemId');
        const evalItemSelect  = document.getElementById('evalAppealItemId');
        glosaItemSelect.innerHTML = '';
        evalItemSelect.innerHTML = '';

        if (data.Items) {
            data.Items.forEach(item => {
                glosaItemSelect.appendChild(new Option(`${item.Description} (${formatGuid(item.Id)})`, item.Id));
                evalItemSelect.appendChild(new Option(`${item.Description} (${formatGuid(item.Id)})`, item.Id));
            });
        }
        updateEvalGlosaSelect();
    } catch (err) {
        showToast(err.message, 'error');
        document.getElementById('managerBillActionsContainer').style.display = 'none';
    } finally {
        setLoading('btnLookupManagerBill', false);
    }
}

function updateEvalGlosaSelect() {
    const itemId = document.getElementById('evalAppealItemId').value;
    const glosaSelect = document.getElementById('evalAppealGlosaId');
    glosaSelect.innerHTML = '';

    if (currentManagerBill && currentManagerBill.Items) {
        const item = currentManagerBill.Items.find(i => i.Id === itemId);
        if (item && item.Glosas) {
            item.Glosas.forEach(g => {
                if (g.Appeal) {
                    glosaSelect.appendChild(new Option(`${glosaReasonLabel(g.Reason)} (${formatGuid(g.Id)})`, g.Id));
                }
            });
        }
    }
    if (glosaSelect.options.length === 0) {
        glosaSelect.appendChild(new Option('Nenhum recurso encontrado', ''));
    }
}

async function applyGlosa() {
    if (!currentManagerBillId) { showToast('Consulte uma fatura primeiro.', 'warning'); return; }
    setLoading('btnApplyGlosa', true);

    const itemId = document.getElementById('glosaItemId').value;
    const body = {
        Reason:  document.getElementById('glosaReason').value,
        Details: document.getElementById('glosaDetails').value
    };

    try {
        const data = await apiCall('POST', `/api/bills/${currentManagerBillId}/items/${itemId}/glosas`, body);
        showToast(`Glosa aplicada! ID: ${formatGuid(data.GlosaId)}`, 'success');
        await getManagerBillStatus();
    } catch (err) {
        showToast(err.message, 'error');
    } finally {
        setLoading('btnApplyGlosa', false);
    }
}

async function evaluateAppeal(approve) {
    if (!currentManagerBillId) { showToast('Consulte uma fatura primeiro.', 'warning'); return; }
    
    const itemId = document.getElementById('evalAppealItemId').value;
    const glosaId = document.getElementById('evalAppealGlosaId').value;

    if (!glosaId) {
        showToast('Selecione um recurso válido.', 'warning');
        return;
    }

    const btnId = approve ? 'btnApproveAppeal' : 'btnDenyAppeal';
    setLoading(btnId, true);

    try {
        await apiCall('PUT', `/api/bills/${currentManagerBillId}/items/${itemId}/glosas/${glosaId}/appeal/evaluate`, { Approve: approve });
        showToast(approve ? 'Recurso aprovado, glosa revertida!' : 'Recurso negado, glosa mantida.', 'success');
        await getManagerBillStatus();
    } catch (err) {
        showToast(err.message, 'error');
    } finally {
        setLoading(btnId, false);
    }
}

async function closeBill() {
    if (!currentManagerBillId) { showToast('Consulte uma fatura primeiro.', 'warning'); return; }
    setLoading('btnCloseBill', true);
    try {
        await apiCall('PUT', `/api/bills/${currentManagerBillId}/close`);
        showToast('Fatura hospitalar encerrada.', 'success');
        await getManagerBillStatus();
    } catch (err) {
        showToast(err.message, 'error');
    } finally {
        setLoading('btnCloseBill', false);
    }
}
