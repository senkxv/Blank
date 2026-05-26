let pendingDeleteDocumentId = null;

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

window.previewSelectedDocument = async function () {
    const docNumber = sessionStorage.getItem('selectedDocumentNumber');
    if (docNumber) {
        var overlay = document.getElementById('loadingOverlay');
        if (overlay) overlay.style.display = 'flex';

        try {
            const response = await fetch('/Print/' + docNumber);
            
            if (response.ok) {
                const blob = await response.blob();
                const url = URL.createObjectURL(blob);
                window.open(url, '_blank');
                setTimeout(() => URL.revokeObjectURL(url), 1000);
            } else {
                alert('Документ не найден');
            }
        } catch (error) {
            alert('Ошибка при загрузке документа');
        } finally {
            if (overlay) overlay.style.display = 'none';
        }
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