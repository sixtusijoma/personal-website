import "./Console.css"
import { ask } from "../api/chat";
import { useRef, useState } from "react";

function Console() {
  const inputRef = useRef<HTMLInputElement>(null);
  const prompt = 'Whats your question: ';
  const [command, setCommand] = useState('');
  const [interactionId, setInteractionId] = useState<string | undefined>();

  const [prevCommands, setPrevCommands] = useState('');

   const handleKeyDown = async (event: React.KeyboardEvent<HTMLInputElement>) => {
    if (event.key !== "Enter") return;

    const question = command.trim();

    if (!question) return;

    // handle the command here
    var result = await ask(question, interactionId);
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
          autoFocus
        />
      </p>
    </div>
  );
}

export default Console;
