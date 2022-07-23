import styled from "styled-components";
import { usePingBlock } from '../blocks/PingBlock';
import { useWeatherBlock } from '../blocks/WeatherBlock';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faSun, faMoon } from '@fortawesome/free-solid-svg-icons';
import { BlockPanelBorderedContainer } from "../dashboard/Container";
import { DateTime } from "luxon";
import { BarChart, BarChartPt, ModifiedLog, ScaleY } from "../components/BarChart";
import { RouterHistoryPoint, useRouterBlock } from "../blocks/RouterBlock";


const WeatherBlockDiv = styled(BlockPanelBorderedContainer)`
    display: grid;
    grid-template-rows: min-content min-content 1fr min-content;
`;
const RecentMinMaxDiv = styled.div`
    padding-left: 1.3vw;
    color: #777;
`;

function WeatherBlock(props: React.HTMLAttributes<HTMLDivElement>): JSX.Element {
    const weather = useWeatherBlock();
    if (!weather.dto)
        return <WeatherBlockDiv state={weather} {...props} />;
    function temp2str(temp: number): string {
        return (temp < 0 ? "–" : "") + Math.abs(temp).toFixed(0);
    }
    return <WeatherBlockDiv state={weather} {...props}>
        <div style={{ color: weather.dto.curTemperatureColor, fontSize: '280%', fontWeight: 'bold', textAlign: 'center', marginTop: '-1.7vw', marginBottom: '0.1vw' }}>{temp2str(weather.dto.curTemperature)} °C</div>
        <div style={{ display: 'grid', gridTemplateColumns: '1fr repeat(4, min-content) 1fr' }}>
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
        </div>
        <div></div>
        <div style={{ display: 'grid', gridTemplateColumns: 'min-content 1fr min-content 1fr min-content', alignItems: 'baseline' }}>
            <div><FontAwesomeIcon icon={faSun} color='#ff0' /> {weather.dto.sunriseTime}</div>
            <div></div>
            <div><FontAwesomeIcon icon={faMoon} color='#4479ff' /> {weather.dto.sunsetTime}</div>
            <div></div>
            <div style={{ fontSize: '80%', color: '#999' }}>{weather.dto.sunsetDeltaTime}</div>
        </div>
    </WeatherBlockDiv>
}

const TimeBlockDiv = styled(BlockPanelBorderedContainer)`
    display: grid;
    grid-template-columns: 1fr min-content min-content 1fr;
`;
function TimeBlock(props: React.HTMLAttributes<HTMLDivElement>): JSX.Element {
    return <TimeBlockDiv state={{ status: 'connected', updates: 0 }} {...props}>
        <div style={{ gridColumnEnd: 'span 4', textAlign: 'center', fontSize: '280%', fontWeight: 'bold', marginTop: '-1.7vw', marginBottom: '0.8vw' }}>{DateTime.local().toFormat('HH:mm')}</div>

        <div></div>
        <div style={{ color: '#777', marginRight: '1.5vw' }}>UTC</div>
        <div>{DateTime.utc().toFormat('HH:mm')}</div>
        <div></div>

        <div></div>
        <div style={{ color: '#777', marginRight: '1.5vw' }}>Can</div>
        <div>{DateTime.utc().setZone('Canada/Mountain').toFormat('HH:mm')}</div>
        <div></div>

        <div></div>
        <div style={{ color: '#777', marginRight: '1.5vw' }}>Ukr</div>
        <div>{DateTime.utc().setZone('Europe/Kiev').toFormat('HH:mm')}</div>
        <div></div>
    </TimeBlockDiv>
}

const PingBlockDiv = styled(BlockPanelBorderedContainer)`
    display: flex;
    flex-direction: column;
`;
function PingBlock(props: React.HTMLAttributes<HTMLDivElement>): JSX.Element {
    const ping = usePingBlock();
    if (!ping.dto)
        return <PingBlockDiv state={ping} {...props} />;

    function pingToPt(ping: number | null): BarChartPt {
        if (ping == null /* missing data */ || ping == -1 /* ping timeout */)
            return { Value: 1.0, Color: ping == null ? "#404040" : "#ff00ff" };
        return {
            Value: ScaleY(ping, 10, 2000, x => ModifiedLog(x, 10)),
            Color: ping > 200 ? "#ff0000" : ping > 40 ? "#1985f3" : "#08b025",
        };
    }

    return <PingBlockDiv state={ping} {...props}>
        <div><div style={{ float: 'right' }}>{ping.dto.last?.toLocaleString('en-UK', { maximumFractionDigits: 0 }) ?? '∞'} ms</div>Ping</div>
        <div style={{ padding: '0 0.5vw', borderBottom: '1px solid #999', flex: '1', minHeight: '0' }}>
            <BarChart Data={ping.dto.recent.map(pingToPt)} BarCount={ping.dto.recent.length} />
        </div>
    </PingBlockDiv>
}

