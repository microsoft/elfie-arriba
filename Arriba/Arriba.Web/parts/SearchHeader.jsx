import "./SearchHeader.scss";

// SearchHeader contains the top bar - branching, the search box, and top-level buttons
export default React.createClass({
    render: function () {
        return <div className="header">
                <div className="title font-light">
                    <a href="/Search.html">{configuration.toolName}</a>
                </div>

                <div className="searchBarAndButtons">
                    {this.props.children}
                    <a title="Feedback" href={"mailto:" + encodeURIComponent(configuration.feedbackEmailAddresses) + "?subject=" + encodeURIComponent(configuration.toolName) + " Feedback"}>
                        <img src="/icons/feedback.svg" alt="feedback"/>
                    </a>
                    <a title="Help" href="/Search.html?help=true">
                        <img src="/icons/help.svg" alt="help"/>
                    </a>
                </div>
            </div>;
    }
});
