import * as React from 'react';
import "./App.css";
import HwInfoBlock from './HwInfoBlock';
import ClockBlock from './ClockBlock';
import PingBlock from './PingBlock';
import WeatherBlock from './WeatherBlock';
import TimeUntilBlock from './TimeUntilBlock';
import { FontAwesomeIcon } from '@fortawesome/react-fontawesome'
import { faSyncAlt } from '@fortawesome/free-solid-svg-icons'

function reload() {
    window.location.reload();
}

function App() {
    return (
        <div className="box">
            <div className="l1t1 w4h1">
                <HwInfoBlock />
            </div>
            <div className="l1t2 w4h1">
                <PingBlock />
            </div>
            <div className="l5t1 w4h4">
                <ClockBlock />
            </div>
            <div className="l5t5 w4h2">
                <WeatherBlock />
            </div>
            <div className="l1t7 w8h2" style={{ overflow: 'hidden' }}>
                <TimeUntilBlock />
            </div>
            <div onClick={reload} style={{ position: "absolute", right: 0, top: 0, width: 60, height: 60, textAlign: "center", fontSize: 40, opacity: 0.6, lineHeight: "60px" }}>
                <FontAwesomeIcon icon={faSyncAlt} />
            </div>
        </div>
    );
}

export default App;