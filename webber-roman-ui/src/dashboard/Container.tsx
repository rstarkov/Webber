import styled from "styled-components";

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
`;

export function BlockPanelContainer({ children, ...rest }: React.HTMLAttributes<HTMLDivElement>): JSX.Element {
    return <BlockPanelContainerDiv {...rest}><BlockPanelStatusDot />{children}</BlockPanelContainerDiv>
}
