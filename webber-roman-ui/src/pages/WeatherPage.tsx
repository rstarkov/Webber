import { DateTime } from "luxon";
import styled from "styled-components";
import { useWeatherBlock } from '../blocks/WeatherBlock';
import { NavOverlay, useNavOverlayState } from "../components/NavOverlay";
import { RainCloudPtDto, useRainCloudBlock } from "../blocks/RainCloudBlock";
import { WeatherBox } from "../components/WeatherBox";
import { PingBox } from "../components/PingBox";

// TODO: these clocks don't update properly!
// TODO: share the times axis between rain and clouds
// TODO: switches to next day too early
// TODO: darker cloud chart

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

const RainCloudDiv = styled.div`
    display: grid;
    grid-auto-flow: column;
    grid-template-rows: 1fr 1fr;
    grid-gap: 2vw 1vw;
`;

interface bar {
    pt: RainCloudPtDto;
    centerX: number;
    widthL?: number;
    widthR?: number;
    samples: barSample[];
}
interface barSample {
    y: number;
    height: number;
    color: string;
}

function RainChart(p: { data: RainCloudPtDto[], hoursStart: number, hoursTotal: number, colormap: string[], scalemap: number[], labelScale: number }): JSX.Element {
    const wb = useWeatherBlock();
    if (!wb.dto)
        return <></>;

    const fr = DateTime.now().startOf('day').plus({ hours: p.hoursStart });
    let sunrise = DateTime.fromISO(wb.dto.sunriseTime);
    let sunset = DateTime.fromISO(wb.dto.sunsetTime);
    if (p.hoursStart >= 24) {
        sunrise = sunrise.plus({ days: 1 });
        sunset = sunset.plus({ days: 1 });
    }
    const nightColor = '#013'; // #081133
    const dayColor = '#330';
    const sunlineColor = '#880'; // sunrise & sunset line
    const gridColor = '#444';

    function getX(dt: DateTime): number { return 100 * (dt.diff(fr)).as('hours') / p.hoursTotal; }
    function getSamples(counts: number[]): barSample[] {
        let total = counts.reduce((a, b) => a + b, 0);
        let y = 0;
        let result: barSample[] = [];
        for (let i = counts.length - 1; i >= 0; i--) {
            if (counts[i] > 0 && p.colormap[i] != '#000') {
                let height = 100 * counts[i] * p.scalemap[i] / total;
                result.push({ y, height, color: p.colormap[i] });
                y += height;
            }
        }
        return result;
    }
    let pts: bar[] = p.data.filter(pt => pt.counts != null).map(pt => ({ pt, centerX: getX(pt.atUtc), samples: getSamples(pt.counts) })).filter(pt => pt.centerX >= 0 && pt.centerX <= 100);
    for (let i = 1; i < pts.length; i++) {
        let mX = (pts[i - 1].centerX + pts[i].centerX) / 2;
        pts[i - 1].widthR = mX - pts[i - 1].centerX;
        pts[i].widthL = pts[i].centerX - mX;
    }
    pts[0].widthL = pts[0].widthR;
    pts[pts.length - 1].widthR = pts[pts.length - 1].widthL;

    let firstHour = fr.startOf('hour');
    let hours = Array.from(Array(p.hoursTotal + 1), (_, i) => firstHour.plus({ hours: i })).map(h => ({ hour: h.hour, centerX: getX(h) })).filter(h => h.centerX > 0 && h.centerX < 100);

    const textHeight = 15 * p.labelScale;
    const tickHeight = 11 * p.labelScale;
    const chartHeight = 100 - textHeight - tickHeight;

    return <svg width='100%' height='100%'>
        <linearGradient id="lighttime" key="lighttime" x1='0' x2='0' y1='0' y2='1'><stop key='1' offset="0%" stopColor='#fff' /><stop key='2' offset="100%" stopColor='#000' /></linearGradient>
        <mask id='lightmask' key='lightmask'><rect fill='url(#lighttime)' x='0%' y='0%' width='100%' height={chartHeight + '%'} /></mask>
        <linearGradient id="twilight1gr" key='twilight1gr'><stop key='1' offset="0%" stopColor={nightColor} /><stop key='2' offset="100%" stopColor={dayColor} /></linearGradient>
        <linearGradient id="twilight2gr" key='twilight2gr'><stop key='1' offset="0%" stopColor={dayColor} /><stop key='2' offset="100%" stopColor={nightColor} /></linearGradient>
        <g key='glight' mask='url(#lightmask)'>
            <rect key='night1' x='0%' y='0%' height={chartHeight + '%'} width={getX(sunrise.plus({ hours: -1 })) + '%'} fill={nightColor} />
            <rect key='twi1' x={getX(sunrise.plus({ hours: -1 })) + '%'} y='0%' height={chartHeight + '%'} width={(100 / p.hoursTotal) + '%'} fill='url(#twilight1gr)' />
            <rect key='day' x={getX(sunrise) + '%'} y='0%' height={chartHeight + '%'} width={(getX(sunset) - getX(sunrise)) + '%'} fill={dayColor} />
            <rect key='twi2' x={getX(sunset) + '%'} y='0%' height={chartHeight + '%'} width={(100 / p.hoursTotal) + '%'} fill='url(#twilight2gr)' />
            <rect key='night2' x={getX(sunset.plus({ hours: 1 })) + '%'} y='0%' height={chartHeight + '%'} width={(100 - getX(sunset.plus({ hours: 1 }))) + '%'} fill={nightColor} />
        </g>

        <line key='xaxis' x1='0%' x2='100%' y1={chartHeight + '%'} y2={chartHeight + '%'} stroke={gridColor} />
        {hours.map((hr, i) => <line key={`${i}_hr`} x1={hr.centerX + '%'} x2={hr.centerX + '%'} y1='0%' y2={(chartHeight + tickHeight * 0.7) + '%'} stroke={gridColor} />)}

        <g key='grise' mask='url(#lightmask)'>
            <line key='sunrise' x1={getX(sunrise) + '%'} x2={getX(sunrise) + '%'} y1='0%' y2={chartHeight + '%'} stroke={sunlineColor} strokeDasharray='3' />
            <line key='sunset' x1={getX(sunset) + '%'} x2={getX(sunset) + '%'} y1='0%' y2={chartHeight + '%'} stroke={sunlineColor} strokeDasharray='3' />
        </g>

        {hours.filter(hr => hr.hour < 9 || (hr.hour % 2) == 0).map((hr, i) => <svg key={`${i}_tx`} x={(hr.centerX - textHeight / 2) + '%'} y={(100 - textHeight) + '%'} width={textHeight + '%'} height={textHeight + '%'} viewBox='0 0 1 1'>
            <text x='0.5' y='0' fontSize='1' fill='#ccc' textAnchor='middle' dominantBaseline='hanging'>{hr.hour}</text>
        </svg>)}
        {pts.map((pt, i) => pt.samples.map((sm, j) => {
            const gap = pt.widthL! + pt.widthR! > 0.4 ? 0.05 : -0.15 /* negative to make them blend together */;
            return <rect
                key={`${i}_${j}_1`}
                x={`${pt.centerX - pt.widthL! + gap}%`}
                y={`${chartHeight / 100 * (100 - sm.y - sm.height)}%`}
                width={`${pt.widthL! + pt.widthR! - gap}%`}
                height={`${chartHeight / 100 * sm.height}%`}
                fill={sm.color}
                strokeWidth='0'>
            </rect>;
        }))}
        <svg key='tick' x={(getX(DateTime.now()) - tickHeight * 1.3 / 2) + '%'} y={chartHeight - tickHeight * 1.3 * 0.45 + '%'} width={tickHeight * 1.3 + '%'} height={tickHeight * 1.3 + '%'} viewBox='-0.1 -0.2 1.2 1.2'>
            <path d='M 0 1 .5 0 1 1 z' fill='red' stroke='#000' strokeWidth='0.15' strokeLinejoin='miter' />
        </svg>
        {/* {<svg opacity='0.5' x={(getX(DateTime.now()) - tickHeight / 2) + '%'} y={chartHeight - tickHeight * 0.3 + '%'} width={tickHeight + '%'} height={tickHeight + '%'} viewBox='0 0 1 1'>
            <path d='M 0 1 .5 0 1 1 z' fill='green' />
        </svg>} */}
    </svg>;
}

