// SearchHeader contains the top bar - branching, the search box, and top-level buttons
export default React.createClass({
    componentDidMount: function() {
        ReactDOM.findDOMNode(this.refs.searchBox).focus();
    },
    render: function () {
        }
        var tables = this.props.tables || [];
        return (
            <div className="header theme-background-medium">
                <div className="title font-light theme-background-vdark">
                    <a href="/">{this.props.name}</a>
                </div>

                <div className="searchBar">
                    <input id="searchBox" ref="searchBox" type="text" placeholder={"Search for " + tables.join(", ") + "..."} tabIndex="1" onInput={this.props.onSearchChange} value={this.props.query} />
                    <div className="searchIcon">
                        <i className="icon-find"></i>
                    </div>
                </div>

                <div className="buttons">
                    <a className="theme-background-dark" href={"mailto:?subject=" + encodeURIComponent(this.props.name) + ": " + encodeURIComponent(this.props.query) + "&body=" + encodeURIComponent(window.location.href)}>
                        <i className="icon-mail" title="Mail"></i>
                    </a>
                    <a className="theme-background-dark" href={"mailto:" + encodeURIComponent(this.props.feedbackEmailAddresses) + "?subject=" + encodeURIComponent(this.props.name) + " Feedback"}>
                        <i className="icon-emoji2" title="Feedback"></i>
                    </a>
                    <a className="theme-background-dark" href="/?help=true">
                        <i className="icon-help" title="Help"></i>
                    </a>
                </div>
            </div>
        );
    }
});