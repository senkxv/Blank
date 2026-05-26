let confirmCallback = null;

function showNotification(message) {
    const modal = document.getElementById('notificationModal');
    const messageEl = document.getElementById('notificationMessage');
    if (modal && messageEl) {
        messageEl.textContent = message;
        modal.style.display = 'block';
    } else {
        alert(message);
    }
}

function showConfirm(message, onConfirm) {
    const modal = document.getElementById('confirmActionModal');
    const messageEl = document.getElementById('confirmActionMessage');
    if (modal && messageEl) {
        messageEl.textContent = message;
        modal.style.display = 'block';
        confirmCallback = onConfirm;
    } else {
        if (confirm(message)) onConfirm();
    }
}

document.addEventListener('DOMContentLoaded', function () {
    const savedTab = sessionStorage.getItem('activeAdminTab');
    if (savedTab) {
        const tab = document.querySelector(`.tab[data-tab="${savedTab}"]`);
        if (tab) {
            document.querySelectorAll('.tab').forEach(b => b.classList.remove('active'));
            document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));
            tab.classList.add('active');
            document.getElementById('tab-' + savedTab).classList.add('active');
        }
        sessionStorage.removeItem('activeAdminTab');
    }
    document.body.classList.remove('admin-loading');

    document.querySelectorAll('.tab').forEach(button => {
        button.addEventListener('click', function () {
            document.querySelectorAll('.tab').forEach(b => b.classList.remove('active'));
            document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));

            this.classList.add('active');
            document.getElementById('tab-' + this.dataset.tab).classList.add('active');
        });
    });

    document.getElementById('confirmActionYes')?.addEventListener('click', function () {
        document.getElementById('confirmActionModal').style.display = 'none';
        if (confirmCallback) confirmCallback();
        confirmCallback = null;
    });

    document.getElementById('confirmActionNo')?.addEventListener('click', function () {
        document.getElementById('confirmActionModal').style.display = 'none';
        confirmCallback = null;
    });

    document.getElementById('confirmActionModal')?.addEventListener('click', function (e) {
        if (e.target === this) {
            this.style.display = 'none';
            confirmCallback = null;
        }
    });

    document.getElementById('closeNotificationBtn')?.addEventListener('click', function () {
        document.getElementById('notificationModal').style.display = 'none';
    });

    document.getElementById('notificationModal')?.addEventListener('click', function (e) {
        if (e.target === this) {
            this.style.display = 'none';
        }
    });

    function setupAddForm(formId, url, getRowData) {
        document.getElementById(formId)?.addEventListener('submit', function (e) {
            e.preventDefault();
            const formData = getRowData(this);

            const activeTab = document.querySelector('.tab.active')?.dataset.tab;
            if (activeTab) {
                sessionStorage.setItem('activeAdminTab', activeTab);
            }

            fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(formData)
            })
                .then(r => r.json())
                .then(res => {
                    if (res.success) {
                        document.getElementById('notificationModal').setAttribute('data-reload', 'true');
                        showNotification('Добавлено');
                    } else {
                        showNotification('Ошибка: ' + (res.error || ''));
                    }
                });
        });
    }

    function setupDelete(tableBodyId, deleteUrl) {
        document.getElementById(tableBodyId)?.addEventListener('click', function (e) {
            if (e.target.classList.contains('btn-delete')) {
                const row = e.target.closest('tr');
                const id = row.dataset.id;

                showConfirm('Удалить?', function () {
                    fetch(`${deleteUrl}?id=${id}`, { method: 'DELETE' })
                        .then(r => r.json())
                        .then(res => {
                            if (res.success) {
                                row.remove();
                            } else {
                                showNotification(res.error || 'Ошибка');
                            }
                        });
                });
            }
        });
    }

    function setupUpdate(tableBodyId, updateUrl, getRowData) {
        document.getElementById(tableBodyId)?.addEventListener('click', function (e) {
            if (e.target.classList.contains('btn-update') || e.target.classList.contains('btn-update-role')) {
                const row = e.target.closest('tr');
                const id = row.dataset.id;
                const data = getRowData(row);
                data.id = id;

                fetch(updateUrl, {
                    method: 'PUT',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify(data)
                })
                    .then(r => r.json())
                    .then(res => {
                        showNotification(res.success ? 'Сохранено' : 'Ошибка');
                    });
            }
        });
    }

    setupAddForm('addOrgForm', '/Admin/AddOrganization', function (form) {
        return {
            name: form.querySelector('[name="name"]').value,
            unp: form.querySelector('[name="unp"]').value,
            address: form.querySelector('[name="address"]').value,
            email: form.querySelector('[name="email"]').value
        };
    });

    setupDelete('orgTableBody', '/Admin/DeleteOrganization');

    setupUpdate('orgTableBody', '/Admin/UpdateOrganization', function (row) {
        return {
            name: row.querySelector('.edit-name').value,
            unp: row.querySelector('.edit-unp').value,
            address: row.querySelector('.edit-address').value,
            email: row.querySelector('.edit-email').value
        };
    });

    setupAddForm('addDriverForm', '/Admin/AddDriver', function (form) {
        return {
            lastName: form.querySelector('[name="lastName"]').value,
            firstName: form.querySelector('[name="firstName"]').value,
            middleName: form.querySelector('[name="middleName"]').value,
            licenseNumber: form.querySelector('[name="licenseNumber"]').value
        };
    });

    setupDelete('driversTableBody', '/Admin/DeleteDriver');

    setupUpdate('driversTableBody', '/Admin/UpdateDriver', function (row) {
        return {
            lastName: row.querySelector('.edit-lastname').value,
            firstName: row.querySelector('.edit-firstname').value,
            middleName: row.querySelector('.edit-middlename').value,
            licenseNumber: row.querySelector('.edit-license').value
        };
    });

    setupAddForm('addTransportForm', '/Admin/AddTransport', function (form) {
        return {
            regNumber: form.querySelector('[name="regNumber"]').value,
            brandName: form.querySelector('[name="brandName"]').value,
            typeName: form.querySelector('[name="typeName"]').value
        };
    });

    setupDelete('transportTableBody', '/Admin/DeleteTransport');

    setupUpdate('transportTableBody', '/Admin/UpdateTransport', function (row) {
        return {
            regNumber: row.querySelector('.edit-regnumber')?.value || '',
            brandId: row.querySelector('.edit-brand')?.value || '1',
            typeId: row.querySelector('.edit-type')?.value || '1'
        };
    });

    setupAddForm('addGoodsForm', '/Admin/AddGoods', function (form) {
        return {
            code: form.querySelector('[name="code"]').value,
            name: form.querySelector('[name="name"]').value,
            unit: form.querySelector('[name="unit"]').value
        };
    });

    setupDelete('goodsTableBody', '/Admin/DeleteGoods');

    setupUpdate('goodsTableBody', '/Admin/UpdateGoods', function (row) {
        return {
            code: row.querySelector('.edit-code').value,
            name: row.querySelector('.edit-name').value,
            unit: row.querySelector('.edit-unit').value
        };
    });

    setupAddForm('addLoadingForm', '/Admin/AddLoadingPoint', function (form) {
        return {
            name: form.querySelector('[name="name"]').value,
            address: form.querySelector('[name="address"]').value
        };
    });

    setupDelete('loadingTableBody', '/Admin/DeleteLoadingPoint');

    setupUpdate('loadingTableBody', '/Admin/UpdateLoadingPoint', function (row) {
        return {
            name: row.querySelector('.edit-name').value,
            address: row.querySelector('.edit-address').value
        };
    });

    setupAddForm('addUnloadingForm', '/Admin/AddUnloadingPoint', function (form) {
        return {
            name: form.querySelector('[name="name"]').value,
            address: form.querySelector('[name="address"]').value
        };
    });

    setupDelete('unloadingTableBody', '/Admin/DeleteUnloadingPoint');

    setupUpdate('unloadingTableBody', '/Admin/UpdateUnloadingPoint', function (row) {
        return {
            name: row.querySelector('.edit-name').value,
            address: row.querySelector('.edit-address').value
        };
    });

    setupAddForm('addUserForm', '/Admin/AddUser', function (form) {
        return {
            email: form.querySelector('[name="email"]').value,
            lastName: form.querySelector('[name="lastName"]').value,
            firstName: form.querySelector('[name="firstName"]').value,
            middleName: form.querySelector('[name="middleName"]').value,
            password: form.querySelector('[name="password"]').value,
            roleId: form.querySelector('[name="roleId"]').value
        };
    });

    setupDelete('usersTableBody', '/Admin/DeleteUser');

    setupUpdate('usersTableBody', '/Admin/UpdateUserRole', function (row) {
        return {
            roleId: row.querySelector('.edit-role').value
        };
    });

    let pointCounter = 1;

    document.getElementById('addPointBtn')?.addEventListener('click', function () {
        pointCounter++;
        const container = document.getElementById('routePointsContainer');

        const firstRow = container.querySelector('.route-point-row');
        const newRow = firstRow.cloneNode(true);

        newRow.querySelector('.point-number').textContent = pointCounter;
        newRow.querySelectorAll('select').forEach(select => {
            select.selectedIndex = 0;
        });

        const removeBtn = newRow.querySelector('.btn-remove-point');
        removeBtn.style.display = 'inline-block';
        removeBtn.addEventListener('click', function () {
            newRow.remove();
            updatePointNumbers();
        });

        container.appendChild(newRow);
    });

    function updatePointNumbers() {
        const rows = document.querySelectorAll('#routePointsContainer .route-point-row');
        rows.forEach((row, index) => {
            row.querySelector('.point-number').textContent = index + 1;
        });
    }

    document.getElementById('addRouteForm')?.addEventListener('submit', function (e) {
        const senders = Array.from(this.querySelectorAll('[name="senderId[]"]')).map(s => s.value);
        const loadingPoints = Array.from(this.querySelectorAll('[name="loadingPointId[]"]')).map(s => s.value);
        const unloadingPoints = Array.from(this.querySelectorAll('[name="unloadingPointId[]"]')).map(s => s.value);
        const receivers = Array.from(this.querySelectorAll('[name="receiverId[]"]')).map(s => s.value);

        const routePointsData = loadingPoints.map((lp, index) => ({
            ид_грузоотправителя: senders[index] ? parseInt(senders[index]) : null,
            ид_пункта_погрузки: lp ? parseInt(lp) : null,
            ид_пункта_разгрузки: unloadingPoints[index] ? parseInt(unloadingPoints[index]) : null,
            ид_грузополучателя: receivers[index] ? parseInt(receivers[index]) : null,
            тип_точки: "погрузка"
        }));

        let hiddenField = this.querySelector('[name="routePointsData"]');
        if (!hiddenField) {
            hiddenField = document.createElement('input');
            hiddenField.type = 'hidden';
            hiddenField.name = 'routePointsData';
            this.appendChild(hiddenField);
        }
        hiddenField.value = JSON.stringify(routePointsData);

        sessionStorage.setItem('activeAdminTab', 'routes');
    });

    document.getElementById('addEditPointBtn')?.addEventListener('click', function () {
        const container = document.getElementById('editRoutePointsContainer');
        const rows = container.querySelectorAll('.route-point-row');
        addEditPointRow(container, null, rows.length + 1);
    });

    document.getElementById('editRouteForm')?.addEventListener('submit', async function (e) {
        e.preventDefault();

        const points = [];
        document.querySelectorAll('#editRoutePointsContainer .route-point-row').forEach((row, index) => {
            const pointIdVal = row.querySelector('.point-id')?.value;
            const senderVal = row.querySelector('.edit-sender')?.value;
            const loadingVal = row.querySelector('.edit-loading')?.value;
            const unloadingVal = row.querySelector('.edit-unloading')?.value;
            const receiverVal = row.querySelector('.edit-receiver')?.value;

            points.push({
                ид_точки: pointIdVal ? parseInt(pointIdVal) : null,
                ид_грузоотправителя: senderVal ? parseInt(senderVal) : null,
                ид_пункта_погрузки: loadingVal ? parseInt(loadingVal) : null,
                ид_пункта_разгрузки: unloadingVal ? parseInt(unloadingVal) : null,
                ид_грузополучателя: receiverVal ? parseInt(receiverVal) : null,
                порядковый_номер: index + 1
            });
        });

        const data = {
            id: document.getElementById('editRouteId').value,
            routeName: document.getElementById('editRouteName').value,
            driverId: document.getElementById('editDriverId').value || null,
            transportId: document.getElementById('editTransportId').value || null,
            carrierId: document.getElementById('editCarrierId').value || null,
            status: document.getElementById('editStatus').value,
            routePointsData: JSON.stringify(points)
        };

        try {
            const response = await fetch('/Admin/UpdateRoute', {
                method: 'PUT',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(data)
            });

            if (response.ok) {
                document.getElementById('editRouteModal').style.display = 'none';
                sessionStorage.setItem('activeAdminTab', 'routes');
                document.getElementById('notificationModal').setAttribute('data-reload', 'true');
                showNotification('Маршрут обновлён!');
            } else {
                const result = await response.json();
                showNotification('Ошибка: ' + (result.error || ''));
            }
        } catch (error) {
            showNotification('Ошибка: ' + error.message);
        }
    });
});

