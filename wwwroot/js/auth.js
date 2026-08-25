// Auth.js — логика авторизации

const Auth = {
    async login(username, password) {
        try {
            const data = await App.apiFetch('/api/login', {
                method: 'POST',
                body: JSON.stringify({ username, password })
            });

            console.log('=== data ===', data);
            console.log('=== data.token ===', data?.token);
            console.log('=== !!data.token ===', !!data?.token);

            if (data && data.token) {
                console.log('=== TOKEN BRANCH ===');
                App.saveToken(data.token);
                console.log('=== token saved ===', localStorage.getItem('token'));
                Toast.show('Login successful', 'success');
                console.log('=== calling showApp ===');
                App.showApp();
                console.log('=== showApp done ===');
                return true;
            } else {
                console.log('=== ERROR BRANCH ===');
                const errorMsg = data?.error || 'Login failed';
                document.getElementById('login-error').textContent = errorMsg;
                Toast.show(errorMsg, 'error');
                return false;
            }
        } catch (error) {
            document.getElementById('login-error').textContent = 'Connection error';
            return false;
        }
    },

    async logout() {
        try {
            await App.apiFetch('/api/logout', { method: 'POST' });
        } catch (e) {
            // Ignore errors on logout
        }

        App.clearToken();
        App.showLogin();
        Toast.show('Logged out', 'success');
    }
};

// Login form handler
document.getElementById('login-form').addEventListener('submit', async (e) => {
    e.preventDefault();
    const username = document.getElementById('username').value;
    const password = document.getElementById('password').value;
    await Auth.login(username, password);
});
