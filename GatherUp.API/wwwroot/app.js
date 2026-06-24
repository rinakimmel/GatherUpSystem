console.debug = console.debug || console.log;

async function api(path, opts = {}) {
    opts.headers = opts.headers || {};
    opts.credentials = opts.credentials || 'include';

    console.debug('api request', path, opts.method || 'GET');
    const res = await fetch(path, opts);
    const text = await res.text();
    let body;
    try { body = JSON.parse(text); } catch { body = text; }
    console.debug('api response', path, res.status, body);
    return { status: res.status, body };
}

function showToast(title, message, isError = false) {
    const id = 't' + Math.random().toString(36).slice(2);
    const container = document.getElementById('toastContainer');
    if (!container) return;
    const el = document.createElement('div');
    el.className = 'toast align-items-center text-bg-' + (isError ? 'danger' : 'success') + ' border-0';
    el.role = 'alert';
    el.ariaLive = 'assertive';
    el.ariaAtomic = 'true';
    el.id = id;
    el.innerHTML = `<div class="d-flex"><div class="toast-body"><strong>${title}</strong><div class="small">${message}</div></div><button type="button" class="btn-close btn-close-white me-2 m-auto" data-bs-dismiss="toast" aria-label="Close"></button></div>`;
    container.appendChild(el);
    const toast = new bootstrap.Toast(el, { delay: 4000 });
    toast.show();
}

function appendActivity(text) {
    const log = document.getElementById('activityLog');
    if (!log) return;
    const time = new Date().toLocaleTimeString();
    log.innerHTML = `<div>[${time}] ${text}</div>` + log.innerHTML;
}

function setRoleControls(role) {
    const isManager = role === 'Manager';
    const createBtn = document.querySelector('#createPollForm button[type="submit"]');
    const uploadBtn = document.querySelector('#uploadForm button[type="submit"]');
    if (createBtn) createBtn.disabled = !isManager;
    if (uploadBtn) uploadBtn.disabled = !isManager;

    const regBtn = document.querySelector('#registerForm button[type="submit"]');
    if (regBtn) regBtn.disabled = !isManager;

    // הצג/הסתר sections בהתאם לתפקיד
    const managerSection = document.getElementById('managerSection');
    const participantSection = document.getElementById('participantSection');
    if (managerSection) managerSection.style.display = isManager ? 'block' : 'none';
    if (participantSection) participantSection.style.display = isManager ? 'none' : 'block';
}

function applyLoginState(user) {
    console.debug('applyLoginState', user);
    if (!user) return;
    const authStatusEl = document.getElementById('authStatus');
    if (authStatusEl) authStatusEl.textContent = `Logged in as ${user.email} (${user.role})`;
    const logoutBtn = document.getElementById('logoutBtn');
    if (logoutBtn) logoutBtn.style.display = 'inline-block';
    const loginView = document.getElementById('loginView');
    const appView = document.getElementById('appView');
    if (loginView) loginView.style.display = 'none';
    if (appView) appView.style.display = 'block';
    const welcome = document.getElementById('welcome');
    if (welcome) welcome.textContent = `Welcome, ${user.email}`;
    const roleInfo = document.getElementById('roleInfo');
    if (roleInfo) roleInfo.textContent = `Role: ${user.role}`;
    setRoleControls(user.role);
    appendActivity(`Logged in as ${user.email} (${user.role})`);

    // הוסף: טען נתונים בעת כניסה
    loadInitialDashboardData(user.role);
}

