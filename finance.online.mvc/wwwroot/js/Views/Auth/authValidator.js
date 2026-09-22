const initCustomRules = () => {
    if (!$.validator.methods.strongPassword) {
        $.validator.addMethod("strongPassword", function (value, element) {
            return this.optional(element)
                || /[A-Z]/.test(value)
                && /[a-z]/.test(value)
                && /[0-9]/.test(value);
        }, "Пароль повинен містити великі, малі літери та цифри.");
    }
}
const loginRules = {
    rules: {
        email: { required: true, email: true },
        password: { required: true }
    },
    messages: {
        email: { required: "Будь ласка, введіть пошту.", email: "Введіть коректну адресу." },
        password: { required: "Будь ласка, введіть пароль." }
    }
};

const registerRules = {
    rules: {
        email: { required: true, email: true },
        password: { required: true, minlength: 6, strongPassword: true }
    },
    messages: {
        email: { required: "Будь ласка, введіть пошту.", email: "Введіть коректну адресу електронної пошти." },
        password: {
            required: "Будь ласка, введіть пароль.",
            minlength: "Пароль має містити щонайменше 6 символів.",
            strongPassword: "Пароль повинен містити принаймні одну велику літеру, одну малу літеру та одну цифру."
        }
    }
};

export { initCustomRules, loginRules, registerRules };