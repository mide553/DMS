const API_URL = 'http://localhost:5000/api';

// Toggle between login and register forms
document.getElementById('showRegister').addEventListener('click', () => {
    document.getElementById('loginForm').style.display = 'none';
    document.getElementById('registerForm').style.display = 'block';
    document.getElementById('formTitle').textContent = 'Create a new account';
    hideMessages();
});

document.getElementById('showLogin').addEventListener('click', () => {
    document.getElementById('registerForm').style.display = 'none';
    document.getElementById('loginForm').style.display = 'block';
    document.getElementById('formTitle').textContent = 'Login to your account';
    hideMessages();
});

// Login form submission
document.getElementById('loginForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    hideMessages();

    const username = document.getElementById('loginUsername').value;
    const password = document.getElementById('loginPassword').value;

    try {
        const response = await fetch(`${API_URL}/auth/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ username, password })
        });

        if (response.ok) {
            const data = await response.json();

            // Store token and user info
            localStorage.setItem('token', data.token);
            localStorage.setItem('username', data.username);
            localStorage.setItem('email', data.email);

            showSuccess('Login successful! Redirecting...');

            // Redirect to dashboard
            setTimeout(() => {
                window.location.href = 'index.html';
            }, 1000);
        } else {
            const error = await response.json();
            showError(error.message || 'Login failed. Please check your credentials.');
        }
    } catch (error) {
        console.error('Login error:', error);
        showError('An error occurred during login. Please try again.');
    }
});

// Register form submission
document.getElementById('registerForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    hideMessages();

    const username = document.getElementById('registerUsername').value;
    const email = document.getElementById('registerEmail').value;
    const password = document.getElementById('registerPassword').value;
    const confirmPassword = document.getElementById('registerConfirmPassword').value;

    // Validate password match
    if (password !== confirmPassword) {
        showError('Passwords do not match!');
        return;
    }

    try {
        const response = await fetch(`${API_URL}/auth/register`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({ username, email, password })
        });

        if (response.ok) {
            showSuccess('Registration successful! Redirecting to login...');

            // Clear both forms
            document.getElementById('registerForm').reset();
            document.getElementById('loginForm').reset();

            // Switch to login form after 2 seconds
            setTimeout(() => {
                document.getElementById('registerForm').style.display = 'none';
                document.getElementById('loginForm').style.display = 'block';
                document.getElementById('formTitle').textContent = 'Login to your account';
                hideMessages();
            }, 2000);
        } else {
            const error = await response.json();
            showError(error.message || 'Registration failed. Please try again.');
        }
    } catch (error) {
        console.error('Registration error:', error);
        showError('An error occurred during registration. Please try again.');
    }
});

function showError(message) {
    const errorDiv = document.getElementById('errorMessage');
    errorDiv.textContent = message;
    errorDiv.style.display = 'block';
}

function showSuccess(message) {
    const successDiv = document.getElementById('successMessage');
    successDiv.textContent = message;
    successDiv.style.display = 'block';
}

function hideMessages() {
    document.getElementById('errorMessage').style.display = 'none';
    document.getElementById('successMessage').style.display = 'none';
}

// Check if user is already logged in
if (localStorage.getItem('token')) {
    window.location.href = 'index.html';
}
