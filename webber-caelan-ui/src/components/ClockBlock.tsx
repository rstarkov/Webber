import * as _ from 'lodash';
import * as React from 'react';
import styled from 'styled-components';
import { withSubscription, BaseDto } from './util';
import moment from 'moment';

const TimeLabel = styled.div`
    height: 30px;
    margin-top: 10px;
    line-height: 30px;
    text-align: center;
    font-weight: bold;
    font-size: 30px;
    opacity: 1;
`;

const Time = styled.div`
    height: 100px;
    line-height: 100px;
    text-align: center;
    font-weight: bold;
    font-size: 100px;
    opacity: 0.9;
`;

const SecondaryTime = styled(Time)`
    height: 30px;
    margin-top: 6px;
    line-height: 30px;
    text-align: center;
    font-weight: bold;
    font-size: 30px;
    opacity: 0.7;
`;
// font-size: 60px;
// line-height: 70px;
// opacity: 0.6;
interface TimeZone {
    displayName: string;
    offsetHours: number;
}

interface ClockBlockDto extends BaseDto {
    timeZones: TimeZone[];
}

const ClockBlock: React.FunctionComponent<{ data: ClockBlockDto }> = ({ data }) => {

    const [time, setTime] = React.useState(moment.utc().valueOf());

    React.useEffect(() => {
        const interval = setInterval(() => { setTime(moment.utc().valueOf()); }, 1000);
        return () => clearInterval(interval);
    });

    function getTimeString(offset: number): string {
        return moment(time).utcOffset(offset).format("HH:mm");
    }

    return (
        <React.Fragment>
            <Time>{getTimeString(data.localOffsetHours)}</Time>
            <div className="l5t1 w4h1" style={{ paddingTop: 2 }}>
                <TimeLabel>{moment(time).format("dddd").substring(0, 3).toUpperCase() + ", " + moment(time).format("DD MMM").toUpperCase()}</TimeLabel>
            </div>
            {_.map(data.timeZones, t => (
                <React.Fragment key={t.displayName}>
                    <SecondaryTime>{t.displayName.substring(0, 5)} &nbsp; {getTimeString(t.offsetHours)}</SecondaryTime>
                    {/* <TimeLabel style={{ marginTop: 0, fontSize: 30 }}>{t.displayName}</TimeLabel>
                    <SecondaryTime>{getTimeString(t.offsetHours)}</SecondaryTime> */}
                </React.Fragment>
            ))}
        </React.Fragment>
    );
}

export default withSubscription(ClockBlock, "TimeBlock");