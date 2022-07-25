import { DateTime } from "luxon";
import { useEffect, useState } from "react";
import styled from "styled-components";
import { timeCorrectionMs } from "../blocks/_BlockBase";
import { BlockPanelBorderedContainer } from "./Container";

const TimeBoxDiv = styled(BlockPanelBorderedContainer)`
    display: grid;
    grid-template-columns: 1fr min-content min-content 1fr;
`;

export function TimeBox(props: React.HTMLAttributes<HTMLDivElement>): JSX.Element {
    const [updates, setUpdates] = useState(0);
    useEffect(() => {
        let timer = 0;
        function setTimer() { timer = setTimeout(() => { setUpdates(u => u + 1); setTimer(); }, 60000 - (Date.now() + timeCorrectionMs) % 60000); }
        setTimer();
        return () => {
            clearTimeout(timer);
        }
    }, []);
    let time = DateTime.local().plus({ milliseconds: timeCorrectionMs });
    if (time.second >= 58) // we schedule the update as close as possible to the minute change; if it triggers slightly before then fast forward it to the next minute
        time = time.plus({ seconds: 60 - time.second });
    return <TimeBoxDiv state={{ status: 'connected', updates }} {...props}>
        <div style={{ gridColumnEnd: 'span 4', textAlign: 'center', fontSize: '280%', fontWeight: 'bold', marginTop: '-1.7vw', marginBottom: '0.8vw' }}>{time.toFormat('HH:mm')}</div>

        <div></div>
        <div style={{ color: '#777', marginRight: '1.5vw' }}>UTC</div>
        <div>{time.toUTC().toFormat('HH:mm')}</div>
        <div></div>

        <div></div>
        <div style={{ color: '#777', marginRight: '1.5vw' }}>Can</div>
        <div>{time.setZone('Canada/Mountain').toFormat('HH:mm')}</div>
        <div></div>

        <div></div>
        <div style={{ color: '#777', marginRight: '1.5vw' }}>Ukr</div>
        <div>{time.setZone('Europe/Kiev').toFormat('HH:mm')}</div>
        <div></div>
    </TimeBoxDiv>
}
