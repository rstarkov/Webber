import { DateTime } from "luxon";
import { usePingBlock } from '../blocks/PingBlock';
import { useWeatherBlock } from '../blocks/WeatherBlock';
import { RemilkPanel } from "./RemilkPanel";
import { TimeUntilPanel } from "./TimeUntilPanel";

function PingText(): JSX.Element {
    const ping = usePingBlock();
    return (
        <p>PING: {ping.dto?.last}</p>
    );
}


function PingHistory(): JSX.Element {
    const ping = usePingBlock();
    return (
        <p>PING: {JSON.stringify(ping.dto?.recent)}</p>
    );
}

export function ClockPanel(): JSX.Element {
    return <div>
        <p>{DateTime.local().toFormat('HH:mm')}</p>
        <p>UTC: {DateTime.utc().toFormat('HH:mm')}</p>
        <p>Ukr: {DateTime.utc().setZone('Europe/Kiev').toFormat('HH:mm')}</p>
        <p>Can: {DateTime.utc().setZone('Canada/Mountain').toFormat('HH:mm')}</p>
    </div>;
}

export function DashboardPage(): JSX.Element {
    const weather = useWeatherBlock();

    return (
        <>
            <b>{weather.dto?.curTemperature.toFixed(0)} °C</b>
            <ClockPanel />
            <PingText />
            <button onClick={() => document.body.requestFullscreen()}>FS</button>
            <TimeUntilPanel style={{ position: 'absolute', left: '10vw', top: '50vh', width: '40vw' }} />
            <RemilkPanel style={{ position: 'absolute', right: 0, top: 0, width: '30vw', borderLeft: '0.5vw solid #888', paddingLeft: '2vw' }} />
        </>
    )
}
