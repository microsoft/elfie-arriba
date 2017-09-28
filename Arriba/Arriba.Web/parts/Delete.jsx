import "./Delete.scss";

export default class extends React.Component {
    render() {
        const {title, ...others} = this.props;
        return <svg className="delete" width="10" height="10" viewBox="0 0 10 10" {...others}>
            <g>
                <title>{title}</title>
                <path
                    transform="translate(0.5, 0.5)"
                    fill="#666666"
                    d="M -0.353553 0.353553L 4.14645 4.85355L 4.85355 4.14645L 0.353553 -0.353553L -0.353553 0.353553ZM 4.85355 4.85355L 9.35355 0.353553L 8.64645 -0.353553L 4.14645 4.14645L 4.85355 4.85355ZM 4.14645 4.85355L 8.64645 9.35355L 9.35355 8.64645L 4.85355 4.14645L 4.14645 4.85355ZM 4.14645 4.14645L -0.353553 8.64645L 0.353553 9.35355L 4.85355 4.85355L 4.14645 4.14645Z"/>
            </g>
        </svg>
    }
}
