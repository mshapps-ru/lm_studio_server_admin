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
                document.getElementById('settings-username').value = data.username || '';
                document.getElementById('settings-port').value = data.port || 7778;
                this._originalPort = data.port;
            }
        } catch (e) {
            // Error already handled in apiFetch
        }
    },

    async saveSettings() {
        const username = document.getElementById('settings-username').value.trim();
        const password = document.getElementById('settings-password').value;
        const port = parseInt(document.getElementById('settings-port').value);

        // Validation
        if (!username) {
            Toast.show('Username cannot be empty', 'error');
            return;
        }

        if (!port || port < 1 || port > 65535) {
            Toast.show('Port must be between 1 and 65535', 'error');
            return;
        }

        try {
            const data = await App.apiFetch('/api/settings', {
                method: 'PUT',
                body: JSON.stringify({ username, password, port })
            });

            if (data && data.success) {
                const portWarning = document.getElementById('port-warning');
                if (port !== this._originalPort) {
                    Toast.show('Settings saved. Port change requires server restart.', 'success');
                } else {
                    Toast.show('Settings saved', 'success');
                }
                this._originalPort = port;
                document.getElementById('settings-password').value = '';
            }
        } catch (e) {
            // Error already handled in apiFetch
        }
    }
};
