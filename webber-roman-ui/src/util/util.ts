import { DateTime } from "luxon";

export function startOfLocalDay(d: DateTime, hasTime: boolean): DateTime {
    // we consider a day to last from 3am to 3am next day, so that what's due today or tomorrow doesn't change right after midnight
    d = d.toLocal();
    var result = DateTime.fromObject({ year: d.year, month: d.month, day: d.day, hour: 3 }, { zone: 'local' });
    if (hasTime && d.hour < 3)
        result = result.minus({ day: 1 }); // this should be 03:00 local time even if there's DST shifts etc
    return result;
}

export function endOfLocalDay(d: DateTime, hasTime: boolean): DateTime {
    return startOfLocalDay(d, hasTime).plus({ day: 1 }); // this should be 03:00 local time even if there's DST shifts etc
}