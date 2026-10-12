/**
 * ai-tourplanner.js  —  AI Tour Planner
 * Handles form submission, day-tab rendering, edit-day inline,
 * budget display, print, and save-plan (localStorage).
 *
 * The browser never sees the API key; it only talks to /TourPlanner/Generate.
 */
(function () {
    'use strict';

    /* ---- State ---- */
    var STORAGE_KEY = 'nexvoy_tourplan_v1';
    var currentData = null;

    /* ---- DOM helpers ---- */
    function q(sel)  { return document.querySelector(sel); }
    function qa(sel) { return document.querySelectorAll(sel); }

    /* ---- Interest chip toggle ---- */
    qa('.ai-chip').forEach(function (chip) {
        chip.addEventListener('click', function () {
            chip.classList.toggle('selected');
        });
    });

    /* ---- Date range: keep end ≥ start ---- */
    var startInput = q('#StartDate');
    var endInput   = q('#EndDate');
    if (startInput && endInput) {
        startInput.addEventListener('change', function () {
            if (endInput.value && endInput.value < startInput.value) {
                endInput.value = startInput.value;
            }
            endInput.min = startInput.value;
        });
    }

    /* ---- Form submit ---- */
    var form = q('#tourForm');
    if (form) {
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            submitForm();
        });
    }

    function submitForm() {
        clearError();
        showLoader(true);
        hideResults();

        var token     = q('input[name="__RequestVerificationToken"]');
        var interests = [];
        qa('.ai-chip.selected').forEach(function (c) { interests.push(c.dataset.value); });

        var data = {
            __RequestVerificationToken: token ? token.value : '',
            FromCity:    q('#FromCity').value.trim(),
            Destination: q('#Destination').value.trim(),
            StartDate:   q('#StartDate').value,
            EndDate:     q('#EndDate').value,
            BudgetMin:   parseFloat(q('#BudgetMin').value) || 0,
            BudgetMax:   parseFloat(q('#BudgetMax').value) || 0,
            Travelers:   parseInt(q('#Travelers').value, 10) || 1,
            Interests:   interests.join(','),
            TravelStyle: q('#TravelStyle').value,
            HealthNotes: q('#HealthNotes').value.trim()
        };

        // Client-side quick validation
        if (!data.FromCity)    { showError('Please enter your starting city.'); showLoader(false); return; }
        if (!data.Destination) { showError('Please enter a destination.'); showLoader(false); return; }
        if (!data.StartDate)   { showError('Please select a start date.'); showLoader(false); return; }
        if (!data.EndDate)     { showError('Please select an end date.'); showLoader(false); return; }
        if (data.EndDate <= data.StartDate) { showError('End date must be after start date.'); showLoader(false); return; }

        fetch('/TourPlanner/Generate', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: encodeForm(data)
        })
        .then(function (r) { return r.json(); })
        .then(function (json) {
            showLoader(false);
            if (!json.success) { showError(json.error); return; }
            currentData = json.data;
            renderResults(currentData);
        })
        .catch(function () {
            showLoader(false);
            showError('Could not reach the server. Please check your connection and try again.');
        });
    }

    function encodeForm(obj) {
        return Object.keys(obj).map(function (k) {
            return encodeURIComponent(k) + '=' + encodeURIComponent(obj[k] == null ? '' : obj[k]);
        }).join('&');
    }

    /* ---- Render results ---- */
    function renderResults(data) {
        var wrap = q('#resultsSection');
        if (!wrap) return;
        wrap.innerHTML = '';

        // Budget card
        if (data.totalBudget) {
            var b  = data.totalBudget;
            var bd = b.breakdown || {};
            var cur = b.currency || 'USD';
            var budgetCard = document.createElement('div');
            budgetCard.className = 'budget-card';
            budgetCard.innerHTML =
                '<h3><i class="fas fa-wallet"></i> Estimated Budget</h3>' +
                '<div class="budget-total">$' + fmt(b.estimated) + ' <small style="font-size:.55em;color:#6b7280;">' + esc(cur) + '</small></div>' +
                '<div class="budget-grid">' +
                    budgetItem('Accommodation', bd.accommodation, cur) +
                    budgetItem('Food',          bd.food,          cur) +
                    budgetItem('Transport',     bd.transport,     cur) +
                    budgetItem('Activities',    bd.activities,    cur) +
                    budgetItem('Misc',          bd.misc,          cur) +
                '</div>';
            wrap.appendChild(budgetCard);
        }

        // Health notes
        if (data.healthNotes) {
            var hBox = document.createElement('div');
            hBox.className = 'health-notes-box';
            hBox.innerHTML =
                '<i class="fas fa-heartbeat"></i>' +
                '<div><h4>Health-Friendly Notes</h4><p>' + esc(data.healthNotes) + '</p></div>';
            wrap.appendChild(hBox);
        }

        // Day tabs
        var days = data.days || [];
        if (!days.length) return;

        var tabBar = document.createElement('div');
        tabBar.className = 'day-tabs';
        wrap.appendChild(tabBar);

        var cardsWrap = document.createElement('div');
        cardsWrap.className = 'day-cards-wrap';
        wrap.appendChild(cardsWrap);

        days.forEach(function (day, idx) {
            // Tab
            var tab = document.createElement('button');
            tab.type = 'button';
            tab.className = 'day-tab' + (idx === 0 ? ' active' : '');
            tab.textContent = 'Day ' + day.dayNumber;
            tab.dataset.dayIdx = idx;
            tab.addEventListener('click', function () {
                qa('.day-tab').forEach(function (t) { t.classList.remove('active'); });
                qa('.day-card').forEach(function (c) { c.classList.remove('active'); c.style.display = 'none'; });
                tab.classList.add('active');
                var card = cardsWrap.querySelector('.day-card[data-day-idx="' + idx + '"]');
                if (card) { card.classList.add('active'); card.style.display = 'block'; }
            });
            tabBar.appendChild(tab);

            // Card
            var card = document.createElement('div');
            card.className = 'day-card' + (idx === 0 ? ' active' : '');
            card.style.display = idx === 0 ? 'block' : 'none';
            card.dataset.dayIdx = idx;

            card.innerHTML =
                '<div class="day-card-header">' +
                '<div class="day-number-badge">' + day.dayNumber + '</div>' +
                '<div><h3>' + esc(day.title || 'Day ' + day.dayNumber) + '</h3>' +
                (day.date ? '<div class="day-date">' + formatDate(day.date) + '</div>' : '') +
                '</div></div>' +
                '<div class="slots-list"></div>';

            var slotList = card.querySelector('.slots-list');
            (day.slots || []).forEach(function (slot) {
                slotList.appendChild(makeSlot(slot));
            });

            cardsWrap.appendChild(card);
        });

        // Show results
        var resultsArea = q('#aiResults');
        if (resultsArea) {
            resultsArea.classList.add('active');
            resultsArea.style.display = 'block';
        }

        setTimeout(function () {
            if (resultsArea) resultsArea.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }, 100);
    }

    function budgetItem(label, val, cur) {
        return '<div class="budget-item">' +
            '<div class="budget-item-label">' + esc(label) + '</div>' +
            '<div class="budget-item-val">$' + fmt(val) + '</div>' +
            '</div>';
    }

    function makeSlot(slot) {
        var period = (slot.period || 'Morning').toLowerCase();
        var div = document.createElement('div');
        div.className = 'slot-item slot-' + period;

        var periodIcons = { morning: 'fa-sun', afternoon: 'fa-cloud-sun', evening: 'fa-moon' };
        var icon = periodIcons[period] || 'fa-circle';

        div.innerHTML =
            '<div class="slot-time-col">' +
            '<div class="slot-period-icon"><i class="fas ' + icon + '"></i></div>' +
            '<span class="slot-time">' + esc(slot.time || '') + '</span>' +
            '<span class="slot-period-label">' + esc(slot.period || '') + '</span>' +
            '</div>' +
            '<div class="slot-content">' +
            '<div class="slot-place">' + esc(slot.place || '') + '</div>' +
            '<div class="slot-desc" data-orig="' + esc(slot.description || '') + '">' + esc(slot.description || '') + '</div>' +
            (slot.estimatedCost ? '<span class="slot-cost">~$' + fmt(slot.estimatedCost) + '</span>' : '') +
            '<div class="slot-edit-area">' +
            '<textarea class="slot-desc-edit"></textarea>' +
            '<div class="slot-edit-btns">' +
            '<button class="btn-slot-save" type="button">Save</button>' +
            '<button class="btn-slot-cancel" type="button">Cancel</button>' +
            '</div></div>' +
            '<button class="btn-ai-ghost" type="button" style="font-size:.75rem;padding:.3rem .85rem;margin-top:.6rem;" onclick="openSlotEdit(this)">Edit <i class="fas fa-pen"></i></button>' +
            '</div>';

        // Save / Cancel
        var editArea = div.querySelector('.slot-edit-area');
        var descEl   = div.querySelector('.slot-desc');
        var ta       = div.querySelector('.slot-desc-edit');
        div.querySelector('.btn-slot-save').addEventListener('click', function () {
            descEl.textContent = ta.value;
            editArea.classList.remove('open');
        });
        div.querySelector('.btn-slot-cancel').addEventListener('click', function () {
            editArea.classList.remove('open');
        });

        return div;
    }

    /* Global helper called by inline onclick */
    window.openSlotEdit = function (btn) {
        var slotContent = btn.closest('.slot-content');
        var editArea = slotContent.querySelector('.slot-edit-area');
        var descEl   = slotContent.querySelector('.slot-desc');
        var ta       = slotContent.querySelector('.slot-desc-edit');
        ta.value = descEl.textContent;
        editArea.classList.toggle('open');
    };

    /* ---- Loader / error / result visibility ---- */
    function showLoader(on) {
        var loader = q('#aiLoader');
        if (!loader) return;
        loader.classList.toggle('active', on);
        loader.style.display = on ? 'flex' : 'none';
        var btn = q('#submitBtn');
        if (btn) btn.disabled = on;
    }

    function showError(msg) {
        var banner = q('#errorBanner');
        if (!banner) return;
        banner.querySelector('.error-msg').textContent = msg;
        banner.classList.add('active');
        banner.style.display = 'flex';
    }

    function clearError() {
        var banner = q('#errorBanner');
        if (!banner) return;
        banner.classList.remove('active');
        banner.style.display = 'none';
    }

    function hideResults() {
        var r = q('#aiResults');
        if (r) { r.classList.remove('active'); r.style.display = 'none'; }
    }

    /* ---- Button actions ---- */
    window.tourPlannerRegenerate = function () { hideResults(); clearError(); submitForm(); };
    window.tourPlannerPrint      = function () { window.print(); };
    window.tourPlannerSave       = function () {
        if (!currentData) return;
        try {
            localStorage.setItem(STORAGE_KEY, JSON.stringify(currentData));
            flashBtn('#saveBtn', '✓ Saved!');
        } catch (e) {}
    };

    /* ---- Restore saved plan ---- */
    var saved = loadFromStorage();
    if (saved) { currentData = saved; renderResults(saved); }

    function loadFromStorage() {
        try {
            var raw = localStorage.getItem(STORAGE_KEY);
            if (raw) return JSON.parse(raw);
        } catch (e) {}
        return null;
    }

    /* ---- Retry button ---- */
    var retryBtn = q('#retryBtn');
    if (retryBtn) retryBtn.addEventListener('click', function () { clearError(); submitForm(); });

    /* ---- Utilities ---- */
    function flashBtn(sel, label) {
        var btn = q(sel);
        if (!btn) return;
        var orig = btn.innerHTML;
        btn.textContent = label;
        setTimeout(function () { btn.innerHTML = orig; }, 1800);
    }

    function fmt(n) {
        if (n == null || isNaN(n)) return '0';
        return Number(n).toLocaleString('en-US', { maximumFractionDigits: 0 });
    }

    function esc(s) {
        if (s == null) return '';
        return String(s)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function formatDate(d) {
        try {
            var dt = new Date(d);
            return dt.toLocaleDateString('en-US', { weekday: 'long', month: 'long', day: 'numeric' });
        } catch (e) { return d; }
    }

})();
