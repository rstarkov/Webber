import * as _ from 'lodash';
import * as React from 'react';
import { withSubscription, BaseDto } from './util';
import { HwLoadGraph, HwNetworkGraph } from "./HwGraphs";

interface TimedMetric {
    timeUtc: string;
    value: number;
}

interface HwInfoDto extends BaseDto {
    cpuCoreHeatmap: number[];
    cpuTotalLoadHistory: TimedMetric[];
    cpuPackageTempHistory: TimedMetric[];
    cpuTotalLoad: number;
    cpuPackageTemp: number;
    gpuLoad: number;
    gpuLoadHistory: TimedMetric[];
    gpuTemp: number;
    gpuTempHistory: TimedMetric[];
    networkDown: number;
    networkDownHistory: TimedMetric[];
    networkUp: number;
    networkUpHistory: TimedMetric[];
    networkPing: number;
    networkPingHistory: TimedMetric[];
    memoryUtiliZation: number;
}

const HwInfoBlock: React.FunctionComponent<{ data: HwInfoDto }> = ({ data }) => {
    // cpu heat label {Math.ceil(l)}
    let memColor = "#7AF57C";
    if (data.memoryUtiliZation > 0.5) memColor = "orange";
    if (data.memoryUtiliZation > 0.9) memColor = "red";

    return (
        <React.Fragment>
            <div className="cpu-heatmap" style={{ position: "absolute", left: 0, top: 0, width: 60, height: 4 * 90 }}>
                {_.map(data.cpuCoreHeatmap, (l, i) => <div key={i} className="cpu-block" style={{ backgroundColor: `rgba(0,149,255,${l / 100})`, color: "rgba(255,255,255,0.7)" }}></div>)}
            </div>
            <div style={{ position: "absolute", left: 47, bottom: 90 * 2, width: 10, backgroundColor: memColor, height: 4 * 90 * data.memoryUtiliZation }} />
            <div className="w4h2">
                <HwLoadGraph
                    packageTemp={data.cpuPackageTemp}
                    packageTempHistory={data.cpuPackageTempHistory}
                    totalLoad={data.cpuTotalLoad}
                    totalLoadHistory={data.cpuTotalLoadHistory}
                    label={"CPU"}
                />
            </div>
            <div className="l1t3 w4h2">
                <HwLoadGraph
                    packageTemp={data.gpuTemp}
                    packageTempHistory={data.gpuTempHistory}
                    totalLoad={data.gpuLoad}
                    totalLoadHistory={data.gpuLoadHistory}
                    label={"GPU"}
                />
            </div>
            <div className="l1t5 w4h2">
                <HwNetworkGraph
                    down={data.networkDown}
                    downHistory={data.networkDownHistory}
                    up={data.networkUp}
                    upHistory={data.networkUpHistory}
                    ping={data.networkPing}
                    pingHistory={data.networkPingHistory}
                />
            </div>
        </React.Fragment>
    );
}

export default withSubscription(HwInfoBlock, "HwInfoBlock");