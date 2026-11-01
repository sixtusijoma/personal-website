import AppIcon from './components/AppIcon';
import './App.css';
import WinBox from 'winbox/src/js/winbox';
import 'winbox/dist/css/winbox.min.css';
import { AppType } from './constants/appType';
import { useRef, type RefObject } from 'react';
import { createRoot } from 'react-dom/client';
import Console from './components/Console';
import resumeIcon from "./assets/word2better.png";
import terminalIcon from "./assets/terminal.png";
import linkedInIcon from "./assets/linkedin.svg";
import gitHubIcon from "./assets/github.svg"

function App() {
  const resumeWindow = useRef<InstanceType<typeof WinBox> | null>(null);
  const consoleWindow = useRef<InstanceType<typeof WinBox> | null>(null);
  const appsOpen = useRef(0);
  const shift = 10;

  const openWindow = (
    windowRef: RefObject<InstanceType<typeof WinBox> | null>,
    options: ConstructorParameters<typeof WinBox>[0],
    onClose?: () => void,
  ) => {
    if (windowRef.current) {
      windowRef.current.focus();
      if (windowRef.current.min) {
        windowRef.current.restore();
      }
      return;
    }

    const winbox = new WinBox({
      x: "center",
      y: "center",
      ...options,
      onclose() {
        onClose?.();
        windowRef.current = null;
        appsOpen.current -= 1;
      },
    });

    windowRef.current = winbox;
    winbox.move(winbox.x + shift * appsOpen.current, winbox.y + shift * appsOpen.current);
    winbox.show();
    appsOpen.current += 1;
  };

  const handleClick = (appType: AppType) => {
    switch (appType) {
      case AppType.Resume:
        openWindow(resumeWindow, {
          title: "Resume",
          icon: "/document-title.svg",
          url: "/sixtus_resume.html",
          ...(window.innerWidth < 768 && { width: "90%", height: "70%", x: "center", y: "center" }),
        });
        break;
      case AppType.Console: {
        if (consoleWindow.current) {
          openWindow(consoleWindow, { title: "Console" });
          break;
        }
        const container = document.createElement("div");
        container.style.height = "100%";
        const root = createRoot(container);
        root.render(<Console />);
        openWindow(consoleWindow, {
          title: "Console",
          background: "#000000",
          icon: "/terminal-title.svg",
          mount: container,
          ...(window.innerWidth < 768 && { width: "90%", height: "60%", x: "center", y: 80 }),
        }, () => root.unmount());
        break;
      }
      case AppType.LinkedIn:
        window.open(
          "https://www.linkedin.com/in/sixtusijoma/",
          "_blank",
          "noopener,noreferrer",
        );
        break;
        case AppType.Github:
          window.open(
            "https://github.com/sixtusijoma",
            "_blank",
            "noopener,noreferrer",
          );
          break;
    }
  };

  return (
    <>
    <main className="desktop">
    <AppIcon name='Resume' imageUrl={resumeIcon} col={1} row={1} onClick={() => handleClick(AppType.Resume)} />
    <AppIcon name='Console' imageUrl={terminalIcon} col={1} row={2} onClick={() => handleClick(AppType.Console)} />
    <AppIcon name='LinkedIn' imageUrl={linkedInIcon} col={1} row={3} onClick={() => handleClick(AppType.LinkedIn)} />
    <AppIcon name='GitHub' imageUrl={gitHubIcon} col={1} row={4} onClick={() => handleClick(AppType.Github)} />
    </main>
    </>
  );
}

export default App;
