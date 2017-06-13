export default class EventedComponent extends React.Component {
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
