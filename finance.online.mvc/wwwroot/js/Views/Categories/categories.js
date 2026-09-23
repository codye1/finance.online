import api from './categoriesApi.js';
import validators from './categoriesValidators.js';
import Modal from '../../helpers/ModalManager.js';
import { showApiErrors } from '../../helpers/showApiErrors.js';

$(function () {
    'use strict';

    function setActiveCategoryType($form, type) {
        $form.find('#category-type').val(type);

        $form.find('.type-toggle-btn').removeClass('active');
        $form.find(`.type-toggle-btn[data-type="${type}"]`).addClass('active');
    }

    function setActiveCategoryColor($form, color) {
        $form.find('#category-color').val(color);

        $form.find('.color-swatch-btn').removeClass('active');
        $form.find(`.color-swatch-btn[data-color="${color}"]`).addClass('active');
    }

    function openAddCategoryModal() {
        if (!Modal) {
            console.error('Global Modal manager library initialization instances not found.');
            return;
        }

        const organizationId = window.TransitData?.organizationId;
        if (!organizationId) {
            alert('Організацію не знайдено');
            return;
        }

        Modal.open('Нова категорія', '#tpl-add-category', {
            ...validators.categoryFormRules,
            showErrors: function () {
                this.defaultShowErrors();

                const $form = $(this.currentForm);
                const $submitBtn = $form.find('#btn-submit-category');

                $submitBtn.prop('disabled', this.numberOfInvalids() > 0);
            },
            submitHandler: (form) => {
                const $form = $(form);

                const categoryData = {
                    organizationId: organizationId,
                    name: $form.find('#category-name').val().trim(),
                    color: $form.find('#category-color').val()
                };

                const $submitBtn = $form.find('#btn-submit-category');
                $submitBtn.prop('disabled', true).addClass('loading');

                api.createCategory(categoryData)
                    .done((html) => {
                        $('#categories-list .categories-muted-row').remove();
                        $('#categories-list').append(html);
                        Modal.close();
                    })
                    .fail((xhr) => {
                        if (xhr.status === 401) {
                            window.location.href = '/login';
                            return;
                        }

                        if ((xhr.status === 400 || xhr.status === 404) && xhr.responseJSON?.errors) {
                            showApiErrors($form, xhr.responseJSON.errors);
                            return;
                        }

                        alert('Помилка створення категорії');
                    })
                    .always(() => {
                        $submitBtn.prop('disabled', false).removeClass('loading');
                    });
            }
        });

        const $modalBody = $('#modal-body-content');
        const $form = $modalBody.find('#form-add-category');

        setActiveCategoryType($form, 'expense');
        setActiveCategoryColor($form, '#2563EB');

        $modalBody.on('click', '.type-toggle-btn', function (e) {
            e.preventDefault();
            setActiveCategoryType($form, $(this).data('type'));
        });

        $modalBody.on('click', '.color-swatch-btn', function (e) {
            e.preventDefault();
            setActiveCategoryColor($form, $(this).data('color'));
        });

        $modalBody.on('click', '#js-close-category-modal', function (e) {
            e.preventDefault();
            Modal.close();
        });
    }

    function initDeleteCategory() {
    $('#categories-list').on('click', '.category-delete-btn', function () {
        const categoryId = $(this).data('category-id');
        if (!categoryId) return;

        if (!confirm('Видалити цю категорію?')) {
            return;
        }

        const $item = $(this).closest('.categories-item');

        api.deleteCategory(categoryId)
            .done(function () {
                $item.remove();

                if ($('#categories-list .categories-item').length === 0) {
                    $('#categories-list').html('<div class="categories-muted-row">Категорій ще немає</div>');
                }
            })
            .fail((xhr) => {
                if (xhr.status === 401) {
                    window.location.href = '/login';
                    return;
                }

                alert('Помилка видалення категорії');
            });
    });
}

    $('#open-add-category-modal-btn').on('click', function () {
        openAddCategoryModal();
    });

    initDeleteCategory();
});