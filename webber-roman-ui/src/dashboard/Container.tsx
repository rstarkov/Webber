import { useEffect, useState } from "react";
import styled from "styled-components";
import { BlockState } from "../blocks/_BlockBase";

export const BlockPanelContainerDiv = styled.div`
    position: relative;
`;
export const BlockPanelStatusDot = styled.div`
    position: absolute;
    width: 0.8vw;
    height: 0.8vw;
    right: 0.8vw;
    top: 0.8vw;
    background-color: yellow;
    box-shadow: 0 0 0.25vw 0.25vw #000;
    z-index: 999;
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
        {children}
    </BlockPanelContainerDiv>;
}
