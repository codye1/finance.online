function getApiErrorMessage(xhr, fallback = 'Сталася помилка. Спробуйте ще раз.') {
    const json = xhr?.responseJSON;

    if (json?.errors && typeof json.errors === 'object') {
        const messages = Object.values(json.errors).flat().filter(Boolean);
        if (messages.length) {
            return messages.join(' ');
        }
    }

    if (Array.isArray(json)) {
        const messages = json.map(e => e?.message).filter(Boolean);
        if (messages.length) {
            return messages.join(' ');
        }
    }

    if (typeof json?.message === 'string' && json.message) {
        return json.message;
    }

    return fallback;
}

function showApiErrors($form, errors) {
    const validatorInstance = $form.validate();
    const errorMap = {};
    const generalMessages = [];

    // прибираємо повідомлення від попередньої відправки
    $form.find('.server-error').remove();

    // name поля у формі (у нижньому регістрі) -> реальний name
    const fieldNames = {};
    $form.find('[name]').each(function () {
        fieldNames[this.name.toLowerCase()] = this.name;
    });

    Object.entries(errors).forEach(([field, messages]) => {
        const list = Array.isArray(messages) ? messages : [messages];
        const message = list[0];
        if (!message) return;

        const realName = fieldNames[field.toLowerCase()];

        if (field === '_general' || !realName) {
            generalMessages.push(message);
            return;
        }

        errorMap[realName] = message;
    });

    generalMessages.forEach((message) => {
        $form.prepend($('<div class="alert alert-danger server-error"></div>').text(message));
    });

    if (Object.keys(errorMap).length) {
        validatorInstance.showErrors(errorMap);
    }
}

export { showApiErrors, getApiErrorMessage };