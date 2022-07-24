import { DateTime } from "luxon";
import styled from "styled-components";
import { useWeatherBlock } from '../blocks/WeatherBlock';
import { RemilkPanel } from "./RemilkPanel";
import { TimeUntilPanel } from "./TimeUntilPanel";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faSun, faMoon } from '@fortawesome/free-solid-svg-icons';
import { BlockPanelContainer } from "./Container";
import { useDebugBlock } from "../blocks/DebugBlock";
import { NavOverlay, useNavOverlayState } from "../components/NavOverlay";


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
        <ZoneName>Ukr</ZoneName><ZoneTime>{DateTime.utc().setZone('Europe/Kiev').toFormat('HH:mm')}</ZoneTime>
        <ZoneName style={{ fontWeight: 'bold' }}>UTC</ZoneName><ZoneTime style={{ fontWeight: 'bold' }}>{DateTime.utc().toFormat('HH:mm')}</ZoneTime>
        <ZoneName>Can</ZoneName><ZoneTime>{DateTime.utc().setZone('Canada/Mountain').toFormat('HH:mm')}</ZoneTime>
    </ZonesClockDiv>;
}

const MainClockDiv = styled.div`
    font-size: 350%;
    font-weight: bold;
    text-align: center;
`;

function MainClock(props: React.HTMLAttributes<HTMLDivElement>): JSX.Element {
    return <MainClockDiv {...props}>
        {DateTime.local().toFormat('HH:mm')}
    </MainClockDiv>;
}

const RecentTemperaturesDiv = styled(BlockPanelContainer)`
    display: grid;
    grid-template-columns: 1fr repeat(4, min-content) 1fr;
`;
const RecentMinMaxDiv = styled.div`
    padding-left: 1.3vw;
    color: #777;
`;
function RecentTemperatures(props: React.HTMLAttributes<HTMLDivElement>): JSX.Element {
    const weather = useWeatherBlock();
    if (!weather.dto)
        return <RecentTemperaturesDiv state={weather} {...props} />
    function temp2str(temp: number): string {
        return (temp < 0 ? "–" : "") + Math.abs(temp).toFixed(0);
    }
    return <RecentTemperaturesDiv state={weather} {...props}>
        <div></div>
        <div style={{ color: weather.dto.minTemperatureColor, textAlign: 'right' }}>{temp2str(weather.dto.minTemperature)} °C</div>
        <RecentMinMaxDiv style={{ paddingRight: '1.3vw' }}>at</RecentMinMaxDiv>
        <div>{weather.dto.minTemperatureAtTime}</div>
        <RecentMinMaxDiv>{weather.dto.minTemperatureAtDay}</RecentMinMaxDiv>
        <div></div>

        <div></div>
        <div style={{ color: weather.dto.maxTemperatureColor, textAlign: 'right' }}>{temp2str(weather.dto.maxTemperature)} °C</div>
        <RecentMinMaxDiv style={{ paddingRight: '1.3vw' }}>at</RecentMinMaxDiv>
        <div>{weather.dto.maxTemperatureAtTime}</div>
        <RecentMinMaxDiv>{weather.dto.maxTemperatureAtDay}</RecentMinMaxDiv>
        <div></div>
    </RecentTemperaturesDiv>
}

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
        <div style={{ fontSize: '80%', color: '#999' }}>{weather.dto?.sunsetDeltaTime}</div>
    </SunTimesDiv>
}

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

export function UnusedPage(): JSX.Element {
    const overlay = useNavOverlayState();

    return (
        <>
            <SunTimes style={{ position: 'absolute', left: '0vw', top: '53vh' }} />
            <MainClock style={{ position: 'absolute', left: '41vw', top: '-5vh', width: '27vw' }} onClick={overlay.show} />
            <ZonesClock style={{ position: 'absolute', left: '41vw', top: '18vh', width: '27vw' }} />
            <RecentTemperatures style={{ position: 'absolute', left: '41vw', top: '35vh', width: '30vw' }} />

            <NavOverlay state={overlay} />
        </>
    );
}