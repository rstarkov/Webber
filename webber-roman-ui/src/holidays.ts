import { DateTime } from "luxon";

export interface Holiday {
    next: DateTime;
    description: string;
    color: string;
    priorityDays: number; // not used at the moment
    interestDays: number;
    pastDays: number;
}

type HolidayFunc = (from: DateTime) => Holiday;

function annual(day: number, month: number, description: string, priorityDays: number, interestDays: number, color?: string): HolidayFunc {
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

function anniversary(day: number, month: number, year: number, description: string, priorityDays: number, interestDays: number, color?: string): HolidayFunc {
    return function (from: DateTime): Holiday {
        const h = annual(day, month, description, priorityDays, interestDays, color)(from);
        h.description = `${h.description} (${h.next.year - year} yr)`;
        h.pastDays = 1;
        return h;
    };
}

function list(description: string, priorityDays: number, interestDays: number, dates: string[], color?: string): HolidayFunc {
    const pdates = dates.map(d => DateTime.fromFormat(d, "dd/MM/yyyy"));
    return function (from: DateTime): Holiday {
        return {
            next: pdates.filter(pd => pd.diff(from, "days").days >= 0)[0],
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

    list("Good Friday", 15, 40, ["07/04/2023", "29/03/2024", "18/04/2025"]),
    list("Easter Monday", 15, 40, ["10/04/2023", "01/04/2024", "21/04/2025"]),
    list("Early May BH", 15, 40, ["01/05/2023", "06/05/2024", "05/05/2025"]),
    list("Spring BH", 15, 40, ["29/05/2023", "27/05/2024", "26/05/2025"]),
    list("Summer BH", 15, 40, ["28/08/2023", "26/08/2024", "25/08/2025"]),
];
