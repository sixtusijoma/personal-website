const FALLBACK = "Something went wrong. Try again in a minute.";

export async function ask(question: string, previousInteractionId?: string, sessionId?: string): Promise<{ answer: string; interactionId?: string }> {
  try {
    const response = await fetch("/api/chat", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ question, previousInteractionId, sessionId }),
      signal: AbortSignal.timeout(30_000),
    });

    if (!response.ok) {
      return { answer: FALLBACK };
    }

    const data = await response.json();

    if (typeof data.answer !== "string" || !data.answer.trim()) {
      return { answer: FALLBACK };
    }

    return { answer: data.answer, interactionId: data.interactionId };
  } catch {
    return { answer: FALLBACK };
  }
}
