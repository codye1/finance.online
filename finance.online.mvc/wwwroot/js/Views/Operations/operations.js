import api from './operationsApi.js';
import validators from './operationsValidators.js';
import Modal from '../../helpers/ModalManager.js';
import { showApiErrors } from '../../helpers/showApiErrors.js';

$(function () {
    'use strict';

    function setActiveType($form, type) {
        $form.find('#operation-type').val(type);

        $form.find('.op-type-toggle-btn').removeClass('active');
        $form.find(`.op-type-toggle-btn[data-type="${type}"]`).addClass('active');
    }

    function openAddOperationModal() {
        if (!Modal) {
            console.error('Global Modal manager library initialization instances not found.');
            return;
        }

        Modal.open('Нова операція', '#tpl-add-operation', {
            ...validators.operationFormRules,
            showErrors: function () {
                this.defaultShowErrors();

                const $form = $(this.currentForm);
                const $submitBtn = $form.find('#btn-submit-operation');

                $submitBtn.prop('disabled', this.numberOfInvalids() > 0);
            },
            submitHandler: (form) => {
                const $form = $(form);

                const operationData = {
                    type: $form.find('#operation-type').val(),
                    amount: parseFloat($form.find('#operation-amount').val()),
                    categoryId: $form.find('#operation-category').val(),
                    description: $form.find('#operation-description').val().trim()
                };

                const $submitBtn = $form.find('#btn-submit-operation');
                $submitBtn.prop('disabled', true).addClass('loading');

                api.createOperation(operationData, window.TransitData.organizationId)
                    .done((html) => {
                        $('#op-empty-state').hide();
                        $('#operations-list').prepend($(html));
                        recomputeSummary();
                        Modal.close();

                        // Новий айтем міг заповнити контейнер (з'явився скрол) або
                        // навпаки — все ще замало, тож перевіряємо необхідність довантаження.
                        fillOperationsListIfNeeded();
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

                        alert('Помилка збереження нової операції');
                    })
                    .always(() => {
                        $submitBtn.prop('disabled', false).removeClass('loading');
                    });
            }
        });

        const $modalBody = $('#modal-body-content');
        const $form = $modalBody.find('#form-add-operation');

        const today = new Date().toISOString().slice(0, 10);
        $form.find('#operation-date').val(today);

        setActiveType($form, $form.find('#operation-type').val() || 'expense');

        $modalBody.on('click', '.op-type-toggle-btn', function (e) {
            e.preventDefault();
            setActiveType($form, $(this).data('type'));
        });

        $modalBody.on('click', '#js-close-operation-modal', function (e) {
            e.preventDefault();
            Modal.close();
        });
    }

    function recomputeSummary() {
        let income = 0;
        let expense = 0;

        $('#operations-list .op-item:visible').each(function () {
            const $item = $(this);
            const amount = parseFloat($item.data('amount')) || 0;
            const type = $item.data('type');

            if (type === 'income') {
                income += amount;
            } else {
                expense += amount;
            }
        });

        $('#op-summary-income').text(income.toFixed(2));
        $('#op-summary-expense').text(expense.toFixed(2));
        $('#op-summary-net').text((income - expense).toFixed(2));
    }

    function applyFilters() {
        const query = $('#op-search-input').val().trim().toLowerCase();
        const typeFilter = $('#op-type-filter').val();
        const categoryFilter = $('#op-category-filter').val();

        let visibleCount = 0;

        $('#operations-list .op-item').each(function () {
            const $item = $(this);
            const type = $item.data('type');
            const category = String($item.data('category') || '');
            const text = $item.find('.op-item-desc').text().toLowerCase() + ' ' + category.toLowerCase();

            const matchesType = typeFilter === 'all' || String(type) === typeFilter;
            const matchesCategory = categoryFilter === 'all' || category === categoryFilter;
            const matchesQuery = !query || text.includes(query);

            const matches = matchesType && matchesCategory && matchesQuery;
            $item.toggle(matches);

            if (matches) {
                visibleCount += 1;
            }
        });

        $('#op-empty-state').toggle(visibleCount === 0);
        recomputeSummary();
    }

    function initDeleteOperation() {
        $('#operations-list').on('click', '.op-delete-btn', function () {
            const operationId = $(this).data('operation-id');
            if (!operationId) return;

            if (!confirm('Видалити цю операцію?')) {
                return;
            }

            const $item = $(this).closest('.op-item');

            api.deleteOperation(operationId)
                .done(function () {
                    $item.remove();
                    applyFilters();

                    // Видалення могло звільнити місце — контейнер міг втратити
                    // скрол, хоча на сервері ще є що довантажити.
                    fillOperationsListIfNeeded();
                })
                .fail((xhr) => {
                    if (xhr.status === 401) {
                        window.location.href = '/login';
                        return;
                    }

                    alert('Помилка видалення операції');
                });
        });
    }

    // ---- Infinite scroll для сторінки "Усі операції" ----
    let operationsPage = 1;
    let operationsHasMore = true;
    let operationsLoading = false;

    function loadMoreOperations() {
        if (operationsLoading || !operationsHasMore) {
            return $.Deferred().resolve().promise();
        }

        const organizationId = window.TransitData?.organizationId;

        if (!organizationId) {
            return $.Deferred().resolve().promise();
        }

        operationsLoading = true;
        const nextPage = operationsPage + 1;
        $('#operations-loading').show();

        return api.loadMoreOperations(organizationId, nextPage)
            .done((html) => {
                const trimmed = (html || '').trim();

                if (!trimmed) {
                    operationsHasMore = false;
                    return;
                }

                const $newItems = $(trimmed);
                $('#operations-list').append($newItems);
                operationsPage = nextPage;

                applyFilters();
            })
            .fail((xhr) => {
                if (xhr.status === 401) {
                    window.location.href = '/login';
                    return;
                }

                operationsHasMore = false;
            })
            .always(() => {
                operationsLoading = false;
                $('#operations-loading').hide();

                // Якщо після довантаження контейнер все ще не заповнений
                // (немає скролу) і дані ще є — довантажуємо далі рекурсивно.
                fillOperationsListIfNeeded();
            });
    }

    // Довантажує операції, поки контейнер не заповниться настільки,
    // щоб з'явився скрол, або поки не закінчаться дані на сервері.
    // Без цього: якщо айтемів мало (або більшість відфільтровано),
    // подія 'scroll' ніколи не виникає і довантаження просто не почнеться.
    function fillOperationsListIfNeeded() {
        const $list = $('#operations-list');
        if (!$list.length || operationsLoading || !operationsHasMore) {
            return;
        }

        const el = $list.get(0);
        const hasScroll = el.scrollHeight > el.clientHeight + 1;

        if (!hasScroll) {
            loadMoreOperations();
        }
    }

    function initOperationsInfiniteScroll() {
        const $list = $('#operations-list');
        if (!$list.length) {
            return;
        }

        $list.on('scroll', function () {
            const el = this;
            const threshold = 80;
            const nearBottom = el.scrollTop + el.clientHeight >= el.scrollHeight - threshold;

            if (nearBottom) {
                loadMoreOperations();
            }
        });

        // Перевіряємо одразу після ініціалізації: раптом айтемів
        // із серверного рендеру замало, щоб з'явився скрол.
        fillOperationsListIfNeeded();

        $(window).on('resize', function () {
            fillOperationsListIfNeeded();
        });
    }

    $('#open-operation-modal-btn').on('click', function () {
        openAddOperationModal();
    });

    $('#op-search-input').on('input', applyFilters);
    $('#op-type-filter, #op-category-filter').on('change', applyFilters);

    initDeleteOperation();
    initOperationsInfiniteScroll();
    recomputeSummary();
});