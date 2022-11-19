import styled from "styled-components";
import { useWeatherForecastBlock, WeatherForecastDayDto } from "../blocks/WeatherForecastBlock";
import { WeatherTypeIcon } from "../components/WeatherTypeIcon";

// TODO: min temp for today and maybe wind speed
// TODO: colors for temperatures
// TODO: better wind image
// TODO: connection/update status indicator

const weekdays = [null, 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat', 'Sun'];

const ForecastDiv = styled.div`
    display: flex;
`;
const ForecastDayDiv = styled.div`
    flex: 1;
    display: grid;
    border: 1px solid #444;
    justify-items: center;
`;

const HeadingDiv = styled.div`
    font-size: 1.9vw;
`;

const WeatherIconDiv = styled.div`
    width: 100%;
    padding: 0 10%;
    position: relative;
`;
const WeatherIcon = styled(WeatherTypeIcon)`
    width: 100%;
    height: 6vw;
`;
const RainProbDiv = styled.div`
    position: absolute;
    left: 0; top: 0; right: 0; bottom: 0.2vw;
    display: grid;
    align-content: center;
    justify-content: center;
    font-size: 2.3vw;
    font-weight: bold;
`;


function ForecastDay(p: { dto: WeatherForecastDayDto, mode: 'today' | 'big' | 'small' }): JSX.Element {
    const cellSize = p.mode == 'today' ? 1.5 : p.mode == 'big' ? 1.0 : 0.7;
    const cellBack = p.dto.date.weekday >= 6 ? '#333' : '#181818';
    const cellBorder = p.dto.date.weekday >= 6 ? '#777' : '#444';
    const cellMargin = p.dto.date.weekday == 7 ? '2vw' : '0';
    const headingText = p.mode == 'today' ? 'TODAY' : weekdays[p.dto.date.weekday];
    const wind = p.mode == 'small' ? 0 : p.dto.windMph > 27 || p.dto.gustMph > 42 ? 1 : p.dto.windMph >= 18 || p.dto.gustMph >= 31 ? 0.6 : 0;
    const rainShowLimit = p.mode == 'today' ? 15 : p.mode == 'big' ? 20 : 999;
    const rainText = p.dto.rainProbability < rainShowLimit ? null : (p.dto.rainProbability / 10).toFixed(0);

    return <ForecastDayDiv style={{ flex: cellSize, background: cellBack, marginRight: cellMargin, borderColor: cellBorder }}>
        <HeadingDiv>{headingText}</HeadingDiv>
        <WeatherIconDiv>
            <WeatherIcon kind={p.dto.weatherKind} night={p.dto.night} wind={wind} />
            {!!rainText && <RainProbDiv style={{ color: '#0000', WebkitTextStroke: '0.25vw #fff' }}>{rainText}</RainProbDiv>}
            {!!rainText && <RainProbDiv style={{ color: '#000' }}>{rainText}</RainProbDiv>}
        </WeatherIconDiv>
        <div>{p.dto.tempMaxC}°</div>
    </ForecastDayDiv>;
}

export function WeatherForecastBox(props: React.HTMLAttributes<HTMLDivElement>): JSX.Element {
    const fc = useWeatherForecastBlock();
    if (!fc.dto)
        return <></>;

    return <ForecastDiv {...props}>
        <ForecastDay dto={fc.dto.days[0]} mode='today' />
        {fc.dto.days.slice(1, 8).map(dto => <ForecastDay dto={dto} mode='big' />)}
        {fc.dto.days.slice(8, 14).map(dto => <ForecastDay dto={dto} mode='small' />)}
    </ForecastDiv>;
}
