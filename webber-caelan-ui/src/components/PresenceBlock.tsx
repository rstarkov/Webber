import * as _ from 'lodash';
import * as React from 'react';
import styled from 'styled-components';
import { withSubscription, BaseDto } from './util';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faLock } from '@fortawesome/free-solid-svg-icons'

const FullBlackout = styled.div`
    position: fixed;
    left: 0;
    top: 0;
    width: 720px;
    height: 720px;
    background-color: rgba(0,0,0,1);
`;

const LockedCalendar = styled.div`
    position: fixed;
    left: 0;
    top: 290px;
    width: 720px;
    height: 430px;
    backdrop-filter: blur(8px);
`;

interface PresenceBlockDto extends BaseDto {
    presenceDetected: boolean;
    sessionUnlocked: boolean;
}

const PresenceBlock: React.FunctionComponent<{ data: PresenceBlockDto }> = ({ data }) => {
    const noUser = !data.presenceDetected && !data.sessionUnlocked;
    const locked = !data.sessionUnlocked;
    return (
        <React.Fragment>
            {locked && <LockedCalendar>
                <span style={{ lineHeight: "430px", height: "430px" }}>
                    <FontAwesomeIcon icon={faLock} style={{ textAlign: "center", width: "100%", fontSize: "80px", opacity: 0.6 }} />
                </span>
            </LockedCalendar>}
            {noUser && <FullBlackout />}
        </React.Fragment>
    );
}

export default withSubscription(PresenceBlock, "PresenceBlock");