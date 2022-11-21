import { NavOverlay, useNavOverlayState } from "../components/NavOverlay";
import { PingPanel } from "../components/PingPanel";
import { RouterPanel } from "../components/RouterPanel";
import { TimePanel } from "../components/TimePanel";
import { WeatherPanel } from "../components/WeatherPanel";

export function ClassicPage(): JSX.Element {
    const overlay = useNavOverlayState();
    return (
        <>
            <WeatherPanel style={{ position: "absolute", top: "0vw", left: "0vw", width: "37vw", height: "26vw" }} />
            <TimePanel style={{ position: "absolute", top: "0vw", left: "39vw", width: "25vw", height: "26vw" }} onClick={overlay.show} />
            <PingPanel style={{ position: "absolute", top: "0vw", right: "0", width: "34vw", height: "26vw" }} />

            {/* <WeatherBox style={{ position: 'absolute', top: '28vw', left: '0vw', width: '45vw', bottom: '0vw' }} />
            <RouterBox style={{ position: 'absolute', top: '28vw', left: '47vw', right: '0vw', bottom: '0vw' }} /> */}
            <RouterPanel style={{ position: "absolute", top: "28vw", left: "0vw", right: "0vw", bottom: "0vw" }} />

            <NavOverlay state={overlay} />
        </>
    )
}
