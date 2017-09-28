export default class extends React.Component {
    shouldComponentUpdate(nextProps, nextState) {
        // Not event-related, but leaving this orphan here for now.
        return !!Object.diff(this.props, nextProps).size || !!Object.diff(this.state, nextState).size;
    }
    componentDidMount() {
        for (const key in this.events) addEventListener(key, this);
    }
    componentWillUnmount() {
        for (const key in this.events) removeEventListener(key, this);
    }
    handleEvent(e) {
        var handler = this.events[e.type];
        if (handler) handler(e);
    }
}
