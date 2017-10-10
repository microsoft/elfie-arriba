import EventedComponent from "./EventedComponent";

// Auto-dismissing when clicking anywhere.
// - Including the contents of the popup, but the contents will get first chnage to handle the click.
// - `wantShow` is used to skip auto-dimiss of a freshly shown popup.
export default class extends EventedComponent {
    constructor(props) {
        super(props);
        this.state = {};
        this.events = {
            "click": e => {
                if (this.wantShow) {
                    this.wantShow = false;
                    this.setState({ show: true });
                    return;
                }
                this.setState({ show: undefined });
            },
        };
    }
    show() {
        if (!this.state.show) this.wantShow = true;
    }
    render() {
        if (!this.state.show) return null;

        const {bottom, left, right, ...others} = this.props;
        return <span style={{ position: "absolute", ...{bottom, left, right} }} >
            <div {...others} style={{ position: "absolute" }}>{this.props.children}</div>
        </span>
    }
}
