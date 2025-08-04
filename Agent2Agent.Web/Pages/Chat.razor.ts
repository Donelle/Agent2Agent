export function ScrollToBottom(elementId) {
    const container = document.getElementById(elementId);
    if (container) {
        container.scrollTop = container.scrollHeight;
    }
}