async function editRoute(routeId) {
    try {
        const response = await fetch(`/Admin/GetRoute/${routeId}`);
        const route = await response.json();

        document.getElementById('editRouteId').value = route.ид_маршрута;
        document.getElementById('editRouteName').value = route.название;
        document.getElementById('editDriverId').value = route.ид_водителя || '';
        document.getElementById('editTransportId').value = route.ид_транспорта || '';
        document.getElementById('editCarrierId').value = route.ид_перевозчика || '';
        document.getElementById('editStatus').value = route.статус || 'активен';

        const container = document.getElementById('editRoutePointsContainer');
        container.innerHTML = '';

        if (route.точки && route.точки.length > 0) {
            route.точки.forEach((point, index) => {
                addEditPointRow(container, point, index + 1);
            });
        } else {
            addEditPointRow(container, null, 1);
        }

        document.getElementById('editRouteModal').style.display = 'block';
    } catch (error) {
        showNotification('Ошибка загрузки маршрута: ' + error.message);
    }
}

function addEditPointRow(container, point, number) {
    const row = document.createElement('div');
    row.className = 'route-point-row';
    row.style.cssText = 'display:flex; gap:10px; align-items:center; margin-bottom:10px; flex-wrap:wrap;';
    row.innerHTML = `
        <span class="point-number">${number}</span>
        <input type="hidden" class="point-id" value="${point?.ид_точки || ''}" />
        <select class="edit-sender" style="flex:1; min-width:120px;">
            <option value="">Грузоотправитель</option>
            ${getOrganizationsOptions(point?.ид_грузоотправителя)}
        </select>
        <select class="edit-loading" style="flex:1; min-width:120px;">
            <option value="">Пункт погрузки</option>
            ${getLoadingPointsOptions(point?.ид_пункта_погрузки)}
        </select>
        <span>→</span>
        <select class="edit-unloading" style="flex:1; min-width:120px;">
            <option value="">Пункт разгрузки</option>
            ${getUnloadingPointsOptions(point?.ид_пункта_разгрузки)}
        </select>
        <select class="edit-receiver" style="flex:1; min-width:120px;">
            <option value="">Грузополучатель</option>
            ${getOrganizationsOptions(point?.ид_грузополучателя)}
        </select>
        <button type="button" class="btn-remove-point" onclick="this.closest('.route-point-row').remove();">✖</button>
    `;
    container.appendChild(row);
}

