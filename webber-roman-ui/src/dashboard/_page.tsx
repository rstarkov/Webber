import { DateTime } from "luxon";
import styled from "styled-components";
import { usePingBlock } from '../blocks/PingBlock';
import { useWeatherBlock } from '../blocks/WeatherBlock';
import { RemilkPanel } from "./RemilkPanel";
import { TimeUntilPanel } from "./TimeUntilPanel";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faSun, faMoon } from '@fortawesome/free-solid-svg-icons';

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

const ZonesClockDiv = styled.div`
    display: grid;
    grid-auto-flow: column;
    grid-template-rows: 1fr 1fr;
    justify-items: center;
`;
const ZoneName = styled.div`
    color: #777;
`;
const ZoneTime = styled.div`
`;

function ZonesClock(props: React.HTMLAttributes<HTMLDivElement>): JSX.Element {
    return <ZonesClockDiv {...props}>
        <ZoneName>UTC</ZoneName><ZoneTime>{DateTime.utc().toFormat('HH:mm')}</ZoneTime>
        <ZoneName>Ukr</ZoneName><ZoneTime>{DateTime.utc().setZone('Europe/Kiev').toFormat('HH:mm')}</ZoneTime>
        <ZoneName>Can</ZoneName><ZoneTime>{DateTime.utc().setZone('Canada/Mountain').toFormat('HH:mm')}</ZoneTime>
    </ZonesClockDiv>;
}

const MainClockDiv = styled.div`
    font-size: 350%;
    font-weight: bold;
`;

function MainClock(props: React.HTMLAttributes<HTMLDivElement>): JSX.Element {
    return <MainClockDiv {...props}>
        {DateTime.local().toFormat('HH:mm')}
    </MainClockDiv>;
}

const BigTemperature = styled.div`
    font-size: 350%;
    font-weight: bold;
`;

const Degrees = styled.span`
    font-size: 70%;
    position: relative;
    top: -2.0vw;
    margin-left: 1.0vw;
`;

const SunTimesDiv = styled.div`
    display: grid;
    justify-items: right;
    opacity: 0.7;
`;

function SunTimes(props: React.HTMLAttributes<HTMLDivElement>): JSX.Element {
    const weather = useWeatherBlock();
    return <SunTimesDiv {...props}>
        <div><FontAwesomeIcon icon={faSun} color='#ff0' /> {weather.dto?.sunriseTime}</div>
        <div><FontAwesomeIcon icon={faMoon} color='#4479ff' /> {weather.dto?.sunsetTime}</div>
        <div style={{fontSize: '80%', color: '#999'}}>{weather.dto?.sunsetDeltaTime}</div>
    </SunTimesDiv>
}

export function DashboardPage(): JSX.Element {
    const weather = useWeatherBlock();

    return (
        <>
            <BigTemperature style={{ position: 'absolute', left: '0vw', top: '-1vw', color: weather.dto?.curTemperatureColor }}>{weather.dto?.curTemperature.toFixed(0)}<Degrees>°C</Degrees></BigTemperature>
            <SunTimes style={{ position: 'absolute', left: '25vw', top: '1vw' }} />
            <MainClock style={{ position: 'absolute', left: '41vw', top: '-1vw' }} onClick={() => document.body.requestFullscreen()} />
            <ZonesClock style={{ position: 'absolute', left: '39vw', top: '12vw', width: '30vw' }} />
            {/* <PingText />
            <button >FS</button> */}
            <TimeUntilPanel style={{ position: 'absolute', left: '0vw', top: '50vh', width: '30vw', bottom: '0vh', overflow: 'hidden' }} />
            <RemilkPanel style={{ position: 'absolute', right: 0, top: 0, width: '30vw', borderLeft: '0.5vw solid #888', paddingLeft: '2vw' }} />
        </>
    )
}
