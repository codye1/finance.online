import api from './categoriesApi.js';
import validators from './categoriesValidators.js';
import Modal from '../../helpers/ModalManager.js';
import { showApiErrors, getApiErrorMessage } from '../../helpers/showApiErrors.js';

$(function () {
    'use strict';

    const DEFAULT_COLOR = '#2563EB';

    // Помилки поза формою (видалення, відсутня організація) — банер на сторінці
    function showPageError(message) {
        $('#categories-error').text(message).show();
    }

    function hidePageError() {
        $('#categories-error').hide().text('');
    }

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

    // category == null  -> створення
    // category != null  -> редагування ({ id, name, color })
    function openCategoryModal(category) {
        if (!Modal) {
            console.error('Global Modal manager library initialization instances not found.');
            return;
        }

        hidePageError();

        const isEdit = !!category;

        const organizationId = window.TransitData?.organizationId;
        if (!isEdit && !organizationId) {
            showPageError('Організацію не знайдено');
            return;
        }

        Modal.open(isEdit ? 'Редагування категорії' : 'Нова категорія', '#tpl-add-category', {
            ...validators.categoryFormRules,
            showErrors: function () {
                this.defaultShowErrors();

                const $form = $(this.currentForm);
                const $submitBtn = $form.find('#btn-submit-category');

                $submitBtn.prop('disabled', this.numberOfInvalids() > 0);
            },
            submitHandler: (form) => {
                const $form = $(form);

                const name = $form.find('#category-name').val().trim();
                const color = $form.find('#category-color').val();

                const $submitBtn = $form.find('#btn-submit-category');
                $submitBtn.prop('disabled', true).addClass('loading');
                $form.find('.server-error').remove();

                const request = isEdit
                    ? api.updateCategory({ categoryId: category.id, name: name, color: color })
                    : api.createCategory({ organizationId: organizationId, name: name, color: color });

                request
                    .done((html) => {
                        if (isEdit) {
                            $(`#categories-list .categories-item[data-category-id="${category.id}"]`).replaceWith(html);
                        } else {
                            $('#categories-list .categories-muted-row').remove();
                            $('#categories-list').append(html);
                        }
                        Modal.close();
                    })
                    .fail((xhr) => {
                        if (xhr.status === 401) {
                            window.location.href = '/login';
                            return;
                        }

                        // 400 / 403 / 404 та будь-який інший статус з { errors: {...} }
                        if (xhr.responseJSON?.errors) {
                            showApiErrors($form, xhr.responseJSON.errors);
                            return;
                        }

                        // 500, мережа, порожня відповідь тощо — загальна помилка у формі
                        const fallback = isEdit ? 'Помилка оновлення категорії' : 'Помилка створення категорії';
                        showApiErrors($form, { _general: [getApiErrorMessage(xhr, fallback)] });
                    })
                    .always(() => {
                        $submitBtn.prop('disabled', false).removeClass('loading');
                    });
            }
        });

        const $modalBody = $('#modal-body-content');
        const $form = $modalBody.find('#form-add-category');

        setActiveCategoryType($form, 'expense');

        if (isEdit) {
            $form.find('#category-name').val(category.name);
            setActiveCategoryColor($form, category.color || DEFAULT_COLOR);
            $form.find('#category-submit-text').text('Зберегти');
        } else {
            setActiveCategoryColor($form, DEFAULT_COLOR);
        }

        // namespace + off, щоб обробники не накопичувалися між відкриттями модалки
        $modalBody.off('.categoryModal');

        $modalBody.on('click.categoryModal', '.type-toggle-btn', function (e) {
            e.preventDefault();
            setActiveCategoryType($form, $(this).data('type'));
        });

        $modalBody.on('click.categoryModal', '.color-swatch-btn', function (e) {
            e.preventDefault();
            setActiveCategoryColor($form, $(this).data('color'));
        });

        $modalBody.on('click.categoryModal', '#js-close-category-modal', function (e) {
            e.preventDefault();
            Modal.close();
        });
    }

    function initEditCategory() {
        $('#categories-list').on('click', '.category-edit-btn', function () {
            const $item = $(this).closest('.categories-item');

            openCategoryModal({
                id: $item.data('category-id'),
                name: $item.attr('data-category-name'),
                color: $item.attr('data-category-color')
            });
        });
    }

    function initDeleteCategory() {
        $('#categories-list').on('click', '.category-delete-btn', function () {
            const categoryId = $(this).data('category-id');
            if (!categoryId) return;

            if (!confirm('Видалити цю категорію?')) {
                return;
            }

            hidePageError();

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

                    showPageError(getApiErrorMessage(xhr, 'Помилка видалення категорії'));
                });
        });
    }

    $('#open-add-category-modal-btn').on('click', function () {
        openCategoryModal(null);
    });

    initEditCategory();
    initDeleteCategory();
});