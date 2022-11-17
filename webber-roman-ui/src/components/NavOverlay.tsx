import { useDialogState, Dialog, DialogState } from 'ariakit/Dialog';
import { useNavigate } from 'react-router-dom';
import styled from 'styled-components';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome';
import { faXmark, faArrowsRotate, faExpand, faCompress } from '@fortawesome/free-solid-svg-icons';

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
            <button onClick={() => navigate('/')}>Page: Tasks</button>
            <button onClick={() => navigate('/weather')}>Page: Weather</button>
            <button onClick={() => navigate('/classic')}>Page: Classic</button>
            <button onClick={() => navigate('/experiments')}>Page: Experiments</button>

            <button onClick={props.state.hide}><FontAwesomeIcon icon={faXmark} /></button>
            <div></div>
            <button onClick={() => location.reload()}><Fi icon={faArrowsRotate} />Reload</button>
            <button onClick={toggleFullScreen}>{isFullScreen ? <><Fi icon={faCompress} />Exit Full Screen</> : <><Fi icon={faExpand} />Full Screen</>}</button>
        </NavOverlayDiv>
    </Dialog>;
}

export const useNavOverlayState = useDialogState;
