// NetPulse Core UI Logic
const API_BASE = '/api/v1';

async function fetchOverview() {
    try {
        const res = await fetch(`${API_BASE}/overview`);
        if (!res.ok) return;
        const result = await res.json();
        if (result.success && result.data) {
            const data = result.data;
            document.getElementById('nodesCountVal').innerText = data.totalNodes;
            document.getElementById('jobsCountVal').innerText = data.totalJobsProcessed + data.activeJobsRunning;
            document.getElementById('uptimeVal').innerText = `Uptime: ${data.processUptime}`;
        }
    } catch (e) {
        console.error('Overview fetch error:', e);
    }
}

async function fetchTelemetry() {
    try {
        const res = await fetch(`${API_BASE}/telemetry/live`);
        if (!res.ok) return;
        const result = await res.json();
        if (result.success && result.data) {
            const t = result.data;
            document.getElementById('cpuVal').innerText = `${t.processCpuPercent}%`;
            document.getElementById('cpuBar').style.width = `${Math.min(100, Math.max(5, t.processCpuPercent))}%`;
            document.getElementById('memVal').innerText = `${t.workingSetMemoryMb} MB`;
            document.getElementById('gcDetails').innerText = `GC Gen0: ${t.gcGen0Collections} | Gen1: ${t.gcGen1Collections} | Gen2: ${t.gcGen2Collections}`;
            document.getElementById('cpuDetails').innerText = `Threads: ${t.threadCount} | Machine: ${t.machineName}`;

            document.getElementById('diagOs').innerText = t.osDescription;
            document.getElementById('diagFramework').innerText = t.frameworkDescription;
            document.getElementById('diagMachine').innerText = t.machineName;
            document.getElementById('diagThreads').innerText = `Worker: ${t.availableWorkerThreads} | I/O: ${t.availableIoThreads}`;
        }
    } catch (e) {
        console.error('Telemetry fetch error:', e);
    }
}

async function fetchNodes() {
    try {
        const res = await fetch(`${API_BASE}/nodes`);
        if (!res.ok) return;
        const result = await res.json();
        const tbody = document.getElementById('nodesTableBody');
        if (!result.success || !result.data || result.data.length === 0) {
            tbody.innerHTML = '<tr><td colspan="7" class="text-center text-muted">No nodes registered in cluster mesh.</td></tr>';
            return;
        }

        tbody.innerHTML = result.data.map(n => `
            <tr>
                <td><code>${n.id}</code></td>
                <td><strong>${escapeHtml(n.nodeName)}</strong></td>
                <td><span class="tag tag-region">${escapeHtml(n.region)}</span></td>
                <td><span class="tag tag-workload">${escapeHtml(n.workloadType)}</span></td>
                <td><code>${n.loadAverage}</code></td>
                <td><span class="tag tag-status">${escapeHtml(n.status)}</span></td>
                <td>
                    <button class="btn-icon" title="Ping Heartbeat" onclick="pingNode('${n.id}')">💓</button>
                    <button class="btn-icon" title="Decommission" onclick="deleteNode('${n.id}')">🗑️</button>
                </td>
            </tr>
        `).join('');
    } catch (e) {
        console.error('Nodes fetch error:', e);
    }
}

async function fetchJobs() {
    try {
        const res = await fetch(`${API_BASE}/jobs`);
        if (!res.ok) return;
        const result = await res.json();
        const container = document.getElementById('jobsListContainer');
        if (!result.success || !result.data || result.data.length === 0) {
            container.innerHTML = '<div class="empty-state">No active jobs in queue.</div>';
            return;
        }

        container.innerHTML = result.data.map(j => `
            <div class="job-item">
                <div class="job-meta">
                    <span class="job-title">${escapeHtml(j.title)}</span>
                    <span class="job-status-badge ${j.status}">${j.status}</span>
                </div>
                <div class="job-progress-bg">
                    <div class="job-progress-bar" style="width: ${j.progressPercent}%"></div>
                </div>
                <div class="job-footer">
                    <span>Type: <strong>${j.jobType}</strong></span>
                    <span>${j.status === 'Completed' ? (j.result || 'Done') : `${j.progressPercent}% in progress...`}</span>
                </div>
            </div>
        `).join('');
    } catch (e) {
        console.error('Jobs fetch error:', e);
    }
}

