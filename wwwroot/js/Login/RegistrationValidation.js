document.addEventListener('DOMContentLoaded', function () {
    const form = document.querySelector('.main-section-form');

    document.querySelectorAll('.toggle-password').forEach(button => {
        button.addEventListener('click', function () {
            const targetId = this.getAttribute('data-target');
            const input = document.getElementById(targetId);
            const svg = this.querySelector('svg');

            if (input && svg) {
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
        });
        const checkboxError = document.querySelector('.checkbox-error');
        if (checkboxError) checkboxError.remove();
    }

    function showError(inputId, message) {
        const input = document.getElementById(inputId);
        if (!input) return;
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

        const email = document.getElementById('email');
        const fio = document.getElementById('FIO');
        const password = document.getElementById('password');
        const passwordRepeat = document.getElementById('password-repeat');
        const checkbox = document.getElementById('checkbox-user-data-computing');

        if (email.value.trim() === '') {
            showError('email', 'Введите email');
            isValid = false;
        } else if (!email.value.includes('@') || !email.value.includes('.')) {
            showError('email', 'Введите корректный email (пример: user@mail.com)');
            isValid = false;
        }

        if (fio.value.trim() === '') {
            showError('FIO', 'Введите ФИО');
            isValid = false;
        } else if (fio.value.trim().length < 2) {
            showError('FIO', 'ФИО должно содержать минимум 2 символа');
            isValid = false;
        }

        if (password.value === '') {
            showError('password', 'Введите пароль');
            isValid = false;
        } else {
            if (password.value.length < 8) {
                showError('password', 'Пароль должен быть не менее 8 символов');
                isValid = false;
            } else if (!/[!@#$%^&*?_]/.test(password.value)) {
                showError('password', 'Пароль должен содержать спецсимвол (!@#$%^&*?_)');
                isValid = false;
            } else if (!/[A-Z]/.test(password.value)) {
                showError('password', 'Пароль должен содержать заглавную букву');
                isValid = false;
            } else if (!/[0-9]/.test(password.value)) {
                showError('password', 'Пароль должен содержать цифру');
                isValid = false;
            }
        }

        if (passwordRepeat.value !== password.value) {
            showError('password-repeat', 'Пароли не совпадают');
            isValid = false;
        }

        if (!checkbox.checked) {
            const wrapper = document.querySelector('.checkbox-wrapper');
            let error = wrapper.querySelector('.checkbox-error');
            if (!error) {
                error = document.createElement('div');
                error.className = 'checkbox-error';
                wrapper.appendChild(error);
            }
            error.innerHTML = 'Подтвердите согласие на обработку персональных данных';
            isValid = false;
        }

        if (!isValid) {
            e.preventDefault();
        }
    });
});