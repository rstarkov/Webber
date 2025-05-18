import { DateTime } from "luxon";

export interface Holiday {
    next: DateTime;
    description: string;
    color: string;
    priorityDays: number; // not used at the moment
    interestDays: number;
    pastDays: number;
}

type HolidayFunc = (from: DateTime) => Holiday | null;

function annual(day: number, month: number, description: string, priorityDays: number, interestDays: number, color?: string) {
    return function (from: DateTime): Holiday {
        let d = DateTime.fromObject({ year: from.year, month, day });
        if (d.toMillis() < from.toMillis())
            d = DateTime.fromObject({ year: from.year + 1, month, day });
        return {
            next: d,
            description,
            color: color ?? "",
            interestDays,
            priorityDays,
            pastDays: 0,
        };
    };
}

function anniversary(day: number, month: number, year: number, description: string, priorityDays: number, interestDays: number, color?: string) {
    return function (from: DateTime): Holiday {
        const h = annual(day, month, description, priorityDays, interestDays, color)(from);
        h.description = `${h.description} (${h.next.year - year} yr)`;
        h.pastDays = 1;
        return h;
    };
}

function list(description: string, priorityDays: number, interestDays: number, dates: string[], color?: string): HolidayFunc {
    const pdates = dates.map(d => DateTime.fromFormat(d, "dd/MM/yyyy"));
    return function (from: DateTime): Holiday | null {
        const nexts = pdates.filter(pd => pd.diff(from, "days").days >= 0);
        if (nexts.length === 0)
            return null;
        return {
            next: nexts[0],
            description,
            color: color ?? "",
            interestDays,
            priorityDays,
            pastDays: 0,
        };
    }
}

export const holidays: HolidayFunc[] = [
    annual(8, 3, "8 марта", 30, 90),
    annual(25, 12, "Christmas", 30, 90),
    annual(31, 12, "Новый Год", 30, 90),

    list("Good Friday", 15, 40, ["18/04/2025", "03/04/2026", "26/03/2027", "14/04/2028", "30/03/2029", "19/04/2030"]),
    list("Easter Monday", 15, 40, ["21/04/2025", "06/04/2026", "29/03/2027", "17/04/2028", "02/04/2029", "22/04/2030"]),
    list("Early May BH", 15, 40, ["05/05/2025", "04/05/2026", "03/05/2027", "01/05/2028", "07/05/2029", "06/05/2030"]),
    list("Spring BH", 15, 40, ["26/05/2025", "25/05/2026", "31/05/2027", "29/05/2028", "28/05/2029", "27/05/2030"]),
    list("Summer BH", 15, 40, ["25/08/2025", "31/08/2026", "30/08/2027", "28/08/2028", "27/08/2029", "26/08/2030"]),
];
