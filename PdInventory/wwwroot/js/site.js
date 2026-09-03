// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// 側邊欄與「維護資料」「維護匯出」兩個群組的收合。
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
    bind('exportToggle', 'export-collapsed', 'pdinv.exportCollapsed');
})();

// 多選欄位（_MultiCheckList.cshtml）：勾選同步到那個 hidden input。
// 欄位在資料庫仍然是一格文字，以 / 分隔——與來源 Excel 的寫法一致。
//
// 沒有動過的欄位不會重寫 hidden 的值，因此不碰它就不會有任何變化；
// 一旦動過，順序就會照維護資料表的順序重排，這是刻意的，讓寫法統一。
(function () {
    var SEPARATOR = '/';

    function checkedValues(container) {
        return [].slice.call(container.querySelectorAll('input[type=checkbox]'))
            .filter(function (box) { return box.checked; })
            .map(function (box) { return box.value; });
    }

    // 摘要一律照勾選狀態算，不去解析 hidden 的字串——那串裡的分隔符有好幾種
    // （/、;、括號內不算），在瀏覽器端重切一次只會跟後端的規則對不起來。
    function summarize(container) {
        var summary = container.querySelector('.pd-multicheck-summary');
        if (!summary) return;

        var checked = checkedValues(container);
        summary.textContent = checked.length
            ? '已選 ' + checked.length + ' 項：' + checked.join('、')
            : '未選取';
    }

    [].forEach.call(document.querySelectorAll('.pd-multicheck'), function (container) {
        // 進畫面只寫摘要，不動 hidden 的值——避免只是打開畫面就把原本的順序改掉
        summarize(container);

        container.addEventListener('change', function (event) {
            if (event.target.type !== 'checkbox') return;

            var hidden = document.getElementById(container.dataset.target);
            if (hidden) hidden.value = checkedValues(container).join(SEPARATOR);
            summarize(container);
        });
    });
})();

// 資產價值＝機密性＋完整性＋可用性，選單一改就立刻重算。
// 伺服器端存檔時還會再算一次（AppDbContext.RecalculateAssetValues），
// 這裡只是讓使用者當下看得到結果，不是唯一的把關。
//
// 超過門檻代表這是核心系統：先以提示提醒，送出時若系統類別仍不是核心系統就擋下來。
(function () {
    var CORE_CATEGORY = '核心系統';
    var CORE_THRESHOLD = 10;   // 要「大於」這個值才算核心系統，剛好等於 10 不算

    function setup(form) {
        var value = form.querySelector('.sw-asset-value');
        if (!value) return;

        var levels = form.querySelectorAll('.sw-cia');
        var category = form.querySelector('.sw-system-category');
        var hint = form.querySelector('.sw-core-hint');

        // 任何一個等級不是數字（例如外購軟體填 N/A）就不計算，維持原值不動
        function total() {
            var sum = 0;
            for (var i = 0; i < levels.length; i++) {
                var n = parseInt(levels[i].value, 10);
                if (isNaN(n)) return null;
                sum += n;
            }
            return sum;
        }

        function refresh() {
            var sum = total();
            if (sum !== null) value.value = String(sum);

            var overThreshold = sum !== null && sum > CORE_THRESHOLD;
            if (!hint) return;
            hint.hidden = !overThreshold;
            if (overThreshold) {
                hint.textContent = '資產價值 ' + sum + ' 已超過 ' + CORE_THRESHOLD
                    + '，系統類別應為「' + CORE_CATEGORY + '」。';
            }
        }

        levels.forEach(function (el) { el.addEventListener('change', refresh); });
        if (category) category.addEventListener('change', refresh);
        refresh();

        form.addEventListener('submit', function (e) {
            var sum = total();
            if (sum === null || sum <= CORE_THRESHOLD) return;
            if (!category || category.value === CORE_CATEGORY) return;

            e.preventDefault();
            block(sum, category);
        });
    }

    // 阻擋視窗。直接用 JS 建出來，兩個共用畫面就不必各放一份 modal 的 HTML。
    function block(sum, category) {
        var modal = document.getElementById('swCoreBlockModal');
        if (!modal) {
            modal = document.createElement('div');
            modal.id = 'swCoreBlockModal';
            modal.className = 'modal fade';
            modal.tabIndex = -1;
            modal.innerHTML =
                '<div class="modal-dialog modal-dialog-centered">'
              +   '<div class="modal-content">'
              +     '<div class="modal-header bg-danger text-white">'
              +       '<h5 class="modal-title">無法儲存</h5>'
              +     '</div>'
              +     '<div class="modal-body"><p class="mb-0 sw-block-message"></p></div>'
              +     '<div class="modal-footer">'
              +       '<button type="button" class="btn btn-primary sw-block-fix">改為核心系統</button>'
              +       '<button type="button" class="btn btn-outline-secondary" data-bs-dismiss="modal">返回修改</button>'
              +     '</div>'
              +   '</div>'
              + '</div>';
            document.body.appendChild(modal);
        }

        modal.querySelector('.sw-block-message').textContent =
            '資產價值 ' + sum + ' 已超過 ' + CORE_THRESHOLD + '，系統類別必須是「'
            + CORE_CATEGORY + '」，目前是「' + (category.value || '未選擇') + '」。';

        var instance = bootstrap.Modal.getOrCreateInstance(modal);
        var fix = modal.querySelector('.sw-block-fix');
        // 每次都換一顆新的按鈕，避免重複掛上事件
        var fresh = fix.cloneNode(true);
        fix.parentNode.replaceChild(fresh, fix);
        fresh.addEventListener('click', function () {
            category.value = CORE_CATEGORY;
            category.dispatchEvent(new Event('change'));
            instance.hide();
        });

        instance.show();
    }

    document.querySelectorAll('form.pd-form').forEach(setup);
})();

// 資產編號下拉：選了編號就把資產名稱帶出來。
// 個資盤點、風險自評、拋轉清單三個表單共用，故放在這裡而不是各自的畫面。
// 名稱欄位是唯讀的，真正的值仍由伺服器端依編號查出後寫入，這裡只是即時回饋。
(function () {
    document.querySelectorAll('select.asset-code').forEach(function (select) {
        var form = select.closest('form');
        var name = form && form.querySelector('.asset-name');
        if (!name) return;

        select.addEventListener('change', function () {
            var option = select.options[select.selectedIndex];
            name.value = option ? (option.getAttribute('data-name') || '') : '';
        });
    });
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

// 到最頂部／到最底部。兩顆按鈕固定顯示，不做捲動位置的顯示/隱藏判斷。
(function () {
    function bind(buttonId, getTarget) {
        var button = document.getElementById(buttonId);
        if (!button) return;

        button.addEventListener('click', function () {
            var reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches;
            window.scrollTo({ top: getTarget(), behavior: reduceMotion ? 'auto' : 'smooth' });
        });
    }

    bind('backToTop', function () { return 0; });
    // 每次點擊才取高度：表格排序或收合側邊欄都會改變頁面總高
    bind('backToBottom', function () { return document.documentElement.scrollHeight; });
})();
