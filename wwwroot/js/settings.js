// Settings.js — логика вкладки Settings

const Settings = {
    _originalPort: null,

    init() {
        this.bindEvents();
        this.loadSettings();
    },

    bindEvents() {
        document.getElementById('settings-form').addEventListener('submit', (e) => {
            e.preventDefault();
            this.saveSettings();
        });

        document.getElementById('settings-port').addEventListener('input', (e) => {
            const portWarning = document.getElementById('port-warning');
            const portValue = parseInt(e.target.value);
            if (this._originalPort !== null && portValue !== this._originalPort) {
                portWarning.classList.remove('hidden');
            } else {
                portWarning.classList.add('hidden');
            }
        });
    },

    async loadSettings() {
        try {
            const data = await App.apiFetch('/api/settings');
            if (data) {
                // Username is not editable in settings
                document.getElementById('settings-port').value = data.port || 7778;
                this._originalPort = data.port;
            }
        } catch (e) {
            // Error already handled in apiFetch
        }
    },

    async saveSettings() {
        const password = document.getElementById('settings-password').value;
        const confirmPassword = document.getElementById('settings-password-confirm').value;
        let portVal = parseInt(document.getElementById('settings-port').value);
        if (isNaN(portVal)) {
            Toast.show('Port must be a number', 'error');
            return;
        }
        const port = portVal;

        // Validation
        if (port < 1 || port > 65535) {
            Toast.show('Port must be between 1 and 65535', 'error');
            return;
        }

        if ((password && !confirmPassword) || (confirmPassword && password !== confirmPassword)) {
            Toast.show('Passwords do not match', 'error');
            return;
        }

        try {
            const data = await App.apiFetch('/api/settings', {
                method: 'PUT',
                body: JSON.stringify({ password, port })
            });

            if (data && data.success) {
                const portWarning = document.getElementById('port-warning');
                if (port !== this._originalPort) {
                    Toast.show('Settings saved. Port change requires server restart.', 'success');
                    // Redirect to new port after short delay
                    // Redirect after short delay so server can restart
                    // Wait a bit to allow server restart, then redirect
                    // Poll until the new port is reachable before redirecting
                    const waitForPort = async (port, timeoutMs) => {
                        const start = Date.now();
                        while (Date.now() - start < timeoutMs) {
                            try {
                                const res = await App.apiFetch(`http://${window.location.hostname}:${port}/api/status`, { method: 'GET' });
                                if (res.ok) return true;
                            } catch { }
                            await new Promise(r => setTimeout(r, 500));
                        }
                        return false;
                    };
                    waitForPort(port, 10000).then(success => {
                        const url = new URL(window.location);
                        url.port = port.toString();
                        window.location.href = url.toString();
                    });
                } else {
                    Toast.show('Settings saved', 'success');
                }
                this._originalPort = port;
                document.getElementById('settings-password').value = '';
                document.getElementById('settings-password-confirm').value = '';
            }
        } catch (e) {
            // Error already handled in apiFetch
        }
    }
};
