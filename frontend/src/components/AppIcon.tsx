import './AppIcon.css';

function AppIcon({name, imageUrl, col, row, onClick }: {name: string, imageUrl: string, col: number, row:number, onClick: () => void }) {
    return (
        <div className="app_container"
        style={{gridColumn:col, gridRow: row}}
        onClick={onClick}>
                <div className="icon" style={{backgroundImage: `url("${imageUrl}")`}}></div>
            <p className="label">{name}</p>
        </div>

    );
}

export default AppIcon;
