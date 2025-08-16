export function ScrollToBottom(containerClass) {
    const container = document.querySelector(`.${containerClass}`);
    if (container) {
        setTimeout(() => {
            container.scroll({ top: container.scrollHeight + 150, behavior: 'smooth' });
            console.log(`Scrolled to the bottom of container with class: ${containerClass}`);
        }, 50); // Small delay to ensure DOM updates
    }
}

export function getThreadId() {
    try {
        return sessionStorage.getItem('chat_thread_id');
    } catch (e) {
        console.warn('getThreadId failed', e);
        return null;
    }
}

export function setThreadId(id) {
    try {
        sessionStorage.setItem('chat_thread_id', id);
    } catch (e) {
        console.warn('setThreadId failed', e);
    }
}