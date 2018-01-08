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

        Object.map = function(o, f) {
            return Object.keys(o).map(function(key) { return f(key, o[key]) });
        }

        Number.prototype.clamp = function(min, max) {
            return Math.min(Math.max(this, min), max)
        }

        Number.prototype.add = function(n, array) {
            return (this + n).clamp(0, array.length - 1)
        }

        String.eq = function(a, b) {
            return a.toLowerCase() === b.toLowerCase()
        }

        String.prototype.ciIncludes = function(other) {
            return this.toLowerCase().includes(other.toLowerCase())
        }

        Array.prototype.each = function(f) {
            this.forEach(f)
            return this
        }

        Array.prototype.remove = function(item) {
            var i = this.indexOf(item)
            if (i >= 0) this.splice(i, 1)
            return this
        }

        Array.prototype.without = function(item) {
            this.remove(item)
            return this
        }

        // Additions are inserted at the front to cater to favorites which is the only current consumer of this method.
        Array.prototype.toggle = function(item) {
            this.includes(item) ? this.remove(item) : this.unshift(item)
            return this
        }

        Array.prototype.sum = function(f) {
            return this.reduce((sum, item) => sum + (f ? f(item) : item), 0)
        }
    })()

class Index extends React.Component {
    constructor(props) {
        super(props)
        this.count = 20
        this.state = { query: this.query }
    }
    componentDidMount() {
        window.require.config({ paths: { 'vs': 'node_modules/monaco-editor/min/vs' }});

    	window.require(['vs/editor/editor.main'], () => {
            monaco.languages.register({ id: 'xform' });
            monaco.languages.registerCompletionItemProvider('xform', {
                triggerCharacters: [' ', '\n', '('],
                provideCompletionItems: (model, position) => {
                    var textUntilPosition = model.getValueInRange({startLineNumber: 1, startColumn: 1, endLineNumber: position.lineNumber, endColumn: position.column});
                    return xhr(`suggest?q=${encodeURIComponent(textUntilPosition)}`).then(o => {
                        const onlyRow = o.rows[0]
                        const valid = onlyRow[o.colIndex.Valid]
                        const type = onlyRow[o.colIndex.ItemCategory]
                        const values = onlyRow[o.colIndex.Values]

                        if (!values) return []

                        const kind = monaco.languages.CompletionItemKind;
                        const suggestions = !values.length ? [] : values.split(";").map(s => ({
                            kind: {
                                verb: kind.Keyword,
                                compareOperator: kind.Keyword,
                                columnName: kind.Field,
                            }[type] || kind.Text,
                            label: s,
                            insertText: s,
                        }))
                        return suggestions
                    })
                }
            });

    		this.editor = monaco.editor.create(document.getElementById('query'), {
    			value: [
    				'read WebRequest',
                    'where [HttpStatus] != 200',
    			].join('\n'),
    			language: 'xform',
                scrollBeyondLastLine: false,
                minimap: { enabled: false },
                automaticLayout: false,
    		});

            this.editor.onDidChangeModelContent(e => {
                this.refresh()
            })

            this.refresh()
    	});
    }
    refresh(addCount) {
        this.count += addCount || 0
        const model = this.editor.getModel()
        const q = encodeURIComponent(model.getValue())
        this.setState({ loading: true })
        xhr(`run?c=${this.count}&q=${q}`).then(o => {
            const onlyRow = o.rows[0]
            if (o.colIndex.Valid === 0 && onlyRow[0] === false) { // Could this ever be true?
                this.setState({ status: `Error: ${onlyRow[o.colIndex.Message || o.colIndex.ErrorMessage]}`, loading: false })
            } else {
                this.setState({ status: `${o.rows.length}+ Results`, results: o, loading: false })
                xhr(`count?q=${q}`).then(o => {
                    const onlyRow = o.rows[0]
                    const count = onlyRow[o.colIndex.Count]
                    this.setState({ status: count !== undefined && `${count} Results` || `Error: ${onlyRow[o.colIndex.ErrorMessage]}` })
                })
            }
        })
    }
    render() {
        var cols, rows
        const results = this.state.results
        if (results) {
            cols = Object.keys(results.colIndex)
            rows = results.rows
        }

        const q = this.editor && encodeURIComponent(this.editor.getModel().getValue())

        return <div className={`root`}>
            <div id="query"></div>
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
                    {q && <a target="_blank" href={`http://localhost:5073/download?fmt=csv&q=${q}`}>CSV</a>}
                    {q && <a target="_blank" href={`http://localhost:5073/download?fmt=tsv&q=${q}`}>TSV</a>}
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
