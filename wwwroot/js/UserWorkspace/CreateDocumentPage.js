const goodsList = window.goodsListData || [];

let goodsRows = [];
let nextId = 1;

function generateGoodsOptions(selectedId) {
    let html = '<option value="">-- Выберите товар --</option>';
    if (goodsList && goodsList.length) {
        goodsList.forEach(g => {
            const selected = (selectedId == g.ид_товара) ? 'selected' : '';
            html += `<option value="${g.ид_товара}" ${selected}>${g.наименование}</option>`;
        });
    }
    return html;
}

function getUnit(goodsId) {
    const goods = goodsList.find(g => g.ид_товара == goodsId);
    return goods ? goods.единицы_измерения : '';
}

function calculateRowAmounts(quantity, price, vatRate) {
    const cost = quantity * price;
    const vatAmount = cost * (vatRate / 100);
    const totalWithVat = cost + vatAmount;
    return { cost, vatAmount, totalWithVat };
}

function recalcTotal() {
    let totalWeight = 0;
    goodsRows.forEach(row => {
        totalWeight += parseFloat(row.weight) || 0;
    });
    const totalWeightElement = document.getElementById('totalWeight');
    if (totalWeightElement) {
        totalWeightElement.textContent = totalWeight.toFixed(3);
    }
}

function updateRowDisplay(row) {
    const quantity = parseFloat(row.quantity) || 0;
    const price = parseFloat(row.price) || 0;
    const vatRate = parseFloat(row.vatRate) || 0;

    const { vatAmount, totalWithVat } = calculateRowAmounts(quantity, price, vatRate);

    const vatElement = document.querySelector(`.vat-amount[data-id="${row.id}"]`);
    const totalElement = document.querySelector(`.total-with-vat[data-id="${row.id}"]`);

    if (vatElement) vatElement.textContent = vatAmount.toFixed(2);
    if (totalElement) totalElement.textContent = totalWithVat.toFixed(2);

    recalcTotal();
}

function renderGoodsTable() {
    const tbody = document.getElementById('goodsTableBody');
    if (!tbody) return;

    tbody.innerHTML = '';

    if (goodsRows.length === 0) {
        tbody.innerHTML = '<tr id="noDataRow"><td colspan="9" style="text-align: center;">Нет добавленных товаров</td></tr>';
        return;
    }

    goodsRows.forEach(row => {
        const quantity = parseFloat(row.quantity) || 0;
        const price = parseFloat(row.price) || 0;
        const vatRate = parseFloat(row.vatRate) || 0;

        const { vatAmount, totalWithVat } = calculateRowAmounts(quantity, price, vatRate);
        const unit = getUnit(row.goodsId);

        const tr = document.createElement('tr');
        tr.setAttribute('data-row-id', row.id);
        tr.innerHTML = `
            <td style="min-width: 150px;">
                <select class="goods-select form-control" data-id="${row.id}" data-goods-id="${row.goodsId}">
                    ${generateGoodsOptions(row.goodsId)}
                </select>
            </td>
            <td class="unit-display" data-id="${row.id}">${unit}</td>
            <td><input type="number" class="quantity form-control" data-id="${row.id}" value="${row.quantity}" step="0.001" style="width: 90px;"></td>
            <td><input type="number" class="price form-control" data-id="${row.id}" value="${row.price}" step="0.01" style="width: 100px;"></td>
            <td><input type="number" class="vat-rate form-control" data-id="${row.id}" value="${row.vatRate}" step="0.5" style="width: 70px;"></td>
            <td class="vat-amount" data-id="${row.id}">${vatAmount.toFixed(2)}</td>
            <td class="total-with-vat" data-id="${row.id}">${totalWithVat.toFixed(2)}</td>
            <td><input type="number" class="weight form-control" data-id="${row.id}" value="${row.weight}" step="0.001" style="width: 90px;"></td>
            <td><button type="button" class="remove-row" data-id="${row.id}">✖</button></td>
        `;
        tbody.appendChild(tr);
    });

    recalcTotal();
}

