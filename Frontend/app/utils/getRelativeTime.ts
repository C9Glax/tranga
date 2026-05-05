// Original work: https://stackoverflow.com/questions/6108819/javascript-timestamp-to-relative-time

// in miliseconds
const units: Record<string, number> = {
    year: 24 * 60 * 60 * 1000 * 365,
    month: (24 * 60 * 60 * 1000 * 365) / 12,
    day: 24 * 60 * 60 * 1000,
    hour: 60 * 60 * 1000,
    minute: 60 * 1000,
    second: 1000,
};

const rtf = new Intl.RelativeTimeFormat('en', { numeric: 'auto' });

/**
 * Returns the relative time string (like 'in 2 days')
 * @param d1 relative Date
 * @param d2 optional second Date (or now)
 */
export const getRelativeTime = (d1: Date, d2 = new Date()) => {
    const elapsed: number = d1.getTime() - d2.getTime();

    // "Math.abs" accounts for both "past" & "future" scenarios
    for (const u in units) {
        const unit = units[u]!;
        if (Math.abs(elapsed) > unit || u == 'second') {
            // @ts-ignore u is RelativeTimeFormatUnit
            return rtf.format(Math.round(elapsed / unit), u);
        }
    }
};
