document.addEventListener('DOMContentLoaded', function () {
    const filterModal = document.getElementById('filterModal');
    const filterLink = document.getElementById('filterLink');
    const closeFilterBtn = document.getElementById('closeFilterBtn');
    const applyFilterBtn = document.getElementById('applyFilterBtn');

    // Открытие окна фильтра
    if (filterLink && filterModal) {
        filterLink.addEventListener('click', function (e) {
            e.preventDefault();
            filterModal.style.display = 'block';
        });
    }

    // Закрытие окна
    if (closeFilterBtn && filterModal) {
        closeFilterBtn.addEventListener('click', function () {
            filterModal.style.display = 'none';
        });
    }

    // Применение фильтра
    if (applyFilterBtn) {
        applyFilterBtn.addEventListener('click', function () {
            const sortField = document.getElementById('sortField').value;
            const sortOrder = document.getElementById('sortOrder').value;

            // 1. Скрываем/показываем столбцы
            const checkboxes = document.querySelectorAll('#filterModal input[type="checkbox"]');
            checkboxes.forEach(cb => {
                const columnClass = 'col-' + cb.value;

                // Скрываем/показываем ячейки данных
                const cells = document.querySelectorAll('td.' + columnClass);
                cells.forEach(cell => {
                    cell.style.display = cb.checked ? '' : 'none';
                });

                // Скрываем/показываем заголовки
                const headers = document.querySelectorAll('th.' + columnClass);
                headers.forEach(header => {
                    header.style.display = cb.checked ? '' : 'none';
                });
            });

            // 2. Сортировка строк
            const tbody = document.querySelector('#mainTable tbody');
            if (!tbody) return;

            const rows = Array.from(tbody.querySelectorAll('tr'));

            const getValue = (row, field) => {
                // Порядковый номер (ид_документа) — берём из второй ячейки (индекс 1)
                if (field === 'ид_документа') {
                    const cell = row.cells[1];
                    return cell ? parseInt(cell.innerText) || 0 : 0;
                }
                // Дата создания
                if (field === 'дата_создания') {
                    const cell = row.querySelector('.col-' + field);
                    return cell ? new Date(cell.innerText) : new Date(0);
                }
                // Остальные текстовые поля
                const cell = row.querySelector('.col-' + field);
                return cell ? cell.innerText : '';
            };

            rows.sort((a, b) => {
                let valA = getValue(a, sortField);
                let valB = getValue(b, sortField);

                // Сравнение строк (регистронезависимое)
                if (typeof valA === 'string') {
                    valA = valA.toLowerCase();
                    valB = valB.toLowerCase();
                }

                if (sortOrder === 'asc') {
                    return valA > valB ? 1 : (valA < valB ? -1 : 0);
                } else {
                    return valA < valB ? 1 : (valA > valB ? -1 : 0);
                }
            });

            // Переставляем строки в отсортированном порядке
            rows.forEach(row => tbody.appendChild(row));

            // Закрываем модальное окно
            filterModal.style.display = 'none';
        });
    }

    // Закрытие при клике вне окна
    window.addEventListener('click', function (e) {
        if (filterModal && e.target === filterModal) {
            filterModal.style.display = 'none';
        }
    });
});