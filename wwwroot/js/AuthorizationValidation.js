document.addEventListener('DOMContentLoaded', function () {
    const form = document.querySelector('.main-section-form');

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