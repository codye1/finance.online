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

export default settingsValidators;