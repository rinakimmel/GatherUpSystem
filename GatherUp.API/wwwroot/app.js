// GatherUp SPA - app.js
'use strict';

// ── Utilities ──────────────────────────────────────────────────────────────
async function api(path, opts = {}) {
    opts.credentials = opts.credentials || 'include';
    opts.headers = opts.headers || {};
    const res = await fetch(path, opts);
    const text = await res.text();
    let body;
    try { body = JSON.parse(text); } catch { body = text; }
    return { status: res.status, body };
}

function showToast(title, msg, isError = false) {
    const container = document.getElementById('toastContainer');
    if (!container) return;
    const el = document.createElement('div');
    el.className = `toast align-items-center text-bg-${isError ? 'danger' : 'success'} border-0`;
    el.setAttribute('role', 'alert');
    el.innerHTML = `<div class="d-flex"><div class="toast-body"><strong>${title}</strong><div class="small">${msg}</div></div>
      <button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast"></button></div>`;
    container.appendChild(el);
    new bootstrap.Toast(el, { delay: 4000 }).show();
}

function fmt(n) { return n == null ? '—' : `\u20AA${Number(n).toLocaleString('he-IL', { minimumFractionDigits: 2, maximumFractionDigits: 2 })}`; }
function fmtDate(d) { if (!d) return '—'; const dt = new Date(d); return isNaN(dt) ? '—' : dt.toLocaleDateString('he-IL', { day: '2-digit', month: '2-digit', year: 'numeric', hour: '2-digit', minute: '2-digit' }); }

function attendanceBadge(v) {
    if (v === true)  return '<span class="badge-confirmed">Confirmed</span>';
    if (v === false) return '<span class="badge-declined">Declined</span>';
    return '<span class="badge-pending">Pending</span>';
}
function paidBadge(v) { return v ? '<span class="badge-paid">Paid</span>' : '<span class="badge-unpaid">Unpaid</span>'; }

// ── State ──────────────────────────────────────────────────────────────────
let currentUser = null;
let currentEventId = null;
let allEvents = [];
let financeChart = null;

// ── Navigation ─────────────────────────────────────────────────────────────
function showPanel(name) {
    document.querySelectorAll('.gu-panel').forEach(p => p.classList.add('d-none'));
    document.querySelectorAll('.nav-pill').forEach(b => b.classList.remove('active'));
    const key = name.charAt(0).toUpperCase() + name.slice(1);
    document.getElementById('panel' + key)?.classList.remove('d-none');
    document.querySelectorAll(`[data-panel="${name}"]`).forEach(b => b.classList.add('active'));
    if (name === 'participants') loadAllParticipants();
    if (name === 'polls')        populatePollEventFilter();
    if (name === 'finance')      populateFinanceEventFilter();
}

document.querySelectorAll('.nav-pill[data-panel]').forEach(btn =>
    btn.addEventListener('click', () => showPanel(btn.dataset.panel)));

// ── Auth ───────────────────────────────────────────────────────────────────
async function checkAuth() {
    const res = await fetch('/auth/me', { credentials: 'include' });
    if (res.ok) applyLogin(await res.json());
    else showLoginView();
}

function applyLogin(user) {
    currentUser = user;
    document.getElementById('loginView').classList.add('d-none');
    document.getElementById('appView').classList.remove('d-none');
    document.getElementById('navLinks').classList.remove('d-none');
    const badge = document.getElementById('authStatus');
    badge.textContent = `${user.email}  (${user.role})`;
    badge.classList.remove('d-none');
    document.getElementById('logoutBtn').classList.remove('d-none');
    const isManager = user.role === 'Manager';
    document.getElementById('btnAddParticipant')?.classList.toggle('d-none', !isManager);
    loadEvents();
}

function showLoginView() {
    currentUser = null;
    document.getElementById('loginView').classList.remove('d-none');
    document.getElementById('appView').classList.add('d-none');
    document.getElementById('navLinks').classList.add('d-none');
    document.getElementById('authStatus').classList.add('d-none');
    document.getElementById('logoutBtn').classList.add('d-none');
}

