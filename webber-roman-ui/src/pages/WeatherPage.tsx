import { DateTime } from "luxon";
import styled from "styled-components";
import { useWeatherBlock } from '../blocks/WeatherBlock';
import { NavOverlay, useNavOverlayState } from "../components/NavOverlay";
import { RainCloudPtDto, useRainCloudBlock } from "../blocks/RainCloudBlock";
import { WeatherBox } from "../components/WeatherBox";
import { PingBox } from "../components/PingBox";

// TODO: these clocks don't update properly!

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
    grid-template-columns: 1fr 1fr;
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

function RainChart(p: { rain: RainCloudPtDto[], cloud: RainCloudPtDto[], from: DateTime, hoursTotal: number, labelScale: number }): JSX.Element {
    const wb = useWeatherBlock();
    if (!wb.dto)
        return <></>;

    let sunrise = DateTime.fromISO(wb.dto.sunriseTime).set({ year: p.from.year, month: p.from.month, day: p.from.day });
    let sunset = DateTime.fromISO(wb.dto.sunsetTime).set({ year: p.from.year, month: p.from.month, day: p.from.day });;

    const nightColor = '#013'; // #081133
    const dayColor = '#330';
    const sunlineColor = '#880'; // sunrise & sunset line
    const gridColor = '#777';

    function getX(dt: DateTime): number { return 100 * (dt.diff(p.from)).as('hours') / p.hoursTotal; }

    function getPts(data: RainCloudPtDto[], colormap: string[], scalemap: number[]) {
        function getSamples(counts: number[]): barSample[] {
            let total = counts.reduce((a, b) => a + b, 0);
            let y = 0;
            let result: barSample[] = [];
            for (let i = counts.length - 1; i >= 0; i--) {
                if (counts[i] > 0 && colormap[i] != '#000') {
                    let height = 100 * counts[i] * scalemap[i] / total;
                    result.push({ y, height, color: colormap[i] });
                    y += height;
                }
            }
            return result;
        }
        let pts: bar[] = data.filter(pt => pt.counts != null).map(pt => ({ pt, centerX: getX(pt.atUtc), samples: getSamples(pt.counts) })).filter(pt => pt.centerX >= 0 && pt.centerX <= 100);
        for (let i = 1; i < pts.length; i++) {
            let mX = (pts[i - 1].centerX + pts[i].centerX) / 2;
            pts[i - 1].widthR = mX - pts[i - 1].centerX;
            pts[i].widthL = pts[i].centerX - mX;
        }
        pts[0].widthL = pts[0].widthR;
        pts[pts.length - 1].widthR = pts[pts.length - 1].widthL;
        return pts;
    }
    const rainPts = getPts(p.rain,
        ['#000', '#0000fe', '#0660fe', '#0cbcfe', '#00a300', '#fecb00', '#fe9800', '#fe0000', '#b30000'],
        [0, 0.4, 0.6, 0.75, 0.9, 1, 1, 1, 1]);
    const cloudPts = getPts(p.cloud,
        ['#aaa0', '#aaa2', '#aaa2', '#aaa2', '#aaa3', '#aaa4', '#aaa5', '#aaa5', '#aaa5', '#aaa5'],
        [0, 0.1, 0.15, 0.2, 0.3, 0.5, 0.7, 1, 1, 1]);

    let firstHour = p.from.startOf('hour');
    let hours = Array.from(Array(p.hoursTotal + 1), (_, i) => firstHour.plus({ hours: i })).map(h => ({ hour: h.hour, centerX: getX(h) })).filter(h => h.centerX > 0 && h.centerX < 100);

    const textHeight = 15 * p.labelScale;
    const tickHeight = 11 * p.labelScale;
    const chartHeight = 100 - textHeight - tickHeight;

    return <svg width='100%' height='100%'>
        <linearGradient id="lighttime" key="lighttime" x1='0' x2='0' y1='0' y2='1'><stop key='1' offset="0%" stopColor='#fff' /><stop key='2' offset="100%" stopColor='#000' /></linearGradient>
        <mask id='lightmask' key='lightmask'>
            <rect key='r1' fill='url(#lighttime)' x='0%' y='0%' width='100%' height={chartHeight + '%'} />
            <rect key='r2' fill='url(#lighttime)' x='0%' y={chartHeight + '%'} width='100%' height={(115 - chartHeight) + '%'} />
        </mask>
        <linearGradient id="cloudgrad" key="cloudgrad" x1='0' x2='0' y1='0' y2='1'><stop key='1' offset="0%" stopColor='#0' /><stop key='2' offset="10%" stopColor='#fff' /></linearGradient>
        <mask id='cloudmask' key='cloudmask'>
            <rect fill='url(#cloudgrad)' x='0%' y='0%' width='100%' height={chartHeight + '%'} />
        </mask>
        <linearGradient id="twilight1gr" key='twilight1gr'><stop key='1' offset="0%" stopColor={nightColor} /><stop key='2' offset="100%" stopColor={dayColor} /></linearGradient>
        <linearGradient id="twilight2gr" key='twilight2gr'><stop key='1' offset="0%" stopColor={dayColor} /><stop key='2' offset="100%" stopColor={nightColor} /></linearGradient>

        <g key='glight' mask='url(#lightmask)'>
            <rect key='night1' x='0%' y='0%' height='100%' width={getX(sunrise.plus({ hours: -1 })) + '%'} fill={nightColor} />
            <rect key='twi1' x={getX(sunrise.plus({ hours: -1 })) + '%'} y='0%' height='100%' width={(100 / p.hoursTotal) + '%'} fill='url(#twilight1gr)' />
            <rect key='day' x={getX(sunrise) + '%'} y='0%' height='100%' width={(getX(sunset) - getX(sunrise)) + '%'} fill={dayColor} />
            <rect key='twi2' x={getX(sunset) + '%'} y='0%' height='100%' width={(100 / p.hoursTotal) + '%'} fill='url(#twilight2gr)' />
            <rect key='night2' x={getX(sunset.plus({ hours: 1 })) + '%'} y='0%' height='100%' width={(100 - getX(sunset.plus({ hours: 1 }))) + '%'} fill={nightColor} />
        </g>

        {hours.filter(hr => (hr.hour % 2) == 0).map((hr, i) => <svg key={`${i}_tx`} x={(hr.centerX - textHeight / 2) + '%'} y={(100 - textHeight) + '%'} width={textHeight + '%'} height={textHeight + '%'} viewBox='0 0 1 1'>
            <text x='0.5' y='0' fontSize='1' fill='#ccc' textAnchor='middle' dominantBaseline='hanging'>{hr.hour.toLocaleString('en-US', { minimumIntegerDigits: 2 })}</text>
        </svg>)}
        <g key='clouds' mask='url(#cloudmask)'>
            {cloudPts.map((pt, i) => pt.samples.map((sm, j) => {
                const gap = pt.widthL! + pt.widthR! > 2 ? 0 : pt.widthL! + pt.widthR! > 0.4 ? 0.05 : -0.15 /* negative to make them blend together */;
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
        </g>
        <g key='grise'>
            <line key='sunrise' x1={getX(sunrise) + '%'} x2={getX(sunrise) + '%'} y1='0%' y2={chartHeight + '%'} stroke={sunlineColor} strokeDasharray='3' />
            <line key='sunset' x1={getX(sunset) + '%'} x2={getX(sunset) + '%'} y1='0%' y2={chartHeight + '%'} stroke={sunlineColor} strokeDasharray='3' />
        </g>
        {rainPts.map((pt, i) => pt.samples.map((sm, j) => {
            const gap = pt.widthL! + pt.widthR! > 0.4 ? 0.05 : -0.15 /* negative to make them blend together */;
            return <rect
                key={`${i}_${j}_2`}
                x={`${pt.centerX - pt.widthL! + gap}%`}
                y={`${chartHeight / 100 * (100 - sm.y - sm.height)}%`}
                width={`${pt.widthL! + pt.widthR! - gap}%`}
                height={`${chartHeight / 100 * sm.height}%`}
                fill={sm.color}
                strokeWidth='0'>
            </rect>;
        }))}

        <line key='xaxis' x1='0%' x2='100%' y1={chartHeight + '%'} y2={chartHeight + '%'} stroke={gridColor} />
        {hours.map((hr, i) => <line key={`${i}_hr`} x1={hr.centerX + '%'} x2={hr.centerX + '%'} y1={chartHeight + '%'} y2={(chartHeight + tickHeight * 0.7) + '%'} stroke={gridColor} strokeWidth={(hr.hour % 2) == 0 ? 3 : 1} />)}

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

    const startHour = 5;
    let from = DateTime.now();
    if (from < from.startOf('day').plus({ hours: startHour }))
        from = from.plus({ days: -1 });
    from = from.startOf('day').plus({ hours: startHour });

    return <RainCloudDiv {...props}>
        <RainChart rain={rb.dto.rain} cloud={rb.dto.cloud} from={from} hoursTotal={24} labelScale={1} />
        <RainChart rain={rb.dto.rain} cloud={rb.dto.cloud} from={from.plus({ days: 1 })} hoursTotal={24} labelScale={1} />
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
            <RainCloud style={{ position: 'absolute', left: '0vw', top: '75vh', width: '100vw', height: '25vh' }} />

            <NavOverlay state={overlay} />
        </>
    );
}