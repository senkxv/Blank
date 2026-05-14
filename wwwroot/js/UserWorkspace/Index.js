window.selectDocument = function (id) {
    sessionStorage.setItem('selectedDocumentId', id);

    // Сохраняем также номер документа
    const row = document.querySelector(`tr[data-id="${id}"]`);
    if (row) {
        const docNumber = row.querySelector('td:nth-child(3)')?.textContent?.trim();
        if (docNumber) {
            sessionStorage.setItem('selectedDocumentNumber', docNumber);
        }
    }

    const radio = document.querySelector(`input[type="radio"][value="${id}"]`);
    if (radio) radio.checked = true;
}

// Функция выбора документа
window.selectDocument = function (id) {
    sessionStorage.setItem('selectedDocumentId', id);

    // Сохраняем также номер документа (столбец 2)
    const row = document.querySelector(`tr[data-id="${id}"]`);
    if (row) {
        const docNumber = row.querySelector('td:nth-child(2)')?.textContent?.trim();
        if (docNumber) {
            sessionStorage.setItem('selectedDocumentNumber', docNumber);
        }
    }

    const radio = document.querySelector(`input[type="radio"][value="${id}"]`);
    if (radio) radio.checked = true;
};

// Редактирование выбранного документа
window.editSelectedDocument = function () {
    const id = sessionStorage.getItem('selectedDocumentId');
    if (id) {
        window.location.href = '/UserWorkspace/EditDocumentPage/' + id;
    } else {
        showNotification('Сначала выберите документ в таблице');
    }
};

// Удаление выбранного документа
window.deleteSelectedDocument = function () {
    const id = sessionStorage.getItem('selectedDocumentId');
    if (id) {
        if (confirm('Вы уверены, что хотите удалить документ?')) {
            window.location.href = '/UserWorkspace/DeleteDocument?id=' + id;
        }
    } else {
        showNotification('Сначала выберите документ в таблице');
    }
};

// Предпросмотр выбранного документа
window.previewSelectedDocument = function () {
    const id = sessionStorage.getItem('selectedDocumentId');
    const docNumber = sessionStorage.getItem('selectedDocumentNumber');

    if (docNumber) {
        // Все документы открываем через общий маршрут /Print/
        window.open('/Print/' + docNumber, '_blank');
    } else if (id) {
        // На случай, если номер не сохранился — используем ID
        window.open('/UserWorkspace/PreviewDocument/' + id, '_blank');
    } else {
        showNotification('Сначала выберите документ в таблице');
    }
};

// Инициализация при загрузке страницы
document.addEventListener('DOMContentLoaded', function () {
    sessionStorage.removeItem('selectedDocumentId');
    sessionStorage.removeItem('selectedDocumentNumber');

    const radios = document.querySelectorAll('input[type="radio"][name="selectedId"]');
    radios.forEach(radio => radio.checked = false);

    const rows = document.querySelectorAll('.rounded-table tbody tr');
    rows.forEach(row => {
        row.addEventListener('click', function (e) {
            if (e.target.type !== 'radio') {
                const id = this.getAttribute('data-id');
                if (id) {
                    window.selectDocument(id);
                }
            }
        });
    });
});

window.editSelectedDocument = function () {
    const id = sessionStorage.getItem('selectedDocumentId');
    if (id) {
        window.location.href = '/UserWorkspace/EditDocumentPage/' + id;
    } else {
        showNotification('Сначала выберите документ в таблице');
    }
}

window.deleteSelectedDocument = function () {
    const id = sessionStorage.getItem('selectedDocumentId');
    if (id) {
        if (confirm('Вы уверены, что хотите удалить документ?')) {
            window.location.href = '/UserWorkspace/DeleteDocument?id=' + id;
        }
    } else {
        showNotification('Сначала выберите документ в таблице');
    }
}

window.previewSelectedDocument = function () {
    const id = sessionStorage.getItem('selectedDocumentId');
    const docNumber = sessionStorage.getItem('selectedDocumentNumber');

    if (id) {
        // Получаем тип документа из таблицы
        const row = document.querySelector(`tr[data-id="${id}"]`);
        const docType = row?.querySelector('td:nth-child(2)')?.textContent?.trim()?.toUpperCase();

        if (docNumber) {
            if (docType === 'CMR') {
                window.open('/CMR/' + docNumber, '_blank');
            } else {
                window.open('/Print/' + docNumber, '_blank');
            }
        } else {
            window.open('/UserWorkspace/PreviewDocument/' + id, '_blank');
        }
    } else {
        showNotification('Сначала выберите документ в таблице');
    }
}

document.addEventListener('DOMContentLoaded', function () {
    // Очищаем сохранённый ID документа при загрузке страницы
    sessionStorage.removeItem('selectedDocumentId');
    sessionStorage.removeItem('selectedDocumentNumber');

    // Очищаем выделение радио-кнопок
    const radios = document.querySelectorAll('input[type="radio"][name="selectedId"]');
    radios.forEach(radio => radio.checked = false);

    // Навешиваем обработчики на строки таблицы
    const rows = document.querySelectorAll('.rounded-table tbody tr');

    rows.forEach(row => {
        row.addEventListener('click', function (e) {
            if (e.target.type !== 'radio') {
                const id = this.getAttribute('data-id');
                if (id) {
                    window.selectDocument(id);
                }
            }
        });
    });
});