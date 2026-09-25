    function showApiErrors($form, errors) {
        const validatorInstance = $form.validate();
        const errorMap = {};
        console.log('API Errors:', errors);
        Object.entries(errors).forEach(([field, messages]) => {
            if (field === '_general') {
                $form.prepend(`<div class="alert alert-danger server-error">${messages[0]}</div>`);
                return;
            }
            errorMap[field] = messages[0];
        });

        if (Object.keys(errorMap).length) {
            validatorInstance.showErrors(errorMap);
        }
    }

    export { showApiErrors };