window.scrollToBottom = (elementId) => {
    const el = document.getElementById(elementId);
    if (el) {
        el.scrollTo({
            top: el.scrollHeight,
            behavior: 'auto'
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

window.preventEnterDefault = (elementId) => {
    const el = document.getElementById(elementId);
    if (!el) return;

    // cancel the Enter default (newline)
    el.addEventListener("keydown", function (e) {
        if (e.key === "Enter" && !e.shiftKey) {
            e.preventDefault();
        }
    }, { once: true }); // only once, Blazor will reattach if needed
};


function filterAssignList(input) {
    let filter = input.value.toLowerCase();
    let items = input.closest(".dropdown-menu").querySelectorAll(".dropdown-item");
    items.forEach(item => {
        item.style.display = item.textContent.toLowerCase().includes(filter) ? "" : "none";
    });
}


window.imageZoomHelper = {
    enableZoom: function (containerId, dotNetRef) {
        const container = document.getElementById(containerId);
        if (!container) return;

        container.addEventListener("wheel", function (e) {
            if (e.ctrlKey) {
                e.preventDefault(); // stop page zoom
                dotNetRef.invokeMethodAsync("OnImageZoom", e.deltaY);
            }
        }, { passive: false });
    }
};


window.scrollToMessage = (messageId) => {
    const el = document.getElementById("msg_" + messageId);
    if (el) {
        el.scrollIntoView({ behavior: "smooth", block: "center" });
        // optional: highlight temporarily
        el.classList.add("highlight");
        setTimeout(() => el.classList.remove("highlight"), 2000);
    }
};



window.focusElement = (id) => {
    const el = document.getElementById(id);
    if (el) el.focus();
};





window.splitter = {
    startDrag: function (dotnetHelper, mouseDownEvent) {
        const container = document.querySelector(".container");
        const leftPanel = container.querySelector(".left-panel");

        const containerRect = container.getBoundingClientRect();
        const leftRect = leftPanel.getBoundingClientRect();

        // distance between mouse and left panel's right edge when drag starts
        const offset = mouseDownEvent.clientX - leftRect.right;

        function onMouseMove(e) {
            let newWidth = e.clientX - containerRect.left - offset;

            // prevent shrinking too much
            if (newWidth < 100) newWidth = 100;
            if (newWidth > containerRect.width - 100) newWidth = containerRect.width - 100;

            dotnetHelper.invokeMethodAsync("UpdateLeftWidth", newWidth);
        }

        function onMouseUp() {
            document.removeEventListener("mousemove", onMouseMove);
            document.removeEventListener("mouseup", onMouseUp);
        }

        document.addEventListener("mousemove", onMouseMove);
        document.addEventListener("mouseup", onMouseUp);
    }
};

