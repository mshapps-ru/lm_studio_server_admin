// App.js — основная логика SPA

const App = {
    init() {
        this.checkAuth();
        this.setupTabs();
        this.setupLogout();
    },

    async checkAuth() {
        const token = this.getToken();
        if (token) {
            this.showApp();
        } else {
            this.showLogin();
        }
    },

    showLogin() {
        const loginScreen = document.getElementById('login-screen');
        const appScreen = document.getElementById('app-screen');
        loginScreen.classList.remove('hidden');
        loginScreen.style.display = '';
        if (appScreen) {
            // Hide the main application screen
            appScreen.classList.add('hidden');
            appScreen.style.display = 'none';
        }
    },

    showApp() {
        const loginScreen = document.getElementById('login-screen');
        if (loginScreen) {
            // Hide the login screen by adding hidden class and setting display to none
            loginScreen.classList.add('hidden');
            loginScreen.style.display = 'none';
        }
        const appScreen = document.getElementById('app-screen');
        if (appScreen) {
            // Show application screen by removing hidden class and resetting display
            appScreen.classList.remove('hidden');
            appScreen.style.display = '';
        }
        Home.init();
        Settings.init();
    },

    setupTabs() {
        const tabs = document.querySelectorAll('.tab');
        tabs.forEach(tab => {
            tab.addEventListener('click', () => {
                const target = tab.dataset.tab;

                // Update active tab
                tabs.forEach(t => t.classList.remove('active'));
                tab.classList.add('active');

                // Show/hide tab content
                document.querySelectorAll('.tab-content').forEach(content => {
                    content.classList.remove('active');
                });
                const targetContent = document.getElementById(`${target}-tab`);
                targetContent.classList.add('active');
            });
        });

        // Activate the first tab by default on load
        if (tabs.length > 0) {
            tabs[0].click();
        }
    },

    setupLogout() {
        document.getElementById('logout-btn').addEventListener('click', async (e) => {
            e.preventDefault();
            await Auth.logout();
        });
    },

    getToken() {
        return localStorage.getItem('token');
    },

    saveToken(token) {
        localStorage.setItem('token', token);
    },

    clearToken() {
        localStorage.removeItem('token');
    },

    async apiFetch(url, options = {}) {
        const headers = {
            'Content-Type': 'application/json',
            ...options.headers
        };

        const token = this.getToken();
        if (token) {
            headers['Authorization'] = `Bearer ${token}`;
        }

        try {
            console.log('Fetching:', url, options);
            const response = await fetch(url, {
                ...options,
                headers
            });
            console.log('Response status:', response.status);

            const data = await response.json();
            console.log('Response data:', data);

            if (response.status === 401) {
                App.showLogin();
                Toast.show('Session expired. Please login again.', 'error');
                return null;
            }

            return data;
        } catch (error) {
            console.error('Fetch error:', error);
            Toast.show(`Request failed: ${error.message}`, 'error');
            throw error;
        }
    }
};

// Toast notifications
const Toast = {
    show(message, type = 'success') {
        const container = document.getElementById('toast-container');
        const toast = document.createElement('div');
        toast.className = `toast toast-${type}`;
        toast.textContent = message;
        container.appendChild(toast);

        setTimeout(() => {
            toast.style.opacity = '0';
            toast.style.transform = 'translateX(100%)';
            toast.style.transition = 'all 0.3s ease';
            setTimeout(() => toast.remove(), 300);
        }, 3000);
    }
};

// Initialize application after DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    App.init();
});
