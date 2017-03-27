// SearchHeader contains the top bar - branching, the search box, and top-level buttons
export default React.createClass({
    getInitialState: function () {
        return { suggestions: [], sel: 0, completed: "", completionCharacters: [] };   
    },
    componentDidMount: function () {
        searchBox.focus();
        this.handleClickDocument = e => {
            if (e.target === searchBox) return; // Don't hide when clicking on input[text]
            this.setState({ suggestions: [] });
        }
        document.addEventListener("click", this.handleClickDocument);
    },
    componentWillUnmount: function() {
        document.removeEventListener("click", this.handleClickDocument);
    },
    onInput: function (e) {
        this.setQuery(e.target.value);
    },
    handleKeyDown: function (e) {
        if (!this.state.suggestions.length) return;
        if (e.key === "ArrowUp") {
            this.setState({ sel: (this.state.sel - 1).clamp(0, this.state.suggestions.length - 1) });
            e.stopPropagation();
        }
        if (e.key === "ArrowDown") {
            this.setState({ sel: (this.state.sel + 1).clamp(0, this.state.suggestions.length - 1) });
            e.stopPropagation();
        }
        if (e.key === "Enter" || this.state.completionCharacters.includes(e.key)) {
            var suffix = (e.key === "Enter" || e.key === "Tab") ? "" : e.key;
            var newQuery = this.state.completed + this.state.suggestions[this.state.sel].completeAs + " " + suffix;
            this.setQuery(newQuery);
            e.preventDefault(); // Suppress focus tabbing.
        }
        if (e.key === "Escape") {
            this.setState({ suggestions: [] });
        }
    },
    handleClick: function (item) {
        this.setQuery(this.state.completed + item.completeAs + " ");
        searchBox.focus();
    },
    setQuery: function (query) {
        this.props.onSearchChange(query);

        if (this.lastRequest) this.lastRequest.abort();
        this.lastRequest = jsonQuery(
            configuration.url + "/suggest?q=" + encodeURIComponent(query),
            data => {
                this.setState({
                    suggestions: data.content.suggestions.filter(x => x.completeAs),
                    sel: 0,
                    completed: data.content.currentCompleteValue, 
                    completionCharacters: data.content.completionCharacters.map(c => ({ "\t": "Tab" })[c] || c),
                });
            },
            (xhr, status, err) => console.error(xhr.url, status, err.toString())
        );
    },
    render: function () {
        var tables = this.props.tables || [];

        var suggestions = this.state.suggestions.length <= 0 ? null :
            <div className="suggestions" >
                {this.state.suggestions.map((item, index) =>
                    <div className={"suggestion " + (this.state.sel == index ? "suggestion-sel" : "" )}
                        onClick={ this.handleClick.bind(this, item) }
                        ><span style={{opacity: 0.3}}>{this.state.completed}</span>{item.value}
                    </div>
                )}
            </div>;

        return (
            <div className="header theme-background-medium">
                <div className="title font-light theme-background-vdark">
                    <a href="/">{this.props.name}</a>
                </div>

                <div className="searchBar">
                    <input id="searchBox" ref="searchBox" type="text" placeholder={"Search for " + tables.join(", ") + "..."} tabIndex="1" onInput={this.onInput} value={this.props.query} onKeyDown={this.handleKeyDown}/>
                    <div className="searchIcon">
                        <i className="icon-find"></i>
                    </div>
                    {suggestions}
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