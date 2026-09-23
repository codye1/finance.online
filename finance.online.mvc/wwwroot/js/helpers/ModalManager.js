class ModalManager {
    constructor(modalId) {
        this.$modal = $(`#${modalId}`);
        this.$title = this.$modal.find('#modal-title');
        this.$body = this.$modal.find('#modal-body-content');
        this.initEvents();
    }

    initEvents() {
        this.$modal.on('click', '#js-close-modal', () => this.close());
        this.$modal.on('click', (e) => {
            if ($(e.target).is(this.$modal)) this.close();
        });
        $(document).on('keydown', (e) => {
            if (e.key === 'Escape' && this.$modal.hasClass('is-active')) this.close();
        });
    }

    open(title, templateId, handlerOrRules = null) {
        this.$title.text(title);
        const htmlContent = $(templateId).html();
        this.$body.html(htmlContent);
        this.$modal.addClass('is-active');

        if (!handlerOrRules) return;

        const $form = this.$body.find('form');
        if (!$form.length) return;

        if (typeof handlerOrRules === 'function') {
            $form.on('submit', function (e) {
                e.preventDefault();
                handlerOrRules($(this));
            });
        } else {
            $form.validate(handlerOrRules);
        }
    }

    close() {
        this.$modal.removeClass('is-active');
        setTimeout(() => {
            const $form = this.$body.find('form');
            if ($form.length) $form.removeData('validator').off();
            this.$body.empty();
        }, 250);
    }
}

const Modal = new ModalManager('app-modal');

export default Modal;