// פונקציה חדשה: טען נתונים בתקווה בעת כניסה
async function loadInitialDashboardData(role) {
    console.debug('loadInitialDashboardData for role', role);

    // טען תוצאות סקר דמו (Poll ID 1 הוא default בדמו)
    try {
        const pollRes = await api('/api/polls/1/results');
        if (pollRes.status === 200) {
            console.debug('Loaded poll results:', pollRes.body);
            renderPollResults(pollRes.body);
            appendActivity('Loaded poll results from dashboard');
        } else {
            console.debug('Poll results not available:', pollRes.status);
        }
    } catch (err) {
        console.error('Error loading poll results:', err);
    }

    // טען סיכום כספי (Event 1 הוא default בדמו)
    if (role === 'Manager') {
        try {
            const finRes = await api('/api/finance/1/summary');
            if (finRes.status === 200) {
                console.debug('Loaded finance summary:', finRes.body);
                appendActivity('Loaded finance summary');
                // הצג סיכום בקונסולה או ב־activity log
                appendActivity(`Finance: Total Income: ${finRes.body.totalIncome}, Total Expenses: ${finRes.body.totalExpenses}`);
            }
        } catch (err) {
            console.error('Error loading finance summary:', err);
        }
    }
}

async function refreshAuthStatus() {
    console.debug('refreshAuthStatus: start');
    const appView = document.getElementById('appView');
    const loginView = document.getElementById('loginView');
    // show loading placeholder in appView while checking
    if (appView) {
        appView.style.display = 'block';
        const loading = document.getElementById('loadingPlaceholder');
        if (!loading) {
            const ph = document.createElement('div');
            ph.id = 'loadingPlaceholder';
            ph.className = 'text-center my-4 text-muted';
            ph.textContent = 'Loading...';
            appView.insertBefore(ph, appView.firstChild);
        }
    }

    try {
        const res = await fetch('/auth/me', { credentials: 'include' });
        console.debug('/auth/me status', res.status);
        if (res.ok) {
            const j = await res.json();
            // remove loading
            const loading = document.getElementById('loadingPlaceholder');
            if (loading && loading.parentNode) loading.parentNode.removeChild(loading);
            applyLoginState(j);
            return;
        }
    } catch (err) {
        console.error('refreshAuthStatus error', err);
    }
    // remove loading
    const loading = document.getElementById('loadingPlaceholder');
    if (loading && loading.parentNode) loading.parentNode.removeChild(loading);

    const authStatusEl = document.getElementById('authStatus');
    if (authStatusEl) authStatusEl.textContent = 'Not logged in';
    const logoutBtn = document.getElementById('logoutBtn');
    if (logoutBtn) logoutBtn.style.display = 'none';
    if (loginView) loginView.style.display = 'block';
    if (appView) appView.style.display = 'none';
}

let pollChart = null;

function renderPollResults(body) {
    const out = document.getElementById('resultsOutput');
    if (!out) return;
    if (!body || !body.poll || !body.poll.questions) {
        out.textContent = 'No results available';
        const ctx = document.getElementById('resultsChart');
        if (ctx && pollChart) { pollChart.destroy(); pollChart = null; }
        return;
    }

    // Build readable text as before
    let s = `Poll: ${body.poll.name}\n${body.poll.description || ''}\n\n`;
    for (const q of body.poll.questions) {
        s += `Question: ${q.questionContent}\n`;
        const total = (q.participantChoices || []).length;
        for (const opt of (q.choiceOptions || [])) {
            const count = (q.participantChoices || []).filter(c => c.choice === opt).length;
            const pct = total === 0 ? 0 : Math.round((count / total) * 1000) / 10; // 1 decimal
            s += `  - ${opt}: ${count} votes (${pct}%)\n`;
        }
        s += '\n';
    }
    out.textContent = s;

    // Build chart for first question only (common case)
    const q0 = body.poll.questions[0];
    if (q0) {
        const labels = (q0.choiceOptions || []);
        const data = labels.map(opt => (q0.participantChoices || []).filter(c => c.choice === opt).length);

        const ctx = document.getElementById('resultsChart');
        if (!ctx) return;
        if (pollChart) pollChart.destroy();
        pollChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: `${body.poll.name} - ${q0.questionContent}`,
                    data: data,
                    backgroundColor: labels.map(() => 'rgba(54, 162, 235, 0.6)'),
                    borderColor: labels.map(() => 'rgba(54, 162, 235, 1)'),
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                scales: { y: { beginAtZero: true, precision: 0 } }
            }
        });
    }
}

