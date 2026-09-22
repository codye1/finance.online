import { login, register } from './authApi.js';
import { initCustomRules, loginRules, registerRules } from './authValidator.js';
import { showApiErrors } from '../../helpers/showApiErrors.js';

$(function () {
    initCustomRules();

    function togglePanels() {
        $('#loginPanel, #registerPanel').toggleClass('hidden');
        $('#loginError, #registerError').hide();
    }

    $('.toggle-auth-panels').on('click', togglePanels);

    $('#loginForm').validate({
        ...loginRules,
        submitHandler: function (form, e) {
            e.preventDefault();
            const $form = $(form);
            const errorBox = $('#loginError').hide();
            const btn = $(form).find('button[type="submit"]');

            btn.prop('disabled', true).addClass('loading');

            login($('#loginEmail').val(), $('#loginPassword').val())
                .done(function () {
                    window.location.href = '/';
                })
                .fail(function (xhr) {
                    handleApiErrors(xhr, errorBox, "Помилка авторизації.");

                    showApiErrors($form, xhr.responseJSON.errors);
                })
                .always(function () {
                    btn.prop('disabled', false).removeClass('loading');
                });
        }
    });

    $('#registerForm').validate({
        ...registerRules,
        submitHandler: function (form, e) {
            e.preventDefault();
            const $form = $(form);
            const errorBox = $('#registerError').hide();
            const btn = $(form).find('button[type="submit"]');

            btn.prop('disabled', true).addClass('loading');

            register($('#registerEmail').val(), $('#registerPassword').val())
                .done(function () {
                    alert("Реєстрація успішна! Тепер ви можете увійти.");
                    togglePanels();
                })
                .fail(function (xhr) {
                    showApiErrors($form, xhr.responseJSON.errors);
                })
                .always(function () {
                    btn.prop('disabled', false).removeClass('loading');
                });
        }
    });

    window.togglePanels = togglePanels;
});