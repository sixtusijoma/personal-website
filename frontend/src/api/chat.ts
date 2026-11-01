const FALLBACK = "Something went wrong. Try again in a minute.";

// Empty means same origin, which the Vite dev server proxies to the local backend.
const API_BASE_URL = (import.meta.env.VITE_API_BASE_URL ?? "").replace(/\/+$/, "");

// A sleeping Render free instance can take about a minute to start before it answers.
const REQUEST_TIMEOUT_MS = 90_000;
const SLOW_RESPONSE_MS = 5_000;

export function wakeServer(): void {
  fetch(`${API_BASE_URL}/api/health`, { signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS) }).catch(() => {});
}

export async function ask(question: string, previousInteractionId?: string, sessionId?: string, onSlow?: () => void): Promise<{ answer: string; interactionId?: string }> {
  const slowTimer = onSlow ? setTimeout(onSlow, SLOW_RESPONSE_MS) : undefined;

  try {
    const response = await fetch(`${API_BASE_URL}/api/chat`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ question, previousInteractionId, sessionId }),
      signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
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
  } finally {
    clearTimeout(slowTimer);
  }
}
