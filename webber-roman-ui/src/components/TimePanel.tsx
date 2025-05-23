import styled from "styled-components";
import { useTime } from "../util/useTime";
import { BlockPanelBorderedContainer, makeState } from "./Container";

const TimeBoxDiv = styled(BlockPanelBorderedContainer)`
    display: grid;
    grid-template-columns: 1fr min-content min-content 1fr;
`;

export function TimePanel(props: React.HTMLAttributes<HTMLDivElement>): React.ReactNode {
    const { time, updates } = useTime();
    return <TimeBoxDiv state={makeState({ updates })} {...props}>
        <div style={{ gridColumnEnd: "span 4", textAlign: "center", fontSize: "280%", fontWeight: "bold", marginTop: "-1.7vw", marginBottom: "0.8vw" }}>{time.toFormat("HH:mm")}</div>

        <div></div>
        <div style={{ color: "#777", marginRight: "1.5vw" }}>UTC</div>
        <div>{time.toUTC().toFormat("HH:mm")}</div>
        <div></div>

        <div></div>
        <div style={{ color: "#777", marginRight: "1.5vw" }}>Can</div>
        <div>{time.setZone("Canada/Mountain").toFormat("HH:mm")}</div>
        <div></div>

        <div></div>
        <div style={{ color: "#777", marginRight: "1.5vw" }}>Ukr</div>
        <div>{time.setZone("Europe/Kiev").toFormat("HH:mm")}</div>
        <div></div>
    </TimeBoxDiv>
}
