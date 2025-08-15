window.scrollToBottom = (elementId, offset = 0) => {
    const el = document.getElementById(elementId);
    if (el) {
        el.scrollTo({
            top: el.scrollHeight,
            behavior: 'smooth'
        });
    }
};


window.resizeTextarea = (elementId) => {
    const element = document.getElementById(elementId);
    if (!element) return;

    element.style.height = 'auto';

    const maxHeight = 96; // px
    const newHeight = Math.min(element.scrollHeight, maxHeight);

    element.style.height = newHeight + 'px';
};