document.getElementById('loginForm').addEventListener('submit', async e => {
    e.preventDefault();
    const errEl = document.getElementById('loginError');
    errEl.classList.add('d-none');
    const fd = new FormData();
    fd.append('email', document.getElementById('loginEmail').value);
    fd.append('password', document.getElementById('loginPassword').value);
    const res = await fetch('/auth/login', { method: 'POST', body: fd, credentials: 'include' });
    if (res.ok) applyLogin(await res.json());
    else { errEl.textContent = 'Invalid email or password.'; errEl.classList.remove('d-none'); }
});

document.getElementById('logoutBtn').addEventListener('click', async () => {
    await fetch('/auth/logout', { method: 'POST', credentials: 'include' });
    showLoginView();
});

window.addEventListener('load', checkAuth);

// ── Events ─────────────────────────────────────────────────────────────────
async function loadEvents() {
    const res = await api('/api/events');
    allEvents = (res.status === 200 && Array.isArray(res.body)) ? res.body : [];
    renderEventCards();
    populateEventSelects();
    showPanel('events');
}

function renderEventCards() {
    const grid = document.getElementById('eventsList');
    if (!allEvents.length) {
        grid.innerHTML = '<div class="col-12"><div class="gu-empty"><i class="bi bi-calendar-x"></i>No events yet. Create your first event!</div></div>';
        return;
    }
    grid.innerHTML = allEvents.map(ev => `<div class="col-md-6 col-xl-4">
      <div class="gu-event-card${currentEventId === ev.id ? ' selected' : ''}" data-id="${ev.id}">
        <div class="d-flex gap-3 align-items-start">
          <div class="gu-event-icon"><i class="bi bi-calendar-event-fill"></i></div>
          <div class="flex-grow-1 overflow-hidden">
            <h6 class="text-truncate mb-1">${ev.name}</h6>
            <div class="gu-event-meta d-flex flex-wrap gap-2">
              ${ev.date ? `<span><i class="bi bi-clock me-1"></i>${fmtDate(ev.date)}</span>` : ''}
              ${ev.location ? `<span><i class="bi bi-geo-alt me-1"></i>${ev.location}</span>` : ''}
            </div>
            <div class="gu-event-meta mt-1 d-flex flex-wrap gap-2">
              <span><i class="bi bi-people me-1"></i>${ev.participantCount} participants</span>
              ${ev.pricePerParticipant ? `<span><i class="bi bi-cash me-1"></i>${fmt(ev.pricePerParticipant)}</span>` : ''}
            </div>
          </div>
        </div>
      </div></div>`).join('');
    grid.querySelectorAll('.gu-event-card').forEach(c =>
        c.addEventListener('click', () => openEventDetail(Number(c.dataset.id))));
}

function populateEventSelects() {
    const none = '<option value="0">— None —</option>';
    const opts = none + allEvents.map(e => `<option value="${e.id}">${e.name}</option>`).join('');
    const apEvt = document.getElementById('apEventId');
    if (apEvt) apEvt.innerHTML = opts;
    const pf = document.getElementById('participantEventFilter');
    if (pf) pf.innerHTML = '<option value="">All Events</option>' + allEvents.map(e => `<option value="${e.id}">${e.name}</option>`).join('');
}

document.getElementById('btnNewEvent').addEventListener('click', () => {
    document.getElementById('eventFormCard').classList.remove('d-none');
    document.getElementById('eventFormTitle').textContent = 'Create New Event';
    document.getElementById('eventForm').reset();
    const btn = document.getElementById('evSubmitBtn');
    btn.textContent = 'Create Event';
    btn.dataset.editId = '';
});
document.getElementById('evCancelBtn').addEventListener('click', () =>
    document.getElementById('eventFormCard').classList.add('d-none'));

