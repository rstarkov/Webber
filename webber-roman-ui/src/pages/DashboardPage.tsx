import { RemilkPanel } from "../components/RemilkPanel";
import { TimeUntilPanel } from "../components/TimeUntilPanel";
import { NavOverlay, useNavOverlayState } from "../components/NavOverlay";
import { HolidaysPanel } from "../components/HolidaysPanel";

export function DashboardPage(): JSX.Element {
    const overlay = useNavOverlayState();

    return (
        <>
            <div style={{ position: "absolute", left: "0vw", top: "0vh", width: "55vw", height: "100vh", overflow: "hidden" }}>
                <TimeUntilPanel style={{ position: "relative", width: "100%", overflow: "hidden", paddingBottom: "2rem" }} onClick={overlay.show} />
                <HolidaysPanel style={{ position: "relative", width: "100%", overflow: "hidden" }} />
            </div>
            <RemilkPanel style={{ position: "absolute", right: 0, top: 0, width: "42vw", height: "100vh" }} />

            <NavOverlay state={overlay} />
        </>
    );
}