function getOrganizationsOptions(selectedId) {
    const firstSenderSelect = document.querySelector('[name="senderId[]"]');
    if (!firstSenderSelect) return '';

    let options = '';
    firstSenderSelect.querySelectorAll('option').forEach(opt => {
        if (opt.value) {
            options += `<option value="${opt.value}" ${opt.value == selectedId ? 'selected' : ''}>${opt.textContent}</option>`;
        }
    });
    return options;
}

function getLoadingPointsOptions(selectedId) {
    const firstLoadingSelect = document.querySelector('[name="loadingPointId[]"]');
    if (!firstLoadingSelect) return '';

    let options = '';
    firstLoadingSelect.querySelectorAll('option').forEach(opt => {
        if (opt.value) {
            options += `<option value="${opt.value}" ${opt.value == selectedId ? 'selected' : ''}>${opt.textContent}</option>`;
        }
    });
    return options;
}

function getUnloadingPointsOptions(selectedId) {
    const firstUnloadingSelect = document.querySelector('[name="unloadingPointId[]"]');
    if (!firstUnloadingSelect) return '';

    let options = '';
    firstUnloadingSelect.querySelectorAll('option').forEach(opt => {
        if (opt.value) {
            options += `<option value="${opt.value}" ${opt.value == selectedId ? 'selected' : ''}>${opt.textContent}</option>`;
        }
    });
    return options;
}

async function deleteRoute(routeId) {
    showConfirm('Удалить маршрут?', async function () {
        try {
            const response = await fetch(`/Admin/DeleteRoute/${routeId}`, {
                method: 'DELETE'
            });

            if (response.ok) {
                sessionStorage.setItem('activeAdminTab', 'routes');
                document.getElementById('notificationModal').setAttribute('data-reload', 'true');
                showNotification('Маршрут удалён');
            } else {
                const text = await response.text();
                let result;
                if (text) {
                    try { result = JSON.parse(text); } catch (e) { result = { error: text }; }
                }
                showNotification('Ошибка: ' + (result?.error || 'Неизвестная ошибка'));
            }
        } catch (error) {
            showNotification('Ошибка: ' + error.message);
        }
    });
}