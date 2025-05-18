import { Dialog } from "@ariakit/react";
import { useNavigate } from "react-router-dom";
import styled from "styled-components";
import { FontAwesomeIcon } from "@fortawesome/react-fontawesome";
import { faArrowsRotate, faCompress, faExpand, faXmark } from "@fortawesome/free-solid-svg-icons";
import { useState } from "react";

const NavOverlayDiv = styled.div`
    position: absolute;
    left: 0;
    right: 0;
    top: 0;
    bottom: 0;
    padding: 10vh 0;
    background: rgba(37, 39, 49, 0.87);
    display: grid;
    align-items: center;
    justify-items: center;
    grid-template-rows: 1fr 1fr 1fr 1fr;
    grid-auto-flow: column;
`;

const Fi = styled(FontAwesomeIcon)`
    margin-right: 2vw;
`;

interface NavOverlayState {
    open: boolean;
    show: () => void;
    hide: () => void;
}

export function NavOverlay(props: { state: NavOverlayState }): React.ReactNode {

    const navigate = useNavigate();

    const isFullScreen = !!window.parent.document.fullscreenElement;
    function toggleFullScreen() {
        if (isFullScreen)
            void window.parent.document.exitFullscreen();
        else
            void (window.parent.document.getElementById("appframe") ?? document.body).requestFullscreen();
        props.state.hide();
    }

    return <Dialog open={props.state.open} onClose={props.state.hide}>
        <NavOverlayDiv>
            <button onClick={() => void navigate("/")}>Page: Tasks</button>
            <button onClick={() => void navigate("/weather")}>Page: Weather</button>
            <button onClick={() => void navigate("/classic")}>Page: Classic</button>
            <button onClick={() => void navigate("/experiments")}>Page: Experiments</button>

            <button onClick={props.state.hide}><FontAwesomeIcon icon={faXmark} /></button>
            <div></div>
            <button onClick={() => location.reload()}><Fi icon={faArrowsRotate} />Reload</button>
            <button onClick={toggleFullScreen}>{isFullScreen ? <><Fi icon={faCompress} />Exit Full Screen</> : <><Fi icon={faExpand} />Full Screen</>}</button>
        </NavOverlayDiv>
    </Dialog>;
}

export function useNavOverlayState(): NavOverlayState {
    const [open, setOpen] = useState(false);
    return {
        open,
        show: () => setOpen(true),
        hide: () => setOpen(false),
    };
}