function addGoodsRow() {
    const noDataRow = document.getElementById('noDataRow');
    if (noDataRow) noDataRow.remove();

    goodsRows.push({
        id: nextId++,
        goodsId: 0,
        quantity: 0,
        price: 0,
        vatRate: 0,
        weight: 0,
        packages: 0,
        note: '',
        discount: 0
    });
    renderGoodsTable();
}

function removeGoodsRow(id) {
    goodsRows = goodsRows.filter(r => r.id != id);
    renderGoodsTable();
}

document.addEventListener('DOMContentLoaded', function () {
    addGoodsRow();


    const addButton = document.getElementById('addGoodsRow');
    if (addButton) {
        addButton.addEventListener('click', addGoodsRow);
    }

    const tbody = document.getElementById('goodsTableBody');
    if (tbody) {
        // Выбор товара
        tbody.addEventListener('change', function (e) {
            if (e.target.classList.contains('goods-select')) {
                const id = parseInt(e.target.dataset.id);
                const goodsId = parseInt(e.target.value);
                const row = goodsRows.find(r => r.id == id);
                if (row) {
                    row.goodsId = goodsId;
                    const unit = getUnit(goodsId);
                    const unitDisplay = document.querySelector(`.unit-display[data-id="${id}"]`);
                    if (unitDisplay) unitDisplay.textContent = unit;
                }
            }
        });

        tbody.addEventListener('input', function (e) {
            if (e.target.classList.contains('quantity')) {
                const id = parseInt(e.target.dataset.id);
                const value = parseFloat(e.target.value) || 0;
                const row = goodsRows.find(r => r.id == id);
                if (row) {
                    row.quantity = value;
                    updateRowDisplay(row);
                }
            }

            if (e.target.classList.contains('price')) {
                const id = parseInt(e.target.dataset.id);
                const value = parseFloat(e.target.value) || 0;
                const row = goodsRows.find(r => r.id == id);
                if (row) {
                    row.price = value;
                    updateRowDisplay(row);
                }
            }

            if (e.target.classList.contains('vat-rate')) {
                const id = parseInt(e.target.dataset.id);
                const value = parseFloat(e.target.value) || 0;
                const row = goodsRows.find(r => r.id == id);
                if (row) {
                    row.vatRate = value;
                    updateRowDisplay(row);
                }
            }

            if (e.target.classList.contains('weight')) {
                const id = parseInt(e.target.dataset.id);
                const value = parseFloat(e.target.value) || 0;
                const row = goodsRows.find(r => r.id == id);
                if (row) {
                    row.weight = value;
                    recalcTotal();
                }
            }
        });

        tbody.addEventListener('click', function (e) {
            if (e.target.classList.contains('remove-row')) {
                const id = parseInt(e.target.dataset.id);
                removeGoodsRow(id);
            }
        });
    }

    const form = document.getElementById('documentForm');
    if (form) {
        form.addEventListener('submit', function (e) {
            const validPositions = goodsRows.filter(row =>
                row.goodsId > 0 &&
                row.quantity > 0 &&
                row.price > 0
            );

            const positions = validPositions.map(row => ({
                id: 0,
                goodsId: row.goodsId,
                quantity: row.quantity,
                price: row.price,
                discount: row.discount || 0,
                vatRate: row.vatRate || 0,
                weight: row.weight || 0,
                packages: row.packages || 0,
                note: row.note || ''
            }));

            const positionsData = document.getElementById('positionsData');
            if (positionsData) {
                positionsData.value = JSON.stringify(positions);
            }

            console.log('Отправляемые позиции:', positions);
            console.log('Количество позиций:', positions.length);

            if (positions.length === 0) {
                alert('Добавьте хотя бы одну позицию товара!');
                e.preventDefault();
                return false;
            }

            return true;
        });
    }
});