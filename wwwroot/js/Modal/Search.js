document.addEventListener('DOMContentLoaded', function () {
    const searchModal = document.getElementById('searchModal');
    const searchLink = document.getElementById('searchLink');
    const closeSearchBtn = document.getElementById('closeSearchBtn');

    // Открытие поиска
    if (searchLink && searchModal) {
        searchLink.addEventListener('click', function (e) {
            e.preventDefault();
            searchModal.style.display = 'block';
            sessionStorage.setItem('searchOpen', 'true');
        });
    }

    // Закрытие поиска
    if (closeSearchBtn && searchModal) {
        closeSearchBtn.addEventListener('click', function () {
            searchModal.style.display = 'none';
            sessionStorage.setItem('searchOpen', 'false');
        });
    }

    // Закрытие по клику на фон
    window.addEventListener('click', function (e) {
        if (searchModal && e.target === searchModal) {
            searchModal.style.display = 'none';
            sessionStorage.setItem('searchOpen', 'false');
        }
    });

    // Восстановление состояния после перезагрузки
    if (sessionStorage.getItem('searchOpen') === 'true') {
        if (searchModal) {
            searchModal.style.display = 'block';
        }
    }
});