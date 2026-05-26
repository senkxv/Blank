const goodsList = window.goodsListData || [];
let deletedIds = [];
let rowToDelete = null;

function showNotification(message) {
    const modal = document.getElementById('notificationModal');
    const messageElement = document.getElementById('notificationMessage');

    if (modal && messageElement) {
        messageElement.textContent = message;
        modal.style.display = 'block';
    } else {
        alert(message);
    }
}

function getUnit(goodsId) {
    const goods = goodsList.find(g => g.ид_товара == goodsId);
    return goods ? goods.единицы_измерения : '';
}

function updateRowCalculations(row) {
    const qty = parseFloat(row.querySelector('.goods-quantity')?.value) || 0;
    const price = parseFloat(row.querySelector('.goods-price')?.value) || 0;
    const discount = parseFloat(row.querySelector('.goods-discount')?.value) || 0;
    const vat = parseFloat(row.querySelector('.goods-vat')?.value) || 0;

    const costWithoutDiscount = qty * price;
    const discountAmount = costWithoutDiscount * (discount / 100);
    const costAfterDiscount = costWithoutDiscount - discountAmount;
    const vatAmount = costAfterDiscount * (vat / 100);
    const totalWithVat = costAfterDiscount + vatAmount;

    const sumWithoutVatCell = row.querySelector('.goods-sum-without-vat');
    const vatAmountCell = row.querySelector('.goods-vat-amount');
    const sumCell = row.querySelector('.goods-sum');

    if (sumWithoutVatCell) sumWithoutVatCell.textContent = costAfterDiscount.toFixed(2);
    if (vatAmountCell) vatAmountCell.textContent = vatAmount.toFixed(2);
    if (sumCell) sumCell.textContent = totalWithVat.toFixed(2);

    calculateTotalWeight();
}

function calculateTotalWeight() {
    let totalWeight = 0;
    document.querySelectorAll('.goods-weight').forEach(input => {
        totalWeight += parseFloat(input.value) || 0;
    });
    const totalWeightElement = document.getElementById('totalWeight');
    if (totalWeightElement) {
        totalWeightElement.textContent = totalWeight.toFixed(3);
    }
}

function addNewRow() {
    const noDataRow = document.getElementById('noDataRow');
    if (noDataRow) noDataRow.remove();

    const tbody = document.getElementById('goodsTableBody');
    const newRow = document.createElement('tr');
    newRow.setAttribute('data-is-existing', 'false');

    const goodsOptions = '<option value="">-- Выберите товар --</option>' +
        goodsList.map(g => `<option value="${g.ид_товара}">${g.наименование}</option>`).join('');

    newRow.innerHTML = `
        <td><select class="goods-select" style="width:100%">${goodsOptions}</select></td>
        <td><input type="text" class="goods-unit" readonly style="width:60px"></td>
        <td><input type="number" class="goods-quantity" value="1" step="0.001" min="0" style="width:90px"></td>
        <td><input type="number" class="goods-price" value="1" step="0.01" min="0" style="width:100px"></td>
        <td><input type="number" class="goods-discount" value="0" step="0.1" min="0" max="100" style="width:80px"></td>
        <td><input type="number" class="goods-vat" value="20" step="0.5" min="0" max="100" style="width:70px"></td>
        <td class="goods-sum-without-vat">1.00</td>
        <td class="goods-vat-amount">0.20</td>
        <td class="goods-sum">1.20</td>
        <td><input type="number" class="goods-weight" value="0" step="0.001" min="0" style="width:80px"></td>
        <td><button type="button" class="remove-goods">✖</button></td>
    `;

    tbody.appendChild(newRow);
    updateRowCalculations(newRow);
}

function removeGoodsRow(row) {
    const id = row.getAttribute('data-id');
    const isExisting = row.getAttribute('data-is-existing') === 'true';

    if (isExisting && id) {
        deletedIds.push(parseInt(id));
        document.getElementById('deletedPositions').value = deletedIds.join(',');
    }

    row.remove();
    calculateTotalWeight();

    const tbody = document.getElementById('goodsTableBody');
    if (tbody && tbody.querySelectorAll('tr').length === 0) {
        tbody.innerHTML = '<tr id="noDataRow"><td colspan="11" style="text-align: center;">Нет добавленных товаров</td></tr>';
    }
}

