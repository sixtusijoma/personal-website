import "./Console.css";
import { ask } from "../api/chat";
import { useRef, useState } from "react";

function Console() {
  const inputRef = useRef<HTMLInputElement>(null);
  const prompt = "Ask a question: ";
  const [command, setCommand] = useState("");
  const [interactionId, setInteractionId] = useState<string | undefined>();
  const sessionIdRef = useRef(newSessionId());

  const [prevCommands, setPrevCommands] = useState("");
  const [pending, setPending] = useState(false);
  const [slow, setSlow] = useState(false);

  const handleKeyDown = async (event: React.KeyboardEvent<HTMLInputElement>) => {
    if (event.key !== "Enter" || pending) {
      return;
    }

    const question = command.trim();

    if (!question) {
      return;
    }

    setPending(true);
    setCommand("");
    setPrevCommands(prev => prev + prompt + question + "\n\n");

    const result = await ask(question, interactionId, sessionIdRef.current, () => setSlow(true));

    if (result.interactionId) {
      setInteractionId(result.interactionId);
    }

    setPrevCommands(prev => prev + result.answer + "\n\n");
    setPending(false);
    setSlow(false);
  };

  return (
    <div className="console" onClick={() => inputRef.current?.focus()}>
      <p className="console-line">Welcome to the Sixtus CLI.</p>
      <br></br>
      <p className="console-line">Ask anything about Sixtus: who he is, what he’s done, or whatever you’re curious about.</p>
      <br></br>
      {prevCommands ? <p className="console-line">{prevCommands}</p> : <></>}
      {pending ? (
        <p className="console-line">{slow ? "Waking up the server… the first answer can take up to a minute." : "Thinking…"}</p>
      ) : (
        <p className="console-line">
          {prompt}
          <input ref={inputRef} className="console-input" value={command}
            onChange={(event) => setCommand(event.target.value)}
            onKeyDown={handleKeyDown}
            onFocus={(event) => event.target.scrollIntoView({ block: "nearest" })}
            autoFocus />
        </p>
      )}
    </div>
  );
}

export default Console;

// crypto.randomUUID only exists on HTTPS or localhost, so fall back on plain HTTP (e.g. phone testing on the LAN)
function newSessionId(): string {
  if (typeof crypto.randomUUID === "function") {
    return crypto.randomUUID();
  }

  const bytes = crypto.getRandomValues(new Uint8Array(16));
  bytes[6] = (bytes[6] & 0x0f) | 0x40;
  bytes[8] = (bytes[8] & 0x3f) | 0x80;
  const hex = Array.from(bytes, (b) => b.toString(16).padStart(2, "0")).join("");
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}
