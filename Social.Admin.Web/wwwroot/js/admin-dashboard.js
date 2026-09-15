/**
 * DotNetCoreSocialApi - Admin Dashboard Client Interactivity
 */

window.adminDashboard = {
    copyToClipboard: async function (text) {
        if (navigator.clipboard) {
            try {
                await navigator.clipboard.writeText(text);
                return true;
            } catch (err) {
                console.error("Failed to copy text: ", err);
            }
        }
        return false;
    },

    scrollToTop: function () {
        window.scrollTo({ top: 0, behavior: 'smooth' });
    },

    formatStandardDate: function (isoDateString) {
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
    }
};
