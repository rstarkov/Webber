import { useEffect, useState } from "react";
import styled from "styled-components";
import { BlockConnectionStatus, BlockState } from "../blocks/_BlockBase";

export const BlockPanelContainerDiv = styled.div`
    position: relative;
    overflow: hidden;
`;
export const BlockPanelDot = styled.div`
    position: absolute;
    width: 0.8vw;
    height: 0.8vw;
    right: 0.8vw;
    top: 0.8vw;
    box-shadow: 0 0 0.25vw 0.25vw #000;
    z-index: 999;
`;
const BlockPanelDisconnectedDot = styled(BlockPanelDot) <{ connection: BlockConnectionStatus }>`
    background-color: ${p => p.connection == 'disconnected' ? 'red' : 'cyan'};
`;
const BlockPanelStatusDot = styled(BlockPanelDot)`
    background-color: yellow;
    animation: fadeout 1s linear 0s 1 normal forwards;

    @keyframes fadeout {
        from { opacity: 1; }
        to { opacity: 0; }
    }
`;

interface BlockPanelProps extends React.HTMLAttributes<HTMLDivElement> {
    state: BlockState;
}

export function BlockPanelContainer({ state, children, ...rest }: BlockPanelProps): JSX.Element {
    const [visible, setVisible] = useState(false);
    useEffect(() => {
        setVisible(true);
        setTimeout(() => setVisible(false), 1500);
    }, [state.updates]);

    return <BlockPanelContainerDiv {...rest}>
        {visible && <BlockPanelStatusDot />}
        {state.status != 'connected' && <BlockPanelDisconnectedDot connection={state.status} />}
        {children}
    </BlockPanelContainerDiv>;
}

export const BlockPanelBorderedContainer = styled(BlockPanelContainer)`
    padding: 1.5vw;
    border: 0.5vw solid #444;
    background: #111;
`;
