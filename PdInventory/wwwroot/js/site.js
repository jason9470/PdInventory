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

// 清單表格欄位排序：點表頭切換升冪/降冪。
// 六張主要清單資料量都在 50 筆內且未分頁，故在前端排序即可，不必為每張表
// 在控制器寫一套欄位對應。標了 data-nosort 的表頭（例如「操作」）不參與。
(function () {
    // numeric:true 讓 SW-21 / SW-104 依數值排、日期 2009/1/12 也早於 2009/10/9
    var collator = new Intl.Collator('zh-Hant', { numeric: true, sensitivity: 'base' });

    function textOf(row, index) {
        var cell = row.cells[index];
        return cell ? cell.textContent.trim() : '';
    }

    function makeSortable(table) {
        if (!table.tHead || !table.tBodies.length) return;
        var headers = Array.prototype.slice.call(table.tHead.rows[0].cells);

        headers.forEach(function (th, index) {
            if (th.hasAttribute('data-nosort')) return;

            th.classList.add('sortable');
            th.tabIndex = 0;
            th.setAttribute('role', 'button');
            th.appendChild(document.createElement('span')).className = 'sort-icon';

            function sort() {
                var descending = th.getAttribute('data-sort-dir') === 'asc';
                headers.forEach(function (h) { h.removeAttribute('data-sort-dir'); });
                th.setAttribute('data-sort-dir', descending ? 'desc' : 'asc');

                var tbody = table.tBodies[0];
                var rows = Array.prototype.slice.call(tbody.rows);
                rows.sort(function (a, b) {
                    var result = collator.compare(textOf(a, index), textOf(b, index));
                    return descending ? -result : result;
                });
                rows.forEach(function (row) { tbody.appendChild(row); });
            }

            th.addEventListener('click', sort);
            th.addEventListener('keydown', function (e) {
                if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); sort(); }
            });
        });
    }

    document.querySelectorAll('table.table-sortable').forEach(makeSortable);
})();

// 回到頂部：捲動超過一段距離才顯示按鈕。
(function () {
    var button = document.getElementById('backToTop');
    if (!button) return;

    var showAfter = 300;

    function sync() {
        button.classList.toggle('is-visible', window.scrollY > showAfter);
    }

    window.addEventListener('scroll', sync, { passive: true });
    sync();

    button.addEventListener('click', function () {
        var reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
        window.scrollTo({ top: 0, behavior: reduceMotion ? 'auto' : 'smooth' });
    });
})();
