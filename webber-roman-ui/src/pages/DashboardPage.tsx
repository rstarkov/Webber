import { RemilkPanel } from "../components/RemilkPanel";
import { TimeUntilPanel } from "../components/TimeUntilPanel";
import { useDebugBlock } from "../blocks/DebugBlock";
import { NavOverlay, useNavOverlayState } from "../components/NavOverlay";


function DebugLog(props: React.HTMLAttributes<HTMLDivElement>): JSX.Element {
    const { logs } = useDebugBlock();
    return <div {...props}>
        {logs.map(s => <p>{s}</p>)}
    </div>
}

export function DashboardPage(): JSX.Element {
    const overlay = useNavOverlayState();

    return (
        <>
            <TimeUntilPanel style={{ position: 'absolute', left: '0vw', top: '0vh', width: '55vw', height: '100vh', overflow: 'hidden' }} onClick={overlay.show} />
            <RemilkPanel style={{ position: 'absolute', right: 0, top: 0, width: '42vw', height: '100vh' }} />

            <NavOverlay state={overlay} />
        </>
    );
}
