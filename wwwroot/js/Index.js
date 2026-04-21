function selectDocument(id) {
    sessionStorage.setItem('selectedDocumentId', id);
    const radio = document.querySelector(`input[type="radio"][value="${id}"]`);
    if (radio) radio.checked = true;
}

function editSelectedDocument() {
    const id = sessionStorage.getItem('selectedDocumentId');
    if (id) {
        window.location.href = '/UserWorkspace/EditDocumentPage?id=' + id;
    } else {
        alert('Сначала выберите документ в таблице');
    }
}

function deleteSelectedDocument() {
    const id = sessionStorage.getItem('selectedDocumentId');
    if (id) {
        if (confirm('Вы уверены, что хотите удалить документ?')) {
            window.location.href = '/UserWorkspace/DeleteDocument?id=' + id;
        }
    } else {
        alert('Сначала выберите документ в таблице');
    }
}

// Привязываем события к строкам таблицы
document.querySelectorAll('.rounded-table tbody tr').forEach(row => {
    row.addEventListener('click', function () {
        const id = this.getAttribute('data-id');
        if (id) selectDocument(id);
    });
});