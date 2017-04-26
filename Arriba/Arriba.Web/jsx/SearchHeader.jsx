function isIE () {
    // Both Chrome and Edge report as "Chrome", only IE doesn't.
    return navigator.userAgent.indexOf('Chrome') === -1;
}

// SearchHeader contains the top bar - branching, the search box, and top-level buttons
export default React.createClass({
    getInitialState: function () {
        return { suggestions: [], sel: 0, completed: "", completionCharacters: [], favs: localStorage.getJson("favorites") || [] };   
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
    handleFocusOrBlur: function () {
        if (isIE()) this.bypassInputOnce = true;
    },
    onInput: function (e) {
        if (this.bypassInputOnce) {
            this.bypassInputOnce = false;
            return;
        }
        this.setQuery(e.target.value);
    },
    handleClick: function (e) {
        // Re-show the suggestion list even if the textbox already has focus.
        // Limiting to "", to avoid spurious re-shows if clicking around existing text.
        if (e.target.value === "") this.setQuery("");
    },
    handleKeyDown: function (e) {
        if (!this.state.suggestions.length) return;
        if (e.key === "ArrowUp") {
            this.setState({ sel: (this.state.sel - 1).clamp(0, this.state.suggestions.length - 1) });
            e.stopPropagation();
            e.preventDefault(); // Prevent cursor moving to beginning of line.
        }
        if (e.key === "ArrowDown") {
            this.setState({ sel: (this.state.sel + 1).clamp(0, this.state.suggestions.length - 1) });
            e.stopPropagation();
        }
        if (e.key === "Enter" || this.state.completionCharacters.includes(e.key)) {
            var suffix = (e.key === "Enter" || e.key === "Tab" || e.key === " ") ? "" : e.key;
            var item = this.state.suggestions[this.state.sel];
            var newQuery = item.replaceAs || (this.state.completed + item.completeAs + " " + suffix);
            this.setQuery(newQuery);
            e.preventDefault(); // Suppress focus tabbing.
        }
        if (e.key === "Escape") {
            this.setState({ suggestions: [] });
        }
    },
    handleClickSuggestion: function (item) {
        this.setQuery(item.replaceAs || this.state.completed + item.completeAs + " ");
        searchBox.focus();
    },
    setQuery: function (query) {
        this.props.onSearchChange(query);

        if (this.lastRequest) this.lastRequest.abort();
        this.lastRequest = jsonQuery(
            configuration.url + "/suggest?q=" + encodeURIComponent(query),
            data => {
                var favs = this.state.favs
                    .filter(fav => 
                        this.props.query.length < fav.length &&
                        fav.toUpperCase().trimIf("[").startsWith(this.props.query.toUpperCase().trimIf("["))
                    ).map(fav => ({
                        display: fav,
                        hint: "\u2605",
                        replaceAs: fav
                    }));

                this.setState({
                    suggestions: favs.concat(data.content.suggestions),
                    sel: 0,
                    completed: data.content.complete, 
                    completionCharacters: data.content.completionCharacters.map(c => ({ "\t": "Tab" })[c] || c),
                });
            },
            (xhr, status, err) => console.error(xhr.url, status, err.toString())
        );
    },
    toggleFavorite: function () {
        if (!this.props.query) return;

        this.state.favs.toggle(this.props.query.trim());
        localStorage.setJson("favorites", this.state.favs);
    },
    render: function () {
        var tables = this.props.tables || [];

        var suggestions = this.state.suggestions.length <= 0 ? null :
            <div className="suggestions" >
                {this.state.suggestions.map((item, index) =>
                    <div className={"suggestion " + (this.state.sel == index ? "suggestion-sel" : "" )}
                        onClick={ this.handleClickSuggestion.bind(this, item) }>
                        <span><span style={{opacity: 0.3}}>{item.replaceAs ? "" : this.state.completed}</span>{item.display}</span>
                        <span className="suggestion-hint">{item.hint}</span>
                    </div>
                )}
            </div>;

        return (
            <div className="header theme-background-medium">
                <div className="title font-light theme-background-vdark">
                    <a href="/Search.html">{configuration.toolName}</a>
                </div>

                <div className="searchBarAndButtons">
                    <div className="searchBar">
                        <div className={ "loading " + (this.props.loading ? "loading-active" : "") }></div>
                        <input id="searchBox" ref="searchBox" type="text" 
                            placeholder={"Search for " + tables.join(", ") + "..."} 
                            tabIndex="1" onInput={this.onInput} value={this.props.query} 
                            onKeyDown={this.handleKeyDown} onClick={this.handleClick} 
                            onFocus={this.handleFocusOrBlur} onBlur={this.handleFocusOrBlur}/>
                        <div className="searchIcon">
                            <i className={this.state.favs.includes(this.props.query.trim()) ? "icon-solid-star" : "icon-outlined-star"} onClick={this.toggleFavorite}></i>
                            <i className="icon-find"></i>
                        </div>
                        {suggestions}
                    </div>

                    <div className="buttons">
                        <a className="theme-background-dark" href={"mailto:?subject=" + encodeURIComponent(configuration.toolName) + ": " + encodeURIComponent(this.props.query) + "&body=" + encodeURIComponent(window.location.href)}>
                            <i className="icon-mail" title="Mail"></i>
                        </a>
                        <a className="theme-background-dark" href={"mailto:" + encodeURIComponent(configuration.feedbackEmailAddresses) + "?subject=" + encodeURIComponent(configuration.toolName) + " Feedback"}>
                            <i className="icon-emoji2" title="Feedback"></i>
                        </a>
                        <a className="theme-background-dark" href="/?help=true">
                            <i className="icon-help" title="Help"></i>
                        </a>
                    </div>
                </div>
            </div>
        );
    }
});