function handleApiErrors(xhr, errorBox, defaultMessage = "Помилка запиту на сервері.") {
    let errors = xhr.responseJSON;
    let errorHtml = '<ul style="margin: 0; padding-left: 20px; text-align: left;">';
    let uniqueMessages = new Set();

    if (Array.isArray(errors)) {
        errors.forEach(e => {
            if (e.message) {
                uniqueMessages.add(e.message);
            }
        });
    }
    else {
        let textError = xhr.responseText || defaultMessage;
        uniqueMessages.add(textError);
    }

    uniqueMessages.forEach(msg => {
        errorHtml += `<li>${msg}</li>`;
    });
    errorHtml += '</ul>';

    errorBox.html(errorHtml).show();
}

export { handleApiErrors };