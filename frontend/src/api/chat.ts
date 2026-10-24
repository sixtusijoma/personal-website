export async function ask(question: string, previousInteractionId?: string): Promise<{ answer: string, interactionId?: string }> {
    const response = await fetch("http://localhost:5035/api/chat", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ question, previousInteractionId }),
    });
    const data = await response.json();
    return { answer: data.answer, interactionId: data.interactionId };
  }