// call on load
window.addEventListener('load', refreshAuthStatus);

// Register participant
async function handleRegister(e) {
    e.preventDefault();
    const form = new FormData();
    const regName = document.getElementById('regName');
    const regEmail = document.getElementById('regEmail');
    const regPassword = document.getElementById('regPassword');
    const regEventId = document.getElementById('regEventId');
    if (!regName || !regEmail || !regPassword || !regEventId) {
        showToast('Register Failed', 'Registration form not available', true);
        return;
    }
    form.append('name', regName.value);
    form.append('email', regEmail.value);
    form.append('password', regPassword.value);
    form.append('eventId', regEventId.value);

    const res = await fetch('/auth/register/participant', { method: 'POST', body: form, credentials: 'include' });
    const text = await res.text();
    const registerResultEl = document.getElementById('registerResult');
    if (registerResultEl) registerResultEl.textContent = `${res.status}\n${text}`;
    if (res.ok) {
        let email = '';
        try { email = JSON.parse(text).email; } catch {}
        showToast('Participant Registered', `Email: ${email}`);
        appendActivity(`Registered participant ${email}`);
    } else {
        showToast('Failed to Register', text, true);
        appendActivity(`Failed to register participant: ${text}`);
    }
    await refreshAuthStatus();
}

document.getElementById('registerForm')?.addEventListener('submit', handleRegister);

// Login
const loginForm = document.getElementById('loginForm');
if (loginForm) {
    loginForm.addEventListener('submit', async e => {
        e.preventDefault();
        const form = new FormData();
        const loginEmail = document.getElementById('loginEmail');
        const loginPassword = document.getElementById('loginPassword');
        if (!loginEmail || !loginPassword) return;
        form.append('email', loginEmail.value);
        form.append('password', loginPassword.value);

        const res = await fetch('/auth/login', { method: 'POST', body: form, credentials: 'include' });
        const text = await res.text();
        const loginResultEl = document.getElementById('loginResult');
        if (loginResultEl) loginResultEl.textContent = `${res.status}\n${text}`;
        if (res.ok) {
            const j = JSON.parse(text);
            applyLoginState(j);
            showToast('Logged in', j.email);
        } else {
            showToast('Login Failed', text, true);
        }
        await refreshAuthStatus();
        // don't reload the page here - keep client state from applyLoginState
        // if (res.ok) window.location.reload();
    });
}

// Logout
const logoutBtn = document.getElementById('logoutBtn');
if (logoutBtn) {
    logoutBtn.addEventListener('click', async e => {
        e.preventDefault();
        const res = await fetch('/auth/logout', { method: 'POST', credentials: 'include' });
        await res.text();
        appendActivity('Logged out');
        await refreshAuthStatus();
    });
}

// Create Poll
const createPollForm = document.getElementById('createPollForm');
if (createPollForm) {
    createPollForm.addEventListener('submit', async e => {
        e.preventDefault();
        const eventId = document.getElementById('pollEventId').value;
        const name = document.getElementById('pollName').value;
        const desc = document.getElementById('pollDescription').value;
        const raw = document.getElementById('pollQuestions').value.trim().split('\n');
        const questions = raw.map(line => {
            const parts = line.split('|').map(p => p.trim());
            return { Question: parts[0], Options: parts.slice(1) };
        });

        const res = await api(`/api/polls/${eventId}`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ Name: name, Description: desc, Questions: questions })
        });

        if (res.status >= 200 && res.status < 300) {
            showToast('Poll Created', `Poll: ${res.body?.name ?? ''}`);
            appendActivity(`Created poll for event ${eventId}`);
            const createPollResultEl = document.getElementById('createPollResult');
            if (createPollResultEl) createPollResultEl.textContent = `Created: ${res.body?.name ?? JSON.stringify(res.body)}`;
        } else {
            showToast('Create Poll Failed', JSON.stringify(res.body), true);
            appendActivity(`Create poll failed: ${JSON.stringify(res.body)}`);
            const createPollResultEl = document.getElementById('createPollResult');
            if (createPollResultEl) createPollResultEl.textContent = JSON.stringify(res, null, 2);
        }
    });
}

