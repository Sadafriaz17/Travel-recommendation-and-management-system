/**
 * ai-packing.js  —  AI Packing Assistant
 * Handles form submission, result rendering, checklist state,
 * progress bar, print, copy, and localStorage persistence.
 *
 * The browser never sees the API key; it only talks to /PackingAssistant/Generate.
 */
(function () {
    'use strict';

    /* ---- State ---- */
    var STORAGE_KEY = 'nexvoy_packing_v1';
    var currentData = null;   // last successful API response

    /* ---- DOM helpers ---- */
    function q(sel)   { return document.querySelector(sel); }
    function qa(sel)  { return document.querySelectorAll(sel); }
    function show(el) { if (el) el.style.display = ''; }
    function hide(el) { if (el) el.style.display = 'none'; }

    /* ---- Counter buttons ---- */
    qa('.counter-btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var target = document.getElementById(btn.dataset.target);
            if (!target) return;
            var val = parseInt(target.value, 10) || 0;
            var delta = btn.dataset.dir === 'up' ? 1 : -1;
            var min = parseInt(target.dataset.min, 10) || 0;
            var max = parseInt(target.dataset.max, 10) || 99;
            target.value = Math.min(max, Math.max(min, val + delta));
        });
    });

    /* ---- Form submit ---- */
    var form = q('#packingForm');
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

        var token = q('input[name="__RequestVerificationToken"]');
        var data = {
            __RequestVerificationToken: token ? token.value : '',
            Destination:  q('#Destination').value.trim(),
            Days:         parseInt(q('#Days').value, 10) || 0,
            StartDate:    q('#StartDate').value,
            EndDate:      q('#EndDate').value,
            TripType:     q('#TripType').value,
            Adults:       parseInt(q('#Adults').value, 10) || 0,
            Children:     parseInt(q('#Children').value, 10) || 0,
            Seniors:      parseInt(q('#Seniors').value, 10) || 0,
            Weather:      q('#Weather').value.trim(),
            SpecialNeeds: q('#SpecialNeeds').value.trim()
        };

        // Client-side quick validation
        if (!data.Destination) { showError('Please enter a destination.'); showLoader(false); return; }
        if (!data.Days || data.Days < 1) { showError('Please enter the number of days.'); showLoader(false); return; }

        fetch('/PackingAssistant/Generate', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: encodeForm(data)
        })
        .then(function (r) { return r.json(); })
        .then(function (json) {
            showLoader(false);
            if (!json.success) { showError(json.error); return; }
            currentData = json.data;
            saveToStorage(currentData);
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

        // Weather tip box
        if (data.weatherTip || (data.generalTips && data.generalTips.length)) {
            var tipBox = document.createElement('div');
            tipBox.className = 'weather-tip-box';
            var tipsHtml = '';
            if (data.generalTips && data.generalTips.length) {
                tipsHtml = '<ul>' + data.generalTips.map(function (t) { return '<li>' + esc(t) + '</li>'; }).join('') + '</ul>';
            }
            tipBox.innerHTML =
                '<i class="fas fa-sun"></i>' +
                '<div class="tip-content"><h4>Weather & Tips</h4>' +
                (data.weatherTip ? '<p>' + esc(data.weatherTip) + '</p>' : '') +
                tipsHtml + '</div>';
            wrap.appendChild(tipBox);
        }

        // Progress bar
        var progWrap = document.createElement('div');
        progWrap.className = 'packing-progress-wrap';
        progWrap.innerHTML =
            '<div class="packing-progress-bar"><div class="packing-progress-fill" id="progressFill" style="width:0%"></div></div>' +
            '<span class="packing-progress-label" id="progressLabel">0 packed</span>';
        wrap.appendChild(progWrap);

        // Categories
        var cats = data.categories || [];
        cats.forEach(function (cat) {
            var sec = document.createElement('div');
            sec.className = 'category-section';
            sec.dataset.category = cat.name;

            var icon = categoryIcon(cat.name);
            sec.innerHTML =
                '<div class="category-header">' +
                '<span class="category-icon"><i class="' + icon + '"></i></span>' +
                '<h3>' + esc(cat.name) + '</h3>' +
                '<span class="category-count">' + (cat.items ? cat.items.length : 0) + ' items</span>' +
                '</div>' +
                '<div class="items-list"></div>' +
                '<div class="add-item-row">' +
                '<input type="text" placeholder="Add custom item..." class="new-item-input" />' +
                '<button type="button" class="add-item-btn"><i class="fas fa-plus"></i> Add</button>' +
                '</div>';

            var list = sec.querySelector('.items-list');
            (cat.items || []).forEach(function (item) {
                list.appendChild(makeCheckItem(item));
            });

            // Add item button
            sec.querySelector('.add-item-btn').addEventListener('click', function () {
                var inp = sec.querySelector('.new-item-input');
                var name = inp.value.trim();
                if (!name) return;
                list.appendChild(makeCheckItem({ name: name, quantity: '1', priority: 'Optional' }));
                inp.value = '';
                updateProgress();
            });
            sec.querySelector('.new-item-input').addEventListener('keydown', function (e) {
                if (e.key === 'Enter') { sec.querySelector('.add-item-btn').click(); e.preventDefault(); }
            });

            wrap.appendChild(sec);
        });

        // Show results section
        var resultsArea = q('#aiResults');
        if (resultsArea) {
            resultsArea.classList.add('active');
            resultsArea.style.display = 'block';
        }
        updateProgress();

        // Scroll into view
        setTimeout(function () {
            if (resultsArea) resultsArea.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }, 100);
    }

    function makeCheckItem(item) {
        var div = document.createElement('div');
        div.className = 'check-item';
        div.dataset.packed = 'false';

        var pClass = 'priority-' + (item.priority || 'optional').toLowerCase();
        div.innerHTML =
            '<div class="item-check" role="checkbox" aria-checked="false" tabindex="0"></div>' +
            '<span class="item-name">' + esc(item.name) + '</span>' +
            '<span class="item-qty">' + esc(item.quantity || '') + '</span>' +
            '<span class="priority-badge ' + pClass + '">' + esc(item.priority || 'Optional') + '</span>' +
            '<button class="item-delete" title="Remove item" aria-label="Remove ' + esc(item.name) + '"><i class="fas fa-times"></i></button>';

        // Toggle packed
        var cb = div.querySelector('.item-check');
        function toggle() {
            var packed = div.dataset.packed === 'true';
            packed = !packed;
            div.dataset.packed = packed;
            div.classList.toggle('packed', packed);
            cb.classList.toggle('checked', packed);
            cb.setAttribute('aria-checked', packed);
            updateProgress();
        }
        cb.addEventListener('click', toggle);
        cb.addEventListener('keydown', function (e) { if (e.key === ' ' || e.key === 'Enter') { e.preventDefault(); toggle(); } });

        // Delete
        div.querySelector('.item-delete').addEventListener('click', function () {
            div.remove();
            updateProgress();
        });

        return div;
    }

    function updateProgress() {
        var all    = qa('#resultsSection .check-item');
        var packed = qa('#resultsSection .check-item.packed');
        var total  = all.length;
        var done   = packed.length;
        var pct    = total === 0 ? 0 : Math.round((done / total) * 100);

        var fill  = q('#progressFill');
        var label = q('#progressLabel');
        if (fill)  fill.style.width = pct + '%';
        if (label) label.textContent = done + ' / ' + total + ' packed (' + pct + '%)';
    }

    /* ---- Loader / error / result visibility ---- */
    function showLoader(on) {
        var loader = q('#aiLoader');
        if (!loader) return;
        loader.classList.toggle('active', on);
        loader.style.display = on ? 'flex' : 'none';
        var submitBtn = q('#submitBtn');
        if (submitBtn) submitBtn.disabled = on;
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

    /* ---- Print ---- */
    window.packingPrint = function () { window.print(); };

    /* ---- Copy as text ---- */
    window.packingCopy = function () {
        if (!currentData) return;
        var lines = [];
        if (currentData.weatherTip) lines.push('Weather & Tips: ' + currentData.weatherTip);
        if (currentData.generalTips) currentData.generalTips.forEach(function (t) { lines.push('  - ' + t); });
        (currentData.categories || []).forEach(function (cat) {
            lines.push('\n--- ' + cat.name.toUpperCase() + ' ---');
            (cat.items || []).forEach(function (item) {
                lines.push('[ ] ' + item.name + ' (x' + item.quantity + ') [' + item.priority + ']');
            });
        });
        copyText(lines.join('\n'));
    };

    function copyText(text) {
        if (navigator.clipboard) {
            navigator.clipboard.writeText(text).then(function () { flashBtn('#copyBtn', 'Copied!'); });
        } else {
            var ta = document.createElement('textarea');
            ta.value = text;
            document.body.appendChild(ta);
            ta.select();
            document.execCommand('copy');
            document.body.removeChild(ta);
            flashBtn('#copyBtn', 'Copied!');
        }
    }

    function flashBtn(sel, label) {
        var btn = q(sel);
        if (!btn) return;
        var orig = btn.textContent;
        btn.textContent = label;
        setTimeout(function () { btn.textContent = orig; }, 1800);
    }

    /* ---- Regenerate ---- */
    window.packingRegenerate = function () {
        hideResults();
        clearError();
        submitForm();
    };

    /* ---- Reset (clear ticks only) ---- */
    window.packingReset = function () {
        qa('#resultsSection .check-item').forEach(function (item) {
            item.dataset.packed = 'false';
            item.classList.remove('packed');
            var cb = item.querySelector('.item-check');
            cb.classList.remove('checked');
            cb.setAttribute('aria-checked', 'false');
        });
        updateProgress();
    };

    /* ---- Try again button (retry button) ---- */
    var retryBtn = q('#retryBtn');
    if (retryBtn) retryBtn.addEventListener('click', function () { clearError(); submitForm(); });

    /* ---- localStorage persistence ---- */
    function saveToStorage(data) {
        try { localStorage.setItem(STORAGE_KEY, JSON.stringify(data)); } catch (e) {}
    }

    function loadFromStorage() {
        try {
            var raw = localStorage.getItem(STORAGE_KEY);
            if (raw) return JSON.parse(raw);
        } catch (e) {}
        return null;
    }

    // On page load: restore previous checklist if present
    var saved = loadFromStorage();
    if (saved) {
        currentData = saved;
        renderResults(saved);
    }

    /* ---- Utility: safe HTML escape ---- */
    function esc(s) {
        if (s == null) return '';
        return String(s)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    /* ---- Category icon map ---- */
    function categoryIcon(name) {
        var map = {
            'clothes':      'fas fa-tshirt',
            'documents':    'fas fa-id-card',
            'electronics':  'fas fa-plug',
            'toiletries':   'fas fa-soap',
            'medicines':    'fas fa-pills',
            'accessories':  'fas fa-glasses',
            'trip-specific':'fas fa-map-marked-alt',
            'trip specific':'fas fa-map-marked-alt'
        };
        return map[(name || '').toLowerCase()] || 'fas fa-box';
    }

})();