function collectPositions() {
    const positions = [];
    let hasValidPositions = false;

    document.querySelectorAll('#goodsTableBody tr').forEach(row => {
        if (row.id === 'noDataRow') return;

        const goodsSelect = row.querySelector('.goods-select');
        const goodsId = parseInt(goodsSelect?.value) || 0;
        const quantity = parseFloat(row.querySelector('.goods-quantity')?.value) || 0;
        const price = parseFloat(row.querySelector('.goods-price')?.value) || 0;

        const isExisting = row.getAttribute('data-is-existing') === 'true';

        if (goodsId > 0 && (quantity > 0 || isExisting) && (price > 0 || isExisting)) {
            positions.push({
                id: isExisting ? (parseInt(row.getAttribute('data-id')) || 0) : 0,
                goodsId: goodsId,
                quantity: quantity,
                price: price,
                vatRate: parseFloat(row.querySelector('.goods-vat')?.value) || 0,
                discount: parseFloat(row.querySelector('.goods-discount')?.value) || 0,
                weight: parseFloat(row.querySelector('.goods-weight')?.value) || 0,
                packages: 0,
                note: ''
            });
            hasValidPositions = true;
        }
    });

    return { positions, hasValidPositions };
}

document.addEventListener('DOMContentLoaded', function () {
    // Закрытие модального окна уведомлений
    document.getElementById('closeNotificationBtn')?.addEventListener('click', function () {
        document.getElementById('notificationModal').style.display = 'none';
    });

    document.getElementById('notificationModal')?.addEventListener('click', function (e) {
        if (e.target === this) {
            this.style.display = 'none';
        }
    });

    // Кнопка добавления товара
    document.getElementById('addGoodsBtn')?.addEventListener('click', addNewRow);

    // Изменение товара в select
    document.getElementById('goodsTableBody')?.addEventListener('change', function (e) {
        if (e.target.classList.contains('goods-select')) {
            const goodsId = parseInt(e.target.value);
            const unit = getUnit(goodsId);
            const row = e.target.closest('tr');
            const unitInput = row.querySelector('.goods-unit');
            if (unitInput) {
                unitInput.value = unit;
            }
            updateRowCalculations(row);
        }
    });

    // Ввод чисел
    document.getElementById('goodsTableBody')?.addEventListener('input', function (e) {
        if (e.target.classList.contains('goods-quantity') ||
            e.target.classList.contains('goods-price') ||
            e.target.classList.contains('goods-discount') ||
            e.target.classList.contains('goods-vat')) {
            updateRowCalculations(e.target.closest('tr'));
        }
        if (e.target.classList.contains('goods-weight')) {
            calculateTotalWeight();
        }
    });

    // Удаление товара — открываем модальное окно
    document.getElementById('goodsTableBody')?.addEventListener('click', function (e) {
        if (e.target.classList.contains('remove-goods')) {
            rowToDelete = e.target.closest('tr');
            const modal = document.getElementById('confirmDeleteModal');
            if (modal) {
                modal.style.display = 'block';
            } else {
                removeGoodsRow(rowToDelete);
                rowToDelete = null;
            }
        }
    });

    // Подтверждение удаления в модальном окне
    document.getElementById('confirmDeleteYes')?.addEventListener('click', function () {
        if (rowToDelete) {
            removeGoodsRow(rowToDelete);
            rowToDelete = null;
        }
        document.getElementById('confirmDeleteModal').style.display = 'none';
    });

    // Отмена удаления
    document.getElementById('confirmDeleteNo')?.addEventListener('click', function () {
        rowToDelete = null;
        document.getElementById('confirmDeleteModal').style.display = 'none';
    });

    // Закрытие модального окна удаления по клику на фон
    document.getElementById('confirmDeleteModal')?.addEventListener('click', function (e) {
        if (e.target === this) {
            rowToDelete = null;
            this.style.display = 'none';
        }
    });

    // Отправка формы
    const form = document.getElementById('documentForm');
    if (form) {
        form.addEventListener('submit', function (e) {
            const { positions, hasValidPositions } = collectPositions();
            const hasRows = document.querySelectorAll('#goodsTableBody tr:not(#noDataRow)').length > 0;

            if (!hasValidPositions) {
                e.preventDefault();
                showNotification('Заполните все обязательные поля в позициях товаров (товар, количество, цена)!');
                return false;
            }

            document.getElementById('positionsData').value = JSON.stringify(positions);
            console.log('Отправляемые позиции:', positions);
        });
    }

    // Инициализация расчётов для существующих строк
    document.querySelectorAll('#goodsTableBody tr[data-is-existing="true"]').forEach(row => {
        updateRowCalculations(row);
    });

    calculateTotalWeight();
});