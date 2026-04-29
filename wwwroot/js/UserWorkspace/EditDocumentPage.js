const goodsList = window.goodsListData || [];
let deletedIds = [];

function getUnit(goodsId) {
    const goods = goodsList.find(g => g.ид_товара == goodsId);
    return goods ? goods.единицы_измерения : '';
}

function updateRowCalculations(row) {
    const qty = parseFloat(row.querySelector('.goods-quantity').value) || 0;
    const price = parseFloat(row.querySelector('.goods-price').value) || 0;
    const sum = qty * price;
    row.querySelector('.goods-sum').textContent = sum.toFixed(2);

    calculateTotalWeight();
}

function calculateTotalWeight() {
    let totalWeight = 0;
    document.querySelectorAll('.goods-weight').forEach(input => {
        totalWeight += parseFloat(input.value) || 0;
    });
    document.getElementById('totalWeight').textContent = totalWeight.toFixed(3);
}

function addNewRow() {
    const noDataRow = document.getElementById('noDataRow');
    if (noDataRow) noDataRow.remove();

    const tbody = document.getElementById('goodsTableBody');
    const newRow = document.createElement('tr');

    const goodsOptions = '<option value="">-- Выберите товар --</option>' +
        goodsList.map(g => `<option value="${g.ид_товара}">${g.наименование}</option>`).join('');

    newRow.innerHTML = `
        <td><select class="goods-select" style="width:100%">${goodsOptions}</select></td>
        <td><input type="text" class="goods-unit" readonly style="width:60px"></td>
        <td><input type="number" class="goods-quantity" value="0" step="0.001" style="width:90px"></td>
        <td><input type="number" class="goods-price" value="0" step="0.01" style="width:100px"></td>
        <td><input type="number" class="goods-vat" value="0" step="0.5" style="width:70px"></td>
        <td class="goods-sum">0.00</td>
        <td><input type="number" class="goods-weight" value="0" step="0.001" style="width:80px"></td>
        <td><button type="button" class="remove-goods">✖</button></td>
    `;

    tbody.appendChild(newRow);
    updateRowCalculations(newRow);
}

function removeGoodsRow(button) {
    const row = button.closest('tr');
    const id = row.dataset.id;
    const isExisting = row.dataset.isExisting === 'true';

    if (isExisting && id) {
        deletedIds.push(parseInt(id));
        document.getElementById('deletedPositions').value = deletedIds.join(',');
    }

    row.remove();
    calculateTotalWeight();

    if (document.querySelectorAll('#goodsTableBody tr').length === 0) {
        document.getElementById('goodsTableBody').innerHTML =
            '<tr id="noDataRow"><td colspan="8" style="text-align: center;">Нет добавленных товаров</td></tr>';
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

        if (goodsId > 0 && quantity > 0 && price > 0) {
            positions.push({
                id: row.dataset.isExisting === 'true' ? (parseInt(row.dataset.id) || 0) : 0,
                goodsId: goodsId,
                quantity: quantity,
                price: price,
                vatRate: parseFloat(row.querySelector('.goods-vat')?.value) || 0,
                weight: parseFloat(row.querySelector('.goods-weight')?.value) || 0,
                discount: 0,
                packages: 0,
                note: ''
            });
            hasValidPositions = true;
        }
    });

    return { positions, hasValidPositions };
}

document.addEventListener('DOMContentLoaded', function () {
    const addBtn = document.getElementById('addGoodsBtn');
    if (addBtn) {
        addBtn.addEventListener('click', addNewRow);
    }

    document.getElementById('goodsTableBody')?.addEventListener('change', function (e) {
        if (e.target.classList.contains('goods-select')) {
            const goodsId = parseInt(e.target.value);
            const unit = getUnit(goodsId);
            e.target.closest('tr').querySelector('.goods-unit').value = unit;
            updateRowCalculations(e.target.closest('tr'));
        }
    });

    document.getElementById('goodsTableBody')?.addEventListener('input', function (e) {
        if (e.target.classList.contains('goods-quantity') ||
            e.target.classList.contains('goods-price') ||
            e.target.classList.contains('goods-vat')) {
            updateRowCalculations(e.target.closest('tr'));
        }
        if (e.target.classList.contains('goods-weight')) {
            calculateTotalWeight();
        }
    });

    document.getElementById('goodsTableBody')?.addEventListener('click', function (e) {
        if (e.target.classList.contains('remove-goods')) {
            removeGoodsRow(e.target);
        }
    });

    const form = document.getElementById('documentForm');
    if (form) {
        form.addEventListener('submit', function (e) {
            const { positions, hasValidPositions } = collectPositions();

            if (!hasValidPositions && positions.length === 0) {
                alert('Добавьте хотя бы одну позицию товара с заполненными полями!');
                e.preventDefault();
                return false;
            }

            document.getElementById('positionsData').value = JSON.stringify(positions);
        });
    }

    document.querySelectorAll('.goods-quantity, .goods-price, .goods-vat').forEach(input => {
        updateRowCalculations(input.closest('tr'));
    });
    calculateTotalWeight();
});