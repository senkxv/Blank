document.getElementById('file').addEventListener('change', function () {
    const fileNameText = document.getElementById('fileNameText');
    const display = document.getElementById('fileNameDisplay');
    const nameSpan = document.getElementById('selectedFileName');

    if (this.files.length > 0) {
        const fileName = this.files[0].name;
        if (fileNameText) {
            fileNameText.textContent = fileName;
            fileNameText.style.color = '#28a745';
        }
        if (nameSpan) {
            nameSpan.textContent = fileName;
        }
        if (display) {
            display.style.display = 'block';
        }
    } else {
        if (fileNameText) {
            fileNameText.textContent = 'Файл не выбран';
            fileNameText.style.color = '#666';
        }
        if (display) {
            display.style.display = 'none';
        }
    }
});

document.getElementById('confirmRestore').addEventListener('change', function () {
    const restoreBtn = document.getElementById('restoreBtn');

    if (this.checked) {
        restoreBtn.disabled = false;
        restoreBtn.classList.add('active');
    } else {
        restoreBtn.disabled = true;
        restoreBtn.classList.remove('active');
    }
});

document.getElementById('restoreForm').addEventListener('submit', function (e) {
    const fileInput = document.getElementById('file');
    const confirmCheck = document.getElementById('confirmRestore');

    if (!fileInput.files.length) {
        e.preventDefault();
        alert('Пожалуйста, выберите файл бэкапа!');
        return false;
    }

    if (!confirmCheck.checked) {
        e.preventDefault();
        alert('Пожалуйста, подтвердите, что понимаете последствия!');
        return false;
    }

    const submitBtn = document.getElementById('restoreBtn');
    submitBtn.disabled = true;
    submitBtn.classList.remove('active');
    submitBtn.textContent = 'Восстановление...';
});