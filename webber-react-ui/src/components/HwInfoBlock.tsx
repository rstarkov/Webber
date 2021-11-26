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
}

const HwInfoBlock: React.FunctionComponent<{ data: HwInfoDto }> = ({ data }) => {
    return (
        <React.Fragment>
            <div className="w4h2">
                <HwLoadGraph
                    packageTemp={data.cpuPackageTemp}
                    packageTempHistory={data.cpuPackageTempHistory}
                    totalLoad={data.cpuTotalLoad}
                    totalLoadHistory={data.cpuTotalLoadHistory}
                />
            </div>
            <div className="cpu-heatmap w4h2 l1t3">
                {_.map(data.cpuCoreHeatmap, (l, i) => <div key={i} className="cpu-block" style={{ backgroundColor: `rgba(0,149,255,${l / 100})`, color: "rgba(255,255,255,0.9)" }}>{Math.ceil(l)}%</div>)}
            </div>
            <div className="l1t5 w4h2">
                <HwLoadGraph
                    packageTemp={data.gpuTemp}
                    packageTempHistory={data.gpuTempHistory}
                    totalLoad={data.gpuLoad}
                    totalLoadHistory={data.gpuLoadHistory}
                />
            </div>
            <div className="l1t7 w4h2">
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