const goodsList = window.goodsListData || [];
let deletedIds = [];

function getUnit(goodsId) {
    const goods = goodsList.find(g => g.ид_товара == goodsId);
    return goods ? goods.единицы_измерения : '';
}

function updateRowCalculations(row) {
    const qty = parseFloat(row.querySelector('.goods-quantity')?.value) || 0;
    const price = parseFloat(row.querySelector('.goods-price')?.value) || 0;
    const discount = parseFloat(row.querySelector('.goods-discount')?.value) || 0;
    const vat = parseFloat(row.querySelector('.goods-vat')?.value) || 0;

    // Стоимость без скидки
    const costWithoutDiscount = qty * price;

    // Скидка в деньгах
    const discountAmount = costWithoutDiscount * (discount / 100);

    // Стоимость после скидки (база для НДС)
    const costAfterDiscount = costWithoutDiscount - discountAmount;

    // НДС от стоимости после скидки
    const vatAmount = costAfterDiscount * (vat / 100);

    // Итого с НДС
    const totalWithVat = costAfterDiscount + vatAmount;

    // Заполняем ячейки (проверяем их наличие)
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

function removeGoodsRow(button) {
    const row = button.closest('tr');
    const id = row.getAttribute('data-id');
    const isExisting = row.getAttribute('data-is-existing') === 'true';

    if (isExisting && id) {
        deletedIds.push(parseInt(id));
        document.getElementById('deletedPositions').value = deletedIds.join(',');
    }

    row.remove();
    calculateTotalWeight();

    // Если все строки удалены - показываем заглушку
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

        // ✅ Разрешаем quantity = 0 и price = 0 для существующих позиций
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

    console.log('Собраны позиции:', positions);  // ✅ Для отладки
    return { positions, hasValidPositions };
}

// Инициализация при загрузке страницы
document.addEventListener('DOMContentLoaded', function () {
    // Кнопка добавления товара
    const addBtn = document.getElementById('addGoodsBtn');
    if (addBtn) {
        addBtn.addEventListener('click', addNewRow);
    }

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

    // Ввод чисел (количество, цена, скидка, НДС)
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

    // Удаление товара
    document.getElementById('goodsTableBody')?.addEventListener('click', function (e) {
        if (e.target.classList.contains('remove-goods')) {
            if (confirm('Удалить эту позицию?')) {
                removeGoodsRow(e.target);
            }
        }
    });

    // Отправка формы
    const form = document.getElementById('documentForm');
    if (form) {
        form.addEventListener('submit', function (e) {
            const { positions, hasValidPositions } = collectPositions();

            // Проверяем, есть ли товары в таблице вообще
            const hasRows = document.querySelectorAll('#goodsTableBody tr:not(#noDataRow)').length > 0;

            if (!hasRows) {
                alert('Добавьте хотя бы одну позицию товара!');
                e.preventDefault();
                return false;
            }

            if (!hasValidPositions) {
                alert('Заполните все обязательные поля в позициях товаров (товар, количество, цена)!');
                e.preventDefault();
                return false;
            }

            document.getElementById('positionsData').value = JSON.stringify(positions);

            // Для отладки можно посмотреть что отправляется
            console.log('Отправляемые позиции:', positions);
        });
    }

    // Инициализация расчётов для существующих строк
    document.querySelectorAll('#goodsTableBody tr[data-is-existing="true"]').forEach(row => {
        updateRowCalculations(row);
    });

    // Первоначальный расчёт общей массы
    calculateTotalWeight();
});