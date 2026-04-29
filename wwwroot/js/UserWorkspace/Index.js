window.selectDocument = function (id) {
    sessionStorage.setItem('selectedDocumentId', id);
    const radio = document.querySelector(`input[type="radio"][value="${id}"]`);
    if (radio) radio.checked = true;
}

window.editSelectedDocument = function () {
    const id = sessionStorage.getItem('selectedDocumentId');
    if (id) {
        window.location.href = '/UserWorkspace/EditDocumentPage/' + id;
    } else {
        if (typeof window.showNotification === 'function') {
            window.showNotification('Сначала выберите документ в таблице');
        } else {
            alert('Сначала выберите документ в таблице');
        }
    }
}

window.deleteSelectedDocument = function () {
    const id = sessionStorage.getItem('selectedDocumentId');
    if (id) {
        if (confirm('Вы уверены, что хотите удалить документ?')) {
            window.location.href = '/UserWorkspace/DeleteDocument?id=' + id;
        }
    } else {
        if (typeof window.showNotification === 'function') {
            window.showNotification('Сначала выберите документ в таблице');
        } else {
            alert('Сначала выберите документ в таблице');
        }
    }
}

window.previewSelectedDocument = function () {
    const id = sessionStorage.getItem('selectedDocumentId');
    if (id) {
        window.open('/UserWorkspace/PreviewDocument/' + id, '_blank');
    } else {
        if (typeof window.showNotification === 'function') {
            window.showNotification('Сначала выберите документ в таблице');
        } else {
            alert('Сначала выберите документ в таблице');
        }
    }
}

document.addEventListener('DOMContentLoaded', function () {
    // Очищаем сохранённый ID документа при загрузке страницы
    sessionStorage.removeItem('selectedDocumentId');

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