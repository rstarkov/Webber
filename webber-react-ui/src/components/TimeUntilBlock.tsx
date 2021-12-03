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
        s: 'NOW',
        ss: 'NOW',
        m: '%dm',
        mm: '%dm',
        h: '%dh',
        hh: '%dh',
        d: '%dd',
        dd: '%dd',
        M: '%dM',
        MM: '%dM',
        y: '%dY',
        yy: '%dY'
    }
});

interface CalendarEvent {
    displayName: string;
    startTimeUtc: string;
    hasStarted: boolean;
    isNextUp: boolean;
}

interface TimeUntilBlockDto extends BaseDto {
    events: CalendarEvent[];
}

function getTimeString(e: CalendarEvent) {
    const momentStr = e.hasStarted
        ? moment(e.startTimeUtc).fromNow(false)
        : moment(e.startTimeUtc).fromNow(!e.isNextUp);

    let opacity = 0.6;
    if (e.hasStarted) opacity = 0.4;
    if (e.isNextUp) opacity = 1;

    return (
        <span style={{ opacity }}>{momentStr} - {e.displayName}</span>
    );
}

const TimeUntilBlock: React.FunctionComponent<{ data: TimeUntilBlockDto }> = ({ data }) => {
    return (
        <React.Fragment>
            {/* <FontAwesomeIcon icon={faCalendarAlt} style={{ color: "#0095FF" }} /> */}
            {_.map(data.events, (e, i) => (
                <Textfit key={i} mode="single" max={40}>{getTimeString(e)}</Textfit>
            ))}
        </React.Fragment>
    );
}

export default withSubscription(TimeUntilBlock, "TimeUntilBlock");