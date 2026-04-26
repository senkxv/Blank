let goodsRows = [];

function addGoodsRow(data = null) {
    const rowId = Date.now();
    const row = {
        id: rowId,
        goodsId: data?.goodsId || '',
        quantity: data?.quantity || 0,
        price: data?.price || 0,
        discount: data?.discount || 0,
        vatRate: data?.vatRate || 0,
        weight: data?.weight || 0,
        packages: data?.packages || 0,
        note: data?.note || ''
    };
    goodsRows.push(row);
    renderGoodsTable();
}

function removeGoodsRow(id) {
    goodsRows = goodsRows.filter(r => r.id !== id);
    renderGoodsTable();
}

function renderGoodsTable() {
    const tbody = $('#goodsTable tbody');
    tbody.empty();

    let totalWeight = 0;
    let totalPackages = 0;
    let totalCost = 0;
    let totalVat = 0;

    goodsRows.forEach(row => {
        const goods = goodsList.find(g => g.ид_товара == row.goodsId);
        const cost = row.quantity * row.price;
        const vatAmount = cost * (row.vatRate / 100);
        const totalWithVat = cost + vatAmount;

        totalWeight += row.weight;
        totalPackages += row.packages;
        totalCost += cost;
        totalVat += vatAmount;

        const tr = $('<tr>');
        tr.append($('<td>').html(`<select class="goods-select" data-id="${row.id}">${generateGoodsOptions(row.goodsId)}</select>`));
        tr.append($('<td>').html(goods ? goods.единицы_измерения : ''));
        tr.append($('<td>').html(`<input type="number" class="quantity" data-id="${row.id}" value="${row.quantity}" step="0.001">`));
        tr.append($('<td>').html(`<input type="number" class="price" data-id="${row.id}" value="${row.price}" step="0.01">`));
        tr.append($('<td>').html(cost.toFixed(2)));
        tr.append($('<td>').html(`<input type="number" class="vat-rate" data-id="${row.id}" value="${row.vatRate}" step="0.01" style="width:60px">`));
        tr.append($('<td>').html(vatAmount.toFixed(2)));
        tr.append($('<td>').html(totalWithVat.toFixed(2)));
        tr.append($('<td>').html(`<input type="number" class="weight" data-id="${row.id}" value="${row.weight}" step="0.001">`));
        tr.append($('<td>').html(`<input type="number" class="packages" data-id="${row.id}" value="${row.packages}">`));
        tr.append($('<td>').html(`<input type="text" class="note" data-id="${row.id}" value="${row.note}" style="width:100px">`));
        tr.append($('<td>').html(`<button type="button" class="remove-row" data-id="${row.id}">✖</button>`));
        tbody.append(tr);
    });

    $('#totalWeight').text(totalWeight.toFixed(3));
    $('#totalPackages').text(totalPackages);
    $('#totalCost').text(totalCost.toFixed(2));
    $('#totalVat').text(totalVat.toFixed(2));
}

function generateGoodsOptions(selectedId) {
    let html = '<option value="">-- Выберите товар --</option>';
    goodsList.forEach(g => {
        html += `<option value="${g.ид_товара}" ${selectedId == g.ид_товара ? 'selected' : ''}>${g.наименование}</option>`;
    });
    return html;
}

// Обработчики событий
$(document).on('change', '.goods-select', function () {
    const id = $(this).data('id');
    const goodsId = $(this).val();
    const goods = goodsList.find(g => g.ид_товара == goodsId);
    const row = goodsRows.find(r => r.id == id);
    if (row && goods) {
        row.goodsId = parseInt(goodsId);
        row.unit = goods.единицы_измерения;
        renderGoodsTable();
    }
});

$(document).on('change', '.quantity', function () {
    const id = $(this).data('id');
    const row = goodsRows.find(r => r.id == id);
    if (row) {
        row.quantity = parseFloat($(this).val()) || 0;
        renderGoodsTable();
    }
});

$(document).on('change', '.price', function () {
    const id = $(this).data('id');
    const row = goodsRows.find(r => r.id == id);
    if (row) {
        row.price = parseFloat($(this).val()) || 0;
        renderGoodsTable();
    }
});

$(document).on('change', '.vat-rate', function () {
    const id = $(this).data('id');
    const row = goodsRows.find(r => r.id == id);
    if (row) {
        row.vatRate = parseFloat($(this).val()) || 0;
        renderGoodsTable();
    }
});

$(document).on('change', '.weight', function () {
    const id = $(this).data('id');
    const row = goodsRows.find(r => r.id == id);
    if (row) {
        row.weight = parseFloat($(this).val()) || 0;
        renderGoodsTable();
    }
});

$(document).on('change', '.packages', function () {
    const id = $(this).data('id');
    const row = goodsRows.find(r => r.id == id);
    if (row) {
        row.packages = parseInt($(this).val()) || 0;
        renderGoodsTable();
    }
});

$(document).on('click', '.remove-row', function () {
    const id = $(this).data('id');
    removeGoodsRow(id);
});