document.getElementById('eventForm').addEventListener('submit', async e => {
    e.preventDefault();
    const editId = document.getElementById('evSubmitBtn').dataset.editId;
    const payload = {
        Name: document.getElementById('evName').value,
        Description: document.getElementById('evDesc').value,
        Date: document.getElementById('evDate').value || null,
        Location: document.getElementById('evLocation').value,
        PricePerParticipant: document.getElementById('evPrice').value ? Number(document.getElementById('evPrice').value) : null,
        PaymentMethods: document.getElementById('evPayment').value,
        ManagerId: currentUser?.linkedId || 0, HostId: 0
    };
    const isEdit = !!editId;
    const res = await api(isEdit ? `/api/events/${editId}` : '/api/events', {
        method: isEdit ? 'PUT' : 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload)
    });
    if (res.status === 200 || res.status === 201) {
        showToast(isEdit ? 'Event Updated' : 'Event Created', payload.Name);
        document.getElementById('eventFormCard').classList.add('d-none');
        await loadEvents();
        if (isEdit) openEventDetail(Number(editId));
    } else { showToast('Error', JSON.stringify(res.body), true); }
});

// ── Event Detail ────────────────────────────────────────────────────────────
async function openEventDetail(eventId) {
    currentEventId = eventId;
    renderEventCards();
    const res = await api(`/api/events/${eventId}`);
    if (res.status !== 200) { showToast('Error', 'Could not load event', true); return; }
    const ev = res.body;
    document.getElementById('detailEventName').textContent = ev.name;
    document.getElementById('detailEventMeta').textContent =
        [ev.location && `📍 ${ev.location}`, ev.date && `🕒 ${fmtDate(ev.date)}`,
         ev.pricePerParticipant && `${fmt(ev.pricePerParticipant)} / person`].filter(Boolean).join('   •   ');
    document.getElementById('eventDetail').classList.remove('d-none');
    document.getElementById('eventDetail').scrollIntoView({ behavior: 'smooth', block: 'start' });

    const parts = ev.participants || [];
    document.getElementById('statParticipants').textContent = parts.length;
    document.getElementById('statConfirmed').textContent   = parts.filter(p => p.isAttending === true).length;
    document.getElementById('statPaid').textContent        = parts.filter(p => p.hasPaid).length;
    document.getElementById('statPolls').textContent       = (ev.polls || []).length;

    const finRes = await api(`/api/finance/${eventId}/summary`);
    if (finRes.status === 200) {
        const f = finRes.body;
        document.getElementById('statIncome').textContent   = fmt(f.totalIncome);
        document.getElementById('statExpenses').textContent = fmt(f.totalExpenses);
        const bal = f.totalIncome - f.totalExpenses;
        const balEl = document.getElementById('statBalance');
        balEl.textContent = fmt(bal);
        balEl.className = 'fw-bold ' + (bal >= 0 ? 'text-success' : 'text-danger');
        renderFinanceTab(ev, f);
    }
    renderParticipantsTab(parts);
    renderPollsTab(ev.polls || []);
    renderHostTab(ev.host);

    const isManager = currentUser?.role === 'Manager';
    document.getElementById('managerInviteActions').classList.toggle('d-none', !isManager);
    document.getElementById('managerPollActions').classList.toggle('d-none', !isManager);
    document.getElementById('managerFinanceActions').classList.toggle('d-none', !isManager);
    document.getElementById('managerHostActions').classList.toggle('d-none', !isManager);
    switchDetailTab('dtParticipants');
}

document.getElementById('btnCloseDetail').addEventListener('click', () => {
    document.getElementById('eventDetail').classList.add('d-none');
    currentEventId = null; renderEventCards();
});
document.querySelectorAll('#detailTabs button').forEach(btn =>
    btn.addEventListener('click', () => switchDetailTab(btn.dataset.tab)));

function switchDetailTab(tabId) {
    document.querySelectorAll('.dt-tab').forEach(t => t.classList.add('d-none'));
    document.querySelectorAll('#detailTabs button').forEach(b => b.classList.remove('active'));
    document.getElementById(tabId)?.classList.remove('d-none');
    document.querySelector(`#detailTabs button[data-tab="${tabId}"]`)?.classList.add('active');
}

