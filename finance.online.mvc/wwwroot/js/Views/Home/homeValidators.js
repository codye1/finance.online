const homeValidators = {
    operationFormRules: {
        rules: {
            type: {
                required: true
            },
            amount: {
                required: true,
                number: true,
                min: 0.01
            },
            categoryId: {
                required: true
            },
            date: {
                required: true
            }
        },
        messages: {
            type: {
                required: "Оберіть тип операції"
            },
            amount: {
                required: "Вкажіть суму",
                number: "Сума має бути числом",
                min: "Сума повинна бути більше 0"
            },
            categoryId: {
                required: "Оберіть категорію"
            },
            date: {
                required: "Вкажіть дату"
            }
        },
        errorClass: "form-error-text",
        errorElement: "span"
    },
    organizationFormRules: {
        rules: {
            name: {
                required: true,
                maxlength: 200
            },
            description: {
                maxlength: 1000
            }
        },
        messages: {
            name: {
                required: "Вкажіть назву організації",
                maxlength: "Назва має бути не довше 200 символів"
            },
            description: {
                maxlength: "Опис має бути не довше 1000 символів"
            }
        },
        errorClass: "form-error-text",
        errorElement: "span"
    }
};

export default homeValidators;
