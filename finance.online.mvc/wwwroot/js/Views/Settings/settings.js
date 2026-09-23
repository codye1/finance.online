import api from './settingsApi.js';
import validators from './settingsValidators.js';
import Modal from '../../helpers/ModalManager.js';
import { showApiErrors } from '../../helpers/showApiErrors.js';

$(function () {
    'use strict';

    function initDeleteOrganizationDialog() {
        const $openButton = $('#open-delete-organization-dialog');
        const $dialog = $('#delete-organization-dialog');
        const $cancelButton = $('#cancel-delete-organization');
        const $deleteForm = $('#delete-organization-form');

        if (!$openButton.length || !$dialog.length) {
            return;
        }

        function openDialog() {
            $dialog.addClass('is-active').attr('aria-hidden', 'false');
        }

        function closeDialog() {
            $dialog.removeClass('is-active').attr('aria-hidden', 'true');
        }

        $openButton.on('click', openDialog);

        $cancelButton.on('click', closeDialog);

        $dialog.on('click', function (e) {
            if (e.target === this) {
                closeDialog();
            }
        });

        $(document).on('keydown', function (e) {
            if (e.key === 'Escape' && $dialog.hasClass('is-active')) {
                closeDialog();
            }
        });

        $deleteForm.on('submit', function (e) {
            e.preventDefault();

            const organizationId = $deleteForm.find('input[name="organizationId"]').val();
            const $submitBtn = $deleteForm.find('button[type="submit"]');
            const originalText = $submitBtn.text();

            $submitBtn.prop('disabled', true).text('Видалення...');

            api.deleteOrganization(organizationId)
                .done(function () {
                    localStorage.removeItem('activeOrganizationId');
                    window.location.href = '/';
                })
                .fail(function (xhr) {
                    if (xhr.status === 401) {
                        window.location.href = '/login';
                        return;
                    }

                    alert('Помилка видалення організації');
                    $submitBtn.prop('disabled', false).text(originalText);
                });
        });
    }

    function openInviteMemberModal() {
        if (!Modal) {
            console.error('Global Modal manager library initialization instances not found.');
            return;
        }

        const organizationId = window.TransitData?.organizationId;
        if (!organizationId) {
            alert('Організацію не знайдено');
            return;
        }

        Modal.open('Новий учасник', '#tpl-invite-member', {
            ...validators.inviteMemberFormRules,
            showErrors: function () {
                this.defaultShowErrors();

                const $form = $(this.currentForm);
                const $submitBtn = $form.find('#btn-submit-invite-member');

                $submitBtn.prop('disabled', this.numberOfInvalids() > 0);
            },
            submitHandler: (form) => {
                const $form = $(form);

                const memberData = {
                    email: $form.find('#member-email').val().trim().toLowerCase(),
                    role: $form.find('#member-role').val()
                };

                const $submitBtn = $form.find('#btn-submit-invite-member');
                $submitBtn.prop('disabled', true).addClass('loading');

                api.inviteMember(memberData, organizationId)
                    .done(function () {
                        window.location.reload();
                    })
                    .fail((xhr) => {
                        if (xhr.status === 401) {
                            window.location.href = '/login';
                            return;
                        }

                        if (xhr.status === 400 && xhr.responseJSON?.errors) {
                            showApiErrors($form, xhr.responseJSON.errors);
                            return;
                        }

                        alert('Помилка додавання учасника');
                    })
                    .always(() => {
                        $submitBtn.prop('disabled', false).removeClass('loading');
                    });
            }
        });

        const $modalBody = $('#modal-body-content');

        $modalBody.on('click', '#js-close-invite-member-modal', function (e) {
            e.preventDefault();
            Modal.close();
        });
    }

    initDeleteOrganizationDialog();

    $('#open-invite-member-modal-btn').on('click', function () {
        openInviteMemberModal();
    });
});