// === УНИВЕРСАЛЬНОЕ МОДАЛЬНОЕ ОКНО ДЛЯ УВЕДОМЛЕНИЙ ===

function showNotification(message) {
    const modal = document.getElementById('notificationModal');
    const messageElement = document.getElementById('notificationMessage');

    if (modal && messageElement) {
        messageElement.textContent = message;
        modal.style.display = 'block';
    }
}

function closeNotification() {
    const modal = document.getElementById('notificationModal');
    if (modal) {
        modal.style.display = 'none';
    }
}

// Закрытие по кнопке
document.addEventListener('DOMContentLoaded', function () {
    const closeBtn = document.getElementById('closeNotificationBtn');
    const modal = document.getElementById('notificationModal');

    if (closeBtn) {
        closeBtn.addEventListener('click', closeNotification);
    }

    // Закрытие при клике на фон
    if (modal) {
        modal.addEventListener('click', function (e) {
            if (e.target === modal) {
                closeNotification();
            }
        });
    }
});