function renderParticipantsTab(parts) {
    const el = document.getElementById('eventParticipantsList');
    if (!parts.length) { el.innerHTML = '<div class="gu-empty"><i class="bi bi-people"></i>No participants yet.</div>'; return; }
    el.innerHTML = `<table class="gu-table"><thead><tr><th>ID</th><th>Name</th><th>Email</th><th>Attendance</th><th>Payment</th><th>Amount</th></tr></thead>
      <tbody>${parts.map(p => `<tr><td class="text-muted small">${p.id}</td><td class="fw-semibold">${p.name}</td>
        <td class="text-muted small">${p.email}</td><td>${attendanceBadge(p.isAttending)}</td>
        <td>${paidBadge(p.hasPaid)}</td><td>${fmt(p.amountContributed)}</td></tr>`).join('')}</tbody></table>`;
}
function renderPollsTab(polls) {
    const el = document.getElementById('eventPollsList');
    if (!polls.length) { el.innerHTML = '<div class="gu-empty"><i class="bi bi-bar-chart"></i>No polls yet.</div>'; return; }
    el.innerHTML = polls.map(p => `<div class="gu-card mb-2 d-flex justify-content-between align-items-center">
      <div><i class="bi bi-bar-chart-fill text-primary me-2"></i><strong>${p.name}</strong>
        <span class="text-muted small ms-2">${p.description || ''}</span></div>
      <button class="btn btn-sm btn-outline-primary" onclick="showPollInPanel(${p.id})">View Results</button>
    </div>`).join('');
}
function renderHostTab(host) {
    const el = document.getElementById('hostInfo');
    el.innerHTML = host
        ? `<div class="gu-card d-flex align-items-center gap-3">
            <div class="gu-event-icon" style="background:#fef3c7;color:#d97706"><i class="bi bi-star-fill"></i></div>
            <div><div class="fw-bold">${host.name}</div><div class="text-muted small">${host.email}</div></div></div>`
        : '<div class="gu-empty"><i class="bi bi-star"></i>No host assigned yet.</div>';
}
function renderFinanceTab(ev, fin) {
    const el = document.getElementById('eventFinanceSummary');
    const vendors = fin.vendors || [];
    let html = `<div class="row g-2 mb-3">
      <div class="col-4"><div class="gu-card text-center p-2"><div class="small text-muted">Income</div><div class="fw-bold text-success">${fmt(fin.totalIncome)}</div></div></div>
      <div class="col-4"><div class="gu-card text-center p-2"><div class="small text-muted">Expenses</div><div class="fw-bold text-danger">${fmt(fin.totalExpenses)}</div></div></div>
      <div class="col-4"><div class="gu-card text-center p-2"><div class="small text-muted">Balance</div><div class="fw-bold ${fin.balance >= 0 ? 'text-success' : 'text-danger'}">${fmt(fin.balance)}</div></div></div></div>`;
    if (vendors.length) html += `<h6 class="fw-semibold mb-2 small">Vendors</h6>
      <table class="gu-table"><thead><tr><th>Vendor</th><th>Owed</th></tr></thead>
      <tbody>${vendors.map(v => `<tr><td>${v.vendorName ?? v.item1 ?? '—'}</td><td class="text-danger">${fmt(v.amountOwed ?? v.item2)}</td></tr>`).join('')}</tbody></table>`;
    el.innerHTML = html;
}

