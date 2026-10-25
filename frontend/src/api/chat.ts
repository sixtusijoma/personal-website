export async function ask(question: string, previousInteractionId?: string, sessionId?: string): Promise<{ answer: string, interactionId?: string }> {
    const response = await fetch("/api/chat", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ question, previousInteractionId, sessionId }),
    });
    const data = await response.json();
    return { answer: data.answer, interactionId: data.interactionId };
  }