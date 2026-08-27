// Settings.js — логика вкладки Settings

const Settings = {
    _originalPort: null,

    init() {
        this.bindEvents();
        this.loadSettings();
        this.loadLmStudioSettings();
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

        document.getElementById('auto-detect-port-btn').addEventListener('click', () => {
            this.autoDetectPort();
        });

        document.getElementById('lmstudio-settings-form').addEventListener('submit', (e) => {
            e.preventDefault();
            this.saveLmStudioSettings();
        });
    },

    async loadSettings() {
        try {
            const data = await App.apiFetch('/api/settings');
            if (data) {
                // Username is not editable in settings
document.getElementById('settings-port').value = data.port || 7778;
this._originalPort = data.port;
// Set verbose logging checkbox
const vlogCheckbox = document.getElementById('verbose-logging');
if (vlogCheckbox) {
    vlogCheckbox.checked = !!data.verboseLogging;
}
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
                body: JSON.stringify({ password, port, verboseLogging: document.getElementById('verbose-logging').checked })
            });

            if (data && data.success) {
                const portWarning = document.getElementById('port-warning');
                if (port !== this._originalPort) {
                    Toast.show('Settings saved. Redirecting to new port...', 'success');
                    // Wait for server to restart on new port, then redirect
                    setTimeout(() => {
                        const url = new URL(window.location);
                        url.port = port.toString();
                        window.location.href = url.toString();
                    }, 5000);
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
    },

    async loadLmStudioSettings() {
        try {
            const data = await App.apiFetch('/api/settings/lmstudio');
            if (data) {
                document.getElementById('settings-lmstudio-port').value = data.lmStudioPort || 1234;
                document.getElementById('settings-bind-address').value = data.bindAddress || '0.0.0.0';
            }
        } catch (e) {
            // Ignore errors
        }
    },

    async autoDetectPort() {
        const btn = document.getElementById('auto-detect-port-btn');
        btn.disabled = true;
        btn.textContent = 'Detecting...';

        try {
            const data = await App.apiFetch('/api/settings/lmstudio/detect', { method: 'POST' });
            if (data && data.success) {
                document.getElementById('settings-lmstudio-port').value = data.port;
                Toast.show(`Port detected: ${data.port}`, 'success');
            } else {
                Toast.show('Failed to detect port', 'error');
            }
        } catch (e) {
            Toast.show('Failed to detect port', 'error');
        } finally {
            btn.disabled = false;
            btn.textContent = 'Auto-detect';
        }
    },

    async saveLmStudioSettings() {
        const lmStudioPort = parseInt(document.getElementById('settings-lmstudio-port').value);
        const bindAddress = document.getElementById('settings-bind-address').value.trim();

        if (isNaN(lmStudioPort) || lmStudioPort < 1 || lmStudioPort > 65535) {
            Toast.show('LM Studio Port must be between 1 and 65535', 'error');
            return;
        }

        if (!bindAddress) {
            Toast.show('Bind Address is required', 'error');
            return;
        }

        try {
            const data = await App.apiFetch('/api/settings/lmstudio', {
                method: 'PUT',
                body: JSON.stringify({ lmStudioPort, bindAddress })
            });

            if (data && data.success) {
                Toast.show('LM Studio settings saved', 'success');
            }
        } catch (e) {
            Toast.show('Failed to save LM Studio settings', 'error');
        }
    }
};
