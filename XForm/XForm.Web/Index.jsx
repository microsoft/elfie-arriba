import "./Index.scss"
import "./shared.jsx"
import React from "react"
import ReactDOM from "react-dom"

(() => {
    window.log = function() { console.log.apply(console, arguments) }

    // TODO: Run on leading edge AND trailing edge of last request.
    window.debounce = function(func, wait) {
    	var timeout
    	return function() {
    		clearTimeout(timeout)
    		timeout = setTimeout(() => {
                timeout = null
    			func.apply(this, arguments)
            }, wait)
    	}
    }

    Array.prototype.remove = function(item) {
        var i = this.indexOf(item);
        if (i >= 0) this.splice(i, 1);
        return this;
    };

    Array.prototype.toggle = function(item) {
        this.includes(item) ? this.remove(item) : this.push(item);
        return this;
    }
})()

class Index extends React.Component {
    constructor(props) {
        super(props)
        this.count = this.baseCount = 50
        this.debouncedQueryChanged = debounce(this.queryChanged, 500)
        this.state = { query: this.query, userCols: [] }
    }
    componentDidMount() {
        window.require.config({ paths: { 'vs': 'node_modules/monaco-editor/min/vs' }});

    	window.require(['vs/editor/editor.main'], () => {
            monaco.languages.register({ id: 'xform' });
            monaco.languages.registerCompletionItemProvider('xform', {
                triggerCharacters: [' ', '\n', '('],
                provideCompletionItems: (model, position) => {
                    const textUntilPosition = model.getValueInRange({
                        startLineNumber: 1,
                        startColumn: 1,
                        endLineNumber: position.lineNumber,
                        endColumn: position.column,
                    })
                    return xhr(`suggest?q=${encodeURIComponent(textUntilPosition)}`).then(o => {
                        if (o.Usage !== this.state.usage) {
                            this.setState({ usage: o.Usage })
                        }

                        if (!o.Values) return []

                        const kind = monaco.languages.CompletionItemKind;
                        const suggestions = !o.Values.length ? [] : o.Values.split(";").map(s => ({
                            kind: {
                                verb: kind.Keyword,
                                compareOperator: kind.Keyword,
                                columnName: kind.Field,
                            }[o.ItemCategory] || kind.Text,
                            label: s,
                            insertText: s,
                        }))
                        return suggestions
                    })
                }
            })

    		this.editor = monaco.editor.create(document.getElementById('queryEditor'), {
    			value: [
    				'read WebRequest',
                    'where [HttpStatus] != 200',
    			].join('\n'),
    			language: 'xform',
                scrollBeyondLastLine: false,
                minimap: { enabled: false },
                automaticLayout: true,
    		});

            this.editor.onDidChangeModelContent(() => this.debouncedQueryChanged())
            this.queryChanged()
    	});
    }
    queryChanged() {
        this.count = this.baseCount
        this.refresh()
        xhr(`run?q=${this.encodedQuery}%0Aschema`).then(o => {
            if (o.rows) {
                this.setState({
                    schemaBody: o.rows.map(r => ({ name: r[0], type: `${r[1]}${r[2] ? '' : '?'}` })),
                })
            }
        })
    }
    get query() {
        return this.editor && this.editor.getModel().getValue()
    }
    get encodedQuery() {
        return encodeURIComponent(this.query)
    }
    refresh(addCount) {
        this.count += addCount || 0
        const q = this.encodedQuery

        if (!q) return // Running with an empty query will return a "" instead of an empty object table.

        this.setState({ loading: true })

        const userCols = this.state.userCols.length && `%0Aselect ${this.state.userCols.map(c => `[${c}]`).join(' ')}` || ''

        xhr(`run?c=${this.count}&q=${q}${userCols}`).then(o => {
            if (o.Message || o.ErrorMessage) {
                this.setState({ status: `Error: ${o.Message || o.ErrorMessage}`, loading: false })
            } else {
                this.setState({ results: o, loading: false })

                if (this.count === this.baseCount) { // No need to recount after the first page of results.
                    xhr(`count?q=${q}`).then(o => {
                        this.setState({ status: typeof o === "number" && `${o.toLocaleString()} Results` || `Error: ${o.ErrorMessage}` })
                    })
                }
            }
        })
    }
    render() {
        var cols, rows
        const results = this.state.results
        if (results) {
            cols = results.cols
            rows = results.rows
        }

        const q = this.encodedQuery

        return <div className={`root`}>
            <div className="query">
                <div className="queryHeader">
                    <input ref="name" type="text" placeholder="Add Title to Save" />
                    <span onClick={e => {
                        const name = this.refs.name.value
                        if (!name || !q) return
                        this.setState({ saving: "Saving..." })
                        xhr(`save?name=${encodeURIComponent(name)}&q=${q}`).then(o => {
                            this.setState({ saving: "Save" })
                        })
                    }}>{ this.state.saving || "Save" }</span>
                </div>
                <div id="queryEditor"></div>
                <div className="queryUsage">{ this.state.usage || `\u200B` }</div>
            </div>
            <div id="schema">
                <div className="schemaHeader">
                    {!this.state.userCols.length && this.state.schemaBody && <span>{this.state.schemaBody.length} Columns</span>}
                    {!!this.state.userCols.length && <span className="schemaButton" onClick={e => this.setState({ userCols: [] }, () => this.queryChanged())}>Reset</span>}
                    {!!this.state.userCols.length && <span className="schemaButton" onClick={e => {
                        const newLine = this.query.endsWith('\n') ? '' : '\n'
                        const userCols = this.state.userCols.length && `${newLine}select ${this.state.userCols.map(c => `[${c}]`).join(' ')}` || ''
                        const r = this.editor.getModel().getFullModelRange()
                        this.editor.executeEdits('my-source', [{
                                identifier: { major: 1, minor: 1 },
                                range: new monaco.Range(r.endLineNumber, r.endColumn, r.endLineNumber, r.endColumn),
                                text: userCols,
                                forceMoveMarkers: true,
                            }])
                        this.setState({ userCols: [] }, () => this.queryChanged())
                    }}>Append to Query</span>}
                </div>
                {this.state.schemaBody && <div className="tableWrapper">
                    <table>
                        <tbody>
                            {this.state.schemaBody && this.state.schemaBody.map((r, i) => <tr key={i}>
                                <td><label><input type="checkbox" checked={this.state.userCols.includes(r.name)} onChange={e => {
                                    this.setState({ userCols: [...this.state.userCols].toggle(r.name) }, () => this.queryChanged())
                                }}/>{r.name}</label></td>
                                <td>{r.type}</td>
                            </tr>)}
                        </tbody>
                    </table>
                </div>}
            </div>
            <div id="results" onScroll={e => {
                    const element = e.target
                    const pixelsFromBottom = (element.scrollHeight - element.clientHeight - element.scrollTop)
                    if (pixelsFromBottom < 100) {
                        this.refresh(10)
                    }
                    // TODO: Inc only if not currently fetching
                }}>
                <div className="resultsHeader">
                    <span>{this.state.status}</span>
                    <span className="flexFill"></span>
                    {q && <a className="button" target="_blank" href={`http://localhost:5073/download?fmt=csv&q=${q}`}>CSV</a>}
                    {q && <a className="button" target="_blank" href={`http://localhost:5073/download?fmt=tsv&q=${q}`}>TSV</a>}
                    <span className={`loading ${ this.state.loading && 'loading-active' }`}></span>
                </div>
                <div className="tableWrapper">
                    <table>
                        <thead>
                            <tr>
                                {cols && cols.map(c => <td key={c}>{c}</td>)}
                            </tr>
                        </thead>
                        <tbody>
                            {rows && rows.map((r, i) => <tr key={i}>{r.map((c, ii) => <td key={i + "x" + ii}>{c}</td>)}</tr>)}
                        </tbody>
                    </table>
                </div>
            </div>
        </div>
    }
}

ReactDOM.render(<Index />, document.getElementById("app"))