async function pingNode(id) {
    try {
        await fetch(`${API_BASE}/nodes/${id}/heartbeat`, { method: 'POST' });
        fetchNodes();
    } catch (e) {
        console.error('Ping error:', e);
    }
}

async function deleteNode(id) {
    if (!confirm('Are you sure you want to decommission this node?')) return;
    try {
        await fetch(`${API_BASE}/nodes/${id}`, { method: 'DELETE' });
        fetchNodes();
        fetchOverview();
    } catch (e) {
        console.error('Delete error:', e);
    }
}

// SSE Live Stream Connection
function connectSSE() {
    const sse = new EventSource(`${API_BASE}/telemetry/stream`);
    const indicator = document.getElementById('streamIndicator');
    const statusText = document.getElementById('streamStatusText');

    sse.onopen = () => {
        indicator.className = 'stream-badge active';
        statusText.innerText = 'SSE Stream Live';
    };

    sse.onmessage = (e) => {
        try {
            const payload = JSON.parse(e.data);
            if (payload.eventType && payload.eventType.startsWith('job_')) {
                fetchJobs();
                fetchOverview();
            } else if (payload.eventType && payload.eventType.startsWith('node_')) {
                fetchNodes();
                fetchOverview();
            }
        } catch (err) {}
    };

    sse.onerror = () => {
        indicator.className = 'stream-badge';
        statusText.innerText = 'Reconnecting...';
        setTimeout(connectSSE, 5000);
    };
}

// Modals Setup
const nodeModal = document.getElementById('nodeModal');
const btnOpenNodeModal = document.getElementById('btnOpenNodeModal');
const btnCloseModal = document.getElementById('btnCloseModal');
const btnCancelModal = document.getElementById('btnCancelModal');
const nodeForm = document.getElementById('nodeForm');

btnOpenNodeModal.onclick = () => nodeModal.classList.add('open');
btnCloseModal.onclick = () => nodeModal.classList.remove('open');
btnCancelModal.onclick = () => nodeModal.classList.remove('open');

nodeForm.onsubmit = async (e) => {
    e.preventDefault();
    const payload = {
        nodeName: document.getElementById('nodeNameInput').value,
        region: document.getElementById('nodeRegionInput').value,
        workloadType: document.getElementById('nodeWorkloadInput').value,
        cpuCores: 8,
        totalMemoryMb: 16384
    };
    try {
        await fetch(`${API_BASE}/nodes`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        nodeModal.classList.remove('open');
        nodeForm.reset();
        fetchNodes();
        fetchOverview();
    } catch (err) {
        console.error(err);
    }
};

const jobModal = document.getElementById('jobModal');
const btnDispatchJob = document.getElementById('btnDispatchJob');
const btnCloseJobModal = document.getElementById('btnCloseJobModal');
const btnCancelJobModal = document.getElementById('btnCancelJobModal');
const jobForm = document.getElementById('jobForm');

btnDispatchJob.onclick = () => jobModal.classList.add('open');
btnCloseJobModal.onclick = () => jobModal.classList.remove('open');
btnCancelJobModal.onclick = () => jobModal.classList.remove('open');

jobForm.onsubmit = async (e) => {
    e.preventDefault();
    const payload = {
        title: document.getElementById('jobTitleInput').value,
        jobType: document.getElementById('jobTypeInput').value,
        estimatedDurationSec: parseInt(document.getElementById('jobDurationInput').value, 10) || 4
    };
    try {
        await fetch(`${API_BASE}/jobs`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(payload)
        });
        jobModal.classList.remove('open');
        jobForm.reset();
        fetchJobs();
        fetchOverview();
    } catch (err) {
        console.error(err);
    }
};

document.getElementById('btnRefreshDiagnostics').onclick = fetchTelemetry;

function escapeHtml(str) {
    if (!str) return '';
    return str.replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;').replace(/"/g, '&quot;');
}

// Init Poll Cycles
fetchOverview();
fetchTelemetry();
fetchNodes();
fetchJobs();
connectSSE();

setInterval(fetchTelemetry, 2500);
setInterval(fetchOverview, 6000);
