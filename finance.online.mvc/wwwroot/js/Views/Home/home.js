import api from './homeApi.js';
import validators from './homeValidators.js';
import Modal from '../../helpers/ModalManager.js';
import { showApiErrors } from '../../helpers/showApiErrors.js';

$(function () {
    'use strict';

    function setActiveType($form, type) {
        $form.find('#operation-type').val(type);

        $form.find('.type-toggle-btn').removeClass('active');
        $form.find(`.type-toggle-btn[data-type="${type}"]`).addClass('active');
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
                        const $newItem = $(html);
                        $('#operations-list').prepend($newItem);
                        applyOperationsFilter(operationsActiveFilter);

                        Modal.close();

                        // Новий айтем міг заповнити список так, що скрол з'явився,
                        // або навпаки — перевіряємо, чи не треба довантажити ще.
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

        $modalBody.on('click', '.type-toggle-btn', function (e) {
            e.preventDefault();
            setActiveType($form, $(this).data('type'));
        });

        $modalBody.on('click', '#js-close-operation-modal', function (e) {
            e.preventDefault();
            Modal.close();
        });
    }

    function setActiveOrganizationColor($form, color) {
        $form.find('#organization-color').val(color);

        $form.find('.color-swatch-btn').removeClass('active');
        $form.find(`.color-swatch-btn[data-color="${color}"]`).addClass('active');
    }

    function openAddOrganizationModal() {
        if (!Modal) {
            console.error('Global Modal manager library initialization instances not found.');
            return;
        }

        Modal.open('Нова організація', '#tpl-add-organization', {
            ...validators.organizationFormRules,
            showErrors: function () {
                this.defaultShowErrors();

                const $form = $(this.currentForm);
                const $submitBtn = $form.find('#btn-submit-organization');

                $submitBtn.prop('disabled', this.numberOfInvalids() > 0);
            },
            submitHandler: (form) => {
                const $form = $(form);
                const name = $form.find('#organization-name').val().trim();
                const description = $form.find('#organization-description').val().trim();

                const organizationData = {
                    name: name,
                    description: description || name,
                    participantUserIds: []
                };

                const $submitBtn = $form.find('#btn-submit-organization');
                $submitBtn.prop('disabled', true).addClass('loading');

                api.createOrganization(organizationData)
                    .done((organization) => {
                        if (organization?.id) {
                            localStorage.setItem('activeOrganizationId', organization.id);
                            window.location.href = '/?organizationId=' + encodeURIComponent(organization.id);
                            return;
                        }

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

                        alert('Помилка створення організації');
                    })
                    .always(() => {
                        $submitBtn.prop('disabled', false).removeClass('loading');
                    });
            }
        });

        const $modalBody = $('#modal-body-content');
        const $form = $modalBody.find('#form-add-organization');

        setActiveOrganizationColor($form, $form.find('#organization-color').val() || '#2563EB');

        $modalBody.on('click', '.color-swatch-btn', function (e) {
            e.preventDefault();
            setActiveOrganizationColor($form, $(this).data('color'));
        });

        $modalBody.on('click', '#js-close-organization-modal', function (e) {
            e.preventDefault();
            Modal.close();
        });
    }

    function initFilter($filterBar, $container, itemSelector, emptyStateHtml, onFilterChange) {
        if (!$filterBar.length || !$container.length) {
            return;
        }

        function applyFilter(filter) {
            $container.find(itemSelector).each(function () {
                const $item = $(this);
                const type = $item.data('type');
                const matches = filter === 'all' || String(type) === filter;
                $item.toggle(matches);
            });

            const visibleCount = $container.find(itemSelector + ':visible').length;
            const hasAnyItems = $container.find(itemSelector).length > 0;

            $container.find('.filter-empty-state').remove();

            if (hasAnyItems && visibleCount === 0 && emptyStateHtml) {
                $container.append(emptyStateHtml);
            }

            if (onFilterChange) {
                onFilterChange(filter);
            }
        }

        $filterBar.on('click', '.filter-toggle', function () {
            const $btn = $(this);
            const filter = $btn.data('filter');

            $filterBar.find('.filter-toggle').removeClass('active');
            $btn.addClass('active');

            applyFilter(filter);
        });
    }

    // ---- Infinite scroll для стрічки операцій ----
    let operationsPage = 1;
    let operationsHasMore = true;
    let operationsLoading = false;
    let operationsActiveFilter = 'all';

    function applyOperationsFilter(filter) {
        operationsActiveFilter = filter;

        $('#operations-list .ledger-item').each(function () {
            const $item = $(this);
            const type = $item.data('type');
            const matches = filter === 'all' || String(type) === filter;
            $item.toggle(matches);
        });
    }

    function loadMoreOperations() {
        if (operationsLoading || !operationsHasMore) {
            return $.Deferred().resolve().promise();
        }

        const organizationId = window.TransitData?.organizationId;
        const activePeriod = window.TransitData?.activePeriod || 'month';

        if (!organizationId) {
            return $.Deferred().resolve().promise();
        }

        operationsLoading = true;
        const nextPage = operationsPage + 1;
        $('#operations-loading').show();

        return api.loadMoreOperations(organizationId, nextPage, activePeriod)
            .done((html) => {
                const trimmed = (html || '').trim();

                if (!trimmed) {
                    operationsHasMore = false;
                    return;
                }

                const $newItems = $(trimmed);
                $('#operations-list').append($newItems);
                operationsPage = nextPage;

                applyOperationsFilter(operationsActiveFilter);
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

                // Якщо після довантаження список все ще не заповнює контейнер
                // (скролу немає) і дані ще є — довантажуємо далі рекурсивно.
                fillOperationsListIfNeeded();
            });
    }

    // Довантажує операції, поки контейнер не заповниться настільки,
    // щоб з'явився скрол, або поки не закінчаться дані на сервері.
    // Потрібно, бо якщо айтемів мало — подія 'scroll' ніколи не виникає,
    // і без цієї перевірки довантаження просто ніколи не запуститься.
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

        // На випадок зміни розміру вікна (наприклад, поворот екрана
        // або зміна layout), коли список міг "розгорнутися" й втратити скрол.
        $(window).on('resize', function () {
            fillOperationsListIfNeeded();
        });
    }

    function initOrganizationDropdown() {
        const toggle = document.getElementById('org-select-toggle');
        const dropdown = document.getElementById('org-select-dropdown');

        if (!toggle || !dropdown) return;

        toggle.addEventListener('click', function () {
            const isOpen = dropdown.style.display !== 'none';

            dropdown.style.display = isOpen ? 'none' : 'block';
            toggle.classList.toggle('is-open', !isOpen);
        });

        document.addEventListener('click', function (e) {
            if (!toggle.contains(e.target) && !dropdown.contains(e.target)) {
                dropdown.style.display = 'none';
                toggle.classList.remove('is-open');
            }
        });

        dropdown.querySelectorAll('.org-select-item').forEach(function (item) {
            item.addEventListener('click', function () {
                const id = item.getAttribute('data-organization-id');

                localStorage.setItem('activeOrganizationId', id);
                window.location.href = '/?organizationId=' + encodeURIComponent(id);
            });
        });
    }

    function initPeriodDropdown() {
        const toggle = document.getElementById('period-select-toggle');
        const dropdown = document.getElementById('period-select-dropdown');

        if (!toggle || !dropdown) return;

        toggle.addEventListener('click', function (e) {
            e.stopPropagation();

            const isOpen = dropdown.style.display !== 'none';

            dropdown.style.display = isOpen ? 'none' : 'block';
            toggle.classList.toggle('is-open', !isOpen);
        });

        document.addEventListener('click', function (e) {
            if (!toggle.contains(e.target) && !dropdown.contains(e.target)) {
                dropdown.style.display = 'none';
                toggle.classList.remove('is-open');
            }
        });
    }

    $('#open-operation-modal-btn').on('click', function () {
        openAddOperationModal();
    });

    window.addEventListener('open-new-operation', function () {
        openAddOperationModal();
    });

    $('#open-organization-modal-btn, .org-сreate-button, .org-create-button').on('click', function (e) {
        e.preventDefault();
        openAddOrganizationModal();
    });

    initFilter(
        $('#operations-filter'),
        $('#operations-list'),
        '.ledger-item',
        '<div class="filter-empty-state flex flex-col items-center" style="padding:2.5rem 0; text-align:center;">' +
        '<p class="text-xs text-muted-foreground">Немає операцій цього типу</p>' +
        '</div>',
        applyOperationsFilter
    );

    initFilter(
        $('#chart-filter'),
        $('#chart-placeholder'),
        '.chart-bar-pair',
        null
    );

    initOrganizationDropdown();
    initPeriodDropdown();
    initOperationsInfiniteScroll();
});