/**
 * DotNetCoreSocialApi - Admin Dashboard Client Interactivity
 *
 * Standalone progressive-enhancement module for the embedded admin console
 * (`/admin/*`, server-rendered by `AdminDashboardController`). Vanilla JS only,
 * no dependencies. Every entry point is null-safe: pages (or test harnesses)
 * without the matching DOM hooks are no-ops and never throw.
 */

(function () {
    'use strict';

    var hasWindow = (typeof window !== 'undefined');
    var root = hasWindow ? window : {};

    function hasDom() {
        return hasWindow && (typeof document !== 'undefined') && !!document.documentElement;
    }

    function prefersReducedMotion() {
        if (!hasWindow || typeof window.matchMedia !== 'function') return false;
        try {
            return window.matchMedia('(prefers-reduced-motion: reduce)').matches === true;
        } catch (err) {
            return false;
        }
    }

    function safeGet(key) {
        try {
            if (!hasWindow || typeof window.localStorage === 'undefined' || window.localStorage == null) return null;
            return window.localStorage.getItem(key);
        } catch (err) {
            return null;
        }
    }

    function safeSet(key, value) {
        try {
            if (!hasWindow || typeof window.localStorage === 'undefined' || window.localStorage == null) return false;
            window.localStorage.setItem(key, value);
            return true;
        } catch (err) {
            return false;
        }
    }

    function getById(id) {
        if (!hasDom() || !id) return null;
        try {
            return document.getElementById(id);
        } catch (err) {
            return null;
        }
    }

    function callIfFunction(fnName) {
        try {
            var fn = root[fnName];
            if (typeof fn === 'function') {
                fn();
                return true;
            }
        } catch (err) { /* page-level loaders own their errors; never break the shell */ }
        return false;
    }

    /* ---------- Theme (data-theme on <html>, persisted to localStorage) ---------- */

    var THEME_STORAGE_KEY = 'admin-theme';
    var THEME_DARK = 'dark';

    function getAdminTheme() {
        if (!hasDom()) return '';
        var stored = safeGet(THEME_STORAGE_KEY);
        if (stored === THEME_DARK) return THEME_DARK;
        try {
            return document.documentElement.getAttribute('data-theme') === THEME_DARK ? THEME_DARK : '';
        } catch (err) {
            return '';
        }
    }

    function setAdminTheme(theme) {
        if (!hasDom()) return '';
        var next = (theme === THEME_DARK) ? THEME_DARK : '';
        try {
            if (next) document.documentElement.setAttribute('data-theme', next);
            else document.documentElement.removeAttribute('data-theme');
        } catch (err) {
            return getAdminTheme();
        }
        safeSet(THEME_STORAGE_KEY, next);
        return next;
    }

    function toggleAdminTheme() {
        if (!hasDom()) return '';
        return setAdminTheme(getAdminTheme() === THEME_DARK ? '' : THEME_DARK);
    }

    function initAdminTheme() {
        if (!hasDom()) return '';
        if (safeGet(THEME_STORAGE_KEY) === THEME_DARK) {
            try {
                document.documentElement.setAttribute('data-theme', THEME_DARK);
            } catch (err) { /* ignore: theme is cosmetic */ }
            return THEME_DARK;
        }
        return getAdminTheme();
    }

    /* ---------- Command palette (Ctrl/Cmd+K, Esc) ---------- */

    var cmdkRoutes = [
        { title: 'Overview', hint: 'Executive dashboard', tab: 'overview', url: '/admin/dashboard' },
        { title: 'Live Traffic', hint: 'Anomaly stream', tab: 'overview', url: '/admin/dashboard' },
        { title: 'User Intelligence', hint: 'Directory + safety', tab: 'users', url: '/admin/users' },
        { title: 'Growth & Cohorts', hint: 'Signup velocity', tab: 'users', url: '/admin/users' },
        { title: 'Platform Velocity', hint: 'Content trends', tab: 'overview', url: '/admin/dashboard' },
        { title: 'Moderation & Safety', hint: 'Review queue', tab: 'moderation', url: '/admin/moderation' },
        { title: 'Reported Posts', hint: 'Report triage queue', tab: 'reports', url: '/admin/reports' },
        { title: 'API Observability', hint: 'Latency + endpoints', tab: 'diagnostics', url: '/admin/system' },
        { title: 'Audit Trail', hint: 'Admin actions', tab: 'audit', url: '/admin/audit-logs' },
        { title: 'System Health', hint: 'Process + cache', tab: 'diagnostics', url: '/admin/system' },
        { title: 'Documentation', hint: 'Admin guide + runbooks', tab: 'docs', url: '/admin/docs' }
    ];

    function escapeHtml(value) {
        return String(value == null ? '' : value)
            .replace(/&/g, '&amp;')
            .replace(/</g, '&lt;')
            .replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;');
    }

    function paletteGo(tab, url) {
        closeCommandPalette();
        try {
            var tabEl = tab ? getById('tab-' + tab) : null;
            if (tab && tabEl && typeof root.showTab === 'function') {
                root.showTab(tab, true);
                return;
            }
        } catch (err) { /* fall through to full navigation */ }
        if (hasWindow && url) {
            try {
                window.location.href = url;
            } catch (err) { /* ignore: navigation unavailable */ }
        }
    }

    function bindPaletteResultsOnce() {
        var results = getById('cmdk-results');
        if (!results || results.getAttribute('data-cmdk-bound') === '1') return;
        try {
            results.setAttribute('data-cmdk-bound', '1');
            results.addEventListener('click', function (ev) {
                var target = ev && ev.target;
                var item = (target && typeof target.closest === 'function')
                    ? target.closest('[data-cmdk-tab]')
                    : null;
                if (!item) return;
                paletteGo(
                    item.getAttribute('data-cmdk-tab'),
                    item.getAttribute('data-cmdk-url')
                );
            });
        } catch (err) { /* palette stays usable via keyboard nav */ }
    }

    function openCommandPalette() {
        var backdrop = getById('cmdk-backdrop');
        var input = getById('cmdk-input');
        if (!backdrop || !input) return false;
        bindPaletteResultsOnce();
        try {
            backdrop.classList.add('open');
        } catch (err) {
            return false;
        }
        try {
            input.value = '';
        } catch (err) { /* ignore */ }
        filterCommandPalette();
        var reduce = prefersReducedMotion();
        if (hasWindow && typeof window.setTimeout === 'function') {
            window.setTimeout(function () {
                try {
                    if (reduce) input.focus({ preventScroll: true });
                    else input.focus();
                } catch (err) {
                    try {
                        input.focus();
                    } catch (inner) { /* focus is best-effort */ }
                }
            }, reduce ? 0 : 30);
        }
        return true;
    }

    function closeCommandPalette() {
        var backdrop = getById('cmdk-backdrop');
        if (!backdrop) return false;
        try {
            backdrop.classList.remove('open');
            return true;
        } catch (err) {
            return false;
        }
    }

    function filterCommandPalette() {
        var input = getById('cmdk-input');
        var results = getById('cmdk-results');
        if (!input || !results) return 0;
        var q = '';
        try {
            q = String(input.value == null ? '' : input.value).toLowerCase();
        } catch (err) {
            q = '';
        }
        var matches = cmdkRoutes.filter(function (r) {
            return ((r.title || '') + ' ' + (r.hint || '')).toLowerCase().indexOf(q) !== -1;
        });
        try {
            results.innerHTML = matches.map(function (r) {
                return '<div class="cmdk-item" data-cmdk-tab="' + escapeHtml(r.tab) +
                    '" data-cmdk-url="' + escapeHtml(r.url) +
                    '"><span>&#9678; ' + escapeHtml(r.title) +
                    '</span><span style="color:var(--text-muted);font-size:0.72rem;">' +
                    escapeHtml(r.hint) + '</span></div>';
            }).join('') || '<div class="cmdk-item">No matches</div>';
        } catch (err) {
            return 0;
        }
        return matches.length;
    }

    var shortcutsBound = false;

    function bindAdminShortcuts() {
        if (shortcutsBound || !hasDom() || typeof document.addEventListener !== 'function') return false;
        shortcutsBound = true;
        try {
            document.addEventListener('keydown', function (e) {
                if (!e) return;
                var key = '';
                try {
                    key = String(e.key == null ? '' : e.key).toLowerCase();
                } catch (err) { /* ignore */ }
                if ((e.ctrlKey || e.metaKey) && key === 'k') {
                    try {
                        e.preventDefault();
                    } catch (err) { /* ignore */ }
                    openCommandPalette();
                } else if (key === 'escape') {
                    closeCommandPalette();
                }
            });
            return true;
        } catch (err) {
            return false;
        }
    }

    /* ---------- Docs search filter (in-page, no backend call) ---------- */

    function filterDocs(query) {
        var tab = getById('tab-docs');
        if (!tab) return 0;
        var q = String(query == null ? '' : query).trim().toLowerCase();
        var articles;
        try {
            articles = tab.querySelectorAll('.docs-article');
        } catch (err) {
            return 0;
        }
        if (!articles || articles.length === 0) return 0;
        var visibleCount = 0;
        var firstVisibleId = '';
        Array.prototype.forEach.call(articles, function (article) {
            var text = '';
            try {
                text = String(article.textContent == null ? '' : article.textContent).toLowerCase();
            } catch (err) { /* treat as non-matching only when querying */ }
            var show = (q === '') || (text.indexOf(q) !== -1);
            try {
                if (show) article.removeAttribute('hidden');
                else article.setAttribute('hidden', 'hidden');
            } catch (err) { /* visibility toggle is best-effort */ }
            if (show) {
                visibleCount += 1;
                if (!firstVisibleId) {
                    try {
                        firstVisibleId = article.getAttribute('id') || '';
                    } catch (err) { /* ignore */ }
                }
            }
        });
        try {
            var toc = tab.querySelector('.docs-toc') || document.querySelector('.docs-toc');
            if (toc) {
                var links = toc.querySelectorAll('a[href^="#"]');
                Array.prototype.forEach.call(links, function (link) {
                    var href = '';
                    try {
                        href = link.getAttribute('href') || '';
                    } catch (err) { /* ignore */ }
                    var isActive = !!firstVisibleId && href === ('#' + firstVisibleId);
                    try {
                        if (isActive) link.classList.add('active');
                        else link.classList.remove('active');
                    } catch (err) { /* ignore */ }
                });
            }
        } catch (err) { /* TOC highlight is cosmetic */ }
        return visibleCount;
    }

    function bindDocsSearchOnce() {
        var search = getById('docs-search');
        if (!search || search.getAttribute('data-docs-bound') === '1') return false;
        if (typeof search.addEventListener !== 'function') return false;
        try {
            search.setAttribute('data-docs-bound', '1');
            search.addEventListener('input', function (ev) {
                var value = '';
                try {
                    value = (ev && ev.target && ev.target.value != null) ? ev.target.value : search.value;
                } catch (err) { /* ignore */ }
                filterDocs(value);
            });
            return true;
        } catch (err) {
            return false;
        }
    }

    /* ---------- Range + heartbeat (existing endpoints only) ---------- */

    var adminRange = '7d';

    function setAdminRange(r) {
        if (!hasDom()) return adminRange;
        var requested = (r == null || r === '') ? '7d' : String(r);
        adminRange = (requested === 'today') ? '7d' : requested;
        try {
            var buttons = document.querySelectorAll('.range-btn');
            Array.prototype.forEach.call(buttons, function (b) {
                var datasetRange = '';
                try {
                    datasetRange = (b.dataset && b.dataset.range) ? String(b.dataset.range) : '';
                } catch (err) { /* ignore */ }
                var isActive = (datasetRange === requested) ||
                    (requested === 'today' && datasetRange === 'today');
                try {
                    b.classList.toggle('active', isActive);
                } catch (err) { /* ignore */ }
            });
        } catch (err) { /* range buttons absent on this page */ }
        callIfFunction('loadExecutive');
        callIfFunction('loadUserIntelligence');
        return adminRange;
    }

    function getAdminRange() {
        return adminRange;
    }

    var heartbeatTimer = null;

    function pollAdminHeartbeat() {
        var dot = getById('health-dot');
        var txt = getById('health-text');
        var badge = getById('env-badge');
        var beat = getById('health-heartbeat');
        if (!dot && !txt && !badge && !beat) return;
        if (!hasWindow || typeof window.fetch !== 'function') return;
        try {
            window.fetch('/api/admin/analytics/diagnostics').then(function (res) {
                if (!res || !res.ok) {
                    if (txt) { try { txt.innerText = 'Degraded'; } catch (err) { /* ignore */ } }
                    if (dot) { try { dot.style.backgroundColor = 'var(--alert)'; } catch (err) { /* ignore */ } }
                    if (badge) { try { badge.classList.add('degraded'); } catch (err) { /* ignore */ } }
                    return null;
                }
                return res.json();
            }).then(function (j) {
                if (j == null) return;
                var d = j.data || j;
                if (txt) { try { txt.innerText = 'Operational'; } catch (err) { /* ignore */ } }
                if (dot) { try { dot.style.backgroundColor = 'var(--growth)'; } catch (err) { /* ignore */ } }
                if (badge) {
                    try {
                        if (d && d.environmentName) badge.innerText = String(d.environmentName).toUpperCase();
                        badge.classList.remove('degraded');
                    } catch (err) { /* ignore */ }
                }
            }).catch(function () {
                if (txt) { try { txt.innerText = 'Unreachable'; } catch (err) { /* ignore */ } }
            });
        } catch (err) { /* heartbeat must never break the page */ }
    }

    function startAdminHeartbeat(intervalMs) {
        if (!hasDom() || !hasWindow || typeof window.fetch !== 'function') return false;
        if (!getById('health-dot') && !getById('health-text') &&
            !getById('health-heartbeat') && !getById('env-badge')) return false;
        if (heartbeatTimer) return true;
        var interval = (typeof intervalMs === 'number' && intervalMs > 0) ? intervalMs : 30000;
        try {
            pollAdminHeartbeat();
            heartbeatTimer = window.setInterval(pollAdminHeartbeat, interval);
            return true;
        } catch (err) {
            return false;
        }
    }

    /* ---------- Namespace (existing functions preserved verbatim in behavior) ---------- */

    var dashboard = root.adminDashboard || {};

    dashboard.copyToClipboard = async function (text) {
        if (typeof navigator !== 'undefined' && navigator.clipboard) {
            try {
                await navigator.clipboard.writeText(text);
                return true;
            } catch (err) {
                if (typeof console !== 'undefined' && console.error) {
                    console.error('Failed to copy text: ', err);
                }
            }
        }
        return false;
    };

    dashboard.scrollToTop = function () {
        if (!hasWindow || typeof window.scrollTo !== 'function') return;
        var reduce = prefersReducedMotion();
        try {
            window.scrollTo({ top: 0, behavior: reduce ? 'auto' : 'smooth' });
        } catch (err) {
            try {
                window.scrollTo(0, 0);
            } catch (inner) { /* scrolling unavailable */ }
        }
    };

    dashboard.formatStandardDate = function (isoDateString) {
        if (!isoDateString) {
            return { display: '--', relative: '', tooltip: 'No date recorded', exactUtc: '--', local: '--' };
        }
        let raw = String(isoDateString).trim();
        if (!raw.endsWith('Z') && !raw.includes('+') && !/T.*\-\d\d:?\d\d$/.test(raw)) {
            raw += 'Z';
        }
        const d = new Date(raw);
        if (isNaN(d.getTime())) {
            return { display: raw, relative: '', tooltip: raw, exactUtc: raw, local: raw };
        }
        const months = ['Jan','Feb','Mar','Apr','May','Jun','Jul','Aug','Sep','Oct','Nov','Dec'];
        const uYear = d.getUTCFullYear();
        const uMonth = months[d.getUTCMonth()];
        const uDate = String(d.getUTCDate()).padStart(2, '0');
        const uHours = String(d.getUTCHours()).padStart(2, '0');
        const uMins = String(d.getUTCMinutes()).padStart(2, '0');
        const uSecs = String(d.getUTCSeconds()).padStart(2, '0');
        const displayUtc = `${uMonth} ${uDate}, ${uYear} · ${uHours}:${uMins} UTC`;
        const exactUtc = `${uYear}-${String(d.getUTCMonth() + 1).padStart(2, '0')}-${uDate} ${uHours}:${uMins}:${uSecs} UTC`;

        const lYear = d.getFullYear();
        const lMonth = months[d.getMonth()];
        const lDate = String(d.getDate()).padStart(2, '0');
        const lHours = String(d.getHours()).padStart(2, '0');
        const lMins = String(d.getMinutes()).padStart(2, '0');
        const lSecs = String(d.getSeconds()).padStart(2, '0');
        const localDisplay = `${lMonth} ${lDate}, ${lYear} · ${lHours}:${lMins}:${lSecs}`;

        const diffSec = Math.max(0, Math.floor((Date.now() - d.getTime()) / 1000));
        let relative = '';
        if (diffSec < 45) relative = 'Just now';
        else if (diffSec < 3600) relative = `${Math.floor(diffSec / 60)}m ago`;
        else if (diffSec < 86400) relative = `${Math.floor(diffSec / 3600)}h ago`;
        else if (diffSec < 604800) relative = `${Math.floor(diffSec / 86400)}d ago`;
        else relative = `${uMonth} ${uDate}`;

        return { display: displayUtc, relative: relative, tooltip: `UTC: ${exactUtc} | Local: ${localDisplay}`, exactUtc: exactUtc, local: localDisplay };
    };

    dashboard.getAdminTheme = getAdminTheme;
    dashboard.setAdminTheme = setAdminTheme;
    dashboard.toggleAdminTheme = toggleAdminTheme;
    dashboard.openCommandPalette = openCommandPalette;
    dashboard.closeCommandPalette = closeCommandPalette;
    dashboard.filterCommandPalette = filterCommandPalette;
    dashboard.filterDocs = filterDocs;
    dashboard.setAdminRange = setAdminRange;
    dashboard.getAdminRange = getAdminRange;
    dashboard.pollAdminHeartbeat = pollAdminHeartbeat;
    dashboard.startAdminHeartbeat = startAdminHeartbeat;
    dashboard.prefersReducedMotion = prefersReducedMotion;

    if (hasWindow) {
        root.adminDashboard = dashboard;

        /* Globals for inline onclick/oninput handlers rendered by the shell. */
        root.toggleAdminTheme = toggleAdminTheme;
        root.openCommandPalette = openCommandPalette;
        root.closeCommandPalette = closeCommandPalette;
        root.filterCommandPalette = filterCommandPalette;
        root.filterDocs = filterDocs;
        root.setAdminRange = setAdminRange;
    }

    /* ---------- Auto-init (all hooks optional; never throws) ---------- */

    try {
        initAdminTheme();
        bindAdminShortcuts();
        bindDocsSearchOnce();
        if (hasDom() && typeof document.addEventListener === 'function') {
            if (document.readyState === 'loading') {
                document.addEventListener('DOMContentLoaded', function () {
                    try {
                        bindDocsSearchOnce();
                        startAdminHeartbeat();
                    } catch (err) { /* init is best-effort */ }
                });
            } else {
                startAdminHeartbeat();
            }
        } else {
            startAdminHeartbeat();
        }
    } catch (err) { /* module init must never break host page */ }
})();
