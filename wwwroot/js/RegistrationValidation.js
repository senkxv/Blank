document.addEventListener('DOMContentLoaded', function () {
    const form = document.querySelector('.main-section-form');

    function clearErrors() {
        document.querySelectorAll('.error-message').forEach(el => el.remove());
        document.querySelectorAll('input').forEach(input => {
            input.style.borderColor = '#999999';
        });
        const checkboxError = document.querySelector('.checkbox-error');
        if (checkboxError) checkboxError.remove();
    }

    function showError(inputId, message) {
        const input = document.getElementById(inputId);
        input.style.borderColor = 'red';

        // Удаляем старую ошибку для этого поля, если есть
        const oldError = input.parentNode.querySelector(`.error-message[data-for="${inputId}"]`);
        if (oldError) oldError.remove();

        // Создаём новую ошибку
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
        const passwordRepeat = document.getElementById('password-repeat');
        const checkbox = document.getElementById('checkbox-user-data-computing');

        // === Проверка EMAIL ===
        if (email.value.trim() === '') {
            showError('email', 'Введите email');
            isValid = false;
        } else if (!email.value.includes('@') || !email.value.includes('.')) {
            showError('email', 'Введите корректный email (пример: user@mail.com)');
            isValid = false;
        }

        // === Проверка ФИО ===
        if (fio.value.trim() === '') {
            showError('FIO', 'Введите ФИО');
            isValid = false;
        } else if (fio.value.trim().length < 2) {
            showError('FIO', 'ФИО должно содержать минимум 2 символа');
            isValid = false;
        }

        // === Проверка ПАРОЛЯ ===
        if (password.value === '') {
            showError('password', 'Введите пароль');
            isValid = false;
        } else {
            if (password.value.length < 8) {
                showError('password', 'Пароль должен быть не менее 8 символов');
                isValid = false;
            } else if (!/[!@#$%^&*]/.test(password.value)) {
                showError('password', 'Пароль должен содержать спецсимвол (!@#$%^&*)');
                isValid = false;
            } else if (!/[A-Z]/.test(password.value)) {
                showError('password', 'Пароль должен содержать заглавную букву');
                isValid = false;
            } else if (!/[0-9]/.test(password.value)) {
                showError('password', 'Пароль должен содержать цифру');
                isValid = false;
            }
        }

        // === Проверка ПОДТВЕРЖДЕНИЯ ПАРОЛЯ ===
        if (passwordRepeat.value !== password.value) {
            showError('password-repeat', 'Пароли не совпадают');
            isValid = false;
        }

        // === Проверка ЧЕКБОКСА ===
        if (!checkbox.checked) {
            const wrapper = document.querySelector('.checkbox-wrapper');
            let error = wrapper.querySelector('.checkbox-error');
            if (!error) {
                error = document.createElement('div');
                error.className = 'checkbox-error';
                error.style.cssText = 'color: #990000; font-size: 12px; margin-top: 5px;';
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