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
            apply(container);
        });

        bindCustomInput(container);
    });

    function apply(container) {
        var hidden = document.getElementById(container.dataset.target);
        if (hidden) hidden.value = checkedValues(container).join(SEPARATOR);
        summarize(container);
    }

    // 「自行輸入」：清單裡沒有的值（委外廠商那種）直接打進來，
    // 加成一個勾好的項目就好——業務端決定這類名稱不回寫維護表。
    function bindCustomInput(container) {
        var group = container.querySelector('.pd-multicheck-custom');
        if (!group) return;

        var input = group.querySelector('input');
        var button = group.querySelector('button');
        var box = container.querySelector('.pd-multicheck-box');

        function add() {
            var value = (input.value || '').trim();
            if (!value) return;

            if (value.indexOf(SEPARATOR) >= 0) {
                input.setCustomValidity('不能包含「' + SEPARATOR + '」，那是分隔符號');
                input.reportValidity();
                return;
            }
            input.setCustomValidity('');

            var existing = [].slice.call(container.querySelectorAll('input[type=checkbox]'))
                .filter(function (b) { return b.value === value; })[0];

            if (existing) {
                existing.checked = true;          // 已經有這個項目，勾起來就好
            } else {
                var id = container.dataset.target + '__c' + Date.now();
                var item = document.createElement('div');
                item.className = 'form-check';
                item.innerHTML =
                    '<input class="form-check-input" type="checkbox" checked id="' + id + '" />' +
                    '<label class="form-check-label" for="' + id + '"></label>';
                item.querySelector('input').value = value;
                // textContent 而不是字串拼接：值是使用者打的，不能當成 HTML
                item.querySelector('label').textContent = value;
                box.appendChild(item);
                item.scrollIntoView({ block: 'nearest' });
            }

            input.value = '';
            apply(container);
        }

        button.addEventListener('click', add);
        input.addEventListener('keydown', function (event) {
            // 在輸入框按 Enter 是「加入」，不要順手把整張表單送出去
            if (event.key === 'Enter') { event.preventDefault(); add(); }
        });
    }
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

// 新增畫面的「無SW資產編號」：勾了就把 SW 與盤點表兩個區塊藏起來，
// 並停用其中的欄位。
//
// 為什麼一定要停用而不是只隱藏：隱藏的欄位照樣會跟著表單送出，後端的
// 「這個區塊有沒有填」就會判斷成有填，於是建出一筆空的 SW 資產。停用的欄位
// 瀏覽器根本不會送。
//
// 這仍然只是操作防呆——改個表單就繞過去了，真正的把關在
// DataController.Create：收到勾選時把兩個區塊的內容整個丟掉。
(function () {
    var checkbox = document.getElementById('noSoftwareAsset');
    if (!checkbox) return;

    var blocks = document.querySelectorAll('.pd-sw-only');

    function refresh() {
        var hide = checkbox.checked;

        blocks.forEach(function (block) {
            block.hidden = hide;

            block.querySelectorAll('input, select, textarea').forEach(function (field) {
                field.disabled = hide;
            });
        });
    }

    checkbox.addEventListener('change', refresh);
    refresh();
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

// 盤點重點（導覽列的「?」）。同一個彈跳視窗裡換三個畫面：清單 → 內容 → 編輯，
// 內容都由 NotesController 以部分檢視回傳，這裡只負責抓回來換進去。
//
// 為什麼不做成三個頁面：這個功能要能從任何一頁打開，看完關掉就回到原本的工作；
// 導頁會把使用者手上的搜尋條件與捲動位置弄丟。
(function () {
    var toggle = document.getElementById('noteToggle');
    var modalEl = document.getElementById('noteModal');
    var body = document.getElementById('noteModalBody');
    if (!toggle || !modalEl || !body) return;

    var modal = new bootstrap.Modal(modalEl);

    function show(html) {
        body.innerHTML = html;
        body.scrollTop = 0;
    }

    function load(url) {
        body.innerHTML = '<div class="text-center text-muted py-4">載入中…</div>';
        return fetch(url, { headers: { 'X-Requested-With': 'XMLHttpRequest' } })
            .then(function (response) {
                if (!response.ok) throw new Error(response.status);
                return response.text();
            })
            .then(show)
            .catch(function () {
                show('<div class="alert alert-danger mb-0">載入失敗，請關掉視窗再試一次。</div>');
            });
    }

    var urls = {
        list: function () { return '/Notes/List'; },
        detail: function (id) { return '/Notes/Detail/' + id; },
        form: function (id) { return id ? '/Notes/Form/' + id : '/Notes/Form'; }
    };

    toggle.addEventListener('click', function () {
        modal.show();
        load(urls.list());
    });

    // 視窗裡的按鈕都是後端送來的，事件掛在容器上才不必每次重綁
    body.addEventListener('click', function (event) {
        var item = event.target.closest('[data-note-id]:not([data-note-action])');
        if (item) { load(urls.detail(item.dataset.noteId)); return; }

        var action = event.target.closest('[data-note-action]');
        if (!action) return;

        var name = action.dataset.noteAction;
        if (urls[name]) load(urls[name](action.dataset.noteId));
    });

    // 編輯與新增：用 fetch 送出，成功就直接換成該筆的內容畫面
    body.addEventListener('submit', function (event) {
        var form = event.target.closest('form.pd-note-form');
        if (!form) return;

        event.preventDefault();
        fetch(form.getAttribute('action') || '/Notes/Save', {
            method: 'POST',
            body: new FormData(form),
            headers: { 'X-Requested-With': 'XMLHttpRequest' }
        })
            .then(function (response) {
                if (!response.ok) throw new Error(response.status);
                return response.text();
            })
            .then(show)   // 存檔成功回內容畫面；驗證沒過回的是帶錯誤訊息的表單
            .catch(function () {
                show('<div class="alert alert-danger mb-0">儲存失敗，請關掉視窗再試一次。</div>');
            });
    });
})();
