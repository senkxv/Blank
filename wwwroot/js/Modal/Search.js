document.addEventListener('DOMContentLoaded', function () {
    const searchModal = document.getElementById('searchModal');
    const searchLink = document.getElementById('searchLink');
    const closeSearchBtn = document.getElementById('closeSearchBtn');

    if (searchLink && searchModal) {
        searchLink.addEventListener('click', function (e) {
            e.preventDefault();
            searchModal.style.display = 'block';
        });
    }

    if (closeSearchBtn && searchModal) {
        closeSearchBtn.addEventListener('click', function () {
            searchModal.style.display = 'none';
        });
    }

    window.addEventListener('click', function (e) {
        if (searchModal && e.target === searchModal) {
            searchModal.style.display = 'none';
        }
    });

    const searchInput = document.getElementById('searchInput');
    if (searchInput) {
        let timeoutId;
        searchInput.addEventListener('input', function () {
            clearTimeout(timeoutId);
            timeoutId = setTimeout(() => {
                const searchValue = this.value;
                window.location.href = '/UserWorkspace/Search?searchString=' + encodeURIComponent(searchValue);
            }, 300);
        });
    }
});