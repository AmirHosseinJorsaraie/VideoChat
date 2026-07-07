function signInLoading(form) {
    return submitLoading(form);
}

function submitLoading(form) {
    const button = form.querySelector("button[type='submit']");

    if (!button || button.disabled)
        return false;

    button.disabled = true;
    button.querySelector(".btn-text")?.classList.add("d-none");
    button.querySelector(".loading-content")?.classList.remove("d-none");

    return true;
}

document.addEventListener("DOMContentLoaded", () => {
    document.body.classList.add("ui-ready");

    document.addEventListener("pointerdown", event => {
        const button = event.target.closest("button");
        if (!button || button.disabled)
            return;

        const rect = button.getBoundingClientRect();
        const ripple = document.createElement("span");
        const size = Math.max(rect.width, rect.height);

        ripple.className = "ripple";
        ripple.style.width = `${size}px`;
        ripple.style.height = `${size}px`;
        ripple.style.left = `${event.clientX - rect.left}px`;
        ripple.style.top = `${event.clientY - rect.top}px`;

        button.appendChild(ripple);
        ripple.addEventListener("animationend", () => ripple.remove(), { once: true });
    });
});
