document.addEventListener('DOMContentLoaded', function () {
    const form = document.querySelector('.main-section-form');

    document.querySelectorAll('.toggle-password').forEach(button => {
        button.addEventListener('click', function () {
            const targetId = this.getAttribute('data-target');
            const input = document.getElementById(targetId);
            const svg = this.querySelector('svg');

            if (input) {
                const isPassword = input.type === 'password';

                if (isPassword) {
                    input.type = 'text';
                    svg.innerHTML = `
                    <path d="M12 5C6 5 2.5 9.5 1 12C2.5 14.5 6 19 12 19C18 19 21.5 14.5 23 12C21.5 9.5 18 5 12 5Z" 
                          stroke="#666" stroke-width="1.5" fill="none" stroke-linecap="round"/>
                    <circle cx="12" cy="12" r="2.5" stroke="#666" stroke-width="1.5" fill="none"/>
                    <line x1="3" y1="3" x2="21" y2="21" stroke="#990000" stroke-width="1.5" stroke-linecap="round"/>
                `;
                } else {
                    input.type = 'password';
                    svg.innerHTML = `
                    <path d="M12 5C6 5 2.5 9.5 1 12C2.5 14.5 6 19 12 19C18 19 21.5 14.5 23 12C21.5 9.5 18 5 12 5Z" 
                          stroke="#666" stroke-width="1.5" fill="none" stroke-linecap="round"/>
                    <circle cx="12" cy="12" r="2.5" stroke="#666" stroke-width="1.5" fill="none"/>
                `;
                }
            }
        });
    });

    function clearErrors() {
        document.querySelectorAll('.error-message').forEach(el => el.remove());
        document.querySelectorAll('input').forEach(input => {
            input.classList.remove('error-border');
            input.style.borderColor = '';
        });
    }

    function showError(inputId, message) {
        const input = document.getElementById(inputId);
        if (!input) return;
        input.style.borderColor = 'red';
        input.classList.add('error-border');

        const oldError = input.parentNode.querySelector(`.error-message[data-for="${inputId}"]`);
        if (oldError) oldError.remove();

        const error = document.createElement('div');
        error.className = 'error-message';
        error.setAttribute('data-for', inputId);
        error.innerHTML = message;
        input.parentNode.insertBefore(error, input.nextSibling);
    }

    form.addEventListener('submit', function (e) {
        clearErrors();

        let isValid = true;

        const email = document.getElementById('Email');    
        const password = document.getElementById('Password'); 

        if (email.value.trim() === '') {
            showError('Email', 'Введите email');
            isValid = false;
        } else if (!email.value.includes('@') || !email.value.includes('.')) {
            showError('Email', 'Введите корректный email');
            isValid = false;
        }

        if (password.value === '') {
            showError('Password', 'Введите пароль');
            isValid = false;
        }
        if (password.value < 8) {
            showError('Password', 'Введите корректный пароль');
        }

        if (!isValid) {
            e.preventDefault();
        }
    });
});