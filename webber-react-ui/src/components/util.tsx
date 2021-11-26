import _ from 'lodash';
import * as React from 'react';
import styled from 'styled-components';
import { HubConnectionBuilder, HubConnection, IRetryPolicy, RetryContext } from '@microsoft/signalr';

const ErrorOverlay = styled.div`
    position: absolute;
    left: 0;
    right: 0;
    top: 0;
    bottom: 0;
    background-color: rgba(127,0,0,0.7);
    color: white;
    padding: 20px;
    border: 2px solid black;
    font-size: 20px;
    overflow: hidden;
    font-weight: bold;
`;

async function setStateAsync<P, S, K extends keyof S>(
    component: React.Component<P, S>,
    state:
        ((prevState: Readonly<S>, props: Readonly<P>) => (Pick<S, K> | S | null)) |
        Pick<S, K> |
        S |
        null
) {
    return new Promise(resolve => component.setState(state, resolve as any));
}

export interface BaseDto {
    errorMessage: string;
}

export function withSubscription<TDto extends BaseDto>(WrappedComponent: (React.ComponentType<{ data: TDto }>), hubName: string) {
    interface BlockPanelBaseState {
        lastUpdate?: TDto;
        errorMessage?: string;
    }
    class InfiniteRetryPolicy implements IRetryPolicy {
        nextRetryDelayInMilliseconds(retryContext: RetryContext): number {
            if (retryContext.previousRetryCount < 3)
                return 2000;
            if (retryContext.previousRetryCount < 10)
                return 10000;
            return 30000;
        }
    }
    return class BlockPanelBase extends React.Component<{}, BlockPanelBaseState>
    {
        connection: HubConnection = null;

        constructor(props: {}) {
            super(props);
            this.state = {};
        }

        componentDidMount = () => {
            this.connect();
        }

        connect = async () => {
            await setStateAsync(this, { errorMessage: "" });

            if (this.connection) {
                try { await this.connection.stop(); } catch { }
                this.connection = null;
            }

            try {
                this.connection = new HubConnectionBuilder()
                    .withUrl(window.location.protocol + '//' + window.location.host + '/hub/' + hubName)
                    .withAutomaticReconnect(new InfiniteRetryPolicy())
                    .build();

                this.connection.on('Update', this.onUpdateReceived);
                this.connection.onreconnecting(this.onReconnecting);
                await this.connection.start();
            } catch (e) {
                if (_.isError(e)) {
                    await setStateAsync(this, { errorMessage: "Failed to connect: " + e.message });
                } else {
                    await setStateAsync(this, { errorMessage: "Failed to connect: " + e });
                }
            }
        }

        onReconnecting = (e: Error) => {
            this.setState({ errorMessage: "Reconnecting: " + e.message });
        }

        onUpdateReceived = (dto: TDto) => {
            this.setState({ lastUpdate: dto, errorMessage: dto.errorMessage });
        }

        render() {
            const { lastUpdate, errorMessage } = this.state;
            return (
                <React.Fragment>
                    {!_.isEmpty(lastUpdate) && <WrappedComponent data={lastUpdate} />}
                    {!_.isEmpty(errorMessage) && <ErrorOverlay onClick={this.connect}>{errorMessage}</ErrorOverlay>}
                </React.Fragment>
            );
        }
    }
}

export function round2places(num: number): number {
    return Math.round((num + Number.EPSILON) * 100) / 100;
}

export function formatBytes(bytes: number, div: number = 1024): string {
    if (bytes < div) return bytes + "b";
    if (bytes < div * div) return round2places(bytes / div).toFixed(2) + "kb";
    if (bytes < div * div * div) return round2places(bytes / (div * div)).toFixed(2) + "mb";
    return round2places(bytes / (div * div * div)).toFixed(2) + "gb";
}