import { DateTime } from "luxon";
import styled from "styled-components";
import { RemilkTask, useRemilkBlock } from "../blocks/RemilkBlock";
import { startOfLocalDay } from "../util/util";
import { BlockPanelContainer } from "./Container";

function TaskToday(p: { task: RemilkTask }): JSX.Element {
    const overdue = p.task.dueUtc < DateTime.utc();
    return <p key={p.task.id}>{overdue && "OVERDUE: "}{p.task.description}</p>
}

function byPriority(a: RemilkTask, b: RemilkTask): number {
    if (a.priority != b.priority)
        return a.priority - b.priority;
    if (a.description != b.description)
        return a.description.localeCompare(b.description);
    return a.id.localeCompare(b.id);
}
function byDueDate(a: RemilkTask, b: RemilkTask): number {
    const c = a.dueUtc.toMillis() - b.dueUtc.toMillis();
    if (c != 0)
        return c;
    return a.id.localeCompare(b.id);
}

const PrioColors = ['', '#EA5200', '#0060BF', '#359AFF', '#555'];
const TaskDiv = styled.div`
    font-size:90%;
    padding-left: 0.3rem;
    line-height: 1.0;
    margin: 0.3rem 0;
    border-left: 0.2rem solid yellow;
`;

function Task(p: { task: RemilkTask }): JSX.Element {
    return <TaskDiv style={{ borderLeftColor: PrioColors[p.task.priority] }}>{p.task.description}</TaskDiv>;
}

export function RemilkPanel({ ...rest }: React.HTMLAttributes<HTMLDivElement>): JSX.Element {
    const remilk = useRemilkBlock();
    const tasks = remilk.dto?.tasks ?? [];

    const cutoffNeglected = startOfLocalDay(DateTime.utc(), true).minus({ day: 5 });
    const cutoffStartOfToday = startOfLocalDay(DateTime.utc(), true);
    const cutoffEndOfToday = cutoffStartOfToday.plus({ day: 1 });
    const cutoffTomorrow = cutoffStartOfToday.plus({ day: 2 });
    const cutoffSoon = cutoffStartOfToday.plus({ day: 5 });

    const tasksNeglected = tasks.filter(t => t.dueUtc <= cutoffNeglected).sort(byPriority);
    const tasksOverdue = tasks.filter(t => t.dueUtc > cutoffNeglected && t.dueUtc <= cutoffStartOfToday).sort(byPriority);
    const tasksToday = tasks.filter(t => t.dueUtc > cutoffStartOfToday && t.dueUtc <= cutoffEndOfToday).sort(byPriority);
    const tasksTomorrow = tasks.filter(t => t.dueUtc > cutoffEndOfToday && t.dueUtc <= cutoffTomorrow).sort(byPriority);
    const tasksSoon = tasks.filter(t => t.dueUtc > cutoffTomorrow && t.dueUtc <= cutoffSoon).sort(byDueDate);

    const showNeglected = tasksNeglected && tasksNeglected.length > 0;
    const showOverdue = tasksOverdue && tasksOverdue.length > 0;

    return <BlockPanelContainer {...rest}>
        {showNeglected && <>
            <h3>Neglected</h3>
            {tasksNeglected.map(t => <Task key={t.id} task={t} />)}
        </>}
        {showOverdue && <>
            <h3>Overdue</h3>
            {tasksOverdue.map(t => <Task key={t.id} task={t} />)}
        </>}
        {tasksToday && tasksToday.length > 0 && <>
            {(showNeglected || showOverdue) && <h3>Today</h3>}
            {tasksToday.map(t => <Task key={t.id} task={t} />)}
        </>}
        <div style={{ opacity: 0.3 }}>
            {tasksTomorrow && tasksTomorrow.length > 0 && <>
                <h3>Tomorrow</h3>
                {tasksTomorrow.map(t => <Task key={t.id} task={t} />)}
            </>}
            {tasksSoon && tasksSoon.length > 0 && <>
                <h3>Soon</h3>
                {tasksSoon.map(t => <Task key={t.id} task={t} />)}
            </>}
        </div>
    </BlockPanelContainer >;
}
