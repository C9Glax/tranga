/**
 * Parses the milliseconds from a timespan
 * @returns time in ticks or undefined if in wrong format
 * @param timespan The time span in [-][d.]hh:mm:ss[.fffffff] format (https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-timespan-format-strings#the-constant-c-format-specifier)
 */
export const parseTimespan = (timespan: string): number | undefined => {
    const result = timespan.match(regexp);
    if (!result || !result.groups) return undefined;
    let val = 0;
    if (result.groups['days']) val += parseInt(result.groups['days']) * 1000 * 60 * 60 * 24;
    if (!result.groups['hours']) return undefined;
    val += parseInt(result.groups['hours']) * 1000 * 60 * 60;
    if (!result.groups['minutes']) return undefined;
    val += parseInt(result.groups['minutes']) * 1000 * 60;
    if (!result.groups['seconds']) return undefined;
    val += parseInt(result.groups['seconds']) * 1000;
    if (result.groups['ticks']) val += parseInt(result.groups['ticks'].slice(0, 3));
    return val;
};

const regexp = new RegExp('-?(?<days>\d\.)?(?<hours>[0-9]{2}):(?<minutes>[0-9]{2}):(?<seconds>[0-9]{2})(?<ticks>\.[0-9]{7})?');
