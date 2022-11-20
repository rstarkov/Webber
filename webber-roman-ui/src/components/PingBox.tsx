import styled from "styled-components";
import { usePingBlock } from "../blocks/PingBlock";
import { BlockPanelBorderedContainer } from "./Container";
import { BarChart, BarChartPt, ModifiedLog, ScaleY } from "./BarChart";

const PingBoxDiv = styled(BlockPanelBorderedContainer)`
    display: flex;
    flex-direction: column;
`;

export function PingBox(props: React.HTMLAttributes<HTMLDivElement>): JSX.Element {
    const ping = usePingBlock();
    if (!ping.dto)
        return <PingBoxDiv state={ping} {...props} />;

    function pingToPt(ping: number | null): BarChartPt {
        if (ping == null /* missing data */ || ping == -1 /* ping timeout */)
            return { Value: 1.0, Color: ping == null ? "#404040" : "#ff00ff" };
        return {
            Value: ScaleY(ping, 10, 2000, x => ModifiedLog(x, 10)),
            Color: ping > 200 ? "#ff0000" : ping > 40 ? "#1985f3" : "#08b025",
        };
    }

    return <PingBoxDiv state={ping} {...props}>
        <div><div style={{ float: "right" }}>{ping.dto.last?.toLocaleString("en-UK", { maximumFractionDigits: 0 }) ?? "∞"} ms</div>Ping</div>
        <div style={{ padding: "0 0.5vw", borderBottom: "1px solid #999", flex: "1", minHeight: "0" }}>
            <BarChart Data={ping.dto.recent.map(pingToPt)} BarCount={ping.dto.recent.length} />
        </div>
    </PingBoxDiv>
}