// Event detail form handlers
document.getElementById('btnSendInvitations').addEventListener('click', async () => {
    if (!currentEventId) return;
    const res = await api(`/api/participants/${currentEventId}/invitations?invitationUrlBase=${encodeURIComponent(window.location.origin)}`, { method: 'POST' });
    showToast(res.status === 204 ? 'Invitations Sent' : 'Error', res.status === 204 ? 'Emails logged.' : JSON.stringify(res.body), res.status !== 204);
});
document.getElementById('btnSendReminders').addEventListener('click', async () => {
    if (!currentEventId) return;
    const res = await api(`/api/participants/${currentEventId}/reminders`, { method: 'POST' });
    showToast(res.status === 204 ? 'Reminders Sent' : 'Error', res.status === 204 ? 'Logged.' : JSON.stringify(res.body), res.status !== 204);
});
document.getElementById('inlinePollForm').addEventListener('submit', async e => {
    e.preventDefault();
    if (!currentEventId) return;
    const questions = document.getElementById('ipQuestions').value.trim().split('\n').filter(l => l.trim()).map(line => {
        const p = line.split('|').map(s => s.trim()); return { Question: p[0], Options: p.slice(1) };
    });
    const res = await api(`/api/polls/${currentEventId}`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ Name: document.getElementById('ipName').value, Description: document.getElementById('ipDesc').value, Questions: questions })
    });
    if (res.status === 201) {
        showToast('Poll Created', document.getElementById('ipName').value);
        document.getElementById('inlinePollForm').reset();
        const evRes = await api(`/api/events/${currentEventId}`);
        if (evRes.status === 200) renderPollsTab(evRes.body.polls || []);
    } else { showToast('Error', JSON.stringify(res.body), true); }
});
document.getElementById('paymentForm').addEventListener('submit', async e => {
    e.preventDefault();
    if (!currentEventId) return;
    const res = await api(`/api/finance/${currentEventId}/payment/${document.getElementById('payParticipantId').value}?amount=${document.getElementById('payAmount').value}`, { method: 'POST' });
    if (res.status === 204) { showToast('Payment Registered', 'Marked as paid'); document.getElementById('paymentForm').reset(); openEventDetail(currentEventId); }
    else showToast('Error', JSON.stringify(res.body), true);
});
document.getElementById('vendorForm').addEventListener('submit', async e => {
    e.preventDefault();
    if (!currentEventId) return;
    const res = await api(`/api/finance/${currentEventId}/vendor-debt?vendorName=${encodeURIComponent(document.getElementById('vendorName').value)}&amount=${document.getElementById('vendorAmount').value}`, { method: 'POST' });
    if (res.status === 204) { showToast('Vendor Added', document.getElementById('vendorName').value); document.getElementById('vendorForm').reset(); openEventDetail(currentEventId); }
    else showToast('Error', JSON.stringify(res.body), true);
});
document.getElementById('inlineUploadForm').addEventListener('submit', async e => {
    e.preventDefault();
    if (!currentEventId) return;
    const fd = new FormData();
    fd.append('file', document.getElementById('iuFile').files[0]);
    fd.append('receiptNumber', document.getElementById('iuNumber').value);
    fd.append('amount', document.getElementById('iuAmount').value);
    const res = await fetch(`/api/finance/${currentEventId}/vendors/${encodeURIComponent(document.getElementById('iuVendor').value)}/receipts`, { method: 'POST', body: fd, credentials: 'include' });
    if (res.ok) { showToast('Receipt Uploaded', document.getElementById('iuVendor').value); document.getElementById('inlineUploadForm').reset(); }
    else showToast('Upload Failed', await res.text(), true);
});
document.getElementById('btnPayReminders').addEventListener('click', async () => {
    if (!currentEventId) return;
    const res = await api(`/api/finance/${currentEventId}/payment-reminders`, { method: 'POST' });
    showToast(res.status === 204 ? 'Reminders Sent' : 'Error', res.status === 204 ? 'Logged.' : JSON.stringify(res.body), res.status !== 204);
});
document.getElementById('hostForm').addEventListener('submit', async e => {
    e.preventDefault();
    if (!currentEventId) return;
    const res = await api(`/api/events/${currentEventId}/host`, {
        method: 'POST', headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ Name: document.getElementById('hostName').value, Email: document.getElementById('hostEmail').value })
    });
    if (res.status === 200) { showToast('Host Set', res.body.name); document.getElementById('hostForm').reset(); renderHostTab(res.body); }
    else showToast('Error', JSON.stringify(res.body), true);
});

