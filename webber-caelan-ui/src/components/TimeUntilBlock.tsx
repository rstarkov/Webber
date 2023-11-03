import * as _ from 'lodash';
import * as React from 'react';
import { useEffect, useState } from 'react';
import { withSubscription, BaseDto } from './util';
import { Textfit } from 'react-textfit';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faCalendarAlt, faCaretRight } from '@fortawesome/free-solid-svg-icons'
import moment from 'moment';

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
    endTimeUtc: string;
    hasStarted: boolean;
    isNextUp: boolean;
    isAllDay: boolean;
}

interface TimeUntilBlockDto extends BaseDto {
    events: CalendarEvent[];
}

function getTimeString(e: CalendarEvent) {

    const dstart = moment(e.startTimeUtc);
    const dend = moment(e.endTimeUtc);
    const secondsUntil = dstart.diff(moment()) / 1000;

    let momentStr = e.hasStarted
        ? dstart.fromNow(false)
        : dstart.fromNow(!e.isNextUp);

    if (secondsUntil > 0 && secondsUntil < 60) {
        momentStr = "in " + secondsUntil.toFixed(0).toString();
    }
    else if (secondsUntil <= 0 && secondsUntil > -90) {
        momentStr = "NOW";
    }

    let color = "white";

    let opacity = 0.6;
    if (e.hasStarted) opacity = 0.4;
    if (e.isNextUp) opacity = 1;

    if (e.isAllDay) {
        opacity = 0.8;
        momentStr = dstart.format("dddd").substring(0, 3).toUpperCase();
        const diff = dend.diff(dstart);
        if (diff > 90000000) // if longer than 25 hours
            momentStr += "~" + dend.format("dddd").substring(0, 3).toUpperCase();
        color = "orange";
    }

    const wrapLen = 50;
    let displayText = momentStr + " - " + e.displayName;

    if (displayText.length > wrapLen) {
        var breakpt = displayText.lastIndexOf(" ", wrapLen);
        var str1 = displayText.substring(0, breakpt);
        var str2 = displayText.substring(breakpt);
        if (str2.length > str1.length) {
            str2 = str2.substring(0, str1.length - 3) + "...";
        }
        return (
            <div style={{ opacity, color, lineHeight: "30px" }}>{str1}<br />{str2}</div>
        );
    }

    return (
        <span style={{ opacity, color }}>{momentStr} - {e.displayName}</span>
    );
}

var audioSoon = new Audio('/soon.wav');
var audioNow = new Audio('/now.mp3');

const TimeUntilBlock: React.FunctionComponent<{ data: TimeUntilBlockDto }> = ({ data }) => {
    const [warn, setWarn] = useState<string>();
    const [now, setNow] = useState<string>();
    const [until, setUntil] = useState<number>();
    useEffect(() => {
        const id = setInterval(() => {
            const nextIdx = data.events.findIndex(e => e.isNextUp);
            if (nextIdx >= 0) {
                const evt = data.events[nextIdx];
                if (evt.isAllDay)
                    return;

                const secondsUntil = moment(evt.startTimeUtc).diff(moment()) / 1000;
                if (secondsUntil <= 120 && secondsUntil >= -120) {
                    setUntil(secondsUntil); // force re-render each tick when approaching event start time
                }
                if (secondsUntil > 30 && secondsUntil < 120 && warn != evt.displayName) {
                    setWarn(evt.displayName);
                    audioSoon.play();
                }
                else if (secondsUntil <= 30 && now != evt.displayName) {
                    setNow(evt.displayName);
                    audioNow.play();
                }
            }
        }, 1000);
        return () => clearInterval(id);
    });
    return (
        <React.Fragment>
            {/* <FontAwesomeIcon icon={faCalendarAlt} style={{ color: "#0095FF" }} /> */}
            {_.map(data.events, (e, i) => (
                <div key={i} style={{ position: "absolute", left: 60, width: 90 * 8 - 60 - 20, top: i * 60, height: 60, lineHeight: "60px" }}>
                    {e.isNextUp && <FontAwesomeIcon icon={faCaretRight} style={{ color: "red", fontSize: 60, position: "absolute", left: -60, top: 0, width: 60, textAlign: "center" }} />}
                    <Textfit mode="single" max={40}>{getTimeString(e)}</Textfit>
                </div>
            ))}
        </React.Fragment>
    );
}

export default withSubscription(TimeUntilBlock, "TimeUntilBlock");