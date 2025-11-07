import { DateTime } from "luxon";
import styled from "styled-components";
import { HolidayInstance, holidays } from "../holidays";
import { useTime } from "../util/useTime";
import { startOfLocalDay } from "../util/util";
import { BirthdaysOverlay, useBirthdaysOverlayState } from "./BirthdaysOverlay";

interface Holiday2 extends HolidayInstance {
    daysUntil: number; // 0 = today, -1 = yesterday
}

const HolidaysDiv = styled.div`
    display: grid;
    grid-template-columns: max-content max-content max-content auto;
    grid-gap: 0 1rem;
`;

const Ldiv = styled.div<{ c?: string, fw: number }>`
    ${p => !!p.c && `color: ${p.c};`}
    font-weight: ${p => p.fw};
`;
const Rdiv = styled(Ldiv)`
    justify-self: end;
`;

function HolidayRow(p: { holiday: Holiday2 }): React.ReactNode {
    const h = p.holiday;
    //const leftDiv = <div style={{ justifySelf: "end" }}>{h.daysUntil}</div>;
    //const leftDiv = <div style={{ justifySelf: "end" }}>{h.daysUntil <= 21 ? `${h.daysUntil}d` : h.daysUntil <= 60 ? `${Math.floor(h.daysUntil / 7).toFixed(0)}w` : `${Math.floor(h.daysUntil / 30.5).toFixed(0)}m`}</div>;
    const leftDiv =
        h.daysUntil <= 3 ? <Rdiv fw={700} c="#ff0">{h.daysUntil}d</Rdiv> :
            h.daysUntil <= 21 ? <Rdiv fw={300} c="#ff0">{h.daysUntil}d</Rdiv> :
                h.daysUntil <= 30 ? <Rdiv fw={300}>{Math.floor(h.daysUntil / 7).toFixed(0)}w</Rdiv> :
                    h.daysUntil <= 60 ? <Rdiv fw={300} c="#666">{Math.floor(h.daysUntil / 7).toFixed(0)}w</Rdiv> : <Rdiv fw={100} c="#666">{Math.floor(h.daysUntil / 30.5).toFixed(0)}m</Rdiv>;
    let fw = 300;
    let clr = h.holiday.color;
    if (h.daysUntil > h.holiday.priorityDays) {
        fw = 100;
        clr = "#666";
    }
    return <>
        <Rdiv fw={fw} c={clr}>{h.date.setLocale("en-US").toFormat("d")}</Rdiv>
        <Ldiv fw={fw} c={clr} style={{ marginLeft: "-0.65rem" }}>{h.date.setLocale("en-US").toFormat("MMM")}</Ldiv>
        {leftDiv}
        <Ldiv fw={fw} c={clr}>{h.description}</Ldiv>
    </>;
}

export function HolidaysPanel({ ...rest }: React.HTMLAttributes<HTMLDivElement>): React.ReactNode {
    useTime(); // refresh every minute - a little much but not worth fixing
    const overlayState = useBirthdaysOverlayState();

    const startOfToday = startOfLocalDay(DateTime.utc(), true);
    const from = DateTime.utc().plus({ days: -30 });
    const hols = holidays.map(h => h.next(from)).filter(h => !!h).sort((a, b) => a.date.toMillis() - b.date.toMillis())
        .map(h => ({ ...h, daysUntil: Math.ceil(h.date.diff(startOfToday, "days").days) }));

    return <>
        <HolidaysDiv {...rest} onClick={overlayState.show}>
            {hols.filter(h => h.daysUntil >= -h.holiday.pastDays && h.daysUntil <= h.holiday.interestDays).map(h => <HolidayRow key={h.description} holiday={h} />)}
        </HolidaysDiv>
        <BirthdaysOverlay state={overlayState} />
    </>;
}