// ── Participants panel ──────────────────────────────────────────────────────
async function loadAllParticipants() {
    const eid = document.getElementById('participantEventFilter').value;
    const res = await api(eid ? `/api/events/${eid}/participants` : '/api/participants');
    const ps  = res.status === 200 ? res.body : [];
    const el  = document.getElementById('allParticipantsTable');
    if (!ps.length) { el.innerHTML = '<div class="gu-empty"><i class="bi bi-person-x"></i>No participants found.</div>'; return; }
    el.innerHTML = `<div class="gu-card"><table class="gu-table">
      <thead><tr><th>ID</th><th>Name</th><th>Email</th><th>Attendance</th><th>Payment</th><th>Amount</th><th>Mail Prefs</th></tr></thead>
      <tbody>${ps.map(p => `<tr><td class="text-muted small">${p.id}</td><td class="fw-semibold">${p.name}</td>
        <td class="text-muted small">${p.email}</td><td>${attendanceBadge(p.isAttending)}</td>
        <td>${paidBadge(p.hasPaid)}</td><td>${fmt(p.amountContributed)}</td>
        <td><span class="text-muted small">${p.mailingPreferences || '—'}</span></td></tr>`).join('')}
      </tbody></table></div>`;
}
document.getElementById('participantEventFilter').addEventListener('change', loadAllParticipants);
document.getElementById('btnAddParticipant').addEventListener('click', () => {
    document.getElementById('addParticipantCard').classList.remove('d-none'); populateEventSelects();
});
document.getElementById('apCancelBtn').addEventListener('click', () =>
    document.getElementById('addParticipantCard').classList.add('d-none'));
document.getElementById('addParticipantForm').addEventListener('submit', async e => {
    e.preventDefault();
    const fd = new FormData();
    fd.append('name', document.getElementById('apName').value);
    fd.append('email', document.getElementById('apEmail').value);
    fd.append('password', document.getElementById('apPassword').value);
    fd.append('eventId', document.getElementById('apEventId').value);
    const res = await fetch('/auth/register/participant', { method: 'POST', body: fd, credentials: 'include' });
    if (res.ok) {
        showToast('Participant Registered', document.getElementById('apEmail').value);
        document.getElementById('addParticipantCard').classList.add('d-none');
        document.getElementById('addParticipantForm').reset();
        await loadEvents(); loadAllParticipants();
    } else { showToast('Registration Failed', await res.text(), true); }
});

// ── Polls panel ─────────────────────────────────────────────────────────────
function populatePollEventFilter() {
    document.getElementById('pollEventFilter').innerHTML = '<option value="">Select an event...</option>' +
        allEvents.map(e => `<option value="${e.id}">${e.name}</option>`).join('');
}
document.getElementById('pollEventFilter').addEventListener('change', async function () {
    const eid = this.value;
    const grid = document.getElementById('pollsGrid');
    if (!eid) { grid.innerHTML = ''; return; }
    const res = await api(`/api/events/${eid}/polls`);
    if (res.status !== 200 || !res.body.length) {
        grid.innerHTML = '<div class="col-12"><div class="gu-empty"><i class="bi bi-bar-chart"></i>No polls for this event.</div></div>'; return;
    }
    grid.innerHTML = res.body.map(p => `<div class="col-md-6"><div class="gu-poll-card" id="pollCard${p.id}">
      <div class="d-flex justify-content-between align-items-start mb-3">
        <div><h6 class="mb-0">${p.name}</h6><p class="text-muted small mb-0">${p.description || ''}</p></div>
        <button class="btn btn-sm btn-outline-primary" onclick="loadPollResults(${p.id})">Results</button>
      </div><div id="pollResults${p.id}"></div>
    </div></div>`).join('');
});

async function showPollInPanel(pollId) {
    showPanel('polls');
    populatePollEventFilter();
    setTimeout(() => loadPollResults(pollId), 100);
}

