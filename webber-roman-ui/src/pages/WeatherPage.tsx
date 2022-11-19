import styled from "styled-components";
import { NavOverlay, useNavOverlayState } from "../components/NavOverlay";
import { WeatherBox } from "../components/WeatherBox";
import { PingBox } from "../components/PingBox";
import { useTime } from "../util/useTime";
import { WeatherForecastBox } from "../components/WeatherForecastBox";
import { RainCloudBox } from "../components/RainCloudBox";

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
    const { time } = useTime();
    return <ZonesClockDiv {...props}>
        <ZoneName>Ukr</ZoneName><ZoneTime>{time.setZone('Europe/Kiev').toFormat('HH:mm')}</ZoneTime>
        <ZoneName style={{ fontWeight: 'bold' }}>UTC</ZoneName><ZoneTime style={{ fontWeight: 'bold' }}>{time.toFormat('HH:mm')}</ZoneTime>
        <ZoneName>Can</ZoneName><ZoneTime>{time.setZone('Canada/Mountain').toFormat('HH:mm')}</ZoneTime>
    </ZonesClockDiv>;
}

const MainClockDiv = styled.div`
    font-size: 350%;
    font-weight: bold;
    text-align: center;
`;

function MainClock(props: React.HTMLAttributes<HTMLDivElement>): JSX.Element {
    const { time } = useTime();
    return <MainClockDiv {...props}>
        {time.toLocal().toFormat('HH:mm')}
    </MainClockDiv>;
}

export function WeatherPage(): JSX.Element {
    const overlay = useNavOverlayState();

    return (
        <>
            <MainClock style={{ position: 'absolute', left: '38vw', top: '-5vh', width: '27vw' }} onClick={overlay.show} />
            <ZonesClock style={{ position: 'absolute', left: '38vw', top: '18vh', width: '27vw' }} />
            <PingBox style={{ position: 'absolute', top: '0vw', right: '0', width: '34vw', height: '26vw' }} />
            <WeatherBox style={{ position: 'absolute', top: '0vw', left: '0vw', width: '37vw', height: '26vw' }} />
            <WeatherForecastBox style={{ position: 'absolute', left: '0vw', top: '48.2vh', width: '100vw' }} />
            <RainCloudBox style={{ position: 'absolute', left: '0vw', top: '75vh', width: '100vw', height: '25vh' }} />

            <NavOverlay state={overlay} />
        </>
    );
}