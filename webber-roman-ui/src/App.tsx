import { useState } from 'react'
import styled from "styled-components";
import { usePingBlock } from './blocks/PingBlock';

const SomePanel = styled.div`
    color: white;
`;

function PingText(): JSX.Element {
    const ping = usePingBlock();
    return (
        <p>PING: {ping.dto?.last}</p>
    );
}


function PingHistory(): JSX.Element {
    const ping = usePingBlock();
    return (
        <p>PING: {JSON.stringify(ping.dto?.recent)}</p>
    );
}


export function App(): JSX.Element {
    const [count, setCount] = useState(0)

    return (
        <SomePanel>
            <PingHistory />
            <PingText />
            <p>
                <button type="button" onClick={() => setCount((count) => count + 1)}>
                    count is: {count}
                </button>
            </p>
        </SomePanel>
    )
}
