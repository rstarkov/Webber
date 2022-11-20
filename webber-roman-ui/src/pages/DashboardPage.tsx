import { RemilkPanel } from "../components/RemilkPanel";
import { TimeUntilPanel } from "../components/TimeUntilPanel";
import { NavOverlay, useNavOverlayState } from "../components/NavOverlay";

export function DashboardPage(): JSX.Element {
    const overlay = useNavOverlayState();

    return (
        <>
            <TimeUntilPanel style={{ position: "absolute", left: "0vw", top: "0vh", width: "55vw", height: "100vh", overflow: "hidden" }} onClick={overlay.show} />
            <RemilkPanel style={{ position: "absolute", right: 0, top: 0, width: "42vw", height: "100vh" }} />

            <NavOverlay state={overlay} />
        </>
    );
}
