import { useDialogState, Dialog, DialogState } from 'ariakit/Dialog';
import { useNavigate } from 'react-router-dom';
import styled from 'styled-components';

const NavOverlayDiv = styled.div`
    position: absolute;
    left: 0;
    right: 0;
    top: 0;
    bottom: 0;
    background: rgba(37, 39, 49, 0.87);
    display: grid;
    align-items: center;
    justify-items: center;
    grid-template-rows: 1fr 1fr 1fr 1fr;
    grid-auto-flow: column;
`;

export function NavOverlay(props: { state: DialogState }): JSX.Element {

    const navigate = useNavigate();

    const isFullScreen = !!window.parent.document.fullscreenElement;
    function toggleFullScreen() {
        if (isFullScreen)
            window.parent.document.exitFullscreen();
        else
            (window.parent.document.getElementById('appframe') ?? document.body).requestFullscreen();
        props.state.hide();
    }

    return <Dialog state={props.state}>
        <NavOverlayDiv>
            <button onClick={() => navigate('/')}>Page: Main</button>
            <button onClick={() => navigate('/classic')}>Page: Classic</button>
            <button onClick={() => navigate('/unused')}>Page: Unused</button>
            <div></div>
            <div></div>
            <button onClick={props.state.hide}>Dismiss</button>
            <div></div>
            <button onClick={() => location.reload()}>Force Reload</button>
            <button onClick={toggleFullScreen}>{isFullScreen ? 'Exit Full Screen' : 'Full Screen'}</button>
        </NavOverlayDiv>
    </Dialog>;
}

export const useNavOverlayState = useDialogState;
