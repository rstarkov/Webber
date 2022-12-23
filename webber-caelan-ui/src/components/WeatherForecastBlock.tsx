import * as _ from 'lodash';
import * as React from 'react';
import styled from 'styled-components';
import { withSubscription, BaseDto } from './util';
import moment from 'moment';

interface WeatherForecastHourDto {
    dateTime: string;
    rainProbability: number;
}

interface WeatherForecastBlockDto extends BaseDto {
    hours: WeatherForecastHourDto[];
}

const RainBar = styled.div`
    position: absolute;
    bottom: 30px;
    width: 30px;
    background-color: rgb(30, 53, 89);
    border-top: 2px solid #8AB4F8;
`

const TimeText = styled.div`
    position: absolute;
    text-align: center;
    width: 90px;
    bottom: 0;
    font-size: 24px;
`;

const PercipText = styled.div`
    position: absolute;
    text-align: center;
    width: 90px;
    top: 0;
    font-size: 24px;
`;

const WeatherForecastBlock: React.FunctionComponent<{ data: WeatherForecastBlockDto }> = ({ data }) => {
    var t4hours = _.take(data.hours, 24);

    const getPercip = (i: number) => {
        let prob = Math.max(data.hours[i].rainProbability, data.hours[i - 1].rainProbability, data.hours[i + 1].rainProbability);
        return prob.toString() + "%";
    }

    return (
        <React.Fragment>
            {_.map(t4hours, (e, i) => (<RainBar style={{ left: i * 30, height: 60 * (e.rainProbability / 100) }} />))}
            <TimeText style={{ left: 90 * 0 }}>{moment(t4hours[1].dateTime).format("HH:mm")}</TimeText>
            <TimeText style={{ left: 90 * 1 }}>{moment(t4hours[4].dateTime).format("HH:mm")}</TimeText>
            <TimeText style={{ left: 90 * 2 }}>{moment(t4hours[7].dateTime).format("HH:mm")}</TimeText>
            <TimeText style={{ left: 90 * 3 }}>{moment(t4hours[10].dateTime).format("HH:mm")}</TimeText>
            <TimeText style={{ left: 90 * 4 }}>{moment(t4hours[13].dateTime).format("HH:mm")}</TimeText>
            <TimeText style={{ left: 90 * 5 }}>{moment(t4hours[16].dateTime).format("HH:mm")}</TimeText>
            <TimeText style={{ left: 90 * 6 }}>{moment(t4hours[19].dateTime).format("HH:mm")}</TimeText>
            <TimeText style={{ left: 90 * 7 }}>{moment(t4hours[22].dateTime).format("HH:mm")}</TimeText>
            <PercipText style={{ left: 90 * 0 }}>{getPercip(1)}</PercipText>
            <PercipText style={{ left: 90 * 1 }}>{getPercip(4)}</PercipText>
            <PercipText style={{ left: 90 * 2 }}>{getPercip(7)}</PercipText>
            <PercipText style={{ left: 90 * 3 }}>{getPercip(10)}</PercipText>
            <PercipText style={{ left: 90 * 4 }}>{getPercip(13)}</PercipText>
            <PercipText style={{ left: 90 * 5 }}>{getPercip(16)}</PercipText>
            <PercipText style={{ left: 90 * 6 }}>{getPercip(19)}</PercipText>
            <PercipText style={{ left: 90 * 7 }}>{getPercip(22)}</PercipText>
        </React.Fragment>
    );
}

export default withSubscription(WeatherForecastBlock, "WeatherForecastBlock");