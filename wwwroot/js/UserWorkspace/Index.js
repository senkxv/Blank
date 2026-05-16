// Переменная для хранения ID документа, ожидающего удаления
let pendingDeleteDocumentId = null;

// Функция выбора документа
window.selectDocument = function (id) {
    sessionStorage.setItem('selectedDocumentId', id);

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
        if (typeof showNotification === 'function') {
            showNotification('Сначала выберите документ в таблице');
        } else {
            alert('Сначала выберите документ в таблице');
        }
    }
};

// Удаление — ОТКРЫВАЕТ МОДАЛЬНОЕ ОКНО
window.deleteSelectedDocument = function () {
    var id = sessionStorage.getItem('selectedDocumentId');

    if (id) {
        pendingDeleteDocumentId = id;
        var modal = document.getElementById('confirmDeleteModal');
        var messageEl = document.getElementById('confirmDeleteMessage');

        if (messageEl) {
            messageEl.textContent = 'Вы уверены, что хотите удалить документ?';
        }
        if (modal) {
            modal.style.display = 'block';
        } else {
            if (confirm('Вы уверены, что хотите удалить документ?')) {
                window.location.href = '/UserWorkspace/DeleteDocument?id=' + id;
            }
        }
    } else {
        if (typeof showNotification === 'function') {
            showNotification('Сначала выберите документ в таблице');
        } else {
            alert('Сначала выберите документ в таблице');
        }
    }
};

// Предпросмотр выбранного документа
window.previewSelectedDocument = function () {
    const docNumber = sessionStorage.getItem('selectedDocumentNumber');
    if (docNumber) {
        window.open('/Print/' + docNumber, '_blank');
    } else {
        const id = sessionStorage.getItem('selectedDocumentId');
        if (id) {
            window.open('/UserWorkspace/PreviewDocument/' + id, '_blank');
        } else {
            if (typeof showNotification === 'function') {
                showNotification('Сначала выберите документ в таблице');
            } else {
                alert('Сначала выберите документ в таблице');
            }
        }
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

    // ===== ОБРАБОТЧИКИ МОДАЛЬНОГО ОКНА УДАЛЕНИЯ ДОКУМЕНТА =====
    const btnYes = document.getElementById('confirmDeleteYes');
    const btnNo = document.getElementById('confirmDeleteNo');
    const modal = document.getElementById('confirmDeleteModal');

    if (btnYes) {
        btnYes.addEventListener('click', function () {
            if (pendingDeleteDocumentId) {
                window.location.href = '/UserWorkspace/DeleteDocument?id=' + pendingDeleteDocumentId;
            }
            if (modal) modal.style.display = 'none';
            pendingDeleteDocumentId = null;
        });
    }

    if (btnNo) {
        btnNo.addEventListener('click', function () {
            if (modal) modal.style.display = 'none';
            pendingDeleteDocumentId = null;
        });
    }

    if (modal) {
        modal.addEventListener('click', function (e) {
            if (e.target === modal) {
                modal.style.display = 'none';
                pendingDeleteDocumentId = null;
            }
        });
    }
});