/**
 * ai-packing.js — NexVoy AI Packing Assistant
 *
 * Reads the exact API shape:
 *   data.ml_items / data.essentials / data.extra_weather_suggestions
 *   item.display_group, item.name, item.quantity, item.confidence, item.source
 *   groups = string[] (only used for section order)
 *   weather.temp_min_c / temp_max_c / temp_mean_c / max_wind_kmh / attribution
 *
 * Rejects non-object data (arrays, strings) with a clear error.
 * Debug: append ?debug=1 to the URL.
 */
(function () {
    'use strict';

    var STORAGE_KEY = 'nexvoy_packing_v1';
    var currentData = null;
    var normalizedData = null;
    var requestInProgress = false;

    var DEBUG = (typeof window !== 'undefined' && window.NEXVOY_DEBUG === true) ||
        (typeof location !== 'undefined' && /[?&]debug=1\b/.test(location.search));

    function log() {
        if (!DEBUG) return;
        try { console.log.apply(console, ['[NexVoy packing]'].concat([].slice.call(arguments))); }
        catch (e) { }
    }

    function q(s) { return document.querySelector(s); }
    function qa(s) { return document.querySelectorAll(s); }
    function safeString(v) { return v == null ? '' : String(v); }
    function isArray(v) { return Object.prototype.toString.call(v) === '[object Array]'; }
    function isObject(v) {
        return v != null && typeof v === 'object' && !isArray(v);
    }

    function esc(v) {
        return safeString(v)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;')
            .replace(/'/g, '&#39;');
    }

    function firstAvailable() {
        for (var i = 0; i < arguments.length; i++) {
            var v = arguments[i];
            if (v !== undefined && v !== null && v !== '') return v;
        }
        return null;
    }

    /* ---------------- Counter buttons ---------------- */

    qa('.counter-btn').forEach(function (btn) {
        btn.addEventListener('click', function () {
            var t = document.getElementById(btn.dataset.target);
            if (!t) return;
            var v = parseInt(t.value, 10) || 0;
            var d = btn.dataset.dir === 'up' ? 1 : -1;
            var min = parseInt(t.dataset.min, 10) || 0;
            var max = parseInt(t.dataset.max, 10) || 99;
            t.value = Math.min(max, Math.max(min, v + d));
        });
    });

    /* ---------------- Activity chips ---------------- */

    var activityChips = qa('#activityChips .ai-chip-opt');

    function syncActivities() {
        var out = [];
        activityChips.forEach(function (chip) {
            var cb = chip.querySelector('input[type=checkbox]');
            if (cb && cb.checked) {
                out.push(chip.getAttribute('data-activity'));
                chip.classList.add('selected');
            } else {
                chip.classList.remove('selected');
            }
        });
        var hidden = q('#Activities');
        if (hidden) hidden.value = out.join(',');
    }

    activityChips.forEach(function (chip) {
        var cb = chip.querySelector('input[type=checkbox]');
        if (cb) cb.addEventListener('change', syncActivities);
        chip.addEventListener('click', function (e) {
            if (e.target === cb) return;
            e.preventDefault();
            cb.checked = !cb.checked;
            syncActivities();
        });
    });

    /* ---------------- Laundry radios ---------------- */

    qa('label[data-radio="laundry"]').forEach(function (lbl) {
        var input = lbl.querySelector('input');
        lbl.addEventListener('click', function () {
            qa('label[data-radio="laundry"]').forEach(function (x) {
                x.classList.remove('selected');
                var i = x.querySelector('input');
                if (i) i.checked = false;
            });
            input.checked = true;
            lbl.classList.add('selected');
        });
    });

    /* ---------------- Advanced toggle ---------------- */

    var advToggle = q('#advToggle');
    var advBody = q('#advBody');
    if (advToggle && advBody) {
        advToggle.addEventListener('click', function () {
            advBody.classList.toggle('open');
        });
    }

    /* ---------------- Sliders ---------------- */

    qa('input[type=range]').forEach(function (r) {
        r.addEventListener('input', function () {
            var out = document.getElementById(r.getAttribute('data-out'));
            if (out) out.textContent = r.value;
        });
    });

    /* ---------------- Days auto-fill ---------------- */

    var startInput = q('#StartDate');
    var endInput = q('#EndDate');
    var daysInput = q('#Days');

    function updateDays() {
        if (!startInput || !endInput || !daysInput) return;
        if (!startInput.value || !endInput.value) return;
        var s = new Date(startInput.value);
        var e = new Date(endInput.value);
        if (isNaN(s) || isNaN(e) || e < s) return;
        daysInput.value = Math.round((e - s) / 86400000) + 1;
    }
    if (startInput) startInput.addEventListener('change', updateDays);
    if (endInput) endInput.addEventListener('change', updateDays);
    updateDays();

    /* ---------------- Form submission ---------------- */

    var form = q('#packingForm');

    if (form) {
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            submitForm();
        });
    }

    function submitForm() {
        if (requestInProgress) return;
        clearError();
        syncActivities();

        var token = q('input[name="__RequestVerificationToken"]');
        function val(sel) { var el = q(sel); return el ? el.value.trim() : ''; }
        function num(sel) { var el = q(sel); return el ? parseInt(el.value, 10) || 0 : 0; }

        var laundryInput = q('input[name="LaundryAvailable"]:checked');

        var city = val('#City');
        var country = val('#Country');

        if (!city) { showError('Please enter the city or destination.'); return; }
        if (!country) { showError('Please select a country.'); return; }

        var destHidden = q('#Destination');
        if (destHidden) destHidden.value = city + ', ' + country;

        var data = {
            __RequestVerificationToken: token ? token.value : '',
            City: city,
            Country: country,
            Destination: city + ', ' + country,
            TripType: val('#TripType'),
            StartDate: val('#StartDate'),
            EndDate: val('#EndDate'),
            Days: num('#Days'),
            Adults: num('#Adults'),
            Children: num('#Children'),
            Seniors: num('#Seniors'),
            AgeGroup: val('#AgeGroup'),
            PackingPreference: val('#PackingPreference'),
            LaundryAvailable: laundryInput ? laundryInput.value : '',
            Transportation: val('#Transportation'),
            Accommodation: val('#Accommodation'),
            Activities: val('#Activities'),
            ClimateZone: val('#ClimateZone'),
            SpecialNeeds: val('#SpecialNeeds'),
            Temperature: val('#Temperature'),
            Budget: val('#Budget'),
            Region: val('#Region'),
            Culture: num('#Culture') || 3,
            Adventure: num('#Adventure') || 3,
            Nature: num('#Nature') || 3,
            Beaches: num('#Beaches') || 3,
            Nightlife: num('#Nightlife') || 3,
            Cuisine: num('#Cuisine') || 3,
            Wellness: num('#Wellness') || 3,
            Urban: num('#Urban') || 3,
            Seclusion: num('#Seclusion') || 3
        };

        if (!data.TripType) { showError('Please choose a trip type.'); return; }
        if (!data.StartDate || !data.EndDate) { showError('Please choose both trip dates.'); return; }
        if (data.EndDate < data.StartDate) { showError('End date must be on or after start date.'); return; }
        var today = new Date(); today.setHours(0, 0, 0, 0);
        if (new Date(data.StartDate) < today) { showError('Trip start date cannot be in the past.'); return; }
        if ((data.Adults + data.Children + data.Seniors) < 1) { showError('Please select at least one traveler.'); return; }

        requestInProgress = true;
        showLoader(true);
        hideResults();

        var endpoint = form.getAttribute('action') || '/PackingAssistant/Generate';

        fetch(endpoint, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/x-www-form-urlencoded; charset=UTF-8',
                'X-Requested-With': 'XMLHttpRequest'
            },
            body: encodeForm(data),
            credentials: 'same-origin'
        })
            .then(function (r) {
                return r.text().then(function (t) {
                    var j;
                    try { j = JSON.parse(t); }
                    catch (e) { throw new Error('The server returned an invalid response (HTTP ' + r.status + ').'); }
                    if (!r.ok) throw new Error(j.error || j.message || ('Request failed: ' + r.status));
                    return j;
                });
            })
            .then(function (j) {
                log('MVC response received. success =', j && j.success);

                if (!j || j.success !== true) {
                    throw new Error(j && j.error ? j.error : 'Unable to generate your packing list.');
                }

                var payload = j.data;

                log('typeof j.data =', typeof payload,
                    '· Array.isArray(j.data) =', isArray(payload),
                    '· isObject(j.data) =', isObject(payload));

                if (typeof payload === 'string') {
                    // Defensive: if server wrapped it in a string.
                    try { payload = JSON.parse(payload); }
                    catch (e) { throw new Error('Server returned data as a string, not an object.'); }
                    log('After string parse: isObject =', isObject(payload));
                }

                if (!isObject(payload)) {
                    throw new Error(
                        'The server returned an unexpected data format (' +
                        (isArray(payload) ? 'array' : typeof payload) +
                        '). Please rebuild the solution and restart IIS Express.'
                    );
                }

                log('Top-level keys in data:', Object.keys(payload));
                log('Counts — ml_items:',
                    isArray(payload.ml_items) ? payload.ml_items.length : 'missing',
                    '· essentials:',
                    isArray(payload.essentials) ? payload.essentials.length : 'missing',
                    '· extra_weather_suggestions:',
                    isArray(payload.extra_weather_suggestions) ? payload.extra_weather_suggestions.length : 'missing');

                currentData = payload;
                saveToStorage(currentData);
                renderResults(currentData);
            })
            .catch(function (err) {
                log('Error:', err);
                showError(err && err.message ? err.message : 'Could not reach the server.');
            })
            .finally(function () {
                requestInProgress = false;
                showLoader(false);
            });
    }

    function encodeForm(obj) {
        return Object.keys(obj).map(function (k) {
            return encodeURIComponent(k) + '=' +
                encodeURIComponent(obj[k] == null ? '' : obj[k]);
        }).join('&');
    }

    /* ---------------- Response normalisation ---------------- */

    function itemToRow(item, sourceArray) {
        if (item == null) return null;
        if (typeof item === 'string' || typeof item === 'number') {
            return {
                name: safeString(item),
                quantity: '1',
                priority: '',
                group: 'Other',
                confidence: null,
                source: sourceArray
            };
        }
        if (typeof item !== 'object') return null;

        var name = firstAvailable(
            item.name, item.item, item.item_name,
            item.label, item.product, item.description, item.item_id
        );
        if (!name) { log('Dropping item with no name:', item); return null; }

        return {
            name: safeString(name),
            quantity: safeString(firstAvailable(item.quantity, item.qty, item.amount, '1')),
            priority: safeString(firstAvailable(
                item.priority, item.importance,
                (item.confidence != null && item.confidence >= 0.8) ? 'Important' : ''
            )),
            group: safeString(firstAvailable(
                item.display_group, item.category, item.group, 'Other'
            )),
            confidence: item.confidence != null ? item.confidence : null,
            source: sourceArray
        };
    }

    function normalizeResponse(data) {
        if (typeof data === 'string') {
            try { data = JSON.parse(data); } catch (e) { data = null; }
        }

        if (!isObject(data)) {
            return { weather: null, groups: [], itemsByGroup: {}, tips: [], totalItems: 0 };
        }

        var arrays = { ml_items: [], essentials: [], extra_weather_suggestions: [] };
        ['ml_items', 'essentials', 'extra_weather_suggestions'].forEach(function (k) {
            if (isArray(data[k])) arrays[k] = data[k];
        });

        var itemsByGroup = {};
        var total = 0;

        function pushItems(list, sourceKey) {
            if (!isArray(list)) return;
            list.forEach(function (it) {
                var row = itemToRow(it, sourceKey);
                if (!row) return;
                if (!itemsByGroup[row.group]) itemsByGroup[row.group] = [];
                itemsByGroup[row.group].push(row);
                total++;
            });
        }

        pushItems(arrays.ml_items, 'ml_items');
        pushItems(arrays.essentials, 'essentials');
        pushItems(arrays.extra_weather_suggestions, 'extra_weather_suggestions');

        log('Normalized item count:', total, '· groups found:', Object.keys(itemsByGroup));

        var order = [];
        if (isArray(data.groups)) {
            data.groups.forEach(function (g) {
                if (typeof g === 'string' && itemsByGroup[g] && order.indexOf(g) === -1) {
                    order.push(g);
                }
            });
        }
        Object.keys(itemsByGroup).forEach(function (g) {
            if (order.indexOf(g) === -1) order.push(g);
        });

        var tips = [];
        ['notes', 'limitations'].forEach(function (k) {
            if (isArray(data[k])) {
                data[k].forEach(function (t) {
                    if (typeof t === 'string') tips.push(t);
                    else if (t && typeof t === 'object') {
                        tips.push(safeString(firstAvailable(t.message, t.tip, t.description, t.text)));
                    }
                });
            }
        });

        return {
            weather: data.weather || null,
            groups: order,
            itemsByGroup: itemsByGroup,
            tips: tips.filter(function (t) { return t; }),
            totalItems: total
        };
    }

    /* ---------------- Rendering ---------------- */

    function renderResults(data) {
        var wrap = q('#resultsSection');
        if (!wrap) return;

        normalizedData = normalizeResponse(data);
        wrap.innerHTML = '';

        // Weather
        if (normalizedData.weather) {
            var w = normalizedData.weather;
            var wParts = [];

            if (w.summary) wParts.push(esc(w.summary));
            if (w.expected_weather) wParts.push('Expected: ' + esc(w.expected_weather));

            var tmin = firstAvailable(w.temp_min_c, w.min_temp_c);
            var tmax = firstAvailable(w.temp_max_c, w.max_temp_c);
            var tmean = firstAvailable(w.temp_mean_c, w.mean_temp_c);

            if (tmin != null && tmax != null) {
                wParts.push('Temp: ' + esc(tmin) + '–' + esc(tmax) + '°C');
            } else if (tmean != null) {
                wParts.push('Mean: ' + esc(tmean) + '°C');
            }
            if (w.precipitation_level) wParts.push('Precipitation: ' + esc(w.precipitation_level));
            var wind = firstAvailable(w.max_wind_kmh, w.wind_speed_max_kmh);
            if (wind != null) wParts.push('Wind: ' + esc(wind) + ' km/h');

            if (wParts.length) {
                var box = document.createElement('div');
                box.className = 'weather-tip-box';
                box.innerHTML =
                    '<i class="fas fa-sun"></i>' +
                    '<div class="tip-content"><h4>Weather & Tips</h4>' +
                    '<p>' + wParts.join(' · ') + '</p>' +
                    (w.attribution
                        ? '<p style="font-size:.78rem;color:#6b7280;margin-top:.4rem;">' + esc(w.attribution) + '</p>'
                        : '') +
                    '</div>';
                wrap.appendChild(box);
            }
        }

        // Progress
        var prog = document.createElement('div');
        prog.className = 'packing-progress-wrap';
        prog.innerHTML =
            '<div class="packing-progress-bar">' +
            '<div class="packing-progress-fill" id="progressFill" style="width:0%"></div>' +
            '</div>' +
            '<span class="packing-progress-label" id="progressLabel">0 packed</span>';
        wrap.appendChild(prog);

        // Groups
        var anyItems = false;

        normalizedData.groups.forEach(function (groupName) {
            var items = normalizedData.itemsByGroup[groupName];
            if (!items || !items.length) return;
            anyItems = true;

            var section = document.createElement('div');
            section.className = 'category-section';
            section.innerHTML =
                '<div class="category-header">' +
                '<span class="category-icon"><i class="' + categoryIcon(groupName) + '"></i></span>' +
                '<h3>' + esc(groupName) + '</h3>' +
                '<span class="category-count">' + items.length + ' items</span>' +
                '</div>' +
                '<div class="items-list"></div>' +
                '<div class="add-item-row">' +
                '<input type="text" placeholder="Add custom item..." class="new-item-input" />' +
                '<button type="button" class="add-item-btn"><i class="fas fa-plus"></i> Add</button>' +
                '</div>';

            var list = section.querySelector('.items-list');
            items.forEach(function (it) { list.appendChild(makeCheckItem(it)); });

            section.querySelector('.add-item-btn').addEventListener('click', function () {
                var inp = section.querySelector('.new-item-input');
                var name = inp.value.trim();
                if (!name) return;
                list.appendChild(makeCheckItem({ name: name, quantity: '1', priority: '' }));
                inp.value = '';
                updateCount(section);
                updateProgress();
            });

            section.querySelector('.new-item-input').addEventListener('keydown', function (e) {
                if (e.key === 'Enter') { e.preventDefault(); section.querySelector('.add-item-btn').click(); }
            });

            wrap.appendChild(section);
        });

        if (!anyItems) {
            var empty = document.createElement('div');
            empty.className = 'packing-empty-state';
            empty.innerHTML =
                '<i class="fas fa-suitcase"></i>' +
                '<h3>No packing items were returned</h3>' +
                '<p>The API responded successfully, but no usable items were found.</p>' +
                (DEBUG
                    ? '<p style="font-size:.8rem;color:#9ca3af;">Debug: total normalized items = ' +
                    normalizedData.totalItems + '. See console for details.</p>'
                    : '');
            wrap.appendChild(empty);
        }

        // Tips
        if (normalizedData.tips.length) {
            var tb = document.createElement('div');
            tb.className = 'weather-tip-box';
            tb.innerHTML =
                '<i class="fas fa-info-circle"></i>' +
                '<div class="tip-content"><h4>Notes & Limitations</h4><ul>' +
                normalizedData.tips.map(function (t) { return '<li>' + esc(t) + '</li>'; }).join('') +
                '</ul></div>';
            wrap.appendChild(tb);
        }

        var area = q('#aiResults');
        if (area) { area.classList.add('active'); area.style.display = 'block'; }
        updateProgress();

        setTimeout(function () {
            if (area) area.scrollIntoView({ behavior: 'smooth', block: 'start' });
        }, 100);
    }

    function makeCheckItem(item) {
        var div = document.createElement('div');
        div.className = 'check-item';
        div.dataset.packed = 'false';
        var priorityClass = (item.priority || 'optional').toLowerCase().replace(/[^a-z0-9-]/g, '-');

        div.innerHTML =
            '<div class="item-check" role="checkbox" aria-checked="false" tabindex="0"></div>' +
            '<span class="item-name">' + esc(item.name) + '</span>' +
            '<span class="item-qty">' + esc(item.quantity || '') + '</span>' +
            (item.priority
                ? '<span class="priority-badge priority-' + esc(priorityClass) + '">' + esc(item.priority) + '</span>'
                : '') +
            '<button type="button" class="item-delete" title="Remove" aria-label="Remove">' +
            '<i class="fas fa-times"></i></button>';

        var cb = div.querySelector('.item-check');

        function toggle() {
            var p = div.dataset.packed === 'true';
            p = !p;
            div.dataset.packed = String(p);
            div.classList.toggle('packed', p);
            cb.classList.toggle('checked', p);
            cb.setAttribute('aria-checked', String(p));
            updateProgress();
        }
        cb.addEventListener('click', toggle);
        cb.addEventListener('keydown', function (e) {
            if (e.key === ' ' || e.key === 'Enter') { e.preventDefault(); toggle(); }
        });

        div.querySelector('.item-delete').addEventListener('click', function () {
            var sec = div.closest('.category-section');
            div.remove();
            if (sec) updateCount(sec);
            updateProgress();
        });

        return div;
    }

    function updateCount(sec) {
        if (!sec) return;
        var c = sec.querySelectorAll('.items-list .check-item').length;
        var lbl = sec.querySelector('.category-count');
        if (lbl) lbl.textContent = c + ' items';
    }

    function updateProgress() {
        var all = qa('#resultsSection .check-item');
        var packed = qa('#resultsSection .check-item.packed');
        var total = all.length, done = packed.length;
        var pct = total ? Math.round((done / total) * 100) : 0;
        var fill = q('#progressFill'), lbl = q('#progressLabel');
        if (fill) fill.style.width = pct + '%';
        if (lbl) lbl.textContent = done + ' / ' + total + ' packed (' + pct + '%)';
    }

    /* ---------------- Loader / error ---------------- */

    function showLoader(on) {
        var l = q('#aiLoader');
        if (l) { l.classList.toggle('active', on); l.style.display = on ? 'flex' : 'none'; }
        var b = q('#submitBtn'); if (b) b.disabled = on;
    }
    function showError(msg) {
        var b = q('#errorBanner'); if (!b) return;
        b.querySelector('.error-msg').textContent = msg;
        b.classList.add('active'); b.style.display = 'flex';
    }
    function clearError() {
        var b = q('#errorBanner'); if (!b) return;
        b.classList.remove('active'); b.style.display = 'none';
    }
    function hideResults() {
        var r = q('#aiResults'); if (r) { r.classList.remove('active'); r.style.display = 'none'; }
    }

    /* ---------------- Public actions ---------------- */

    window.packingPrint = function () { window.print(); };

    window.packingCopy = function () {
        if (!currentData) return;
        var d = normalizeResponse(currentData);
        var lines = [];
        if (d.weather && d.weather.summary) lines.push('Weather: ' + d.weather.summary);
        d.groups.forEach(function (g) {
            lines.push('\n--- ' + g.toUpperCase() + ' ---');
            (d.itemsByGroup[g] || []).forEach(function (it) {
                lines.push('[ ] ' + it.name + ' (x' + it.quantity + ')');
            });
        });
        copyText(lines.join('\n'));
    };

    function copyText(text) {
        if (navigator.clipboard && window.isSecureContext) {
            navigator.clipboard.writeText(text).then(function () {
                flashBtn('#copyBtn', 'Copied!');
            }).catch(function () { fallbackCopy(text); });
        } else {
            fallbackCopy(text);
        }
    }
    function fallbackCopy(text) {
        var ta = document.createElement('textarea');
        ta.value = text; ta.style.position = 'fixed'; ta.style.opacity = '0';
        document.body.appendChild(ta); ta.select();
        try { document.execCommand('copy'); flashBtn('#copyBtn', 'Copied!'); }
        catch (e) { flashBtn('#copyBtn', 'Copy failed'); }
        document.body.removeChild(ta);
    }
    function flashBtn(sel, label) {
        var b = q(sel); if (!b) return;
        var o = b.textContent; b.textContent = label;
        setTimeout(function () { b.textContent = o; }, 1800);
    }

    window.packingRegenerate = function () {
        if (requestInProgress) return;
        hideResults(); clearError(); submitForm();
    };

    window.packingReset = function () {
        qa('#resultsSection .check-item').forEach(function (i) {
            i.dataset.packed = 'false';
            i.classList.remove('packed');
            var c = i.querySelector('.item-check');
            if (c) { c.classList.remove('checked'); c.setAttribute('aria-checked', 'false'); }
        });
        updateProgress();
    };

    /* ---------------- Retry ---------------- */

    var retry = q('#retryBtn');
    if (retry) retry.addEventListener('click', function () {
        if (requestInProgress) return;
        clearError(); submitForm();
    });

    /* ---------------- Storage ---------------- */

    function saveToStorage(d) {
        try { localStorage.setItem(STORAGE_KEY, JSON.stringify(d)); } catch (e) { }
    }
    function loadFromStorage() {
        try { var r = localStorage.getItem(STORAGE_KEY); return r ? JSON.parse(r) : null; }
        catch (e) { return null; }
    }

    var saved = loadFromStorage();
    if (saved) { currentData = saved; renderResults(saved); }

    /* ---------------- Icons ---------------- */

    function categoryIcon(name) {
        var n = safeString(name).toLowerCase();
        var m = {
            'clothing': 'fas fa-tshirt',
            'clothes': 'fas fa-tshirt',
            'documents': 'fas fa-id-card',
            'electronics': 'fas fa-plug',
            'toiletries': 'fas fa-soap',
            'health & safety': 'fas fa-pills',
            'health': 'fas fa-pills',
            'accessories': 'fas fa-glasses',
            'footwear': 'fas fa-shoe-prints',
            'travel essentials': 'fas fa-passport',
            'weather-specific essentials': 'fas fa-cloud-sun',
            'activity-specific essentials': 'fas fa-map-marked-alt',
            'miscellaneous': 'fas fa-box',
            'weather': 'fas fa-cloud-sun',
            'essentials': 'fas fa-star',
            'other': 'fas fa-box'
        };
        return m[n] || 'fas fa-box';
    }
})();