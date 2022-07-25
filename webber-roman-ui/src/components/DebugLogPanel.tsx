import { useDebugBlock } from "../blocks/DebugBlock";

export function DebugLogPanel(props: React.HTMLAttributes<HTMLDivElement>): JSX.Element {
    const { logs } = useDebugBlock();
    return <div {...props}>
        {logs.map(s => <p>{s}</p>)}
    </div>
}
