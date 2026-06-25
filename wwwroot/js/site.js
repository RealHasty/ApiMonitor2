// -------------------------------------------------------
// Live connection test — called by the Test button in Index
// Updates the card's status badge, HTTP code, and response
// time without a full page reload.
// -------------------------------------------------------

async function testApi(id, btn) {
    const originalText = btn.textContent;
    btn.textContent = 'Testing…';
    btn.disabled = true;

    try {
        // POST /Api/Test/{id}  — returns JSON from ApiController.Test()
        const res = await fetch(`/Api/Test/${id}`, {
            method: 'POST',
            headers: {
                'RequestVerificationToken': getAntiForgeryToken()
            }
        });

        if (!res.ok) throw new Error(`HTTP ${res.status}`);

        const data = await res.json();

        // Update status badge
        const badge = document.getElementById(`status-${id}`);
        if (badge) {
            badge.className = `status-badge ${data.isRunning ? 'status-up' : 'status-down'}`;
            badge.textContent = data.isRunning ? '● Online' : '● Offline';
        }

        // Update meta items
        const codeEl = document.getElementById(`code-${id}`);
        if (codeEl) codeEl.textContent = `HTTP ${data.statusCode || '—'}`;

        const timeEl = document.getElementById(`time-${id}`);
        if (timeEl) timeEl.textContent = data.responseTimeMs != null ? `${data.responseTimeMs}ms` : '—';

    } catch (err) {
        console.error('Test failed:', err);
        alert('Connection test failed. Check the console.');
    } finally {
        btn.textContent = originalText;
        btn.disabled = false;
    }
}

// Grab the ASP.NET Core anti-forgery token from the page
function getAntiForgeryToken() {
    const el = document.querySelector('input[name="__RequestVerificationToken"]');
    return el ? el.value : '';
}
