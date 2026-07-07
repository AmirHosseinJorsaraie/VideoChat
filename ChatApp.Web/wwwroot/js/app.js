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