const RouterBlockDiv = styled(BlockPanelBorderedContainer)`
    display: flex;
    flex-direction: column;
`;
const RouterChartDiv = styled.div`
    width: 100%;
    height: 100%;
    min-width: 0;
    min-height: 0;
    padding: 0 0.5vw;
    border-bottom: 1px solid #999;
`;
const RouterChartDownDiv = styled(RouterChartDiv)`
    border-bottom: none;
    border-top: 1px solid #999;
`;
function RouterBlock(props: React.HTMLAttributes<HTMLDivElement>): JSX.Element {
    const router = useRouterBlock();
    if (!router.dto)
        return <RouterBlockDiv state={router} {...props} />;

    function rate2str(rate: number): string { return rate >= 1_000_000 ? `${(rate / 1_000_000).toFixed(1)}M` : `${(rate / 1000).toFixed(0)}`; }

    function getPt(val: number | undefined, max: number, c1: string, c2: string, c3: string, c4: string): BarChartPt {
        if (val === undefined)
            return { Value: 1.0, Color: "#404040" };
        return {
            Value: ScaleY(val, max / 1000, max, x => Math.log10(x)),
            Color: val < max / 100 ? c1 : val < max / 10 ? c2 : val < max * 0.6 ? c3 : c4,
        };
    }

    function getTxPt(pt: RouterHistoryPoint | null) {
        return getPt(pt?.txRate, 2_900_000, "#05305c", "#0959aa", "#1985f3", "#64adf7");
    }
    function getRxPt(pt: RouterHistoryPoint | null) {
        return getPt(pt?.rxRate, 26_000_000, "#573805", "#966008", "#ed980d", "#f6bb5a");
    }

    return <RouterBlockDiv state={router} {...props}>
        <div style={{ display: 'grid', width: '100%', height: '100%', gap: '2vw', gridTemplateColumns: 'min-content 1fr min-content 1fr min-content', gridTemplateRows: 'min-content 1fr 1fr', alignItems: 'center', justifyItems: 'center' }}>
            <div style={{ color: '#777' }}>KB/s</div>
            <div>Recent traffic</div>
            <div></div>
            <div>Hourly traffic</div>
            <div style={{ color: '#777' }}>KB/s</div>

            <div>{rate2str(router.dto.txLast)}</div>
            <RouterChartDownDiv>
                <BarChart Data={router.dto.historyRecent.map(getTxPt)} BarCount={24} Downwards={true} />
            </RouterChartDownDiv>
            <div style={{ color: '#1985f3' }}>Up</div>
            <RouterChartDownDiv>
                <BarChart Data={router.dto.historyHourly.map(getTxPt)} BarCount={24} Downwards={true} />
            </RouterChartDownDiv>
            <div>{rate2str(router.dto.txAverageRecent)}</div>

            <div>{rate2str(router.dto.rxLast)}</div>
            <RouterChartDiv>
                <BarChart Data={router.dto.historyRecent.map(getRxPt)} BarCount={24} />
            </RouterChartDiv>
            <div style={{ color: '#ed980d' }}>Dn</div>
            <RouterChartDiv>
                <BarChart Data={router.dto.historyHourly.map(getRxPt)} BarCount={24} />
            </RouterChartDiv>
            <div>{rate2str(router.dto.rxAverageRecent)}</div>
            {/* <div style={{ padding: '0 0.5vw', borderBottom: '1px solid #999', flex: '1', minHeight: '0' }}>
                <BarChart Data={router.dto.recent.map(pingToPt)} BarCount={router.dto.recent.length} />
            </div> */}
        </div>
    </RouterBlockDiv>
}

export function ClassicPage(): JSX.Element {
    return (
        <>
            <WeatherBlock style={{ position: 'absolute', top: '0vw', left: '0vw', width: '37vw', height: '26vw' }} />
            <TimeBlock style={{ position: 'absolute', top: '0vw', left: '39vw', width: '25vw', height: '26vw' }} />
            <PingBlock style={{ position: 'absolute', top: '0vw', right: '0', width: '34vw', height: '26vw' }} />

            {/* <WeatherBlock style={{ position: 'absolute', top: '28vw', left: '0vw', width: '45vw', bottom: '0vw' }} />
            <RouterBlock style={{ position: 'absolute', top: '28vw', left: '47vw', right: '0vw', bottom: '0vw' }} /> */}
            <RouterBlock style={{ position: 'absolute', top: '28vw', left: '0vw', right: '0vw', bottom: '0vw' }} />
        </>
    )
}
