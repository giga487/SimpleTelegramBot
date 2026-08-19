(() => {
    const initialize = () => {
        initializeSendForm();
        initializeHistory();
    };

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', initialize);
    } else {
        initialize();
    }

    document.addEventListener('enhancedload', initialize);

    function initializeSendForm() {
        const form = document.querySelector('[data-telegram-send-form]');

        if (!form || form.dataset.initialized === 'true') {
            return;
        }

        form.dataset.initialized = 'true';
        const status = form.querySelector('[data-send-status]');

        form.addEventListener('submit', async event => {
            event.preventDefault();
            setStatus(status, 'Invio in corso...', 'info');

            const submitButton = form.querySelector('button[type="submit"]');
            submitButton.disabled = true;

            try {
                const payload = {
                    caller: form.elements.caller.value.trim(),
                    message: form.elements.message.value.trim()
                };

                const response = await fetch(form.dataset.apiUrl, {
                    method: 'POST',
                    headers: {
                        'Content-Type': 'application/json'
                    },
                    body: JSON.stringify(payload)
                });

                if (!response.ok) {
                    throw new Error(await readError(response));
                }

                const result = await response.json();
                form.elements.message.value = '';
                setStatus(status, `Messaggio inviato. Request id: ${result.requestId}`, 'success');
            } catch (error) {
                setStatus(status, error.message, 'danger');
            } finally {
                submitButton.disabled = false;
            }
        });
    }

    function initializeHistory() {
        const root = document.querySelector('[data-telegram-history]');

        if (!root || root.dataset.initialized === 'true') {
            return;
        }

        root.dataset.initialized = 'true';
        const status = root.querySelector('[data-history-status]');
        const summary = root.querySelector('[data-history-summary]');
        const groups = root.querySelector('[data-history-groups]');
        const takeInput = root.querySelector('[data-history-take]');
        const callerInput = root.querySelector('[data-history-caller]');

        root.querySelector('[data-history-refresh]').addEventListener('click', loadHistory);
        root.querySelector('[data-history-clear]').addEventListener('click', async () => {
            if (!confirm('Vuoi cancellare tutta la memoria delle richieste?')) {
                return;
            }

            setStatus(status, 'Cancellazione in corso...', 'info');

            try {
                const response = await fetch(root.dataset.apiUrl, { method: 'DELETE' });

                if (!response.ok) {
                    throw new Error(await readError(response));
                }

                setStatus(status, 'Memoria cancellata.', 'success');
                await loadHistory();
            } catch (error) {
                setStatus(status, error.message, 'danger');
            }
        });

        loadHistory();

        async function loadHistory() {
            setStatus(status, 'Caricamento in corso...', 'info');

            try {
                const url = new URL(root.dataset.apiUrl, window.location.origin);
                const take = takeInput.value || '20';
                const caller = callerInput.value.trim();

                url.searchParams.set('take', take);

                if (caller.length > 0) {
                    url.searchParams.set('caller', caller);
                }

                const response = await fetch(url);

                if (!response.ok) {
                    throw new Error(await readError(response));
                }

                const snapshot = await response.json();
                renderSummary(summary, snapshot);
                renderGroups(groups, snapshot.callers);
                setStatus(status, 'Riepilogo aggiornato.', 'success');
            } catch (error) {
                setStatus(status, error.message, 'danger');
            }
        }
    }

    async function readError(response) {
        try {
            const problem = await response.json();

            if (problem.errors) {
                return Object.values(problem.errors).flat().join(' ');
            }

            return problem.detail || problem.title || `Errore HTTP ${response.status}`;
        } catch {
            return `Errore HTTP ${response.status}`;
        }
    }

    function setStatus(element, message, kind) {
        element.className = `telegram-status mt-3 alert alert-${kind}`;
        element.textContent = message;
    }

    function renderSummary(container, snapshot) {
        container.replaceChildren();

        const alert = document.createElement('div');
        alert.className = 'alert alert-secondary';
        alert.textContent = `Memoria totale: ${snapshot.totalStored}. Richieste corrispondenti: ${snapshot.matchingStored}. Mostrate: ${snapshot.requests.length}.`;
        container.appendChild(alert);
    }

    function renderGroups(container, callers) {
        container.replaceChildren();

        if (!callers || callers.length === 0) {
            const empty = document.createElement('p');
            empty.className = 'text-muted';
            empty.textContent = 'Nessuna richiesta in memoria.';
            container.appendChild(empty);
            return;
        }

        callers.forEach(group => {
            const card = document.createElement('article');
            card.className = 'card mb-3';

            const header = document.createElement('div');
            header.className = 'card-header d-flex justify-content-between align-items-center';

            const title = document.createElement('strong');
            title.textContent = group.caller;

            const count = document.createElement('span');
            count.className = 'badge text-bg-primary';
            count.textContent = `${group.count} richieste`;

            header.append(title, count);
            card.appendChild(header);

            const body = document.createElement('div');
            body.className = 'card-body p-0';
            body.appendChild(createRequestsTable(group.requests));
            card.appendChild(body);

            container.appendChild(card);
        });
    }

    function createRequestsTable(requests) {
        const table = document.createElement('table');
        table.className = 'table table-sm table-striped mb-0 telegram-history-table';

        const thead = document.createElement('thead');
        const headerRow = document.createElement('tr');
        ['Data', 'Stato', 'Messaggio', 'Errore'].forEach(text => {
            const th = document.createElement('th');
            th.scope = 'col';
            th.textContent = text;
            headerRow.appendChild(th);
        });
        thead.appendChild(headerRow);
        table.appendChild(thead);

        const tbody = document.createElement('tbody');
        requests.forEach(request => {
            const row = document.createElement('tr');
            appendCell(row, new Date(request.requestedAt).toLocaleString());
            appendCell(row, request.sent ? 'Inviato' : 'Fallito');
            appendCell(row, request.message);
            appendCell(row, request.error || '');
            tbody.appendChild(row);
        });
        table.appendChild(tbody);

        return table;
    }

    function appendCell(row, text) {
        const td = document.createElement('td');
        td.textContent = text;
        row.appendChild(td);
    }
})();
