import "./SplitPane.scss";

// Inspired by https://github.com/tomkp/react-split-pane [MIT License]
export default React.createClass({
    defaultProps: { isFirstVisible: true, isSecondVisible: true },
    onMouseUp: function () {
        document.removeEventListener('mouseup', this.onMouseUp);
        document.removeEventListener('mousemove', this.onMouseMove);
    },
    onMouseDown: function(e) {
        document.addEventListener('mouseup', this.onMouseUp);
        document.addEventListener('mousemove', this.onMouseMove);

        this.setState({ totalWidth: this.refs.container.getBoundingClientRect().width })
    },
    onMouseMove: function(e) {
        var node = ReactDOM.findDOMNode(this.refs.first);
        var newWidth = e.clientX - node.offsetLeft;

        if (this.props.minSize) {
            if (newWidth < this.props.minSize) newWidth = this.props.minSize;
            if (newWidth > this.state.totalWidth - this.props.minSize) newWidth = this.state.totalWidth - this.props.minSize;
        }

        node.style.width = newWidth + 'px';
        node.style.flex = 'none';
    },
    render: function () {
        if (!this.props.isFirstVisible) return this.props.children[1];
        if (!this.props.isSecondVisible) return this.props.children[0];

        return (
            <div ref="container" className="splitpane">
                <div ref="first" className="pane">
                    {this.props.children[0]}
                </div>
                <div ref="handle"
                     className="resizer vertical"
                     onMouseDown={this.onMouseDown} />
                <div ref="second" className="pane">
                    {this.props.children[1]}
                </div>
            </div>
        );
    }
});
