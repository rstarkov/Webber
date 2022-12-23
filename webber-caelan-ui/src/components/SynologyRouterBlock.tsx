import * as _ from 'lodash';
import * as React from 'react';
import styled from 'styled-components';
import { withSubscription, BaseDto } from './util';
import { formatBytes } from './util';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faMoon, faSun, faCaretSquareUp, faCaretSquareDown } from '@fortawesome/free-solid-svg-icons'

interface SynologyRouterBlockDto extends BaseDto {
    rx: number;
    rxMax: number;
    rxHistory: number[];
    tx: number;
    txMax: number;
    txHistory: number[];
}

const PingBubble = styled.div`
    margin: 6px;
    display: block;
    flex-grow: 0;
    flex-shrink: 1;
    flex-basis: auto;
    align-self: auto;
    order: 0;
`;

const FillDiv = styled.div`
    position: absolute;
    top: 0;
    right: 0;
    bottom: 0;
    left: 0;
    overflow: hidden;
`;

const LabelContainer = styled.div`
    display: flex;
    position: relative;
    flex-direction: row;
    flex-wrap: nowrap;
    justify-content: flex-start;
    align-items: flex-start;
    align-content: normal;
`;

const PingText = styled.span`
    align-self: center;
    font-size: 24px;
    opacity: 0.9;
    padding: 0px 4px;
    margin-right: 10px;
    background-color: rgba(0,0,0,0.7);
`

function pingColor(ping: number) {
    let color = "lime";
    if (ping > 12)
        color = "yellow";
    if (ping > 24)
        color = "red";
    return color;
}

function getClr(v: number, warn: number, danger: number, defaultClr: string = "rgb(0,149,255)"): string {
    if (v >= danger) return "red";
    if (v >= warn) return "yellow";
    return defaultClr;
}

function getBars(series: number[], minHeight: number, warn: number, danger: number, defaultClr: string = "rgb(0,149,255)") {
    const vMax = Math.max(minHeight, _.max(series));
    const barW = 4;
    const barS = 2;

    var min_f = Math.log(10000) / Math.log(10),
        max_f = Math.log(vMax) / Math.log(10),
        range = max_f - min_f;

    const getH = (n: number) => {
        var position_px = (Math.log(n) / Math.log(10) - min_f) / range * 50;
        return Math.max(1, position_px);
    };
    const getL = (idx: number) => (idx * (barW + barS));
    const bars = _.map(series, (v, i) => (
        <div key={i} style={{ position: "absolute", width: barW, height: getH(v), left: getL(i), bottom: 0, backgroundColor: getClr(v, warn, danger, defaultClr) }}></div>
    ));

    const lineNums = _.range(0, range - 1, 1);
    const lines = _.map(lineNums, (i) => (
        <div style={{height: 1, position: "absolute", left: 8, right: 8, backgroundColor: "rgba(255, 255, 255, 0.3)", top: 40 + (50 / range * i)}} />
    ));

    return (
        <FillDiv>
            {bars}
            {lines}
        </FillDiv>
    );
}

const SynologyRouterBlock: React.FunctionComponent<{ data: SynologyRouterBlockDto }> = ({ data }) => {

    const { rx, rxMax, tx, txMax } = data;

    const rxIconClr = getClr(rx, rxMax * 0.5, rxMax * 0.75);
    const rxTextClr = getClr(rx, rxMax * 0.5, rxMax * 0.75, "rgba(255, 255, 255, 0.9)");
    const txIconClr = getClr(tx, txMax * 0.5, txMax * 0.75, "#FF6A00");
    const txTextClr = getClr(tx, txMax * 0.5, txMax * 0.75, "rgba(255, 255, 255, 0.9)");

    const _10MB = 1 * 1000 * 1000;

    return (
        <React.Fragment>
            <div className="l1t1 w2h1">
                {getBars(data.rxHistory, data.rxMax, data.rxMax * 0.75, data.rxMax * 0.75)}
                <LabelContainer>
                    <PingBubble>
                        <FontAwesomeIcon icon={faCaretSquareDown} style={{ fontSize: 27, color: rxIconClr }} />
                    </PingBubble>
                    <PingText style={{ color: rxTextClr }}>{formatBytes(rx)}</PingText>
                </LabelContainer>
            </div>
            <div className="l3t1 w2h1">
                {getBars(data.txHistory, data.txMax, data.txMax * 0.75, data.txMax * 0.75, "#FF6A00")}
                <LabelContainer>
                    <PingBubble>
                        <FontAwesomeIcon icon={faCaretSquareUp} style={{ fontSize: 27, color: txIconClr }} />
                    </PingBubble>
                    <PingText style={{ color: txTextClr }}>{formatBytes(tx)}</PingText>
                </LabelContainer>
            </div>
        </React.Fragment>
    );
}

export default withSubscription(SynologyRouterBlock, "SynologyRouterBlock");