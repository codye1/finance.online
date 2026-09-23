const settingsValidators = {
    inviteMemberFormRules: {
        rules: {
            email: {
                required: true,
                email: true
            },
            role: {
                required: true
            }
        },
        messages: {
            email: {
                required: "Вкажіть email користувача",
                email: "Введіть коректний email"
            },
            role: {
                required: "Оберіть роль"
            }
        },
        errorClass: "form-error-text",
        errorElement: "span"
    }
};

export default settingsValidators;