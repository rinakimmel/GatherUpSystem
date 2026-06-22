async function api(path, opts = {}) {
    opts.headers = opts.headers || {};

    // שימוש בנתיב יחסי רגיל - השרת מזהה את הפורט של עצמו אוטומטית
    const res = await fetch(path, opts);
    const text = await res.text();
    let body;
    try { body = JSON.parse(text); } catch { body = text; }
    return { status: res.status, body };
}

// Create Poll
document.getElementById('createPollForm').addEventListener('submit', async e => {
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

    document.getElementById('createPollResult').textContent = JSON.stringify(res, null, 2);
});

// Vote
document.getElementById('voteForm').addEventListener('submit', async e => {
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

    document.getElementById('voteResult').textContent = JSON.stringify(res, null, 2);
});

// Results
document.getElementById('resultsForm').addEventListener('submit', async e => {
    e.preventDefault();
    const pollId = document.getElementById('resultsPollId').value;
    const res = await api(`/api/polls/${pollId}/results`);

    document.getElementById('resultsOutput').textContent = JSON.stringify(res.body, null, 2);
});

// Upload receipt
document.getElementById('uploadForm').addEventListener('submit', async e => {
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

    // הוחזר לנתיב יחסי תקין עבור העלאת הקבצים
    const res = await fetch(`/api/finance/${eventId}/vendors/${vendor}/receipts`, {
        method: 'POST',
        body: form
    });

    const body = await res.text();
    document.getElementById('uploadResult').textContent = `${res.status}\n${body}`;
});