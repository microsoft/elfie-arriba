import "./SearchHeader.scss";

// SearchHeader contains the top bar - branching, the search box, and top-level buttons
export default React.createClass({
    render: function () {
        return <div className="header">
                <a className="title font-light" href="/">{configuration.toolName}</a>
                <div className="searchBoxAndButtons">
                    {this.props.children}
                    <a title="Feedback" href={"mailto:" + encodeURIComponent(configuration.feedbackEmailAddresses) + "?subject=" + encodeURIComponent(configuration.toolName) + " Feedback"}>
                        <img src="/icons/feedback.svg" alt="feedback"/>
                    </a>
                    <a title="Help" href="/?help=true">
                        <img src="/icons/help.svg" alt="help"/>
                    </a>
                </div>
            </div>;
    }
});
