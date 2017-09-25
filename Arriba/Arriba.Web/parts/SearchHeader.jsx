import "./SearchHeader.scss";

// SearchHeader contains the top bar - branching, the search box, and top-level buttons
export default React.createClass({
    render: function () {
        return <div className="header">
                <a className="title font-light" onClick={this.props.reset}>{configuration.toolName}</a>
                <div className="searchBoxAndButtons">
                    {this.props.children}
                </div>
            </div>;
    }
});
