import Help from "./Help";

export default React.createClass({
    getInitialState: function () {
        return { showHelp: (this.props.showHelp) };
    },
    toggle: function () {
        this.setState({ showHelp: !this.state.showHelp });
    },
    render: function () {
        if (!this.state.showHelp) {
            return (
                <div className="splash">
                    <div style={{ width: "100%" } }>
                        {configuration.splashContent}
                        <div className="buttons">
                            <a className="theme-background-dark" href="/?help=true">
                                <i className="icon-help" title="Help"></i>
                            </a>
                        </div>
                    </div>
                </div>
            );
        }

        return <Help />;
    }
});