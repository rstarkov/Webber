import styled from "styled-components";
import { usePingBlock } from '../blocks/PingBlock';
import { useWeatherBlock } from '../blocks/WeatherBlock';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faSun, faMoon } from '@fortawesome/free-solid-svg-icons';
import { BlockPanelBorderedContainer } from "../dashboard/Container";


const WeatherBlockDiv = styled(BlockPanelBorderedContainer)`
    display: grid;
    justify-items: right;
    opacity: 0.7;
`;

function WeatherBlock(props: React.HTMLAttributes<HTMLDivElement>): JSX.Element {
    const weather = useWeatherBlock();
    return <WeatherBlockDiv state={weather} {...props}>
        <div><FontAwesomeIcon icon={faSun} color='#ff0' /> {weather.dto?.sunriseTime}</div>
        <div><FontAwesomeIcon icon={faMoon} color='#4479ff' /> {weather.dto?.sunsetTime}</div>
        <div style={{ fontSize: '80%', color: '#999' }}>{weather.dto?.sunsetDeltaTime}</div>
    </WeatherBlockDiv>
}

export function ClassicPage(): JSX.Element {
    return (
        <>
            <WeatherBlock style={{ position: 'absolute', top: '0vw', left: '0vw', width: '37vw', height: '26vw' }} />
            <WeatherBlock style={{ position: 'absolute', top: '0vw', left: '39vw', width: '25vw', height: '26vw' }} />
            <WeatherBlock style={{ position: 'absolute', top: '0vw', right: '0', width: '34vw', height: '26vw' }} />

            <WeatherBlock style={{ position: 'absolute', top: '28vw', left: '0vw', width: '45vw', bottom: '0vw' }} />
            <WeatherBlock style={{ position: 'absolute', top: '28vw', left: '47vw', right: '0vw', bottom: '0vw' }} />
        </>
    )
}
