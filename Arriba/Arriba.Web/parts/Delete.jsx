import "./Delete.scss";

export default class extends React.Component {
    render() {
        const {title, className, ...others} = this.props;
        return <svg className={"delete " + className} width="9" height="9" viewBox="0 0 9 9" {...others}>
            <g>
                <title>{title}</title>
                <path
                    transform="translate(0.5, 0.5)"
                    fill="#666666"
                    d="M -0.353437 0.353548L 3.64644 4.35355L 4.35356 3.64645L 0.353681 -0.353548L -0.353437 0.353548ZM 4.35355 4.35355L 8.35355 0.353553L 7.64645 -0.353553L 3.64645 3.64645L 4.35355 4.35355ZM 3.64645 4.35355L 7.64645 8.35355L 8.35355 7.64645L 4.35355 3.64645L 3.64645 4.35355ZM 3.64645 3.64645L -0.353553 7.64645L 0.353553 8.35355L 4.35355 4.35355L 3.64645 3.64645Z"/>
            </g>
        </svg>
    }
}
