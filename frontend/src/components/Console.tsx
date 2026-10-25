import "./Console.css"
import { ask } from "../api/chat";
import { useRef, useState } from "react";

function Console() {
  const inputRef = useRef<HTMLInputElement>(null);
  const prompt = 'Whats your question: ';
  const [command, setCommand] = useState('');
  const [interactionId, setInteractionId] = useState<string | undefined>();
  const sessionIdRef = useRef(newSessionId());

  const [prevCommands, setPrevCommands] = useState('');

   const handleKeyDown = async (event: React.KeyboardEvent<HTMLInputElement>) => {
    if (event.key !== "Enter") return;

    const question = command.trim();

    if (!question) return;

    // handle the command here
    var result = await ask(question, interactionId, sessionIdRef.current);
    var answer = result.answer;
    if (result.interactionId) {
      setInteractionId(result.interactionId);
    }
    if(prevCommands){
      setPrevCommands(prevCommands + "\n\n");
    }
    setPrevCommands(prevCommands + prompt + question + "\n" + answer + "\n\n");

    setCommand("");

  }

  return (
    <div className="console"  onClick={() => inputRef.current?.focus()}>
      <p className="console-line">Welcome to the Sixtus CLI.</p><br></br>
      {prevCommands ? <p className="console-line">{prevCommands}</p> : <></>}
      <p className="console-line">
        {prompt}
        <input
          ref={inputRef}
          className="console-input"
          value={command}
          onChange={(event) => setCommand(event.target.value)}
          onKeyDown={handleKeyDown}
          onFocus={(event) => event.target.scrollIntoView({ block: "nearest" })}
          autoFocus
        />
      </p>
    </div>
  );
}

export default Console;

// crypto.randomUUID only exists on HTTPS or localhost, so fall back on plain HTTP (e.g. phone testing on the LAN)
function newSessionId(): string {
  if (typeof crypto.randomUUID === "function") return crypto.randomUUID();
  const bytes = crypto.getRandomValues(new Uint8Array(16));
  bytes[6] = (bytes[6] & 0x0f) | 0x40;
  bytes[8] = (bytes[8] & 0x3f) | 0x80;
  const hex = Array.from(bytes, (b) => b.toString(16).padStart(2, "0")).join("");
  return `${hex.slice(0, 8)}-${hex.slice(8, 12)}-${hex.slice(12, 16)}-${hex.slice(16, 20)}-${hex.slice(20)}`;
}
