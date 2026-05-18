const goodsList = window.goodsListData || [];
let deletedIds = [];

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

    const goodsOptions = '<option value="">-Выберите товар-</option>' +
        goodsList.map(g => `<option value="${g.ид_товара}">${g.наименование}</option>`).join('');

    newRow.innerHTML = `
        <td><select class="goods-select">${goodsOptions}</select></td>
        <td><input type="text" class="goods-unit" readonly></td>
        <td><input type="number" class="goods-quantity" value="1" step="0.001" min="0"></td>
        <td><input type="number" class="goods-price" value="1" step="0.01" min="0"></td>
        <td><input type="number" class="goods-discount" value="0" step="0.1" min="0" max="100"></td>
        <td><input type="number" class="goods-vat" value="20" step="0.5" min="0" max="100"></td>
        <td class="goods-sum-without-vat">1.00</td>
        <td class="goods-vat-amount">0.20</td>
        <td class="goods-sum">1.20</td>
        <td><input type="number" class="goods-weight" value="0" step="0.001" min="0"></td>
        <td><button type="button" class="remove-goods">✖</button></td>
    `;

    tbody.appendChild(newRow);
    updateRowCalculations(newRow);
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

        if (goodsId > 0 && quantity > 0 && price > 0) {
            positions.push({
                id: 0,
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

    // ============ КНОПКА ПРОПУСКА ТОЧКИ ============
    document.getElementById('skipPointBtn')?.addEventListener('click', function (e) {
        e.preventDefault();

        var form = document.getElementById('documentForm');
        var input = document.createElement('input');
        input.type = 'hidden';
        input.name = 'action';
        input.value = 'skip';
        form.appendChild(input);
        form.submit();
    });

    // ============ ДОБАВЛЕНИЕ ТОВАРА ============
    document.getElementById('addGoodsBtn')?.addEventListener('click', addNewRow);

    // ============ ИЗМЕНЕНИЕ ТОВАРА В SELECT ============
    document.getElementById('goodsTableBody')?.addEventListener('change', function (e) {
        if (e.target.classList.contains('goods-select')) {
            const goodsId = parseInt(e.target.value);
            const unit = getUnit(goodsId);
            e.target.closest('tr').querySelector('.goods-unit').value = unit;
            updateRowCalculations(e.target.closest('tr'));
        }
    });

    // ============ ВВОД ЧИСЕЛ ============
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

    // ============ УДАЛЕНИЕ ТОВАРА ============
    document.getElementById('goodsTableBody')?.addEventListener('click', function (e) {
        if (e.target.classList.contains('remove-goods')) {
            e.target.closest('tr').remove();
            calculateTotalWeight();
            if (document.querySelectorAll('#goodsTableBody tr').length === 0) {
                document.getElementById('goodsTableBody').innerHTML = '<tr id="noDataRow"><td colspan="11" style="text-align: center;">Нет добавленных товаров</td></tr>';
            }
        }
    });

    // ============ ОТПРАВКА ФОРМЫ ============
    const form = document.getElementById('documentForm');
    if (form) {
        form.addEventListener('submit', function (e) {
            const submitter = e.submitter;
            if (submitter && submitter.getAttribute('value') === 'skip') {
                return true;
            }

            const { positions, hasValidPositions } = collectPositions();
            const hasRows = document.querySelectorAll('#goodsTableBody tr:not(#noDataRow)').length > 0;

            if (!hasRows) {
                e.preventDefault();
                showNotification('Добавьте хотя бы одну позицию товара!');
                return false;
            }

            if (!hasValidPositions) {
                e.preventDefault();
                showNotification('Заполните все обязательные поля в позициях товаров (товар, количество, цена)!');
                return false;
            }

            document.getElementById('positionsData').value = JSON.stringify(positions);
            console.log('Отправляемые позиции:', positions);
        });
    }

    // ============ ПЕРВИЧНЫЙ РАСЧЁТ ============
    calculateTotalWeight();
});