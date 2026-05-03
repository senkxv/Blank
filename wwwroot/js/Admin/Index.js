document.addEventListener('DOMContentLoaded', function () {

    // ============ ВКЛАДКИ ============
    document.querySelectorAll('.tab').forEach(button => {
        button.addEventListener('click', function () {
            document.querySelectorAll('.tab').forEach(b => b.classList.remove('active'));
            document.querySelectorAll('.tab-content').forEach(c => c.classList.remove('active'));

            this.classList.add('active');
            document.getElementById('tab-' + this.dataset.tab).classList.add('active');
        });
    });

    // ============ ПРОФИЛЬ КОМПАНИИ ============
    document.getElementById('companyForm')?.addEventListener('submit', function (e) {
        e.preventDefault();
        const data = {
            name: document.getElementById('companyName').value,
            unp: document.getElementById('companyUnp').value,
            address: document.getElementById('companyAddress').value,
            email: document.getElementById('companyEmail').value
        };

        fetch('/Admin/UpdateCompany', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify(data)
        })
            .then(r => r.json())
            .then(res => {
                alert(res.success ? 'Профиль обновлён' : 'Ошибка');
            });
    });

    // ============ УНИВЕРСАЛЬНЫЕ ФУНКЦИИ ДЛЯ ТАБЛИЦ ============

    // Добавление записи
    function setupAddForm(formId, url, getRowData, tableBodyId) {
        document.getElementById(formId)?.addEventListener('submit', function (e) {
            e.preventDefault();
            const formData = getRowData(this);

            fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(formData)
            })
                .then(r => r.json())
                .then(res => {
                    if (res.success) {
                        alert('Добавлено');
                        location.reload();
                    } else {
                        alert('Ошибка');
                    }
                });
        });
    }

    // Удаление записи
    function setupDelete(tableBodyId, deleteUrl) {
        document.getElementById(tableBodyId)?.addEventListener('click', function (e) {
            if (e.target.classList.contains('btn-delete')) {
                if (!confirm('Удалить?')) return;
                const row = e.target.closest('tr');
                const id = row.dataset.id;

                fetch(`${deleteUrl}?id=${id}`, { method: 'DELETE' })
                    .then(r => r.json())
                    .then(res => {
                        if (res.success) {
                            row.remove();
                        } else {
                            alert('Ошибка');
                        }
                    });
            }
        });
    }

    // Обновление записи
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
                        alert(res.success ? 'Сохранено' : 'Ошибка');
                    });
            }
        });
    }

    // ============ ВОДИТЕЛИ ============
    setupAddForm('addDriverForm', '/Admin/AddDriver', function (form) {
        return {
            lastName: form.querySelector('[name="lastName"]').value,
            firstName: form.querySelector('[name="firstName"]').value,
            middleName: form.querySelector('[name="middleName"]').value,
            licenseNumber: form.querySelector('[name="licenseNumber"]').value
        };
    }, 'driversTableBody');

    setupDelete('driversTableBody', '/Admin/DeleteDriver');

    setupUpdate('driversTableBody', '/Admin/UpdateDriver', function (row) {
        return {
            lastName: row.querySelector('.edit-lastname').value,
            firstName: row.querySelector('.edit-firstname').value,
            middleName: row.querySelector('.edit-middlename').value,
            licenseNumber: row.querySelector('.edit-license').value
        };
    });

    // ============ ТРАНСПОРТ ============
    setupAddForm('addTransportForm', '/Admin/AddTransport', function (form) {
        return {
            regNumber: form.querySelector('[name="regNumber"]').value,
            brandId: form.querySelector('[name="brandId"]').value,
            typeId: form.querySelector('[name="typeId"]').value
        };
    }, 'transportTableBody');

    setupDelete('transportTableBody', '/Admin/DeleteTransport');

    setupUpdate('transportTableBody', '/Admin/UpdateTransport', function (row) {
        return {
            regNumber: row.querySelector('.edit-regnumber').value,
            brandId: row.querySelector('.edit-brand').value,
            typeId: row.querySelector('.edit-type').value
        };
    });

    // ============ ТОВАРЫ ============
    setupAddForm('addGoodsForm', '/Admin/AddGoods', function (form) {
        return {
            code: form.querySelector('[name="code"]').value,
            name: form.querySelector('[name="name"]').value,
            unit: form.querySelector('[name="unit"]').value
        };
    }, 'goodsTableBody');

    setupDelete('goodsTableBody', '/Admin/DeleteGoods');

    setupUpdate('goodsTableBody', '/Admin/UpdateGoods', function (row) {
        return {
            code: row.querySelector('.edit-code').value,
            name: row.querySelector('.edit-name').value,
            unit: row.querySelector('.edit-unit').value
        };
    });

    // ============ ПУНКТЫ ПОГРУЗКИ ============
    setupAddForm('addLoadingForm', '/Admin/AddLoadingPoint', function (form) {
        return {
            name: form.querySelector('[name="name"]').value,
            address: form.querySelector('[name="address"]').value
        };
    }, 'loadingTableBody');

    setupDelete('loadingTableBody', '/Admin/DeleteLoadingPoint');

    setupUpdate('loadingTableBody', '/Admin/UpdateLoadingPoint', function (row) {
        return {
            name: row.querySelector('.edit-name').value,
            address: row.querySelector('.edit-address').value
        };
    });

    // ============ ПУНКТЫ РАЗГРУЗКИ ============
    setupAddForm('addUnloadingForm', '/Admin/AddUnloadingPoint', function (form) {
        return {
            name: form.querySelector('[name="name"]').value,
            address: form.querySelector('[name="address"]').value
        };
    }, 'unloadingTableBody');

    setupDelete('unloadingTableBody', '/Admin/DeleteUnloadingPoint');

    setupUpdate('unloadingTableBody', '/Admin/UpdateUnloadingPoint', function (row) {
        return {
            name: row.querySelector('.edit-name').value,
            address: row.querySelector('.edit-address').value
        };
    });

    // ============ ПОЛЬЗОВАТЕЛИ ============
    setupAddForm('addUserForm', '/Admin/AddUser', function (form) {
        return {
            email: form.querySelector('[name="email"]').value,
            lastName: form.querySelector('[name="lastName"]').value,
            firstName: form.querySelector('[name="firstName"]').value,
            middleName: form.querySelector('[name="middleName"]').value,
            password: form.querySelector('[name="password"]').value,
            roleId: form.querySelector('[name="roleId"]').value
        };
    }, 'usersTableBody');

    setupDelete('usersTableBody', '/Admin/DeleteUser');

    setupUpdate('usersTableBody', '/Admin/UpdateUserRole', function (row) {
        return {
            roleId: row.querySelector('.edit-role').value
        };
    });
});