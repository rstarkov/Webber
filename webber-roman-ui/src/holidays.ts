import { DateTime } from "luxon";

export interface HolidayInstance {
    date: DateTime;
    description: string;
    holiday: Holiday;
}

export interface Holiday {
    next: (from: DateTime) => HolidayInstance | null;
    annual?: { day: number; month: number };
    year?: number;
    description: string;
    color: string;
    priorityDays: number; // not used at the moment
    interestDays: number;
    pastDays: number;
}

type NextFunc = Holiday["next"];

function annual(day: number, month: number, description: string, priorityDays: number, interestDays: number, color?: string): Holiday {
    const holiday = {
        next: (() => null) as NextFunc,
        annual: { day, month },
        description,
        color: color ?? "",
        priorityDays,
        interestDays,
        pastDays: 0,
    };
    holiday.next = (from) => {
        let date = DateTime.fromObject({ year: from.year, month, day });
        if (date.toMillis() < from.toMillis())
            date = DateTime.fromObject({ year: from.year + 1, month, day });
        return { date, description, holiday };
    };
    return holiday;
}

function anniversary(day: number, month: number, year: number, description: string, priorityDays: number, interestDays: number, color?: string): Holiday {
    const holiday = annual(day, month, description, priorityDays, interestDays, color);
    holiday.pastDays = 1;
    holiday.year = year;
    const next = holiday.next;
    holiday.next = (from) => {
        const h = next(from);
        if (!h) return null;
        h.description = `${h.description} (${h.date.year - year} yr)`;
        return h;
    };
    return holiday;
}

function list(description: string, priorityDays: number, interestDays: number, dates: string[], color?: string): Holiday {
    const holiday = {
        next: (() => null) as NextFunc,
        description,
        color: color ?? "",
        interestDays,
        priorityDays,
        pastDays: 0,
    };
    const pdates = dates.map(d => DateTime.fromFormat(d, "dd/MM/yyyy"));
    holiday.next = (from) => {
        const nexts = pdates.filter(pd => pd.diff(from, "days").days >= 0);
        if (nexts.length === 0)
            return null;
        return { date: nexts[0], description, holiday };
    }
    return holiday;
}

export const holidays: Holiday[] = [
    annual(8, 3, "8 марта", 30, 90),
    annual(25, 12, "Christmas", 30, 90),
    annual(31, 12, "Новый Год", 30, 90),

    list("Good Friday", 15, 40, ["18/04/2025", "03/04/2026", "26/03/2027", "14/04/2028", "30/03/2029", "19/04/2030"]),
    list("Easter Monday", 15, 40, ["21/04/2025", "06/04/2026", "29/03/2027", "17/04/2028", "02/04/2029", "22/04/2030"]),
    list("Early May BH", 15, 40, ["05/05/2025", "04/05/2026", "03/05/2027", "01/05/2028", "07/05/2029", "06/05/2030"]),
    list("Spring BH", 15, 40, ["26/05/2025", "25/05/2026", "31/05/2027", "29/05/2028", "28/05/2029", "27/05/2030"]),
    list("Summer BH", 15, 40, ["25/08/2025", "31/08/2026", "30/08/2027", "28/08/2028", "27/08/2029", "26/08/2030"]),
];
