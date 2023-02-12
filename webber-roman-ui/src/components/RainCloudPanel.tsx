import { DateTime } from "luxon";
import styled from "styled-components";
import { useWeatherBlock } from "../blocks/WeatherBlock";
import { RainCloudPtDto, useRainCloudBlock } from "../blocks/RainCloudBlock";
import { BlockPanelContainer, joinState } from "./Container";
import { useWeatherDotComBlock } from "../blocks/WeatherDotComBlock";

const RainCloudDiv = styled(BlockPanelContainer)`
    display: grid;
    grid-template-columns: 1fr;
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

function RainChart(p: { from: DateTime }): JSX.Element {
    const rb = useRainCloudBlock();
    const wdc = useWeatherDotComBlock();
    const wb = useWeatherBlock();

    const hoursTotal = 48;
    function getX(dt: DateTime): number { return 100 * (dt.diff(p.from)).as("hours") / hoursTotal; }

    const nightColor = "#013"; // #081133
    const dayColor = "#330";
    const sunlineColor = "#880"; // sunrise & sunset line
    const gridColor = "#777";

    let daynight;
    if (wb.dto) {
        const sunrise1 = DateTime.fromISO(wb.dto.sunriseTime).set({ year: p.from.year, month: p.from.month, day: p.from.day });
        const sunset1 = DateTime.fromISO(wb.dto.sunsetTime).set({ year: p.from.year, month: p.from.month, day: p.from.day });
        const sunrise2 = sunrise1.plus({ day: 1 }); // should get exact times from server...
        const sunset2 = sunset1.plus({ day: 1 });
        daynight = {
            nend1: getX(sunrise1.plus({ hours: -1 })),
            rise1: getX(sunrise1),
            set1: getX(sunset1),
            nbeg2: getX(sunset1.plus({ hours: 1 })),
            nend2: getX(sunrise2.plus({ hours: -1 })),
            rise2: getX(sunrise2),
            set2: getX(sunset2),
            nbeg3: getX(sunset2.plus({ hours: 1 })),
        };
    }

    function getPts(data: RainCloudPtDto[], colormap: string[], scalemap: number[]) {
        function getSamples(p: RainCloudPtDto): barSample[] {
            const total = p.counts.reduce((a, b) => a + b, 0);
            let y = 0;
            const result: barSample[] = [];
            for (let i = p.counts.length - 1; i >= 0; i--) {
                if (p.counts[i] > 0 && colormap[i] != "#000") {
                    const height = 100 * p.counts[i] * (p.isForecast ? scalemap[i] : 1) / total;
                    result.push({ y, height, color: colormap[i] });
                    y += height;
                }
            }
            return result;
        }
        const pts: bar[] = data.filter(pt => pt.counts != null).map(pt => ({ pt, centerX: getX(pt.atUtc), samples: getSamples(pt) })).filter(pt => pt.centerX >= 0 && pt.centerX <= 100);
        for (let i = 1; i < pts.length; i++) {
            const mX = (pts[i - 1].centerX + pts[i].centerX) / 2;
            pts[i - 1].widthR = mX - pts[i - 1].centerX;
            pts[i].widthL = pts[i].centerX - mX;
        }
        if (pts.length > 0) {
            pts[0].widthL = pts[0].widthR;
            pts[pts.length - 1].widthR = pts[pts.length - 1].widthL;
        }
        return pts;
    }
    const rainPts = rb.dto && getPts(rb.dto.rain,
        ["#000", "#0000fe", "#0660fe", "#0cbcfe", "#00a300", "#fecb00", "#fe9800", "#fe0000", "#b30000"],
        [0, 0.4, 0.6, 0.75, 0.9, 1, 1, 1, 1]);
    // const cloudPts = rb.dto && getPts(rb.dto.cloud,
    //     ["#aaa0", "#aaa2", "#aaa2", "#aaa2", "#aaa3", "#aaa4", "#aaa5", "#aaa5", "#aaa5", "#aaa5"],
    //     [0, 0.1, 0.15, 0.2, 0.3, 0.4, 0.5, 0.7, 0.9, 1]);
    function getPrecipMmColor(mm: number): string {
        if (mm <= 0.1) return "#777";
        if (mm <= 0.3) return "#0000fe";
        if (mm <= 0.6) return "#0660fe";
        if (mm <= 1.0) return "#0cbcfe"; // like metoffice but shifted by one
        if (mm <= 2.0) return "#00a300";
        if (mm <= 4.0) return "#fecb00";
        if (mm <= 8.0) return "#fe9800";
        if (mm <= 16) return "#fe0000";
        return "#b30000";
    }

    const firstHour = p.from.startOf("hour");
    const hours = Array.from(Array(hoursTotal + 1), (_, i) => firstHour.plus({ hours: i })).map(h => ({ hour: h.hour, centerX: getX(h) })).filter(h => h.centerX > 0 && h.centerX < 100);

    const textHeight = 15;
    const tickHeight = 11;
    const markerHeight = tickHeight * 1.3;
    const chartHeight = 100 - textHeight - tickHeight;

    let rainlines = wdc.dto && wdc.dto.hours.map(h => ({ xm1: getX(h.dateTime), y: 100 - h.precipChance, c: getPrecipMmColor(h.precipMm), ym1: 0, yp1: 0, x: 0, xp1: 999 }));
    if (rainlines) {
        for (let i = 1; i < rainlines.length - 1; i++) {
            rainlines[i].ym1 = (rainlines[i].y + rainlines[i - 1].y) / 2;
            rainlines[i].yp1 = (rainlines[i].y + rainlines[i + 1].y) / 2;
            rainlines[i].x = rainlines[i].xm1 + 0.5 * 100 / hoursTotal;
            rainlines[i].xp1 = rainlines[i].xm1 + 100 / hoursTotal;
        }
        rainlines = rainlines.filter(h => h.xm1 >= 0 && h.xp1 <= 100);
    }

    return <svg width="100%" height="100%">
        <linearGradient id="lighttime" key="lighttime" x1="0" x2="0" y1="0" y2="1"><stop key="1" offset="0%" stopColor="#fff" /><stop key="2" offset="100%" stopColor="#000" /></linearGradient>
        <mask id="lightmask" key="lightmask">
            <rect key="r1" fill="url(#lighttime)" x="0%" y="0%" width="100%" height={chartHeight + "%"} />
            <rect key="r2" fill="url(#lighttime)" x="0%" y={chartHeight + "%"} width="100%" height={(115 - chartHeight) + "%"} />
        </mask>
        <linearGradient id="cloudgrad" key="cloudgrad" x1="0" x2="0" y1="0" y2="1"><stop key="1" offset="0%" stopColor="#0" /><stop key="2" offset="10%" stopColor="#fff" /></linearGradient>
        <mask id="cloudmask" key="cloudmask">
            <rect fill="url(#cloudgrad)" x="0%" y="0%" width="100%" height={chartHeight + "%"} />
        </mask>
        <linearGradient id="twilight1gr" key="twilight1gr"><stop key="1" offset="0%" stopColor={nightColor} /><stop key="2" offset="100%" stopColor={dayColor} /></linearGradient>
        <linearGradient id="twilight2gr" key="twilight2gr"><stop key="1" offset="0%" stopColor={dayColor} /><stop key="2" offset="100%" stopColor={nightColor} /></linearGradient>

        {daynight && <g key="glight" mask="url(#lightmask)">
            <rect key="night1" x="0%" y="0%" height="100%" width={daynight.nend1 + "%"} fill={nightColor} />
            <rect key="twi1r" x={daynight.nend1 + "%"} y="0%" height="100%" width={(daynight.rise1 - daynight.nend1) + "%"} fill="url(#twilight1gr)" />
            <rect key="day1" x={daynight.rise1 + "%"} y="0%" height="100%" width={(daynight.set1 - daynight.rise1) + "%"} fill={dayColor} />
            <rect key="twi1s" x={daynight.set1 + "%"} y="0%" height="100%" width={(daynight.nbeg2 - daynight.set1) + "%"} fill="url(#twilight2gr)" />
            <rect key="night2" x={daynight.nbeg2 + "%"} y="0%" height="100%" width={(daynight.nend2 - daynight.nbeg2) + "%"} fill={nightColor} />
            <rect key="twi2r" x={daynight.nend2 + "%"} y="0%" height="100%" width={(daynight.rise2 - daynight.nend2) + "%"} fill="url(#twilight1gr)" />
            <rect key="day2" x={daynight.rise2 + "%"} y="0%" height="100%" width={(daynight.set2 - daynight.rise2) + "%"} fill={dayColor} />
            <rect key="twi2s" x={daynight.set2 + "%"} y="0%" height="100%" width={(daynight.nbeg3 - daynight.set2) + "%"} fill="url(#twilight2gr)" />
            <rect key="night3" x={daynight.nbeg3 + "%"} y="0%" height="100%" width={(100 - daynight.nbeg3) + "%"} fill={nightColor} />
        </g>}

        {hours.filter(hr => (hr.hour % 2) == 0).map((hr, i) => <svg key={`${i}_tx`} x={(hr.centerX - textHeight / 2) + "%"} y={(100 - textHeight) + "%"} width={textHeight + "%"} height={textHeight + "%"} viewBox="0 0 1 1">
            <text x="0.5" y="0" fontSize="1" fill="#ccc" textAnchor="middle" dominantBaseline="hanging">{hr.hour.toLocaleString("en-US", { minimumIntegerDigits: 2 })}</text>
        </svg>)}
        {wdc.dto && <g key="clouds" mask="url(#cloudmask)">
            {wdc.dto.hours.map((pt, i) =>
                <rect
                    key={`${i}_1`}
                    x={`${getX(pt.dateTime)}%`}
                    y={`${chartHeight / 100 * (100 - pt.cloudCover)}%`}
                    width={`${100 / hoursTotal}%`}
                    height={`${chartHeight / 100 * pt.cloudCover}%`}
                    fill={`#aaaaaa${pt.cloudCover > 50 ? "55" : Math.round(0x11 + 0x33 * pt.cloudCover / 60).toString(16)}`}
                    strokeWidth="0">
                </rect>
            )}
        </g>}
        {daynight && <g key="grise">
            <line key="sunrise1" x1={daynight.rise1 + "%"} x2={daynight.rise1 + "%"} y1="0%" y2={chartHeight + "%"} stroke={sunlineColor} strokeDasharray="3" />
            <line key="sunset1" x1={daynight.set1 + "%"} x2={daynight.set1 + "%"} y1="0%" y2={chartHeight + "%"} stroke={sunlineColor} strokeDasharray="3" />
            <line key="sunrise2" x1={daynight.rise2 + "%"} x2={daynight.rise2 + "%"} y1="0%" y2={chartHeight + "%"} stroke={sunlineColor} strokeDasharray="3" />
            <line key="sunset2" x1={daynight.set2 + "%"} x2={daynight.set2 + "%"} y1="0%" y2={chartHeight + "%"} stroke={sunlineColor} strokeDasharray="3" />
        </g>}
        {rainPts && rainPts.map((pt, i) => pt.samples.map((sm, j) => {
            const gap = pt.widthL! + pt.widthR! > 0.4 ? 0.05 : -0.15; // negative to make them blend together
            return <rect
                key={`${i}_${j}_2`}
                x={`${pt.centerX - pt.widthL! + gap}%`}
                y={`${chartHeight / 100 * (100 - sm.y - sm.height)}%`}
                width={`${pt.widthL! + pt.widthR! - gap}%`}
                height={`${chartHeight / 100 * sm.height}%`}
                fill={sm.color}
                strokeWidth="0">
            </rect>;
        }))}

        <line key="xaxis" x1="0%" x2="100%" y1={chartHeight + "%"} y2={chartHeight + "%"} stroke={gridColor} />
        <line key="topb" x1="0%" x2="100%" y1="0%" y2="0%" stroke={gridColor} />
        {hours.map((hr, i) => <line key={`${i}_hr`} x1={hr.centerX + "%"} x2={hr.centerX + "%"} y1={chartHeight + "%"} y2={(chartHeight + tickHeight * 0.7) + "%"} stroke={gridColor} strokeWidth={(hr.hour % 2) == 0 ? 3 : 1} />)}

        {rainlines && <svg key="rpch" x="0" y="0" width="100%" height={chartHeight + "%"} viewBox="0 0 100 100" preserveAspectRatio="none">
            {rainlines.map((pt, i) => <path key={`k${i}`} stroke={pt.c} strokeWidth="0.3vw" fill="none" d={`M ${pt.xm1} ${pt.ym1} ${pt.x} ${pt.y} ${pt.xp1} ${pt.yp1}`} vectorEffect="non-scaling-stroke" />)}
        </svg>}

        <svg key="marker" x={(getX(DateTime.now()) - markerHeight / 2) + "%"} y={(chartHeight - markerHeight + tickHeight * 0.7 / 2) + "%"} width={markerHeight + "%"} height={markerHeight + "%"} viewBox="-0.1 -0.2 1.2 1.1">
            <path d="M 0 1 .5 0 1 1 z" fill="red" stroke="#000" strokeWidth="0.15" strokeLinejoin="miter" />
        </svg>
    </svg>;
}

export function RainCloudPanel(props: React.HTMLAttributes<HTMLDivElement>): JSX.Element {
    const rb = useRainCloudBlock();
    const wdc = useWeatherDotComBlock();

    const startHour = 5;
    let from = DateTime.now();
    if (from < from.startOf("day").plus({ hours: startHour }))
        from = from.plus({ days: -1 });
    from = from.startOf("day").plus({ hours: startHour });

    return <RainCloudDiv state={joinState(rb, wdc)} {...props}>
        <RainChart from={from} />
    </RainCloudDiv >;
}
