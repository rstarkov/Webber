import * as _ from 'lodash';
import * as React from 'react';
import { withSubscription, BaseDto } from './util';
import styled from "styled-components";
import { Textfit } from 'react-textfit';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCalendarAlt } from '@fortawesome/free-solid-svg-icons'
import moment from 'moment';
import pad from "pad-left";

moment.locale('en', {
    relativeTime: {
        future: 'in %s',
        past: '%s ago',
        s: 'seconds',
        ss: '%ss',
        m: 'a minute',
        mm: '%dm',
        h: 'an hour',
        hh: '%dh',
        d: 'a day',
        dd: '%dd',
        M: 'a month',
        MM: '%dM',
        y: 'a year',
        yy: '%dY'
    }
});

interface CalendarEvent {
    displayName: string;
    time: string;
}

interface TimeUntilBlockDto extends BaseDto {
    events: CalendarEvent[];
}

const EventLabel = styled.div`
    
`;

const TimeUntilBlock: React.FunctionComponent<{ data: TimeUntilBlockDto }> = ({ data }) => {
    return (
        <React.Fragment>
            {/* <FontAwesomeIcon icon={faCalendarAlt} style={{ color: "#0095FF" }} /> */}
            {_.map(data.events, (e, i) => (
                <Textfit key={i} mode="single" max={40}><span style={{ opacity: i != 0 ? 0.6 : 1, padding: 20 }}>{moment(e.time).fromNow(true)} - {e.displayName}</span></Textfit>
            ))}
        </React.Fragment>
    );
}

export default withSubscription(TimeUntilBlock, "TimeUntilBlock");