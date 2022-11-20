import { NavOverlay, useNavOverlayState } from "../components/NavOverlay";
import { PingBox } from "../components/PingBox";
import { RouterBox } from "../components/RouterBox";
import { TimeBox } from "../components/TimeBox";
import { WeatherBox } from "../components/WeatherBox";

export function ClassicPage(): JSX.Element {
    const overlay = useNavOverlayState();
    return (
        <>
            <WeatherBox style={{ position: "absolute", top: "0vw", left: "0vw", width: "37vw", height: "26vw" }} />
            <TimeBox style={{ position: "absolute", top: "0vw", left: "39vw", width: "25vw", height: "26vw" }} onClick={overlay.show} />
            <PingBox style={{ position: "absolute", top: "0vw", right: "0", width: "34vw", height: "26vw" }} />

            {/* <WeatherBox style={{ position: 'absolute', top: '28vw', left: '0vw', width: '45vw', bottom: '0vw' }} />
            <RouterBox style={{ position: 'absolute', top: '28vw', left: '47vw', right: '0vw', bottom: '0vw' }} /> */}
            <RouterBox style={{ position: "absolute", top: "28vw", left: "0vw", right: "0vw", bottom: "0vw" }} />

            <NavOverlay state={overlay} />
        </>
    )
}
