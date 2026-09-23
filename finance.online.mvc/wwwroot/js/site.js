// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
document.addEventListener('click', function (event) {
    var trigger = event.target.closest('[data-open-new-operation]');

    if (!trigger) return;

    event.preventDefault();
    window.dispatchEvent(new CustomEvent('open-new-operation'));
});
