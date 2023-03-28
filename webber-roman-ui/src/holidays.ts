import { DateTime } from "luxon";

export interface Holiday {
    next: DateTime;
    description: string;
    color: string;
    priorityDays: number;
    interestDays: number;
}

function annual(day: number, month: number, description: string, priorityDays: number, interestDays: number, color?: string): (from: DateTime) => Holiday {
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
        };
    };
}

function anniversary(day: number, month: number, year: number, description: string, priorityDays: number, interestDays: number, color?: string): (from: DateTime) => Holiday {
    return function (from: DateTime): Holiday {
        const h = annual(day, month, description, priorityDays, interestDays, color)(from);
        h.description = `${h.description} (${h.next.year - year} yr)`;
        return h;
    };
}

export const holidays: ((from: DateTime) => Holiday)[] = [
    annual(8, 3, "8 марта", 30, 90),
    annual(31, 12, "Новый Год", 30, 90),
];
