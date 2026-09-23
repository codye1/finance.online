const operationsValidators = {
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
    }
};

export default operationsValidators;
