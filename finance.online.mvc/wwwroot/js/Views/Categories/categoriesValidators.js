const categoriesValidators = {
    categoryFormRules: {
        rules: {
            name: {
                required: true,
                maxlength: 100
            }
        },
        messages: {
            name: {
                required: "Вкажіть назву категорії",
                maxlength: "Назва має бути не довше 100 символів"
            }
        },
        errorClass: "form-error-text",
        errorElement: "span"
    }
};

export default categoriesValidators;