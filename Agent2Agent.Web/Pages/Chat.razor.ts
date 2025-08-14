export function ScrollToBottom(containerClass) {
    const container = document.querySelector(`.${containerClass}`);
    if (container) {
        setTimeout(() => {
            container.scroll({ top: container.scrollHeight + 150, behavior: 'smooth' });
            console.log(`Scrolled to the bottom of container with class: ${containerClass}`);
        }, 50); // Small delay to ensure DOM updates
    }
}