// Vote
const voteForm = document.getElementById('voteForm');
if (voteForm) {
    voteForm.addEventListener('submit', async e => {
        e.preventDefault();
        const pollId = document.getElementById('votePollId').value;
        const qid = document.getElementById('voteQuestionId').value;
        const pid = document.getElementById('voteParticipantId').value;
        const choice = document.getElementById('voteChoice').value;

        const res = await api(`/api/polls/${pollId}/vote`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ QuestionId: parseInt(qid), ParticipantId: parseInt(pid), Choice: choice })
        });
ז
        const voteResultEl = document.getElementById('voteResult');
        if (res.status === 204) {
            showToast('Vote Submitted', `Poll ${pollId}`);
            appendActivity(`Vote submitted by participant ${pid} on poll ${pollId}`);
            if (voteResultEl) voteResultEl.textContent = 'Vote recorded';
            const results = await api(`/api/polls/${pollId}/results`);
            if (results.status === 200) {
                renderPollResults(results.body);
                appendActivity(`Fetched results for poll ${pollId}`);
                showToast('Results Updated', `Poll ${pollId}`);
            }
        } else {
            showToast('Vote Failed', JSON.stringify(res.body), true);
            appendActivity(`Vote failed: ${JSON.stringify(res.body)}`);
            if (voteResultEl) voteResultEl.textContent = JSON.stringify(res, null, 2);
        }
    });
}

// Results
const resultsForm = document.getElementById('resultsForm');
if (resultsForm) {
    resultsForm.addEventListener('submit', async e => {
        e.preventDefault();
        const pollId = document.getElementById('resultsPollId').value;
        const res = await api(`/api/polls/${pollId}/results`);

        if (res.status === 200) {
            renderPollResults(res.body);
            appendActivity(`Fetched results for poll ${pollId}`);
        } else {
            const resultsOutput = document.getElementById('resultsOutput');
            if (resultsOutput) resultsOutput.textContent = JSON.stringify(res, null, 2);
        }
    });
}

// Upload receipt
const uploadForm = document.getElementById('uploadForm');
if (uploadForm) {
    uploadForm.addEventListener('submit', async e => {
        e.preventDefault();
        const eventId = document.getElementById('uploadEventId').value;
        const vendor = encodeURIComponent(document.getElementById('uploadVendor').value);
        const receiptNumber = document.getElementById('uploadNumber').value;
        const amount = document.getElementById('uploadAmount').value;
        const file = document.getElementById('uploadFile').files[0];

        const form = new FormData();
        form.append('file', file);
        form.append('receiptNumber', receiptNumber);
        form.append('amount', amount);

        const res = await fetch(`/api/finance/${eventId}/vendors/${vendor}/receipts`, {
            method: 'POST',
            body: form,
            credentials: 'include'
        });

        const body = await res.text();
        const uploadResultEl = document.getElementById('uploadResult');
        if (uploadResultEl) uploadResultEl.textContent = `${res.status}\n${body}`;
        if (res.ok) {
            showToast('Receipt Uploaded', `Vendor: ${decodeURIComponent(vendor)}`);
            appendActivity(`Uploaded receipt ${receiptNumber} to ${decodeURIComponent(vendor)}`);
        } else {
            showToast('Upload Failed', body, true);
            appendActivity(`Upload failed: ${body}`);
        }
    });
}