function RainCloud(props: React.HTMLAttributes<HTMLDivElement>): JSX.Element {
    const rb = useRainCloudBlock();
    if (!rb.dto)
        return <></>;

    const rainColors = ['#000', '#0000b1', '#0051dd', '#0cbcfe', '#00a300', '#fecb00', '#fe9800', '#fe0000', '#b30000'];
    const cloudColors = ['#0800ff', '#1b14f7', '#2f28ef', '#423de7', '#5551df', '#6965d6', '#7c79ce', '#8f8ec6', '#a3a2be', '#B6B6B6'];
    const rainScales = [0, 0.4, 0.6, 0.75, 0.9, 1, 1, 1, 1];
    const cloudScales = [0, 0.1, 0.15, 0.2, 0.3, 0.4, 0.5, 0.75, 1, 1];

    return <RainCloudDiv {...props}>
        <RainChart data={rb.dto.rain} hoursStart={4.5} hoursTotal={24} colormap={rainColors} scalemap={rainScales} labelScale={1} />
        <RainChart data={rb.dto.cloud} hoursStart={4.5} hoursTotal={24} colormap={cloudColors} scalemap={cloudScales} labelScale={1} />
        <RainChart data={rb.dto.rain} hoursStart={24 + 4.5} hoursTotal={24} colormap={rainColors} scalemap={rainScales} labelScale={1} />
        <RainChart data={rb.dto.cloud} hoursStart={24 + 4.5} hoursTotal={24} colormap={cloudColors} scalemap={cloudScales} labelScale={1} />
    </RainCloudDiv >;
}

export function WeatherPage(): JSX.Element {
    const overlay = useNavOverlayState();

    return (
        <>
            <MainClock style={{ position: 'absolute', left: '38vw', top: '-5vh', width: '27vw' }} onClick={overlay.show} />
            <ZonesClock style={{ position: 'absolute', left: '38vw', top: '18vh', width: '27vw' }} />
            <PingBox style={{ position: 'absolute', top: '0vw', right: '0', width: '34vw', height: '26vw' }} />
            <WeatherBox style={{ position: 'absolute', top: '0vw', left: '0vw', width: '37vw', height: '26vw' }} />
            <RainCloud style={{ position: 'absolute', left: '0vw', top: '50vh', width: '100vw', height: '50vh' }} />

            <NavOverlay state={overlay} />
        </>
    );
}