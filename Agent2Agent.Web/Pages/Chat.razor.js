export function ScrollToBottom(containerClass) {
    const container = document.querySelector(`.${containerClass}`);
    if (container) {
        container.scroll({ top: container.scrollHeight + 50, behavior: 'smooth' });
        console.log(`Scrolled to the bottom of container with class: ${containerClass}`);
    }
}
//# sourceMappingURL=Chat.razor.js.map