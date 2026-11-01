import './AppIcon.css';

function AppIcon({name, imageUrl, col, row, onClick }: {name: string, imageUrl: string, col: number, row:number, onClick: () => void }) {
    return (
        <button type="button" className="app_container"
        style={{gridColumn:col, gridRow: row}}
        onClick={onClick}>
                <span className="icon" aria-hidden="true" style={{backgroundImage: `url("${imageUrl}")`}}></span>
            <span className="label">{name}</span>
        </button>

    );
}

export default AppIcon;
