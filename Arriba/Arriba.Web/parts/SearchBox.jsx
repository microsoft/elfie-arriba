import "./SearchBox.scss";
import EventedComponent from "./EventedComponent";
import Suggestions from "./Suggestions";

export default class extends EventedComponent {
    constructor(props) {
        super(props);
        this.state = {};
        this.events = {
            "storage": e => {
                if (!["favorites"].includes(e.key)) return;
                setTimeout(() => this.forceUpdate()); // Just to update the star.
            },
            "click": e => { // Ideally use input.blur, but that fires before suggestions.item.onClick.
                if (e.target === this.refs.input) return;
                this.setState({ showSuggest: undefined });
            },
        };
    }
    componentDidMount() {
        super.componentDidMount();
        this.focus();
    }
    componentWillReceiveProps(nextProps) {
        if (this.props.query != nextProps.query) this.refs.peek.clearCache();
    }
    componentDidUpdate(prevProps, prevState) {
        // Skip onInteract in situations where the input is not focused. Namely, when a the query is set from the Recents/Favs.
        const diffProps = Object.diff(prevProps, this.props);
        const hasFocus = this.refs.input === document.activeElement;
        if (diffProps.hasAny("query") && hasFocus && this.props.query !== "*") {
            this.setState({ showSuggest: true });
        }
    }
    focus() {
        this.refs.input.focus();
    }
    toggleFavorite() {
        if (!this.props.parsedQuery) return;
        localStorage.updateJson("favorites", favs => (favs || []).toggle(this.props.parsedQuery));
    }
    render() {
        var star = (localStorage.getJson("favorites") || []).includes(this.props.parsedQuery) ? "icon-solid-star" : "icon-outlined-star"
        return <div className="searchBox">
            <div className={ "loading " + (this.props.loading ? "loading-active" : "") }></div>
            <input ref="input" type="text"
                spellCheck="false"
                placeholder="Search for..."
                tabIndex="1"
                value={this.props.query}
                onClick={e => this.setState({ showSuggest: true })}
                onInput={e => {
                    // IE focus/blur spurriously triggers onInput(), this works around that.
                    const query = e.target.value;
                    if (this.props.query !== query) this.props.queryChanged(query);
                }}
                onKeyDown={e => this.refs.suggestions.onKeyDown(e)} />
            <div className="rail">
                {this.state.completed}
                <span style={{ position: "relative" }} >
                    <div className="railContents">
                        <Suggestions
                            ref="suggestions"
                            query={this.state.showSuggest ? this.props.query : undefined}
                            hide={() => this.setState({ showSuggest: undefined })}
                            queryChanged={q => this.props.queryChanged(q)}
                            userSelectedTable={this.props.userSelectedTable}
                            completedChanged={c => this.setState({ completed: c })}
                            selectedChanged={s => this.setState({ selected: s && s.category === "ColumnName" && s || undefined }) }
                            refocus={() => this.refs.input.focus()} />

                        <Suggestions
                            ref="peek"
                            marginTop={this.state.selected && this.state.selected.offsetTop || 0}
                            cache={true}
                            query={this.state.selected && this.state.completed + this.state.selected.completeAs + ' = ' || undefined}
                            hide={() => this.setState({ selected: undefined })}
                            queryChanged={q => this.props.queryChanged(q)}
                            sel={-1}
                            completedChanged={c => {}}
                            refocus={() => this.refs.input.focus()} />
                    </div>
                </span>
            </div>
            <i className={"searchBoxIcon clickable " + star} onClick={e => this.toggleFavorite(e)} title="Toggle Favorite"></i>
        </div>
    }
}
