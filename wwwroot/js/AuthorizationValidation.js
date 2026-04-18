document.addEventListener('DOMContentLoaded', function () {
    const form = document.querySelector('.main-section-form');

    function clearErrors() {
        document.querySelectorAll('.error-message').forEach(el => el.remove());
        document.querySelectorAll('input').forEach(input => {
            input.style.borderColor = '#999999';
        });
    }

    function showError(inputId, message) {
        const input = document.getElementById(inputId);
        input.style.borderColor = 'red';

        const oldError = input.parentNode.querySelector(`.error-message[data-for="${inputId}"]`);
        if (oldError) oldError.remove();

        const error = document.createElement('div');
        error.className = 'error-message';
        error.setAttribute('data-for', inputId);
        error.style.cssText = 'color: #990000; font-size: 12px; margin-top: -10px; margin-bottom: 10px;';
        error.innerHTML = message;
        input.parentNode.insertBefore(error, input.nextSibling);
    }

    form.addEventListener('submit', function (e) {
        clearErrors();

        let isValid = true;

        const email = document.getElementById('email');
        const fio = document.getElementById('FIO');
        const password = document.getElementById('password');

        // Email
        if (email.value.trim() === '') {
            showError('email', 'Введите email');
            isValid = false;
        } else if (!email.value.includes('@') || !email.value.includes('.')) {
            showError('email', 'Введите корректный email');
            isValid = false;
        }

        // ФИО
        if (fio.value.trim() === '') {
            showError('FIO', 'Введите ФИО');
            isValid = false;
        } else if (fio.value.trim().length < 2) {
            showError('FIO', 'ФИО должно содержать минимум 2 символа');
            isValid = false;
        }

        // Пароль
        if (password.value === '') {
            showError('password', 'Введите пароль');
            isValid = false;
        }

        if (!isValid) {
            e.preventDefault();
        }
    });
});