async function loadPollResults(pollId) {
    const res = await api(`/api/polls/${pollId}/results`);
    const container = document.getElementById(`pollResults${pollId}`);
    if (!container || res.status !== 200) return;
    const body = res.body;
    if (!body?.poll?.questions?.length) { container.innerHTML = '<p class="text-muted small">No questions.</p>'; return; }
    const isParticipant = currentUser?.role !== 'Manager';
    const myId = currentUser?.linkedId;
    container.innerHTML = body.poll.questions.map(q => {
        const total = (q.participantChoices || []).length;
        const myChoice = (q.participantChoices || []).find(c => c.participantId === myId)?.choice;
        const opts = (q.choiceOptions || []).map(opt => {
            const count = (q.participantChoices || []).filter(c => c.choice === opt).length;
            const pct = total === 0 ? 0 : Math.round(count / total * 1000) / 10;
            const isMine = opt === myChoice;
            return `<div class="gu-vote-option${isMine ? ' my-vote' : ''}" data-poll="${pollId}" data-qid="${q.id}" data-choice="${opt}">
              <span>${opt}${isMine ? ' <i class="bi bi-check-circle-fill text-primary ms-1"></i>' : ''}</span>
              <div class="d-flex align-items-center gap-2" style="min-width:110px">
                <div class="gu-progress"><div class="gu-progress-bar" style="width:${pct}%"></div></div>
                <span class="text-muted small">${count} (${pct}%)</span></div></div>`;
        }).join('');
        return `<div class="mb-3"><p class="fw-semibold mb-2">${q.questionContent}</p>${opts}</div>`;
    }).join('');
    if (isParticipant) {
        container.querySelectorAll('.gu-vote-option').forEach(opt =>
            opt.addEventListener('click', async () => {
                const r = await api(`/api/polls/${opt.dataset.poll}/vote`, {
                    method: 'POST', headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({ QuestionId: Number(opt.dataset.qid), ParticipantId: myId, Choice: opt.dataset.choice })
                });
                if (r.status === 204) { showToast('Vote Cast', opt.dataset.choice); loadPollResults(pollId); }
                else showToast('Error', JSON.stringify(r.body), true);
            }));
    }
}

// ── Finance panel ───────────────────────────────────────────────────────────
function populateFinanceEventFilter() {
    document.getElementById('financeEventFilter').innerHTML = '<option value="">Select an event...</option>' +
        allEvents.map(e => `<option value="${e.id}">${e.name}</option>`).join('');
}
document.getElementById('financeEventFilter').addEventListener('change', async function () {
    const eid = this.value;
    if (!eid) return;
    const res = await api(`/api/finance/${eid}/summary`);
    if (res.status !== 200) { showToast('Error', 'Could not load finance', true); return; }
    const f = res.body;
    document.getElementById('kpiIncome').textContent    = fmt(f.totalIncome);
    document.getElementById('kpiExpenses').textContent  = fmt(f.totalExpenses);
    document.getElementById('kpiBalance').textContent   = fmt(f.totalIncome - f.totalExpenses);
    document.getElementById('kpiPaidCount').textContent = (f.paidParticipants || []).length;
    const paidList = f.paidParticipants || [];
    document.getElementById('paymentStatusTable').innerHTML = paidList.length
        ? `<table class="gu-table"><thead><tr><th>Name</th><th>Paid</th></tr></thead>
           <tbody>${paidList.map(p => `<tr><td class="fw-semibold">${p.name ?? p.item1 ?? '—'}</td><td class="text-success">${fmt(p.amount ?? p.item2)}</td></tr>`).join('')}</tbody></table>`
        : '<div class="gu-empty"><i class="bi bi-people"></i>No payments yet.</div>';
    const vendors = f.vendors || [];
    document.getElementById('vendorsTable').innerHTML = vendors.length
        ? `<table class="gu-table"><thead><tr><th>Vendor</th><th>Owed</th></tr></thead>
           <tbody>${vendors.map(v => `<tr><td class="fw-semibold">${v.vendorName ?? v.item1 ?? '—'}</td><td class="text-danger">${fmt(v.amountOwed ?? v.item2)}</td></tr>`).join('')}</tbody></table>`
        : '<div class="gu-empty"><i class="bi bi-shop"></i>No vendors.</div>';
    const ctx = document.getElementById('financeChart');
    if (financeChart) { financeChart.destroy(); financeChart = null; }
    financeChart = new Chart(ctx, {
        type: 'doughnut',
        data: { labels: ['Income', 'Expenses'], datasets: [{ data: [f.totalIncome || 0, f.totalExpenses || 0], backgroundColor: ['#22c55e', '#ef4444'], borderWidth: 0 }] },
        options: { responsive: true, plugins: { legend: { position: 'bottom' } } }
    });
});
