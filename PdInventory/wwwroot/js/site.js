// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// 側邊欄與「維護資料」群組的收合。
// 本站每次導覽都是整頁重載，故狀態存 localStorage；
// 首次繪製前的套用在 _Layout.cshtml 的 <head> inline script，這裡只處理點擊。
(function () {
    function bind(buttonId, className, storageKey) {
        var button = document.getElementById(buttonId);
        if (!button) return;

        button.addEventListener('click', function () {
            var collapsed = document.documentElement.classList.toggle(className);
            button.setAttribute('aria-expanded', collapsed ? 'false' : 'true');
            try {
                localStorage.setItem(storageKey, collapsed ? '1' : '0');
            } catch (e) { /* 無痕模式等情境：不保存，僅本頁生效 */ }
        });

        button.setAttribute('aria-expanded',
            document.documentElement.classList.contains(className) ? 'false' : 'true');
    }

    bind('sidebarToggle', 'sidebar-collapsed', 'pdinv.sidebarCollapsed');
    bind('maintToggle', 'maint-collapsed', 'pdinv.maintCollapsed');
})();
