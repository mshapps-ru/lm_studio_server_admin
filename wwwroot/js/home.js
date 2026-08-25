// Home.js — логика вкладки Home

const Home = {
    _intervalId: null,

    init() {
        this.bindEvents();
        this.startPolling();
    },

    bindEvents() {
        document.getElementById('start-btn').addEventListener('click', () => this.startServer());
        document.getElementById('stop-btn').addEventListener('click', () => this.stopServer());
    },

    async getStatus() {
        const data = await App.apiFetch('/api/status');
        if (data) {
            this.updateStatus(data.status, data.message);
        }
    },

    updateStatus(status, message) {
        const badge = document.getElementById('status-badge');
        const msg = document.getElementById('status-message');

        // Remove all status classes
        badge.className = 'badge';

        switch (status) {
            case 'running':
                badge.classList.add('badge-running');
                badge.textContent = 'Running';
                break;
            case 'stopped':
                badge.classList.add('badge-stopped');
                badge.textContent = 'Stopped';
                break;
            case 'unknown':
                badge.classList.add('badge-unknown');
                badge.textContent = 'Unknown';
                break;
            case 'error':
                badge.classList.add('badge-error');
                badge.textContent = 'Error';
                break;
            default:
                badge.classList.add('badge-unknown');
                badge.textContent = status || 'Unknown';
        }

        msg.textContent = message || '';
    },

    async startServer() {
        const btn = document.getElementById('start-btn');
        btn.disabled = true;
        btn.textContent = 'Starting...';

        try {
            const data = await App.apiFetch('/api/start', { method: 'POST' });
            if (data && data.success) {
                Toast.show('Server started', 'success');
                await this.getStatus();
            }
        } catch (e) {
            // Error already handled in apiFetch
        } finally {
            btn.disabled = false;
            btn.textContent = 'Start';
        }
    },

    async stopServer() {
        const btn = document.getElementById('stop-btn');
        btn.disabled = true;
        btn.textContent = 'Stopping...';

        try {
            const data = await App.apiFetch('/api/stop', { method: 'POST' });
            if (data && data.success) {
                Toast.show('Server stopped', 'success');
                await this.getStatus();
            }
        } catch (e) {
            // Error already handled in apiFetch
        } finally {
            btn.disabled = false;
            btn.textContent = 'Stop';
        }
    },

    startPolling() {
        // Initial status check
        this.getStatus();

        // Poll every 10 seconds
        this._intervalId = setInterval(() => {
            this.getStatus();
        }, 10000);
    },

    stopPolling() {
        if (this._intervalId) {
            clearInterval(this._intervalId);
            this._intervalId = null;
        }
    }
};
