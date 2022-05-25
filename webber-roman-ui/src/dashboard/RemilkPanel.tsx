import { DateTime } from "luxon";
import { RemilkTask, useRemilkBlock } from "../blocks/RemilkBlock";
import { startOfLocalDay } from "../util/util";
import { BlockPanelContainer } from "./Container";

function TaskToday(p: { task: RemilkTask }): JSX.Element {
    const overdue = p.task.dueUtc < DateTime.utc();
    return <p key={p.task.id}>{overdue && "OVERDUE: "}{p.task.description}</p>
}

export function RemilkPanel({ ...rest }: React.HTMLAttributes<HTMLDivElement>): JSX.Element {
    const remilk = useRemilkBlock();
    const tasks = remilk.dto?.tasks ?? [];

    const cutoffNeglected = startOfLocalDay(DateTime.utc(), true).minus({ day: 5 });
    const cutoffStartOfToday = startOfLocalDay(DateTime.utc(), true);
    const cutoffEndOfToday = cutoffStartOfToday.plus({ day: 1 });
    const cutoffTomorrow = cutoffStartOfToday.plus({ day: 2 });
    const cutoffSoon = cutoffStartOfToday.plus({ day: 5 });

    const tasksNeglected = tasks.filter(t => t.dueUtc <= cutoffNeglected);
    const tasksOverdue = tasks.filter(t => t.dueUtc > cutoffNeglected && t.dueUtc <= cutoffStartOfToday);
    const tasksToday = tasks.filter(t => t.dueUtc > cutoffStartOfToday && t.dueUtc <= cutoffEndOfToday);
    const tasksTomorrow = tasks.filter(t => t.dueUtc > cutoffEndOfToday && t.dueUtc <= cutoffTomorrow);
    const tasksSoon = tasks.filter(t => t.dueUtc > cutoffTomorrow && t.dueUtc <= cutoffSoon);

    return <BlockPanelContainer {...rest}>
        {tasksNeglected && <>
            <h3>Neglected</h3>
            {tasksNeglected.map(t => <p key={t.id}>{t.description}</p>)}
        </>}
        {
            false && tasksOverdue && <>
                <h3>Overdue</h3>
                {tasksOverdue.map(t => <p key={t.id}>{t.description}</p>)}
            </>
        }
        {
            tasksToday && tasksToday.length > 0 && <>
                <h3>Today</h3>
                {tasksToday.map(t => <TaskToday task={t} />)}
            </>
        }
        {
            tasksTomorrow && tasksTomorrow.length > 0 && <>
                <h3>Tomorrow</h3>
                {tasksTomorrow.map(t => <p key={t.id}>{t.description}</p>)}
            </>
        }
        {
            tasksSoon && tasksSoon.length > 0 && <>
                <h3>Soon</h3>
                {tasksSoon.map(t => <p key={t.id}>{t.description}</p>)}
            </>
        }
    </BlockPanelContainer >;
}
