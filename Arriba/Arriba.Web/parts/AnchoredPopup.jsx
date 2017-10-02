export default class extends React.Component {
    render() {
        const {bottom, left, right, ...others} = this.props;
        return <span style={{ position: "absolute", ...{bottom, left, right} }} >
            <div {...others} style={{ position: "absolute" }}>{this.props.children}</div>
        </span>
    }
}
