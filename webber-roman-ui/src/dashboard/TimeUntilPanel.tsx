import { useTimeUntilBlock } from "../blocks/TimeUntilBlock";
import { BlockPanelContainer } from "./Container";

export function TimeUntilPanel({ ...rest }: React.HTMLAttributes<HTMLDivElement>): JSX.Element {
    const calendar = useTimeUntilBlock();
    return <BlockPanelContainer {...rest}>
        {calendar.dto?.events.map(e => <p>{e.displayName}</p>)}
    </BlockPanelContainer>
}