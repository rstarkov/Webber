import * as _ from 'lodash';
import * as React from 'react';
import styled from 'styled-components';
import { withSubscription, BaseDto } from './util';
import { formatBytes } from './util';

interface SynologyRouterBlockDto extends BaseDto {
    rx: number;
    tx: number;
}

const PingBubble = styled.div`
    background-color: red;
    width: 24px;
    height: 24px;
    border-radius: 6px;
    margin: 15px;
    display: block;
    flex-grow: 0;
    flex-shrink: 1;
    flex-basis: auto;
    align-self: auto;
    order: 0;
    border: 2px solid black;
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

const getBars = (series: number[], minHeight: number, clrFn: any) => {
    const vMax = Math.max(minHeight, _.max(series));
    const barW = 4;
    const barS = 2;
    const getH = (ping: number) => (ping / vMax) * 90;
    const getR = (idx: number) => (idx * (barW + barS));
    const bars = _.map(series, (v, i) => (<div style={{ position: "absolute", width: barW, height: getH(v), left: getR(i), bottom: 0, backgroundColor: clrFn(v) }}></div>));
    return (<FillDiv>{bars}</FillDiv>);
}

const SynologyRouterBlock: React.FunctionComponent<{ data: SynologyRouterBlockDto }> = ({ data }) => {

    const { rx, tx } = data;

    return (
        <React.Fragment>
            <div>
                <LabelContainer>
                    <PingText>{formatBytes(rx)}</PingText>
                    <PingText>{formatBytes(tx)}</PingText>
                </LabelContainer>
            </div>
        </React.Fragment>
    );
}

export default withSubscription(SynologyRouterBlock, "SynologyRouterBlock");