import * as React from 'react';
import { withSubscription, BaseDto, isTimeBetween } from './util';
import styled from "styled-components";
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faMoon, faSun } from '@fortawesome/free-solid-svg-icons'
import moment from 'moment';

interface WeatherBlockDto extends BaseDto {
    curTemperature: number;
    curTemperatureColor: string;
    minTemperature: number;
    minTemperatureColor: string;
    minTemperatureAtTime: string;
    minTemperatureAtDay: string;
    maxTemperature: number;
    maxTemperatureColor: string;
    maxTemperatureAtTime: string;
    maxTemperatureAtDay: string;
    sunriseTime: string;
    solarNoonTime: string;
    sunsetTime: string;
    sunsetDeltaTime: string;
}

const CurrentWeatherLabel = styled.div`
    height: 120px;
    line-height: 120px;
    text-align: center;
    font-weight: bold;
    font-size: 100px;
    opacity: 0.9;
`;

const SunriseContainer = styled.div`
    height: 50px;
    line-height: 50px;
    font-weight: bold;
    font-size: 30px;
    text-align: center;
`;

const SunsetDimmer = styled.div`
    position: fixed;
    left: 0;
    right: 0;
    top: 0;
    bottom: 0;
    background-color: rgba(0,0,0,0.5);
`

const WeatherBlock: React.FunctionComponent<{ data: WeatherBlockDto }> = ({ data }) => {
    const shouldDim = !isTimeBetween(moment(), data.sunriseTime, data.sunsetTime);
    return (
        <React.Fragment>
            <CurrentWeatherLabel style={{ color: data.curTemperatureColor }}>{data.curTemperature.toFixed(1)}°C</CurrentWeatherLabel>
            <SunriseContainer>
                <FontAwesomeIcon icon={faSun} style={{ paddingRight: 10, color: "#EDBF24" }} />
                <span>{data.sunriseTime}</span>
                <FontAwesomeIcon icon={faMoon} style={{ paddingLeft: 40, paddingRight: 10, fontSize: 26, color: "#548BAB" }} />
                <span>{data.sunsetTime}</span>
            </SunriseContainer>
            {shouldDim && <SunsetDimmer />}
        </React.Fragment>
    );
}

export default withSubscription(WeatherBlock, "WeatherBlock");